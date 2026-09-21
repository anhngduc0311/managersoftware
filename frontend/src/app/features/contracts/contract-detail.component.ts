import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ContractService } from '@core/services/contract.service';
import { SoftwareService, SoftwareDto } from '@core/services/software.service';
import { DeploymentService } from '@core/services/deployment.service';
import { DocumentService } from '@core/services/document.service';
import { AuthService } from '@core/services/auth.service';
import {
  ContractDto,
  ContractItemDto,
  LicenseEntitlementDto,
  LicenseAllocationDto,
  UpdateContractRequest
} from '@core/models/contract.models';
import { DocumentAttachmentDto } from '@core/models/document.models';
import { DeploymentDto } from '@core/models/deployment.models';

@Component({
  selector: 'app-contract-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './contract-detail.component.html',
  styleUrls: ['./contract-detail.component.scss']
})
export class ContractDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private contractService = inject(ContractService);
  private softwareService = inject(SoftwareService);
  private deploymentService = inject(DeploymentService);
  private documentService = inject(DocumentService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

  contractId = signal<string>('');
  contract = signal<ContractDto | null>(null);
  etag = signal<string>('');
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  // Tabs: 'items' | 'allocations' | 'documents'
  activeTab = signal<'items' | 'allocations' | 'documents'>('items');

  // Software & Deployment Lookups
  availableSoftware = signal<SoftwareDto[]>([]);
  availableDeployments = signal<DeploymentDto[]>([]);

  // Allocations list under this contract
  allocations = signal<LicenseAllocationDto[]>([]);

  // Document attachments
  attachments = signal<DocumentAttachmentDto[]>([]);
  isUploadingDoc = signal<boolean>(false);
  docUploadError = signal<string | null>(null);

  // Add Item Modal
  isAddItemModalOpen = signal<boolean>(false);
  itemForm = this.fb.group({
    softwareId: ['', [Validators.required]],
    description: [''],
    amount: [0, [Validators.required, Validators.min(0)]]
  });

  // Add Entitlement Modal
  isAddEntitlementModalOpen = signal<boolean>(false);
  selectedItemIdForEntitlement = signal<string>('');
  entitlementForm = this.fb.group({
    licenseType: ['Seat', [Validators.required]],
    quantity: [10, [Validators.min(1)]],
    validFrom: ['', [Validators.required]],
    validTo: ['', [Validators.required]]
  });

  // Allocate License Modal
  isAllocateModalOpen = signal<boolean>(false);
  allocateForm = this.fb.group({
    entitlementId: ['', [Validators.required]],
    deploymentId: ['', [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]]
  });
  allocateError = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/contracts']);
      return;
    }
    this.contractId.set(id);
    this.loadContractDetail();
    this.loadLookups();
  }

  loadContractDetail(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.contractService.getContractWithEtag(this.contractId()).subscribe({
      next: ({ contract, etag }) => {
        this.contract.set(contract);
        this.etag.set(etag);
        this.isLoading.set(false);
        this.loadAllocations();
        this.loadAttachments();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.detail || 'Không thể tải chi tiết hợp đồng.');
      }
    });
  }

  loadLookups(): void {
    this.softwareService.getSoftware({ pageSize: 100 }).subscribe({
      next: (res) => this.availableSoftware.set(res.items),
      error: () => console.error('Lỗi tải danh mục phần mềm')
    });

    this.deploymentService.getDeployments({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => this.availableDeployments.set(res.items),
      error: () => console.error('Lỗi tải hồ sơ triển khai')
    });
  }

  loadAllocations(): void {
    const c = this.contract();
    if (!c) return;

    const allAllocations: LicenseAllocationDto[] = [];
    const entitlementIds: string[] = [];
    c.items.forEach(item => {
      item.entitlements.forEach(ent => entitlementIds.push(ent.id));
    });

    if (entitlementIds.length === 0) {
      this.allocations.set([]);
      return;
    }

    let loadedCount = 0;
    entitlementIds.forEach(eid => {
      this.contractService.getAllocationsByEntitlement(eid).subscribe({
        next: (allocs) => {
          allAllocations.push(...allocs);
          loadedCount++;
          if (loadedCount === entitlementIds.length) {
            this.allocations.set(allAllocations);
          }
        },
        error: () => {
          loadedCount++;
          if (loadedCount === entitlementIds.length) {
            this.allocations.set(allAllocations);
          }
        }
      });
    });
  }

  loadAttachments(): void {
    this.documentService.getAttachments('Contract', this.contractId()).subscribe({
      next: (docs) => this.attachments.set(docs),
      error: () => console.error('Lỗi tải danh sách tài liệu')
    });
  }

  // Add Item
  openAddItemModal(): void {
    this.itemForm.reset({
      softwareId: this.availableSoftware()[0]?.id || '',
      description: '',
      amount: 0
    });
    this.isAddItemModalOpen.set(true);
  }

  closeAddItemModal(): void {
    this.isAddItemModalOpen.set(false);
  }

  submitAddItem(): void {
    if (this.itemForm.invalid) return;

    const val = this.itemForm.value;
    const req = {
      softwareId: val.softwareId!,
      description: val.description || undefined,
      amount: Number(val.amount || 0)
    };

    this.contractService.addContractItem(this.contractId(), req).subscribe({
      next: () => {
        this.closeAddItemModal();
        this.loadContractDetail();
      },
      error: (err) => alert(err.error?.detail || 'Lỗi thêm hạng mục')
    });
  }

  deleteItem(itemId: string): void {
    if (!confirm('Bạn có chắc chắn muốn xóa hạng mục này khỏi hợp đồng?')) return;

    this.contractService.deleteContractItem(this.contractId(), itemId).subscribe({
      next: () => this.loadContractDetail(),
      error: (err) => alert(err.error?.detail || 'Lỗi xóa hạng mục')
    });
  }

  // Add Entitlement
  openAddEntitlementModal(itemId: string): void {
    const c = this.contract();
    this.selectedItemIdForEntitlement.set(itemId);
    this.entitlementForm.reset({
      licenseType: 'Seat',
      quantity: 10,
      validFrom: c?.startDate || '',
      validTo: c?.endDate || ''
    });
    this.isAddEntitlementModalOpen.set(true);
  }

  closeAddEntitlementModal(): void {
    this.isAddEntitlementModalOpen.set(false);
  }

  submitAddEntitlement(): void {
    if (this.entitlementForm.invalid) return;

    const val = this.entitlementForm.value;
    const isUnlimited = val.licenseType === 'Unlimited';

    const req = {
      licenseType: val.licenseType as 'Seat' | 'Unlimited',
      quantity: isUnlimited ? undefined : Number(val.quantity),
      validFrom: val.validFrom!,
      validTo: val.validTo!
    };

    this.contractService.addEntitlement(this.contractId(), this.selectedItemIdForEntitlement(), req).subscribe({
      next: () => {
        this.closeAddEntitlementModal();
        this.loadContractDetail();
      },
      error: (err) => alert(err.error?.detail || 'Lỗi thêm hạn mức license')
    });
  }

  // Allocate License Modal
  openAllocateModal(preselectedEntitlementId?: string): void {
    this.allocateError.set(null);
    this.allocateForm.reset({
      entitlementId: preselectedEntitlementId || '',
      deploymentId: '',
      quantity: 1
    });
    this.isAllocateModalOpen.set(true);
  }

  closeAllocateModal(): void {
    this.isAllocateModalOpen.set(false);
    this.allocateError.set(null);
  }

  getFilteredDeploymentsForSelectedEntitlement(): DeploymentDto[] {
    const eid = this.allocateForm.value.entitlementId;
    if (!eid || !this.contract()) return this.availableDeployments();

    let targetSoftwareId = '';
    for (const item of this.contract()!.items) {
      if (item.entitlements.some(e => e.id === eid)) {
        targetSoftwareId = item.softwareId;
        break;
      }
    }

    if (!targetSoftwareId) return this.availableDeployments();
    return this.availableDeployments().filter(d => d.softwareId === targetSoftwareId);
  }

  submitAllocate(): void {
    if (this.allocateForm.invalid) return;

    const val = this.allocateForm.value;
    const req = {
      entitlementId: val.entitlementId!,
      deploymentId: val.deploymentId!,
      quantity: Number(val.quantity || 1)
    };

    this.contractService.allocateLicense(req).subscribe({
      next: () => {
        this.closeAllocateModal();
        this.loadContractDetail();
      },
      error: (err) => {
        this.allocateError.set(err.error?.detail || err.error?.title || 'Không thể phân bổ license.');
      }
    });
  }

  revokeAllocation(allocId: string): void {
    if (!confirm('Bạn có chắc chắn muốn thu hồi phân bổ license này?')) return;

    this.contractService.revokeAllocation(allocId).subscribe({
      next: () => this.loadContractDetail(),
      error: (err) => alert(err.error?.detail || 'Lỗi thu hồi phân bổ')
    });
  }

  // Document Management
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (file.size > 20 * 1024 * 1024) {
      this.docUploadError.set('Tệp vượt quá kích thước cho phép (tối đa 20 MB).');
      input.value = '';
      return;
    }

    this.isUploadingDoc.set(true);
    this.docUploadError.set(null);

    this.documentService.uploadDocument(file, 'Contract', this.contractId()).subscribe({
      next: () => {
        this.isUploadingDoc.set(false);
        input.value = '';
        this.loadAttachments();
      },
      error: (err) => {
        this.isUploadingDoc.set(false);
        this.docUploadError.set(err.error?.detail || 'Tải lên thất bại. Tệp có thể vi phạm định dạng hoặc chính sách an toàn.');
        input.value = '';
      }
    });
  }

  downloadAttachment(doc: DocumentAttachmentDto): void {
    if (doc.scanStatus !== 'Clean') {
      alert('Tệp này chưa được xác nhận Clean hoặc bị từ chối do quét an toàn.');
      return;
    }

    this.documentService.downloadDocument(doc.documentId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.originalName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => alert('Không thể tải tệp. Vui lòng kiểm tra lại quyền truy cập.')
    });
  }

  unlinkAttachment(doc: DocumentAttachmentDto): void {
    if (!confirm(`Bạn có chắc chắn muốn gỡ tài liệu '${doc.originalName}' khỏi hợp đồng?`)) return;

    this.documentService.unlinkAttachment(doc.attachmentId).subscribe({
      next: () => this.loadAttachments(),
      error: (err) => alert(err.error?.detail || 'Lỗi gỡ tài liệu')
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  formatCurrency(amount?: number, code: string = 'VND'): string {
    if (amount === undefined || amount === null) return '—';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: code }).format(amount);
  }
}
