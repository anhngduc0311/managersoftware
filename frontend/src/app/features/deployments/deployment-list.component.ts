import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { DeploymentService } from '@core/services/deployment.service';
import { SoftwareService, SoftwareDto } from '@core/services/software.service';
import { OrganizationService, OrganizationDto } from '@core/services/organization.service';
import { AuthService } from '@core/services/auth.service';
import { DeploymentDto, DeploymentFilter } from '@core/models/deployment.models';

@Component({
  selector: 'app-deployment-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './deployment-list.component.html',
  styleUrls: ['./deployment-list.component.scss']
})
export class DeploymentListComponent implements OnInit {
  private deploymentService = inject(DeploymentService);
  private softwareService = inject(SoftwareService);
  private organizationService = inject(OrganizationService);
  public authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  deployments = signal<DeploymentDto[]>([]);
  softwareList = signal<SoftwareDto[]>([]);
  organizations = signal<OrganizationDto[]>([]);
  isLoading = signal<boolean>(false);
  totalCount = signal<number>(0);
  page = signal<number>(1);
  pageSize = signal<number>(20);

  // Active filter tab
  activeTab = signal<'all' | 'approved' | 'drafts'>('all');

  // Filters
  selectedOrgId = signal<string>('');
  selectedSoftwareId = signal<string>('');
  selectedEnvironment = signal<string>('');
  selectedOperationalStatus = signal<string>('');
  selectedWorkflowStatus = signal<string>('');
  searchTerm = signal<string>('');

  // Create Modal
  isCreateModalOpen = signal<boolean>(false);
  modalError = signal<string | null>(null);

  createForm = this.fb.group({
    softwareId: ['', [Validators.required]],
    organizationId: ['', [Validators.required]],
    environment: ['Production', [Validators.required]],
    instanceKey: ['default', [Validators.required]],
    operationalStatus: ['NotInUse', [Validators.required]],
    startDate: [''],
    goLiveDate: ['']
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadDeployments();
  }

  loadLookups(): void {
    this.softwareService.getSoftware().subscribe({
      next: (res) => this.softwareList.set(res.items || []),
      error: () => {}
    });

    this.organizationService.getOrganizations().subscribe({
      next: (res) => this.organizations.set(res.items || []),
      error: () => {}
    });
  }

  loadDeployments(): void {
    this.isLoading.set(true);

    let wfStatus: string | undefined = this.selectedWorkflowStatus() || undefined;
    if (this.activeTab() === 'approved') {
      wfStatus = 'Approved';
    }

    const filter: DeploymentFilter = {
      page: this.page(),
      pageSize: this.pageSize(),
      organizationId: this.selectedOrgId() || undefined,
      softwareId: this.selectedSoftwareId() || undefined,
      environment: this.selectedEnvironment() || undefined,
      operationalStatus: this.selectedOperationalStatus() || undefined,
      workflowStatus: wfStatus,
      search: this.searchTerm() || undefined
    };

    this.deploymentService.getDeployments(filter).subscribe({
      next: (res) => {
        let items = res.items || [];
        if (this.activeTab() === 'drafts') {
          items = items.filter(d => d.activeRevision != null);
        }
        this.deployments.set(items);
        this.totalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  setTab(tab: 'all' | 'approved' | 'drafts'): void {
    this.activeTab.set(tab);
    this.page.set(1);
    this.loadDeployments();
  }

  applyFilters(): void {
    this.page.set(1);
    this.loadDeployments();
  }

  resetFilters(): void {
    this.selectedOrgId.set('');
    this.selectedSoftwareId.set('');
    this.selectedEnvironment.set('');
    this.selectedOperationalStatus.set('');
    this.selectedWorkflowStatus.set('');
    this.searchTerm.set('');
    this.page.set(1);
    this.loadDeployments();
  }

  openCreateModal(): void {
    this.modalError.set(null);
    this.createForm.reset({
      softwareId: '',
      organizationId: '',
      environment: 'Production',
      instanceKey: 'default',
      operationalStatus: 'NotInUse',
      startDate: '',
      goLiveDate: ''
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
    const currentUserId = this.authService.currentUser()?.id;

    if (val.operationalStatus === 'Active' && !val.goLiveDate) {
      this.modalError.set('Ngày đưa vào sử dụng là bắt buộc khi trạng thái là "Đang sử dụng" (Active).');
      return;
    }

    this.deploymentService.createDeployment({
      softwareId: val.softwareId!,
      organizationId: val.organizationId!,
      environment: val.environment!,
      instanceKey: val.instanceKey || 'default',
      operationalStatus: val.operationalStatus!,
      startDate: val.startDate || undefined,
      goLiveDate: val.goLiveDate || undefined,
      responsibleUserId: currentUserId
    }).subscribe({
      next: (created) => {
        this.closeCreateModal();
        this.router.navigate(['/deployments', created.id]);
      },
      error: (err) => {
        this.modalError.set(err.error?.detail || err.error?.title || 'Không thể tạo hồ sơ triển khai. Vui lòng thử lại.');
      }
    });
  }

  viewDetail(deploymentId: string): void {
    this.router.navigate(['/deployments', deploymentId]);
  }

  getOperationalBadgeClass(status?: string): string {
    switch (status) {
      case 'Active': return 'badge-active';
      case 'NotInUse': return 'badge-notinuse';
      case 'Suspended': return 'badge-suspended';
      case 'Retired': return 'badge-retired';
      default: return 'badge-default';
    }
  }

  getOperationalLabel(status?: string): string {
    switch (status) {
      case 'Active': return 'Đang sử dụng';
      case 'NotInUse': return 'Chưa sử dụng';
      case 'Suspended': return 'Tạm dừng';
      case 'Retired': return 'Ngừng sử dụng';
      default: return status || '—';
    }
  }

  getWorkflowBadgeClass(status?: string): string {
    switch (status) {
      case 'Approved': return 'badge-wf-approved';
      case 'Submitted': return 'badge-wf-submitted';
      case 'Draft': return 'badge-wf-draft';
      case 'Rejected': return 'badge-wf-rejected';
      default: return 'badge-default';
    }
  }

  getWorkflowLabel(status?: string): string {
    switch (status) {
      case 'Approved': return 'Chính thức';
      case 'Submitted': return 'Chờ duyệt';
      case 'Draft': return 'Bản nháp';
      case 'Rejected': return 'Bị từ chối';
      default: return status || 'Chưa duyệt';
    }
  }
}
