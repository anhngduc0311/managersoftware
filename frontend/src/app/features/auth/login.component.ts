import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loginForm = this.fb.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/software';
      this.router.navigateByUrl(returnUrl);
    }
  }


  quickAccounts = [
    { label: 'Quản trị hệ thống', username: 'admin', pass: 'Admin@123456', role: 'SystemAdmin' },
    { label: 'Quản trị danh mục Sở', username: 'catalog_mgr', pass: 'User@123456', role: 'CatalogManager' },
    { label: 'Cán bộ CNTT Bảo Thắng', username: 'editor_baothang', pass: 'User@123456', role: 'UnitEditor' },
    { label: 'Lãnh đạo UBND Bảo Thắng', username: 'approver_baothang', pass: 'User@123456', role: 'UnitApprover' },
    { label: 'Giám sát Tỉnh', username: 'viewer_prov', pass: 'User@123456', role: 'Viewer' }
  ];

  fillAccount(username: string, pass: string) {
    this.loginForm.patchValue({ username, password: pass });
    this.errorMessage.set(null);
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const val = this.loginForm.getRawValue();
    this.authService.login({
      username: val.username!,
      password: val.password!
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/software';
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        this.isLoading.set(false);
        const detail = err.problem?.detail || err.error?.detail || 'Tên đăng nhập hoặc mật khẩu không chính xác.';
        this.errorMessage.set(detail);
      }
    });
  }
}
