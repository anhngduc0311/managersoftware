import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard-container">
      <div class="page-header">
        <h2>Tổng quan Hệ thống Quản lý Phần mềm CĐS</h2>
        <p class="subtitle">Trung tâm theo dõi tình trạng triển khai phần mềm, hợp đồng và bản quyền tại tỉnh Lào Cai</p>
      </div>

      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-icon blue">
            <span class="material-icons-outlined">apps</span>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Phần mềm sử dụng</span>
            <span class="kpi-value">24</span>
            <span class="kpi-trend positive">+2 phần mềm mới</span>
          </div>
        </div>

        <div class="kpi-card">
          <div class="kpi-icon green">
            <span class="material-icons-outlined">rocket_launch</span>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Lượt triển khai hoạt động</span>
            <span class="kpi-value">158</span>
            <span class="kpi-trend">Tại 32 cơ quan/đơn vị</span>
          </div>
        </div>

        <div class="kpi-card">
          <div class="kpi-icon amber">
            <span class="material-icons-outlined">pending_actions</span>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Hồ sơ chờ phê duyệt</span>
            <span class="kpi-value">7</span>
            <span class="kpi-trend warning">Cần xử lý trong tuần</span>
          </div>
        </div>

        <div class="kpi-card">
          <div class="kpi-icon purple">
            <span class="material-icons-outlined">history_edu</span>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Hợp đồng sắp hết hạn</span>
            <span class="kpi-value">3</span>
            <span class="kpi-trend warning">Trong vòng 30 ngày</span>
          </div>
        </div>
      </div>

      <div class="welcome-banner">
        <div class="banner-icon">
          <span class="material-icons-outlined">verified</span>
        </div>
        <div class="banner-text">
          <h3>Giai đoạn 1: Nền tảng Kỹ thuật đã khởi tạo thành công!</h3>
          <p>Hệ thống sẵn sàng cho việc triển khai Giai đoạn 2 (IAM, Quản lý đơn vị & Danh mục dùng chung).</p>
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
        color: var(--text-main);
      }
      .subtitle {
        font-size: 13.5px;
        color: var(--text-muted);
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
      border: 1px solid var(--border-color);
      border-radius: var(--radius-md);
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      box-shadow: var(--shadow-sm);
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-md);
      }

      .kpi-icon {
        width: 48px;
        height: 48px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;

        .material-icons-outlined {
          font-size: 24px;
        }

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
          color: var(--text-muted);
          font-weight: 500;
        }

        .kpi-value {
          font-size: 24px;
          font-weight: 700;
          color: var(--text-main);
          line-height: 1.2;
          margin: 2px 0;
        }

        .kpi-trend {
          font-size: 11.5px;
          color: var(--text-muted);

          &.positive { color: #16a34a; font-weight: 600; }
          &.warning { color: #d97706; font-weight: 600; }
        }
      }
    }

    .welcome-banner {
      background: linear-gradient(135deg, #0d5f9e, #0284c7);
      color: white;
      border-radius: var(--radius-md);
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
          margin-bottom: 4px;
        }
        p {
          font-size: 13.5px;
          opacity: 0.9;
        }
      }
    }
  `]
})
export class DashboardComponent {}
