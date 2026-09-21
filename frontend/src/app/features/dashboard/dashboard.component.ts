import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { DashboardService, DashboardKpiDto, CoverageEligibilityDto, CreateCoverageEligibilityRequest } from '@core/services/dashboard.service';
import { OrganizationService, OrganizationDto } from '@core/services/organization.service';
import { SoftwareService, SoftwareDto } from '@core/services/software.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="dashboard-container">
      <!-- Header -->
      <div class="dashboard-header">
        <div class="header-left">
          <div class="badge-tag">HỆ THỐNG GIÁM SÁT DÙNG CHUNG</div>
          <h1 class="page-title">Trung tâm Điều hành & Báo cáo Chuyển đổi số</h1>
          <p class="subtitle">Theo dõi độ phủ phần mềm, tình hình triển khai và hiệu lực bản quyền toàn tỉnh Lào Cai</p>
        </div>

        <div class="header-actions">
          <div class="as-of-picker">
            <span class="picker-label">📅 Mốc báo cáo (asOf):</span>
            <input
              type="date"
              class="date-input"
              [(ngModel)]="asOfDate"
              (change)="loadDashboard()"
            />
            <button class="btn-refresh" (click)="resetToToday()" title="Quay về thời điểm hiện tại">
              Hôm nay
            </button>
          </div>

          <button class="btn-config-coverage" *ngIf="canManageCoverage()" (click)="openCoverageModal()">
            ⚙️ Cấu hình Độ phủ
          </button>
        </div>
      </div>

      <!-- Reporting Timestamp Indicator -->
      <div class="reporting-banner" *ngIf="kpis()">
        <div class="banner-info">
          <span class="pulse-indicator"></span>
          <span>Báo cáo số liệu chốt đến ngày: <strong>{{ kpis()?.asOfDate }}</strong> (Tạo lúc: {{ kpis()?.generatedAtUtc | date:'HH:mm:ss dd/MM/yyyy' }})</span>
        </div>
        <div class="banner-quick-links">
          <a routerLink="/deployments/excel" class="quick-link">📥 Nhập / Xuất Excel</a>
          <a routerLink="/contracts" class="quick-link">📜 Hợp đồng & Bản quyền</a>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="isLoading()">
        <div class="spinner"></div>
        <p>Đang tổng hợp số liệu báo cáo thời gian thực...</p>
      </div>

      <!-- KPI Cards Grid -->
      <div class="kpi-grid" *ngIf="!isLoading() && kpis()">
        <!-- 1. Danh mục phần mềm -->
        <div class="kpi-card" routerLink="/software">
          <div class="kpi-icon-wrapper blue">
            <span>💻</span>
          </div>
          <div class="kpi-data">
            <span class="kpi-title">DANH MỤC PHẦN MỀM</span>
            <div class="kpi-main-number">{{ kpis()?.totalSoftwareCount }}</div>
            <span class="kpi-sub">Phần mềm nền tảng dùng chung</span>
          </div>
        </div>

        <!-- 2. Hồ sơ triển khai -->
        <div class="kpi-card" routerLink="/deployments">
          <div class="kpi-icon-wrapper purple">
            <span>🚀</span>
          </div>
          <div class="kpi-data">
            <span class="kpi-title">HỒ SƠ TRIỂN KHAI</span>
            <div class="kpi-main-number">{{ kpis()?.totalDeploymentsCount }}</div>
            <span class="kpi-sub">Tổng phiên bản đã phê duyệt</span>
          </div>
        </div>

        <!-- 3. Đơn vị đang sử dụng -->
        <div class="kpi-card" routerLink="/organizations">
          <div class="kpi-icon-wrapper emerald">
            <span>🏢</span>
          </div>
          <div class="kpi-data">
            <span class="kpi-title">ĐƠN VỊ ĐANG SỬ DỤNG</span>
            <div class="kpi-main-number">{{ kpis()?.activeOrganizationsCount }}</div>
            <span class="kpi-sub">Cơ quan hành chính & sự nghiệp</span>
          </div>
        </div>

        <!-- 4. Tỷ lệ bao phủ -->
        <div class="kpi-card coverage-card">
          <div class="kpi-icon-wrapper teal">
            <span>🎯</span>
          </div>
          <div class="kpi-data">
            <div class="coverage-header">
              <span class="kpi-title">TỶ LỆ BAO PHỦ TOÀN TỈNH</span>
              <span class="coverage-val">
                {{ kpis()?.coveragePercentage !== null ? (kpis()?.coveragePercentage | number:'1.1-1') + '%' : 'N/A' }}
              </span>
            </div>
            <div class="progress-track">
              <div class="progress-fill" [style.width.%]="kpis()?.coveragePercentage || 0"></div>
            </div>
            <span class="kpi-sub">
              {{ kpis()?.eligibleActiveOrganizationsCount }} / {{ kpis()?.eligibleOrganizationsCount }} đơn vị đủ điều kiện
            </span>
          </div>
        </div>

        <!-- 5. Chờ duyệt -->
        <div class="kpi-card" routerLink="/deployments">
          <div class="kpi-icon-wrapper amber">
            <span>⏳</span>
          </div>
          <div class="kpi-data">
            <span class="kpi-title">HỒ SƠ CHỜ PHÊ DUYỆT</span>
            <div class="kpi-main-number">{{ kpis()?.pendingApprovalCount }}</div>
            <span class="kpi-sub">Bản nháp gửi duyệt cần xử lý</span>
          </div>
        </div>

        <!-- 6. Tổng kinh phí hợp đồng -->
        <div class="kpi-card finance-card" routerLink="/contracts">
          <div class="kpi-icon-wrapper red">
            <span>💰</span>
          </div>
          <div class="kpi-data">
            <span class="kpi-title">TỔNG GIÁ TRỊ HỢP ĐỒNG</span>
            <div class="kpi-main-number" *ngIf="kpis()?.totalContractAmount !== null">
              {{ kpis()?.totalContractAmount | number:'1.0-0' }} <small>{{ kpis()?.currencyCode }}</small>
            </div>
            <div class="kpi-main-number security-protected" *ngIf="kpis()?.totalContractAmount === null">
              🔒 Bảo mật
            </div>
            <span class="kpi-sub" *ngIf="kpis()?.totalContractAmount !== null">
              {{ kpis()?.expiringContractsCount }} hợp đồng sắp đến hạn
            </span>
            <span class="kpi-sub" *ngIf="kpis()?.totalContractAmount === null">
              Yêu cầu quyền contracts.read để xem số liệu tài chính
            </span>
          </div>
        </div>
      </div>

      <!-- Charts & Visual Analytics Section -->
      <div class="analytics-grid" *ngIf="!isLoading() && kpis()">
        <!-- Breakdowns: Status & Environment -->
        <div class="analytics-card">
          <div class="card-header">
            <h3>📊 Phân bố theo Trạng thái Vận hành</h3>
            <span class="card-subtitle">Tình hình hoạt động thực tế của các hệ thống</span>
          </div>
          <div class="breakdown-list">
            <div class="breakdown-item" *ngFor="let item of kpis()?.statusBreakdown">
              <div class="item-label">
                <span class="status-badge" [ngClass]="item.status.toLowerCase()">{{ getStatusLabel(item.status) }}</span>
                <span class="count-badge">{{ item.count }} hồ sơ</span>
              </div>
              <div class="item-bar">
                <div
                  class="item-bar-fill"
                  [ngClass]="item.status.toLowerCase()"
                  [style.width.%]="getPercentage(item.count, kpis()?.totalDeploymentsCount || 1)"
                ></div>
              </div>
            </div>
            <div class="empty-hint" *ngIf="!kpis()?.statusBreakdown?.length">
              Chưa có dữ liệu hồ sơ triển khai trong mốc báo cáo
            </div>
          </div>
        </div>

        <!-- Environment Breakdown -->
        <div class="analytics-card">
          <div class="card-header">
            <h3>🌐 Phân bố theo Môi trường Triển khai</h3>
            <span class="card-subtitle">Hạ tầng Production, Staging và Thử nghiệm</span>
          </div>
          <div class="breakdown-list">
            <div class="breakdown-item" *ngFor="let env of kpis()?.environmentBreakdown">
              <div class="item-label">
                <span class="env-badge">{{ env.environment }}</span>
                <span class="count-badge">{{ env.count }} hệ thống</span>
              </div>
              <div class="item-bar">
                <div
                  class="item-bar-fill env"
                  [style.width.%]="getPercentage(env.count, kpis()?.totalDeploymentsCount || 1)"
                ></div>
              </div>
            </div>
            <div class="empty-hint" *ngIf="!kpis()?.environmentBreakdown?.length">
              Chưa có dữ liệu môi trường trong mốc báo cáo
            </div>
          </div>
        </div>
      </div>

      <!-- Top Deploying Organizations Table -->
      <div class="analytics-card full-width" *ngIf="!isLoading() && kpis()">
        <div class="card-header">
          <div class="header-flex">
            <div>
              <h3>🏆 Top Đơn vị Tiêu biểu Triển khai Phần mềm</h3>
              <span class="card-subtitle">Đơn vị có số lượng dịch vụ và phần mềm chuyển đổi số vận hành tích cực</span>
            </div>
            <a routerLink="/deployments" class="btn-link">Xem tất cả hồ sơ →</a>
          </div>
        </div>

        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th style="width: 50px;">#</th>
                <th>Tên Cơ quan / Đơn vị</th>
                <th style="width: 180px;">Tổng triển khai</th>
                <th style="width: 180px;">Đang hoạt động (Active)</th>
                <th style="width: 200px;">Tỷ lệ kích hoạt</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let org of kpis()?.orgBreakdown; let i = index">
                <td class="text-center font-bold">{{ i + 1 }}</td>
                <td>
                  <strong>{{ org.organizationName }}</strong>
                </td>
                <td>
                  <span class="num-tag blue">{{ org.deploymentCount }} phần mềm</span>
                </td>
                <td>
                  <span class="num-tag green">{{ org.activeCount }} đang chạy</span>
                </td>
                <td>
                  <div class="progress-cell">
                    <div class="progress-bar-small">
                      <div class="fill" [style.width.%]="getPercentage(org.activeCount, org.deploymentCount)"></div>
                    </div>
                    <span class="percent-text">{{ getPercentage(org.activeCount, org.deploymentCount) | number:'1.0-0' }}%</span>
                  </div>
                </td>
              </tr>
              <tr *ngIf="!kpis()?.orgBreakdown?.length">
                <td colspan="5" class="empty-cell">Không có số liệu đơn vị trong mốc thời gian này</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Coverage Eligibility Modal -->
    <div class="modal-backdrop" *ngIf="isCoverageModalOpen()">
      <div class="modal-card wide">
        <div class="modal-header">
          <div>
            <h2>Cấu hình Tập Đơn vị Đủ Điều kiện Tính Độ phủ</h2>
            <p class="modal-sub">Quản lý danh sách các cơ quan bắt buộc phải triển khai phần mềm (mẫu số tỷ lệ bao phủ)</p>
          </div>
          <button class="btn-close" (click)="closeCoverageModal()">✕</button>
        </div>

        <div class="modal-body">
          <!-- Form to add new eligibility -->
          <div class="add-eligibility-form">
            <h4>➕ Thiết lập Đơn vị Đủ Điều kiện</h4>
            <div class="form-row">
              <div class="form-group">
                <label>Phần mềm (*)</label>
                <select [(ngModel)]="newEligibility.softwareId" class="form-control">
                  <option value="">-- Chọn phần mềm --</option>
                  <option *ngFor="let s of softwareList()" [value]="s.id">{{ s.name }} ({{ s.code }})</option>
                </select>
              </div>

              <div class="form-group">
                <label>Đơn vị / Cơ quan (*)</label>
                <select [(ngModel)]="newEligibility.organizationId" class="form-control">
                  <option value="">-- Chọn đơn vị --</option>
                  <option *ngFor="let o of organizationList()" [value]="o.id">{{ o.name }} ({{ o.code }})</option>
                </select>
              </div>

              <div class="form-group">
                <label>Hiệu lực từ ngày (*)</label>
                <input type="date" [(ngModel)]="newEligibility.validFrom" class="form-control" />
              </div>

              <div class="form-group">
                <label>Hiệu lực đến ngày</label>
                <input type="date" [(ngModel)]="newEligibility.validTo" class="form-control" />
              </div>
            </div>

            <div class="form-row-full">
              <div class="form-group" style="flex: 1;">
                <label>Ghi chú / Quyết định giao nhiệm vụ</label>
                <input type="text" [(ngModel)]="newEligibility.note" placeholder="Ví dụ: Quyết định 123/QĐ-UBND tỉnh" class="form-control" />
              </div>
              <button class="btn-save" (click)="saveEligibility()" [disabled]="!isEligibilityFormValid()">
                Lưu cấu hình
              </button>
            </div>
          </div>

          <!-- Existing Eligibilities List -->
          <div class="eligibility-table-wrapper">
            <h4>Danh sách Đơn vị trong Tập Mẫu số</h4>
            <table class="data-table">
              <thead>
                <tr>
                  <th>Phần mềm</th>
                  <th>Đơn vị đủ điều kiện</th>
                  <th>Thời gian hiệu lực</th>
                  <th>Trạng thái</th>
                  <th>Ghi chú</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of eligibilityList()">
                  <td><strong>{{ item.softwareName }}</strong></td>
                  <td>{{ item.organizationName }}</td>
                  <td>{{ item.validFrom }} {{ item.validTo ? '→ ' + item.validTo : '→ Vô thời hạn' }}</td>
                  <td>
                    <span class="status-badge" [class.active]="item.isEligible">
                      {{ item.isEligible ? 'Đủ điều kiện' : 'Miễn trừ' }}
                    </span>
                  </td>
                  <td>{{ item.note || '-' }}</td>
                </tr>
                <tr *ngIf="!eligibilityList().length">
                  <td colspan="5" class="empty-cell">Chưa có cấu hình độ phủ nào được thiết lập</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
      padding: 4px 0 32px;
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      flex-wrap: wrap;
      gap: 16px;

      .badge-tag {
        display: inline-block;
        padding: 4px 10px;
        background: #e0f2fe;
        color: #0284c7;
        font-size: 11px;
        font-weight: 700;
        border-radius: 9999px;
        letter-spacing: 0.5px;
        margin-bottom: 6px;
      }

      .page-title {
        font-size: 24px;
        font-weight: 800;
        color: #0f172a;
        margin: 0;
      }

      .subtitle {
        font-size: 14px;
        color: #64748b;
        margin: 4px 0 0;
      }
    }

    .header-actions {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
    }

    .as-of-picker {
      display: flex;
      align-items: center;
      gap: 8px;
      background: #ffffff;
      padding: 6px 12px;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);

      .picker-label {
        font-size: 13px;
        font-weight: 600;
        color: #334155;
      }

      .date-input {
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        padding: 4px 8px;
        font-size: 13px;
        color: #0f172a;
        outline: none;
      }

      .btn-refresh {
        background: #f1f5f9;
        border: none;
        padding: 5px 10px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        color: #475569;
        cursor: pointer;
        &:hover { background: #e2e8f0; }
      }
    }

    .btn-config-coverage {
      background: #0284c7;
      color: #ffffff;
      border: none;
      padding: 8px 16px;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      box-shadow: 0 1px 3px rgba(2, 132, 199, 0.3);
      &:hover { background: #0369a1; }
    }

    .reporting-banner {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-left: 4px solid #0284c7;
      border-radius: 8px;
      padding: 10px 16px;
      font-size: 13px;
      color: #334155;

      .banner-info {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .pulse-indicator {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #10b981;
        box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2);
      }

      .banner-quick-links {
        display: flex;
        gap: 16px;

        .quick-link {
          color: #0284c7;
          text-decoration: none;
          font-weight: 600;
          &:hover { text-decoration: underline; }
        }
      }
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px 0;
      color: #64748b;

      .spinner {
        width: 36px;
        height: 36px;
        border: 3px solid #e2e8f0;
        border-top-color: #0284c7;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
        margin-bottom: 12px;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 16px;
    }

    .kpi-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 12px -2px rgba(0, 0, 0, 0.08);
        border-color: #cbd5e1;
      }

      .kpi-icon-wrapper {
        width: 52px;
        height: 52px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 24px;

        &.blue { background: #e0f2fe; }
        &.purple { background: #f3e8ff; }
        &.emerald { background: #d1fae5; }
        &.teal { background: #ccfbf1; }
        &.amber { background: #fef3c7; }
        &.red { background: #fee2e2; }
      }

      .kpi-data {
        flex: 1;
        display: flex;
        flex-direction: column;

        .kpi-title {
          font-size: 11.5px;
          font-weight: 700;
          color: #64748b;
          letter-spacing: 0.5px;
        }

        .kpi-main-number {
          font-size: 24px;
          font-weight: 800;
          color: #0f172a;
          margin: 2px 0;

          small {
            font-size: 13px;
            font-weight: 600;
            color: #64748b;
          }

          &.security-protected {
            font-size: 16px;
            color: #b91c1c;
            font-weight: 700;
          }
        }

        .kpi-sub {
          font-size: 12px;
          color: #94a3b8;
        }
      }
    }

    .coverage-card {
      .coverage-header {
        display: flex;
        justify-content: space-between;
        align-items: center;

        .coverage-val {
          font-size: 20px;
          font-weight: 800;
          color: #0d9488;
        }
      }

      .progress-track {
        height: 8px;
        background: #f1f5f9;
        border-radius: 9999px;
        margin: 8px 0;
        overflow: hidden;

        .progress-fill {
          height: 100%;
          background: linear-gradient(90deg, #14b8a6, #0d9488);
          border-radius: 9999px;
          transition: width 0.4s ease;
        }
      }
    }

    .analytics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(380px, 1fr));
      gap: 20px;
    }

    .analytics-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);

      &.full-width {
        grid-column: 1 / -1;
      }

      .card-header {
        margin-bottom: 20px;

        h3 {
          font-size: 16px;
          font-weight: 700;
          color: #0f172a;
          margin: 0;
        }

        .card-subtitle {
          font-size: 13px;
          color: #64748b;
        }

        .header-flex {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .btn-link {
            font-size: 13px;
            font-weight: 600;
            color: #0284c7;
            text-decoration: none;
            &:hover { text-decoration: underline; }
          }
        }
      }
    }

    .breakdown-list {
      display: flex;
      flex-direction: column;
      gap: 16px;

      .breakdown-item {
        .item-label {
          display: flex;
          justify-content: space-between;
          font-size: 13px;
          margin-bottom: 6px;

          .count-badge {
            font-weight: 700;
            color: #334155;
          }
        }

        .item-bar {
          height: 8px;
          background: #f1f5f9;
          border-radius: 4px;
          overflow: hidden;

          .item-bar-fill {
            height: 100%;
            border-radius: 4px;

            &.active { background: #10b981; }
            &.notinuse { background: #94a3b8; }
            &.suspended { background: #f59e0b; }
            &.retired { background: #ef4444; }
            &.env { background: #3b82f6; }
          }
        }
      }
    }

    .status-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;

      &.active { background: #d1fae5; color: #065f46; }
      &.notinuse { background: #f1f5f9; color: #475569; }
      &.suspended { background: #fef3c7; color: #92400e; }
      &.retired { background: #fee2e2; color: #991b1b; }
    }

    .env-badge {
      font-weight: 600;
      color: #1e40af;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 13.5px;

      th {
        background: #f8fafc;
        color: #475569;
        font-weight: 600;
        text-align: left;
        padding: 10px 12px;
        border-bottom: 2px solid #e2e8f0;
      }

      td {
        padding: 12px;
        border-bottom: 1px solid #f1f5f9;
        color: #1e293b;
      }

      .text-center { text-align: center; }
      .font-bold { font-weight: 700; }

      .num-tag {
        display: inline-block;
        padding: 3px 8px;
        border-radius: 6px;
        font-weight: 600;
        font-size: 12px;

        &.blue { background: #eff6ff; color: #1d4ed8; }
        &.green { background: #f0fdf4; color: #15803d; }
      }

      .progress-cell {
        display: flex;
        align-items: center;
        gap: 8px;

        .progress-bar-small {
          flex: 1;
          height: 6px;
          background: #f1f5f9;
          border-radius: 9999px;
          overflow: hidden;

          .fill {
            height: 100%;
            background: #10b981;
          }
        }

        .percent-text {
          font-size: 12px;
          font-weight: 700;
          color: #334155;
          min-width: 32px;
        }
      }

      .empty-cell {
        text-align: center;
        padding: 32px;
        color: #94a3b8;
        font-style: italic;
      }
    }

    /* Modal Styling */
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(15, 23, 42, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 999;
      padding: 16px;
    }

    .modal-card {
      background: #ffffff;
      border-radius: 12px;
      width: 100%;
      max-width: 600px;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);

      &.wide { max-width: 900px; }

      .modal-header {
        padding: 20px 24px;
        border-bottom: 1px solid #e2e8f0;
        display: flex;
        justify-content: space-between;
        align-items: flex-start;

        h2 { font-size: 18px; font-weight: 700; margin: 0; color: #0f172a; }
        .modal-sub { font-size: 13px; color: #64748b; margin: 4px 0 0; }
        .btn-close { background: none; border: none; font-size: 18px; cursor: pointer; color: #94a3b8; }
      }

      .modal-body {
        padding: 24px;
        display: flex;
        flex-direction: column;
        gap: 24px;
      }
    }

    .add-eligibility-form {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 16px;

      h4 { margin: 0 0 12px; font-size: 14px; font-weight: 700; color: #1e293b; }

      .form-row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 12px;
        margin-bottom: 12px;
      }

      .form-row-full {
        display: flex;
        gap: 12px;
        align-items: flex-end;
      }

      .form-group {
        display: flex;
        flex-direction: column;
        gap: 4px;

        label { font-size: 12px; font-weight: 600; color: #475569; }
        .form-control {
          padding: 8px 10px;
          border: 1px solid #cbd5e1;
          border-radius: 6px;
          font-size: 13px;
          outline: none;
          &:focus { border-color: #0284c7; }
        }
      }

      .btn-save {
        padding: 9px 20px;
        background: #0284c7;
        color: #ffffff;
        border: none;
        border-radius: 6px;
        font-weight: 600;
        font-size: 13px;
        cursor: pointer;
        &:disabled { background: #94a3b8; cursor: not-allowed; }
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly orgService = inject(OrganizationService);
  private readonly softwareService = inject(SoftwareService);
  private readonly authService = inject(AuthService);

  kpis = signal<DashboardKpiDto | null>(null);
  isLoading = signal<boolean>(true);
  isCoverageModalOpen = signal<boolean>(false);

  asOfDate: string = new Date().toISOString().substring(0, 10);

  // Reference lookups for modal
  organizationList = signal<OrganizationDto[]>([]);
  softwareList = signal<SoftwareDto[]>([]);
  eligibilityList = signal<CoverageEligibilityDto[]>([]);

  newEligibility: CreateCoverageEligibilityRequest = {
    organizationId: '',
    softwareId: '',
    validFrom: new Date().toISOString().substring(0, 10),
    validTo: null,
    isEligible: true,
    note: ''
  };

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.dashboardService.getOverview({ asOf: this.asOfDate }).subscribe({
      next: data => {
        this.kpis.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  resetToToday(): void {
    this.asOfDate = new Date().toISOString().substring(0, 10);
    this.loadDashboard();
  }

  canManageCoverage(): boolean {
    return this.authService.hasPermission('reports.read') || this.authService.hasPermission('access.manage');
  }

  openCoverageModal(): void {
    this.isCoverageModalOpen.set(true);
    this.loadCoverageData();
  }

  closeCoverageModal(): void {
    this.isCoverageModalOpen.set(false);
  }

  loadCoverageData(): void {
    this.dashboardService.getCoverageEligibilities().subscribe(list => {
      this.eligibilityList.set(list);
    });

    this.orgService.getOrganizations({ isActive: true }).subscribe(res => {
      this.organizationList.set(res.items);
    });

    this.softwareService.getSoftware({ page: 1, pageSize: 100 }).subscribe(res => {
      this.softwareList.set(res.items);
    });
  }

  isEligibilityFormValid(): boolean {
    return !!this.newEligibility.organizationId && !!this.newEligibility.softwareId && !!this.newEligibility.validFrom;
  }

  saveEligibility(): void {
    if (!this.isEligibilityFormValid()) return;

    this.dashboardService.setCoverageEligibility(this.newEligibility).subscribe({
      next: () => {
        this.loadCoverageData();
        this.loadDashboard();
        this.newEligibility = {
          organizationId: '',
          softwareId: '',
          validFrom: new Date().toISOString().substring(0, 10),
          validTo: null,
          isEligible: true,
          note: ''
        };
      }
    });
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Active': return 'Đang hoạt động';
      case 'NotInUse': return 'Chưa sử dụng';
      case 'Suspended': return 'Tạm ngưng';
      case 'Retired': return 'Đã ngừng sử dụng';
      default: return status;
    }
  }

  getPercentage(val: number, total: number): number {
    if (!total || total <= 0) return 0;
    return Math.min(100, Math.round((val / total) * 100));
  }
}
