import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard-container">
      <div class="page-header">
        <h2>Tổng quan Hệ thống Quản trị Phần mềm Chuyển đổi số</h2>
        <p class="subtitle">Trung tâm điều hành danh mục phần mềm dùng chung và quản lý phân cấp đơn vị tỉnh Lào Cai</p>
      </div>

      <div class="kpi-grid">
        <div class="kpi-card" routerLink="/software">
          <div class="kpi-icon blue">💻</div>
          <div class="kpi-content">
            <span class="kpi-label">Danh mục phần mềm</span>
            <span class="kpi-value">6+</span>
            <span class="kpi-trend positive">Phần mềm nền tảng dùng chung</span>
          </div>
        </div>

        <div class="kpi-card" routerLink="/organizations">
          <div class="kpi-icon green">🏢</div>
          <div class="kpi-content">
            <span class="kpi-label">Đơn vị trên toàn tỉnh</span>
            <span class="kpi-value">5+</span>
            <span class="kpi-trend">Cấp Tỉnh, Sở/Ban/Ngành, Huyện & Xã</span>
          </div>
        </div>

        <div class="kpi-card" routerLink="/software/proposals">
          <div class="kpi-icon amber">💡</div>
          <div class="kpi-content">
            <span class="kpi-label">Đề xuất bổ sung danh mục</span>
            <span class="kpi-value">Quy trình</span>
            <span class="kpi-trend warning">Đơn vị gửi -> Sở TTTT thẩm định</span>
          </div>
        </div>

        <div class="kpi-card" routerLink="/admin/roles">
          <div class="kpi-icon purple">🛡️</div>
          <div class="kpi-content">
            <span class="kpi-label">Phân quyền IAM & Vai trò</span>
            <span class="kpi-value">7 Vai trò</span>
            <span class="kpi-trend">18 Quyền hạn ma trận nghiêm ngặt</span>
          </div>
        </div>
      </div>

      <div class="welcome-banner">
        <div class="banner-icon">🚀</div>
        <div class="banner-text">
          <h3>Giai đoạn 2: Quản trị Hạt nhân (IAM, Tổ chức & Danh mục dùng chung)</h3>
          <p>
            Hệ thống hỗ trợ quản lý lịch sử đơn vị (temporal versioning), phân quyền đa tầng (Global vs Organization + bao gồm cấp dưới),
            và quy trình thẩm định đề xuất phần mềm với kiểm soát phiên bản độc nhất.
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      h2 {
        font-size: 20px;
        font-weight: 700;
        color: #0f172a;
      }
      .subtitle {
        font-size: 13.5px;
        color: #64748b;
        margin-top: 4px;
      }
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 16px;
    }

    .kpi-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 0.75rem;
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
        border-color: #cbd5e1;
      }

      .kpi-icon {
        width: 48px;
        height: 48px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 24px;

        &.blue { background: #e0f2fe; color: #0284c7; }
        &.green { background: #dcfce7; color: #16a34a; }
        &.amber { background: #fef3c7; color: #d97706; }
        &.purple { background: #f3e8ff; color: #9333ea; }
      }

      .kpi-content {
        display: flex;
        flex-direction: column;

        .kpi-label {
          font-size: 12.5px;
          color: #64748b;
          font-weight: 500;
        }

        .kpi-value {
          font-size: 20px;
          font-weight: 700;
          color: #0f172a;
          line-height: 1.2;
          margin: 2px 0;
        }

        .kpi-trend {
          font-size: 11.5px;
          color: #64748b;

          &.positive { color: #16a34a; font-weight: 600; }
          &.warning { color: #d97706; font-weight: 600; }
        }
      }
    }

    .welcome-banner {
      background: linear-gradient(135deg, #1e3a8a, #2563eb);
      color: white;
      border-radius: 0.75rem;
      padding: 24px;
      display: flex;
      align-items: center;
      gap: 20px;

      .banner-icon {
        font-size: 36px;
      }

      .banner-text {
        h3 {
          font-size: 16px;
          font-weight: 700;
          margin: 0 0 6px;
        }
        p {
          font-size: 13.5px;
          opacity: 0.95;
          margin: 0;
          line-height: 1.5;
        }
      }
    }
  `]
})
export class DashboardComponent {
  public authService = inject(AuthService);
}
