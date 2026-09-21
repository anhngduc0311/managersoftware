import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuditService, AuditLogDto, AuditFilterDto } from '@core/services/audit.service';

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="audit-container">
      <div class="page-header">
        <div class="breadcrumbs">
          <a routerLink="/admin/roles">Quản trị Hệ thống</a>
          <span>/</span>
          <span>Nhật ký Kiểm toán</span>
        </div>
        <div class="header-flex">
          <div>
            <h1 class="page-title">Nhật ký Kiểm toán & Dấu vết Hệ thống (Audit Trail)</h1>
            <p class="subtitle">Tra cứu toàn bộ biến động dữ liệu, truy vết người thực hiện và đối soát tính toàn vẹn (Chỉ ghi Append-Only)</p>
          </div>
          <div class="security-badge">
            Append-Only Protected
          </div>
        </div>
      </div>

      <!-- Filters Panel -->
      <div class="filters-card">
        <div class="filter-grid">
          <div class="form-group">
            <label>Loại thực thể (EntityType)</label>
            <input
              type="text"
              [(ngModel)]="filter.entityType"
              placeholder="VD: Deployment, Contract, User..."
              class="form-control"
              (keyup.enter)="applyFilters()"
            />
          </div>

          <div class="form-group">
            <label>Hành động (Action)</label>
            <select [(ngModel)]="filter.action" class="form-control" (change)="applyFilters()">
              <option value="">-- Tất cả hành động --</option>
              <option value="Create">Tạo mới (Create)</option>
              <option value="Update">Chỉnh sửa (Update)</option>
              <option value="Delete">Xóa (Delete)</option>
              <option value="Submit">Gửi duyệt (Submit)</option>
              <option value="Approve">Phê duyệt (Approve)</option>
              <option value="Reject">Từ chối (Reject)</option>
            </select>
          </div>

          <div class="form-group">
            <label>Mã truy vết (Correlation ID)</label>
            <input
              type="text"
              [(ngModel)]="filter.correlationId"
              placeholder="Nhập Correlation ID..."
              class="form-control"
              (keyup.enter)="applyFilters()"
            />
          </div>

          <div class="form-group">
            <label>Tìm kiếm chung</label>
            <input
              type="text"
              [(ngModel)]="filter.searchTerm"
              placeholder="Tìm theo ID, từ khóa..."
              class="form-control"
              (keyup.enter)="applyFilters()"
            />
          </div>
        </div>

        <div class="filter-actions">
          <button class="btn-reset" (click)="resetFilters()">Đặt lại bộ lọc</button>
          <button class="btn-search" (click)="applyFilters()">Tìm kiếm</button>
        </div>
      </div>

      <!-- Audit Logs Table -->
      <div class="table-card">
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th style="width: 170px;">Thời điểm (UTC+7)</th>
                <th style="width: 160px;">Người thực hiện</th>
                <th style="width: 120px;">Hành động</th>
                <th style="width: 160px;">Loại thực thể</th>
                <th>Mã thực thể (ID)</th>
                <th>Đơn vị liên quan</th>
                <th style="width: 140px;">Correlation ID</th>
                <th style="width: 100px; text-align: center;">Chi tiết</th>
              </tr>
            </thead>
            <tbody>
              @if (isLoading()) {
                <tr>
                  <td colspan="8" class="loading-cell">
                    <div class="spinner-small"></div> Đang tải nhật ký kiểm toán...
                  </td>
                </tr>
              } @else if (logs().length === 0) {
                <tr>
                  <td colspan="8" class="empty-cell">Không tìm thấy bản ghi nhật ký kiểm toán nào phù hợp</td>
                </tr>
              } @else {
                @for (log of logs(); track log.id) {
                  <tr>
                    <td class="time-cell">{{ log.occurredAt | date:'dd/MM/yyyy HH:mm:ss' }}</td>
                    <td>
                      <div class="actor-info">
                        <strong>{{ log.actorName || 'Hệ thống' }}</strong>
                      </div>
                    </td>
                    <td>
                      <span class="action-badge" [ngClass]="log.action.toLowerCase()">{{ log.action }}</span>
                    </td>
                    <td>
                      <span class="entity-badge">{{ log.entityType }}</span>
                    </td>
                    <td>
                      <code class="id-code" title="{{ log.entityId }}">{{ truncateId(log.entityId) }}</code>
                    </td>
                    <td>{{ log.organizationName || '-' }}</td>
                    <td>
                      <code class="corr-code" title="{{ log.correlationId }}">{{ truncateId(log.correlationId || '') }}</code>
                    </td>
                    <td class="text-center">
                      <button class="btn-diff" (click)="openDiffModal(log)">
                        Xem Diff
                      </button>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pagination-bar" *ngIf="totalPages() > 1">
          <span class="page-info">Trang {{ currentPage() }} / {{ totalPages() }} (Tổng số {{ totalCount() }} bản ghi)</span>
          <div class="page-controls">
            <button [disabled]="currentPage() === 1" (click)="goToPage(currentPage() - 1)">← Trước</button>
            <button [disabled]="currentPage() === totalPages()" (click)="goToPage(currentPage() + 1)">Sau →</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal: JSON Diff Viewer -->
    @if (selectedLog()) {
      <div class="modal-backdrop" (click)="closeDiffModal()">
        <div class="modal-card wide" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div>
              <h2>Chi tiết Thay đổi Dữ liệu: {{ selectedLog()?.entityType }} ({{ selectedLog()?.action }})</h2>
              <p class="modal-sub">Mã thực thể: <code>{{ selectedLog()?.entityId }}</code> | Người thực hiện: <strong>{{ selectedLog()?.actorName || 'Hệ thống' }}</strong></p>
            </div>
            <button class="btn-close" (click)="closeDiffModal()">✕</button>
          </div>

          <div class="modal-body">
            <div class="diff-container">
              <!-- Before State -->
              <div class="diff-pane">
                <div class="pane-header before">
                  <span>Trạng thái Trước (Before)</span>
                </div>
                <pre class="json-viewer">{{ formatJson(selectedLog()?.beforeJson) }}</pre>
              </div>

              <!-- After State -->
              <div class="diff-pane">
                <div class="pane-header after">
                  <span>Trạng thái Sau (After)</span>
                </div>
                <pre class="json-viewer">{{ formatJson(selectedLog()?.afterJson) }}</pre>
              </div>
            </div>

            <div class="sanitization-notice">
              <strong>Lưu ý bảo mật:</strong> Toàn bộ trường nhạy cảm (mật khẩu, khóa bảo mật, token xác thực) đã được tự động loại bỏ / băm bảo vệ.
            </div>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .audit-container {
      display: flex;
      flex-direction: column;
      gap: 20px;
      padding-bottom: 32px;
    }

    .breadcrumbs {
      display: flex;
      gap: 8px;
      font-size: 13px;
      color: #64748b;
      margin-bottom: 6px;
      a { color: #0284c7; text-decoration: none; &:hover { text-decoration: underline; } }
    }

    .header-flex {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 16px;
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

    .security-badge {
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
      color: #16a34a;
      padding: 6px 12px;
      border-radius: 9999px;
      font-size: 12px;
      font-weight: 700;
    }

    .filters-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;

      .filter-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 14px;
      }

      .form-group {
        display: flex;
        flex-direction: column;
        gap: 4px;

        label { font-size: 12.5px; font-weight: 600; color: #334155; }
        .form-control {
          padding: 8px 10px;
          border: 1px solid #cbd5e1;
          border-radius: 6px;
          font-size: 13px;
          outline: none;
          &:focus { border-color: #0284c7; }
        }
      }

      .filter-actions {
        display: flex;
        justify-content: flex-end;
        gap: 10px;

        .btn-reset {
          background: #f1f5f9;
          border: 1px solid #cbd5e1;
          padding: 7px 16px;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 600;
          cursor: pointer;
        }

        .btn-search {
          background: #0284c7;
          color: #ffffff;
          border: none;
          padding: 7px 20px;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 600;
          cursor: pointer;
          &:hover { background: #0369a1; }
        }
      }
    }

    .table-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 13px;

      th {
        background: #f8fafc;
        padding: 12px 14px;
        color: #475569;
        font-weight: 600;
        text-align: left;
        border-bottom: 2px solid #e2e8f0;
      }

      td {
        padding: 12px 14px;
        border-bottom: 1px solid #f1f5f9;
        color: #1e293b;
      }

      .time-cell { font-family: monospace; color: #475569; font-size: 12px; }
      .text-center { text-align: center; }

      .action-badge {
        display: inline-block;
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 700;

        &.create { background: #dcfce7; color: #15803d; }
        &.update { background: #e0f2fe; color: #0369a1; }
        &.delete { background: #fee2e2; color: #b91c1c; }
        &.submit { background: #fef3c7; color: #92400e; }
        &.approve { background: #d1fae5; color: #065f46; }
        &.reject { background: #ffe4e6; color: #be123c; }
      }

      .entity-badge {
        font-weight: 600;
        color: #1e40af;
        background: #eff6ff;
        padding: 2px 6px;
        border-radius: 4px;
        font-size: 12px;
      }

      .id-code, .corr-code {
        font-size: 11px;
        background: #f1f5f9;
        padding: 2px 5px;
        border-radius: 3px;
      }

      .btn-diff {
        background: #f8fafc;
        border: 1px solid #cbd5e1;
        padding: 4px 10px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
        &:hover { background: #e2e8f0; }
      }
    }

    .loading-cell, .empty-cell {
      text-align: center;
      padding: 36px;
      color: #64748b;
    }

    .pagination-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 14px 20px;
      border-top: 1px solid #e2e8f0;
      background: #f8fafc;

      .page-info { font-size: 13px; color: #64748b; }
      .page-controls {
        display: flex;
        gap: 8px;
        button {
          background: #ffffff;
          border: 1px solid #cbd5e1;
          padding: 6px 12px;
          border-radius: 6px;
          font-size: 12.5px;
          font-weight: 600;
          cursor: pointer;
          &:disabled { background: #f1f5f9; color: #94a3b8; cursor: not-allowed; }
        }
      }
    }

    /* Modal */
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(15, 23, 42, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 16px;
    }

    .modal-card {
      background: #ffffff;
      border-radius: 12px;
      width: 100%;
      max-width: 960px;
      max-height: 90vh;
      display: flex;
      flex-direction: column;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);

      .modal-header {
        padding: 18px 24px;
        border-bottom: 1px solid #e2e8f0;
        display: flex;
        justify-content: space-between;
        align-items: flex-start;

        h2 { font-size: 17px; font-weight: 700; margin: 0; color: #0f172a; }
        .modal-sub { font-size: 13px; color: #64748b; margin: 4px 0 0; }
        .btn-close { background: none; border: none; font-size: 18px; cursor: pointer; color: #94a3b8; }
      }

      .modal-body {
        padding: 24px;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
    }

    .diff-container {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;

      @media (max-width: 768px) {
        grid-template-columns: 1fr;
      }

      .diff-pane {
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        overflow: hidden;
        background: #f8fafc;

        .pane-header {
          padding: 8px 12px;
          font-size: 12px;
          font-weight: 700;

          &.before { background: #fee2e2; color: #991b1b; }
          &.after { background: #dcfce7; color: #166534; }
        }

        .json-viewer {
          padding: 14px;
          margin: 0;
          font-family: Consolas, Monaco, monospace;
          font-size: 12px;
          line-height: 1.4;
          white-space: pre-wrap;
          word-break: break-word;
          max-height: 400px;
          overflow-y: auto;
        }
      }
    }

    .sanitization-notice {
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      padding: 10px 14px;
      border-radius: 6px;
      font-size: 12.5px;
      color: #1e40af;
    }
  `]
})
export class AuditLogListComponent implements OnInit {
  private readonly auditService = inject(AuditService);

  logs = signal<AuditLogDto[]>([]);
  isLoading = signal<boolean>(true);
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  totalPages = signal<number>(1);
  totalCount = signal<number>(0);

  selectedLog = signal<AuditLogDto | null>(null);

  filter: AuditFilterDto = {
    entityType: '',
    action: '',
    searchTerm: '',
    correlationId: '',
    page: 1,
    pageSize: 20
  };

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading.set(true);
    this.filter.page = this.currentPage();
    this.filter.pageSize = this.pageSize();

    this.auditService.getAuditLogs(this.filter).subscribe({
      next: res => {
        this.logs.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadLogs();
  }

  resetFilters(): void {
    this.filter = {
      entityType: '',
      action: '',
      searchTerm: '',
      correlationId: '',
      page: 1,
      pageSize: 20
    };
    this.currentPage.set(1);
    this.loadLogs();
  }

  goToPage(page: number): void {
    this.currentPage.set(page);
    this.loadLogs();
  }

  openDiffModal(log: AuditLogDto): void {
    this.selectedLog.set(log);
  }

  closeDiffModal(): void {
    this.selectedLog.set(null);
  }

  truncateId(id: string): string {
    if (!id) return '';
    return id.length > 8 ? `${id.substring(0, 8)}...` : id;
  }

  formatJson(jsonStr?: string | null): string {
    if (!jsonStr) return '(Không có dữ liệu)';
    try {
      const obj = JSON.parse(jsonStr);
      return JSON.stringify(obj, null, 2);
    } catch {
      return jsonStr;
    }
  }
}
