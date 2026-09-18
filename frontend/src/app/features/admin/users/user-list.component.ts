import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserManagementService, UserDetailDto, RoleDto, UserGrantDetailDto } from '@core/services/user-management.service';
import { OrganizationService, OrganizationDto } from '@core/services/organization.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit {
  private userMgmtService = inject(UserManagementService);
  private orgService = inject(OrganizationService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

  // Data states
  users = signal<UserDetailDto[]>([]);
  totalCount = signal<number>(0);
  page = signal<number>(1);
  pageSize = signal<number>(15);
  searchQuery = signal<string>('');
  statusFilter = signal<string>('');
  isLoading = signal<boolean>(false);

  roles = signal<RoleDto[]>([]);
  organizations = signal<OrganizationDto[]>([]);

  // Modals state
  isCreateModalOpen = signal<boolean>(false);
  isEditModalOpen = signal<boolean>(false);
  isResetPasswordModalOpen = signal<boolean>(false);
  isGrantsModalOpen = signal<boolean>(false);
  isAddGrantModalOpen = signal<boolean>(false);

  selectedUser = signal<UserDetailDto | null>(null);
  userGrants = signal<UserGrantDetailDto[]>([]);
  modalError = signal<string | null>(null);

  // Forms
  createForm = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    displayName: ['', [Validators.required]],
    email: ['', [Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  editForm = this.fb.group({
    displayName: ['', [Validators.required]],
    email: ['', [Validators.email]],
    isActive: [true]
  });

  resetPasswordForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  grantForm = this.fb.group({
    roleId: ['', [Validators.required]],
    scopeType: ['Organization', [Validators.required]], // 'Global' | 'Organization'
    organizationId: [''],
    includeDescendants: [false],
    validFrom: [new Date().toISOString().substring(0, 16), [Validators.required]],
    validTo: ['']
  });

  ngOnInit(): void {
    this.loadUsers();
    this.loadLookups();
  }

  loadUsers() {
    this.isLoading.set(true);
    const isActive = this.statusFilter() === '' ? undefined : this.statusFilter() === 'true';

    this.userMgmtService.getUsers({
      search: this.searchQuery() || undefined,
      isActive,
      page: this.page(),
      pageSize: this.pageSize()
    }).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.totalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadLookups() {
    this.userMgmtService.getRoles().subscribe(r => this.roles.set(r));
    this.orgService.getOrganizations({ pageSize: 100 }).subscribe(o => this.organizations.set(o.items));
  }

  onSearch() {
    this.page.set(1);
    this.loadUsers();
  }

  onFilterChange() {
    this.page.set(1);
    this.loadUsers();
  }

  // Create User
  openCreateModal() {
    this.createForm.reset({
      username: '',
      displayName: '',
      email: '',
      password: ''
    });
    this.modalError.set(null);
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal() {
    this.isCreateModalOpen.set(false);
  }

  submitCreate() {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const val = this.createForm.getRawValue();
    this.modalError.set(null);

    this.userMgmtService.createUser({
      username: val.username!,
      displayName: val.displayName!,
      email: val.email || undefined,
      password: val.password!
    }).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadUsers();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo người dùng.');
      }
    });
  }

  // Edit User
  openEditModal(user: UserDetailDto) {
    this.selectedUser.set(user);
    this.editForm.reset({
      displayName: user.displayName,
      email: user.email || '',
      isActive: user.isActive
    });
    this.modalError.set(null);
    this.isEditModalOpen.set(true);
  }

  closeEditModal() {
    this.isEditModalOpen.set(false);
    this.selectedUser.set(null);
  }

  submitEdit() {
    if (this.editForm.invalid || !this.selectedUser()) {
      this.editForm.markAllAsTouched();
      return;
    }

    const val = this.editForm.getRawValue();
    this.modalError.set(null);

    this.userMgmtService.updateUser(this.selectedUser()!.id, {
      displayName: val.displayName!,
      email: val.email || undefined,
      isActive: val.isActive ?? true
    }).subscribe({
      next: () => {
        this.closeEditModal();
        this.loadUsers();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể cập nhật người dùng.');
      }
    });
  }

  // Toggle Lock
  toggleLock(user: UserDetailDto) {
    const actionText = user.isActive ? 'khóa' : 'mở khóa';
    if (!confirm(`Bạn có chắc chắn muốn ${actionText} tài khoản "${user.userName}"?`)) {
      return;
    }

    this.userMgmtService.toggleLock(user.id).subscribe({
      next: () => this.loadUsers(),
      error: (err) => alert(err.problem?.detail || err.error?.detail || 'Lỗi thay đổi trạng thái tài khoản.')
    });
  }

  // Reset Password
  openResetPasswordModal(user: UserDetailDto) {
    this.selectedUser.set(user);
    this.resetPasswordForm.reset({ newPassword: '' });
    this.modalError.set(null);
    this.isResetPasswordModalOpen.set(true);
  }

  closeResetPasswordModal() {
    this.isResetPasswordModalOpen.set(false);
    this.selectedUser.set(null);
  }

  submitResetPassword() {
    if (this.resetPasswordForm.invalid || !this.selectedUser()) {
      this.resetPasswordForm.markAllAsTouched();
      return;
    }

    const val = this.resetPasswordForm.getRawValue();
    this.modalError.set(null);

    this.userMgmtService.resetPassword(this.selectedUser()!.id, val.newPassword!).subscribe({
      next: (res) => {
        alert(res.message || 'Đặt lại mật khẩu thành công.');
        this.closeResetPasswordModal();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể đặt lại mật khẩu.');
      }
    });
  }

  // Grants Management
  openGrantsModal(user: UserDetailDto) {
    this.selectedUser.set(user);
    this.loadUserGrants(user.id);
    this.isGrantsModalOpen.set(true);
  }

  closeGrantsModal() {
    this.isGrantsModalOpen.set(false);
    this.selectedUser.set(null);
  }

  loadUserGrants(userId: string) {
    this.userMgmtService.getGrants({ userId }).subscribe({
      next: (grants) => this.userGrants.set(grants)
    });
  }

  openAddGrantModal() {
    this.grantForm.reset({
      roleId: this.roles()[0]?.id || '',
      scopeType: 'Organization',
      organizationId: this.organizations()[0]?.id || '',
      includeDescendants: false,
      validFrom: new Date().toISOString().substring(0, 16),
      validTo: ''
    });
    this.modalError.set(null);
    this.isAddGrantModalOpen.set(true);
  }

  closeAddGrantModal() {
    this.isAddGrantModalOpen.set(false);
  }

  submitAddGrant() {
    if (this.grantForm.invalid || !this.selectedUser()) {
      this.grantForm.markAllAsTouched();
      return;
    }

    const val = this.grantForm.getRawValue();
    this.modalError.set(null);

    const isGlobal = val.scopeType === 'Global';
    if (!isGlobal && !val.organizationId) {
      this.modalError.set('Vui lòng chọn đơn vị khi phân quyền theo cấp đơn vị.');
      return;
    }

    this.userMgmtService.createGrant({
      userId: this.selectedUser()!.id,
      roleId: val.roleId!,
      scopeType: val.scopeType!,
      organizationId: isGlobal ? null : val.organizationId,
      includeDescendants: isGlobal ? false : !!val.includeDescendants,
      validFrom: new Date(val.validFrom!).toISOString(),
      validTo: val.validTo ? new Date(val.validTo).toISOString() : null
    }).subscribe({
      next: () => {
        this.closeAddGrantModal();
        this.loadUserGrants(this.selectedUser()!.id);
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể cấp quyền.');
      }
    });
  }

  revokeGrant(grantId: string) {
    if (!confirm('Bạn có chắc chắn muốn thu hồi phân quyền này? Phiên đăng nhập của người dùng sẽ bị hủy.')) {
      return;
    }

    this.userMgmtService.revokeGrant(grantId).subscribe({
      next: () => {
        if (this.selectedUser()) {
          this.loadUserGrants(this.selectedUser()!.id);
        }
      },
      error: (err) => alert(err.problem?.detail || err.error?.detail || 'Không thể thu hồi quyền.')
    });
  }

  getOrgName(orgId?: string): string {
    if (!orgId) return '—';
    const org = this.organizations().find(o => o.id === orgId);
    return org ? `${org.name} (${org.code})` : orgId;
  }
}
