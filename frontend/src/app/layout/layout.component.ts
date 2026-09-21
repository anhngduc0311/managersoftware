import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@core/services/auth.service';
import { NotificationService } from '@core/services/notification.service';
import { NotificationDto } from '@core/models/notification.models';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  public authService = inject(AuthService);
  public notificationService = inject(NotificationService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  currentUser = this.authService.currentUser;
  isSidebarOpen = true;
  isUserMenuOpen = signal<boolean>(false);
  isNotificationMenuOpen = signal<boolean>(false);
  isChangePasswordModalOpen = signal<boolean>(false);
  passwordModalError = signal<string | null>(null);
  passwordModalSuccess = signal<string | null>(null);

  changePasswordForm = this.fb.group({
    oldPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  toggleUserMenu() {
    this.isNotificationMenuOpen.set(false);
    this.isUserMenuOpen.set(!this.isUserMenuOpen());
  }

  closeUserMenu() {
    this.isUserMenuOpen.set(false);
  }

  toggleNotificationMenu() {
    this.isUserMenuOpen.set(false);
    const nextState = !this.isNotificationMenuOpen();
    this.isNotificationMenuOpen.set(nextState);
    if (nextState) {
      this.notificationService.loadRecent();
    }
  }

  closeNotificationMenu() {
    this.isNotificationMenuOpen.set(false);
  }

  onNotificationClick(item: NotificationDto) {
    if (!item.readAt) {
      this.notificationService.markAsRead(item.id).subscribe();
    }
    this.closeNotificationMenu();
    if (item.targetRoute) {
      this.router.navigateByUrl(item.targetRoute);
    }
  }

  markAllNotificationsAsRead() {
    this.notificationService.markAllAsRead().subscribe();
  }

  openChangePasswordModal() {
    this.closeUserMenu();
    this.changePasswordForm.reset();
    this.passwordModalError.set(null);
    this.passwordModalSuccess.set(null);
    this.isChangePasswordModalOpen.set(true);
  }

  closeChangePasswordModal() {
    this.isChangePasswordModalOpen.set(false);
  }

  submitChangePassword() {
    if (this.changePasswordForm.invalid) {
      this.changePasswordForm.markAllAsTouched();
      return;
    }

    const val = this.changePasswordForm.getRawValue();
    this.passwordModalError.set(null);
    this.passwordModalSuccess.set(null);

    this.authService.changePassword({
      currentPassword: val.oldPassword!,
      newPassword: val.newPassword!
    }).subscribe({
      next: () => {
        this.passwordModalSuccess.set('Đổi mật khẩu thành công! Vui lòng đăng nhập lại.');
        setTimeout(() => {
          this.closeChangePasswordModal();
          this.logout();
        }, 1500);
      },
      error: (err) => {
        this.passwordModalError.set(err.problem?.detail || err.error?.detail || 'Không thể đổi mật khẩu.');
      }
    });
  }

  logout() {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: () => {
        this.router.navigate(['/login']);
      }
    });
  }
}
