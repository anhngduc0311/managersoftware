import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserManagementService, RoleDto, PermissionDto } from '@core/services/user-management.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './role-list.component.html',
  styleUrls: ['./role-list.component.scss']
})
export class RoleListComponent implements OnInit {
  private userMgmtService = inject(UserManagementService);
  public authService = inject(AuthService);

  roles = signal<RoleDto[]>([]);
  permissions = signal<PermissionDto[]>([]);
  isLoading = signal<boolean>(false);
  activeTab = signal<'cards' | 'matrix'>('cards');
  selectedRole = signal<RoleDto | null>(null);

  ngOnInit(): void {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    this.userMgmtService.getRoles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        if (roles.length > 0 && !this.selectedRole()) {
          this.selectedRole.set(roles[0]);
        }
      }
    });

    this.userMgmtService.getPermissions().subscribe({
      next: (perms) => {
        this.permissions.set(perms);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  selectRole(role: RoleDto) {
    this.selectedRole.set(role);
  }

  hasRolePermission(role: RoleDto, permissionCode: string): boolean {
    return role.permissions.some(p => p.code === permissionCode);
  }

  getPermissionGroups(): { groupName: string; permissions: PermissionDto[] }[] {
    const map = new Map<string, PermissionDto[]>();
    for (const p of this.permissions()) {
      const g = p.groupName || 'Chung';
      if (!map.has(g)) map.set(g, []);
      map.get(g)!.push(p);
    }
    return Array.from(map.entries()).map(([groupName, permissions]) => ({ groupName, permissions }));
  }
}
