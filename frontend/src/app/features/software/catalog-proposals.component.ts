import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CatalogProposalService, CatalogProposalDto } from '@core/services/catalog-proposal.service';
import { SoftwareService, SoftwareDto, SoftwareCategoryDto, VendorDto } from '@core/services/software.service';
import { OrganizationService, OrganizationDto } from '@core/services/organization.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-catalog-proposals',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './catalog-proposals.component.html',
  styleUrls: ['./catalog-proposals.component.scss']
})
export class CatalogProposalsComponent implements OnInit {
  private proposalService = inject(CatalogProposalService);
  private softwareService = inject(SoftwareService);
  private orgService = inject(OrganizationService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

  proposals = signal<CatalogProposalDto[]>([]);
  categories = signal<SoftwareCategoryDto[]>([]);
  vendors = signal<VendorDto[]>([]);
  softwareList = signal<SoftwareDto[]>([]);
  organizations = signal<OrganizationDto[]>([]);
  isLoading = signal<boolean>(false);
  selectedStatus = signal<string>('');

  // Modals state
  isCreateModalOpen = signal<boolean>(false);
  isAcceptModalOpen = signal<boolean>(false);
  isRejectModalOpen = signal<boolean>(false);
  selectedProposal = signal<CatalogProposalDto | null>(null);
  modalError = signal<string | null>(null);

  // Forms
  proposalForm = this.fb.group({
    organizationId: ['', [Validators.required]],
    softwareName: ['', [Validators.required]],
    description: ['']
  });

  acceptForm = this.fb.group({
    actionType: ['create', [Validators.required]], // 'create' | 'link'
    existingSoftwareId: [''],
    newSoftwareCode: [''],
    categoryId: [''],
    vendorId: ['']
  });

  rejectForm = this.fb.group({
    reason: ['', [Validators.required, Validators.maxLength(2000)]]
  });

  ngOnInit(): void {
    this.loadProposals();
    this.loadLookups();
  }

  loadProposals() {
    this.isLoading.set(true);
    this.proposalService.getProposals({ status: this.selectedStatus() || undefined, pageSize: 50 }).subscribe({
      next: (res) => {
        this.proposals.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadLookups() {
    this.softwareService.getCategories().subscribe(c => this.categories.set(c));
    this.softwareService.getVendors().subscribe(v => this.vendors.set(v));
    this.softwareService.getSoftware({ pageSize: 100 }).subscribe(s => this.softwareList.set(s.items));
    this.orgService.getOrganizations({ pageSize: 100 }).subscribe(o => this.organizations.set(o.items));
  }

  onStatusFilterChange() {
    this.loadProposals();
  }

  // Create Proposal
  openCreateModal() {
    const userScopes = this.authService.currentUser()?.scopes || [];
    const defaultOrg = userScopes.find(s => s.organizationId)?.organizationId || '';

    this.proposalForm.reset({
      organizationId: defaultOrg,
      softwareName: '',
      description: ''
    });
    this.modalError.set(null);
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal() {
    this.isCreateModalOpen.set(false);
  }

  submitCreateProposal() {
    if (this.proposalForm.invalid) {
      this.proposalForm.markAllAsTouched();
      return;
    }

    const val = this.proposalForm.getRawValue();
    this.modalError.set(null);

    this.proposalService.createProposal({
      organizationId: val.organizationId!,
      softwareName: val.softwareName!,
      description: val.description || ''
    }).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadProposals();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể gửi đề xuất.');
      }
    });
  }

  // Accept Proposal
  openAcceptModal(proposal: CatalogProposalDto) {
    this.selectedProposal.set(proposal);
    this.acceptForm.reset({
      actionType: 'create',
      existingSoftwareId: '',
      newSoftwareCode: '',
      categoryId: this.categories()[0]?.id || '',
      vendorId: this.vendors()[0]?.id || ''
    });
    this.modalError.set(null);
    this.isAcceptModalOpen.set(true);
  }

  closeAcceptModal() {
    this.isAcceptModalOpen.set(false);
    this.selectedProposal.set(null);
  }

  submitAccept() {
    if (!this.selectedProposal()) return;

    const val = this.acceptForm.getRawValue();
    this.modalError.set(null);

    const isLink = val.actionType === 'link';
    if (isLink && !val.existingSoftwareId) {
      this.modalError.set('Vui lòng chọn phần mềm có sẵn để liên kết.');
      return;
    }

    if (!isLink && (!val.newSoftwareCode || !val.categoryId || !val.vendorId)) {
      this.modalError.set('Vui lòng nhập đầy đủ mã phần mềm, nhóm và nhà cung cấp.');
      return;
    }

    this.proposalService.acceptProposal(this.selectedProposal()!.id, {
      existingSoftwareId: isLink ? val.existingSoftwareId : null,
      newSoftwareCode: isLink ? null : val.newSoftwareCode,
      categoryId: isLink ? null : val.categoryId,
      vendorId: isLink ? null : val.vendorId
    }).subscribe({
      next: () => {
        this.closeAcceptModal();
        this.loadProposals();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể phê duyệt đề xuất.');
      }
    });
  }

  // Reject Proposal
  openRejectModal(proposal: CatalogProposalDto) {
    this.selectedProposal.set(proposal);
    this.rejectForm.reset({ reason: '' });
    this.modalError.set(null);
    this.isRejectModalOpen.set(true);
  }

  closeRejectModal() {
    this.isRejectModalOpen.set(false);
    this.selectedProposal.set(null);
  }

  submitReject() {
    if (this.rejectForm.invalid || !this.selectedProposal()) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    const val = this.rejectForm.getRawValue();
    this.modalError.set(null);

    this.proposalService.rejectProposal(this.selectedProposal()!.id, val.reason!).subscribe({
      next: () => {
        this.closeRejectModal();
        this.loadProposals();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể từ chối đề xuất.');
      }
    });
  }
}
