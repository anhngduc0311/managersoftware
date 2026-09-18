import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrganizationService, OrganizationDto, OrganizationHistoryDto, OrganizationSuccessionDto } from '@core/services/organization.service';
import { OrganizationTreeComponent } from './organization-tree.component';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-organization-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, OrganizationTreeComponent],
  templateUrl: './organization-list.component.html',
  styleUrls: ['./organization-list.component.scss']
})
export class OrganizationListComponent implements OnInit {
  private orgService = inject(OrganizationService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

  activeTab = signal<'tree' | 'table' | 'successions'>('tree');
  organizations = signal<OrganizationDto[]>([]);
  successions = signal<OrganizationSuccessionDto[]>([]);
  isLoading = signal<boolean>(false);
  searchTerm = signal<string>('');

  // Modals state
  isCreateModalOpen = signal<boolean>(false);
  isEditModalOpen = signal<boolean>(false);
  isHistoryModalOpen = signal<boolean>(false);
  isSuccessionModalOpen = signal<boolean>(false);

  selectedOrg = signal<OrganizationDto | null>(null);
  orgHistory = signal<OrganizationHistoryDto[]>([]);
  modalError = signal<string | null>(null);

  // Forms
  orgForm = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]],
    parentId: [''],
    validFrom: [new Date().toISOString().substring(0, 10), [Validators.required]],
    validTo: ['']
  });

  editOrgForm = this.fb.group({
    name: ['', [Validators.required]],
    parentId: [''],
    validFrom: ['', [Validators.required]],
    validTo: [''],
    isActive: [true]
  });

  successionForm = this.fb.group({
    predecessorId: ['', [Validators.required]],
    successorId: ['', [Validators.required]],
    effectiveDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
    note: ['']
  });

  ngOnInit(): void {
    this.loadOrganizations();
    this.loadSuccessions();
  }

  setTab(tab: 'tree' | 'table' | 'successions') {
    this.activeTab.set(tab);
  }

  loadOrganizations() {
    this.isLoading.set(true);
    this.orgService.getOrganizations({ search: this.searchTerm(), pageSize: 100 }).subscribe({
      next: (res) => {
        this.organizations.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadSuccessions() {
    this.orgService.getSuccessions().subscribe({
      next: (data) => this.successions.set(data),
      error: () => {}
    });
  }

  onSearch(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.searchTerm.set(val);
    this.loadOrganizations();
  }

  openCreateModal() {
    this.orgForm.reset({
      code: '',
      name: '',
      parentId: '',
      validFrom: new Date().toISOString().substring(0, 10),
      validTo: ''
    });
    this.modalError.set(null);
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal() {
    this.isCreateModalOpen.set(false);
  }

  submitCreateOrg() {
    if (this.orgForm.invalid) {
      this.orgForm.markAllAsTouched();
      return;
    }

    const val = this.orgForm.getRawValue();
    this.modalError.set(null);

    this.orgService.createOrganization({
      code: val.code!,
      name: val.name!,
      parentId: val.parentId ? val.parentId : null,
      validFrom: new Date(val.validFrom!).toISOString(),
      validTo: val.validTo ? new Date(val.validTo).toISOString() : null
    }).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadOrganizations();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo đơn vị.');
      }
    });
  }

  openEditModal(org: OrganizationDto) {
    this.selectedOrg.set(org);
    this.editOrgForm.patchValue({
      name: org.name,
      parentId: org.parentId || '',
      validFrom: org.validFrom ? new Date(org.validFrom).toISOString().substring(0, 10) : '',
      validTo: org.validTo ? new Date(org.validTo).toISOString().substring(0, 10) : '',
      isActive: org.isActive
    });
    this.modalError.set(null);
    this.isEditModalOpen.set(true);
  }

  closeEditModal() {
    this.isEditModalOpen.set(false);
    this.selectedOrg.set(null);
  }

  submitEditOrg() {
    if (this.editOrgForm.invalid || !this.selectedOrg()) {
      this.editOrgForm.markAllAsTouched();
      return;
    }

    const val = this.editOrgForm.getRawValue();
    this.modalError.set(null);

    this.orgService.updateOrganization(this.selectedOrg()!.id, {
      name: val.name!,
      parentId: val.parentId ? val.parentId : null,
      validFrom: new Date(val.validFrom!).toISOString(),
      validTo: val.validTo ? new Date(val.validTo).toISOString() : null,
      isActive: val.isActive ?? true
    }).subscribe({
      next: () => {
        this.closeEditModal();
        this.loadOrganizations();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể cập nhật đơn vị.');
      }
    });
  }

  openHistoryModal(org: OrganizationDto) {
    this.selectedOrg.set(org);
    this.orgService.getOrganizationHistory(org.id).subscribe({
      next: (history) => {
        this.orgHistory.set(history);
        this.isHistoryModalOpen.set(true);
      }
    });
  }

  closeHistoryModal() {
    this.isHistoryModalOpen.set(false);
    this.selectedOrg.set(null);
  }

  openSuccessionModal() {
    this.successionForm.reset({
      predecessorId: '',
      successorId: '',
      effectiveDate: new Date().toISOString().substring(0, 10),
      note: ''
    });
    this.modalError.set(null);
    this.isSuccessionModalOpen.set(true);
  }

  closeSuccessionModal() {
    this.isSuccessionModalOpen.set(false);
  }

  submitSuccession() {
    if (this.successionForm.invalid) {
      this.successionForm.markAllAsTouched();
      return;
    }

    const val = this.successionForm.getRawValue();
    this.modalError.set(null);

    this.orgService.createSuccession({
      predecessorId: val.predecessorId!,
      successorId: val.successorId!,
      effectiveDate: new Date(val.effectiveDate!).toISOString(),
      note: val.note || ''
    }).subscribe({
      next: () => {
        this.closeSuccessionModal();
        this.loadSuccessions();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể ghi nhận sáp nhập/kế nhiệm.');
      }
    });
  }
}
