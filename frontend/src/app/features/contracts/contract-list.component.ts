import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ContractService } from '@core/services/contract.service';
import { SoftwareService, VendorDto } from '@core/services/software.service';
import { OrganizationService, OrganizationDto } from '@core/services/organization.service';
import { AuthService } from '@core/services/auth.service';
import { ContractDto, ContractFilter } from '@core/models/contract.models';

@Component({
  selector: 'app-contract-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './contract-list.component.html',
  styleUrls: ['./contract-list.component.scss']
})
export class ContractListComponent implements OnInit {
  private contractService = inject(ContractService);
  private softwareService = inject(SoftwareService);
  private organizationService = inject(OrganizationService);
  public authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  contracts = signal<ContractDto[]>([]);
  vendors = signal<VendorDto[]>([]);
  organizations = signal<OrganizationDto[]>([]);
  isLoading = signal<boolean>(false);
  totalCount = signal<number>(0);
  page = signal<number>(1);
  pageSize = signal<number>(20);

  // Filters
  searchTerm = signal<string>('');
  selectedStatus = signal<string>('');
  selectedOrgId = signal<string>('');
  selectedVendorId = signal<string>('');

  // Create Modal
  isCreateModalOpen = signal<boolean>(false);
  modalError = signal<string | null>(null);
  isSubmitting = signal<boolean>(false);

  createForm = this.fb.group({
    contractNo: ['', [Validators.required]],
    owningOrganizationId: ['', [Validators.required]],
    vendorId: ['', [Validators.required]],
    status: ['Active', [Validators.required]],
    signedDate: [new Date().toISOString().split('T')[0], [Validators.required]],
    startDate: [new Date().toISOString().split('T')[0], [Validators.required]],
    endDate: ['', [Validators.required]],
    totalAmount: [0, [Validators.required, Validators.min(0)]],
    currencyCode: ['VND', [Validators.required]],
    maintenanceStartDate: [''],
    maintenanceEndDate: ['']
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadContracts();
  }

  loadLookups(): void {
    this.softwareService.getVendors().subscribe({
      next: (data) => this.vendors.set(data),
      error: () => console.error('Không thể tải danh sách nhà cung cấp')
    });

    this.organizationService.getOrganizations().subscribe({
      next: (res) => this.organizations.set(res.items),
      error: () => console.error('Không thể tải danh sách đơn vị')
    });
  }

  loadContracts(): void {
    this.isLoading.set(true);
    const filter: ContractFilter = {
      search: this.searchTerm() || undefined,
      status: this.selectedStatus() || undefined,
      organizationId: this.selectedOrgId() || undefined,
      vendorId: this.selectedVendorId() || undefined,
      page: this.page(),
      pageSize: this.pageSize()
    };

    this.contractService.getContracts(filter).subscribe({
      next: (res) => {
        this.contracts.set(res.items);
        this.totalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Lỗi tải danh sách hợp đồng', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch(): void {
    this.page.set(1);
    this.loadContracts();
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set('');
    this.selectedOrgId.set('');
    this.selectedVendorId.set('');
    this.page.set(1);
    this.loadContracts();
  }

  openCreateModal(): void {
    this.modalError.set(null);
    this.createForm.reset({
      contractNo: '',
      owningOrganizationId: this.organizations()[0]?.id || '',
      vendorId: this.vendors()[0]?.id || '',
      status: 'Active',
      signedDate: new Date().toISOString().split('T')[0],
      startDate: new Date().toISOString().split('T')[0],
      endDate: '',
      totalAmount: 0,
      currencyCode: 'VND',
      maintenanceStartDate: '',
      maintenanceEndDate: ''
    });
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
    this.modalError.set(null);
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const val = this.createForm.value;
    if (new Date(val.endDate!) < new Date(val.startDate!)) {
      this.modalError.set('Ngày kết thúc hiệu lực không được trước ngày bắt đầu.');
      return;
    }

    this.isSubmitting.set(true);
    this.modalError.set(null);

    const req = {
      contractNo: val.contractNo!,
      owningOrganizationId: val.owningOrganizationId!,
      vendorId: val.vendorId!,
      status: val.status!,
      signedDate: val.signedDate!,
      startDate: val.startDate!,
      endDate: val.endDate!,
      totalAmount: Number(val.totalAmount || 0),
      currencyCode: val.currencyCode || 'VND',
      maintenanceStartDate: val.maintenanceStartDate || undefined,
      maintenanceEndDate: val.maintenanceEndDate || undefined
    };

    this.contractService.createContract(req).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.closeCreateModal();
        this.router.navigate(['/contracts', res.id]);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.modalError.set(err.error?.title || err.error?.detail || 'Không thể tạo hợp đồng. Vui lòng kiểm tra lại dữ liệu.');
      }
    });
  }

  viewDetail(contractId: string): void {
    this.router.navigate(['/contracts', contractId]);
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Active': return 'status-active';
      case 'Expired': return 'status-expired';
      case 'Terminated': return 'status-terminated';
      case 'Draft': return 'status-draft';
      default: return '';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Active': return 'Đang hiệu lực';
      case 'Expired': return 'Hết hiệu lực';
      case 'Terminated': return 'Đã chấm dứt';
      case 'Draft': return 'Dự thảo';
      default: return status;
    }
  }

  formatCurrency(amount?: number, code: string = 'VND'): string {
    if (amount === undefined || amount === null) return '—';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: code }).format(amount);
  }
}
