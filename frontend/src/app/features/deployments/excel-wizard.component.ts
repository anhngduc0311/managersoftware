import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ExcelService, ImportBatchDto, ExportRequestDto, ExportFilterDto } from '@core/services/excel.service';
import { SoftwareService, SoftwareDto } from '@core/services/software.service';
import { OrganizationService, OrganizationDto } from '@core/services/organization.service';

@Component({
  selector: 'app-excel-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="wizard-container">
      <div class="page-header">
        <div class="breadcrumbs">
          <a routerLink="/deployments">Hồ sơ Triển khai</a>
          <span>/</span>
          <span>Nhập & Xuất Excel</span>
        </div>
        <h1 class="page-title">Nhập / Xuất Dữ liệu Triển khai Hàng loạt</h1>
        <p class="subtitle">Tiện ích tải mẫu chuẩn, kiểm tra lỗi toàn diện theo cơ chế All-or-Nothing và xuất báo cáo nền có bảo vệ chống Formula Injection</p>
      </div>

      <!-- Tab Navigation -->
      <div class="tabs-nav">
        <button
          class="tab-btn"
          [class.active]="activeTab() === 'import'"
          (click)="activeTab.set('import')"
        >
          Nhập Dữ liệu Triển khai (Import Excel)
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab() === 'export'"
          (click)="activeTab.set('export')"
        >
          Xuất Báo cáo Excel (Background Export)
        </button>
      </div>

      <!-- Tab Content: IMPORT -->
      <div class="tab-content" *ngIf="activeTab() === 'import'">
        <!-- Step 1: Download Template -->
        <div class="guide-card">
          <div class="guide-content">
            <h4>Quy trình nhập dữ liệu an toàn</h4>
            <p>
              Hệ thống áp dụng cơ chế <strong>All-or-Nothing</strong>: Toàn bộ tệp chỉ được nhập vào hệ thống dưới dạng <em>Bản nháp (Draft)</em> khi có <strong>0 lỗi</strong>.
              Mọi ký tự công thức nguy hiểm (<code>=</code>, <code>+</code>, <code>-</code>, <code>&#64;</code>) sẽ bị từ chối tự động. Giới hạn tối đa 5.000 dòng.
            </p>
            <button class="btn-download-template" (click)="downloadTemplate()" [disabled]="isDownloadingTemplate()">
              <span *ngIf="!isDownloadingTemplate()">Tải Tệp Mẫu Excel Chuẩn (.xlsx)</span>
              <span *ngIf="isDownloadingTemplate()">Đang tạo tệp mẫu...</span>
            </button>
          </div>
        </div>

        <!-- Step 2: Upload Dropzone -->
        <div class="upload-section">
          <div
            class="dropzone"
            (dragover)="onDragOver($event)"
            (dragleave)="onDragLeave($event)"
            (drop)="onFileDrop($event)"
            [class.dragging]="isDragging()"
          >
            <div class="dropzone-text">
              <h3>Kéo & thả tệp Excel vào đây</h3>
              <p>Hoặc bấm vào nút bên dưới để chọn tệp từ máy tính (.xlsx, tối đa 20MB)</p>
            </div>
            <input
              type="file"
              #fileInput
              accept=".xlsx"
              style="display: none;"
              (change)="onFileSelected($event)"
            />
            <button class="btn-select-file" (click)="fileInput.click()" [disabled]="isUploading()">
              <span *ngIf="!isUploading()">Chọn tệp Excel</span>
              <span *ngIf="isUploading()">Đang tải lên & quét an toàn...</span>
            </button>
          </div>
        </div>

        <!-- Step 3: Batch Results & Validation -->
        <div class="batch-results-card" *ngIf="currentBatch()">
          <div class="results-header">
            <div>
              <h3>Kết quả Kiểm tra Tệp: {{ currentBatch()?.originalFileName }}</h3>
              <p class="batch-meta">Mã lô: <code>{{ currentBatch()?.id }}</code> | Thời gian: {{ currentBatch()?.createdAt | date:'HH:mm:ss dd/MM/yyyy' }}</p>
            </div>
            <div class="status-indicator">
              <span class="badge" [ngClass]="currentBatch()?.status?.toLowerCase()">
                {{ getBatchStatusLabel(currentBatch()?.status) }}
              </span>
            </div>
          </div>

          <!-- Counters -->
          <div class="batch-metrics">
            <div class="metric-item">
              <span class="m-label">Tổng số dòng</span>
              <span class="m-val">{{ currentBatch()?.totalRows }}</span>
            </div>
            <div class="metric-item valid">
              <span class="m-label">Dòng hợp lệ</span>
              <span class="m-val">{{ currentBatch()?.validRows }}</span>
            </div>
            <div class="metric-item error">
              <span class="m-label">Dòng phát hiện lỗi</span>
              <span class="m-val">{{ currentBatch()?.errorRows }}</span>
            </div>
          </div>

          <!-- Error Message banner if any -->
          <div class="alert alert-danger" *ngIf="currentBatch()?.failureReason">
            {{ currentBatch()?.failureReason }}
          </div>

          <!-- Error Grid -->
          <div class="error-table-wrapper" *ngIf="currentBatch()?.errors?.length">
            <h4>Danh sách Chi tiết các Lỗi cần Khắc phục ({{ currentBatch()?.errors?.length }} lỗi):</h4>
            <table class="data-table error-table">
              <thead>
                <tr>
                  <th style="width: 80px;">Dòng</th>
                  <th style="width: 150px;">Cột dữ liệu</th>
                  <th style="width: 180px;">Mã lỗi</th>
                  <th>Nội dung thông báo lỗi</th>
                  <th style="width: 200px;">Giá trị tệp</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let err of currentBatch()?.errors">
                  <td class="text-center font-bold">{{ err.rowIndex }}</td>
                  <td><span class="col-tag">{{ err.columnName || '-' }}</span></td>
                  <td><span class="code-tag">{{ err.errorCode || 'VALIDATION_ERROR' }}</span></td>
                  <td class="error-text">{{ err.errorMessage }}</td>
                  <td><code>{{ err.rawValue || '(Trống)' }}</code></td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Commit Actions -->
          <div class="commit-actions">
            <div class="commit-notes">
              <span *ngIf="currentBatch()?.status === 'ReadyToCommit'" class="text-success">
                Tệp kiểm tra đạt 0 lỗi! Bạn có thể nhấn nút Commit bên phải để nhập các hồ sơ triển khai ở trạng thái Bản nháp.
              </span>
              <span *ngIf="currentBatch()?.status === 'FailedValidation'" class="text-danger">
                Tệp chứa lỗi. Vui lòng sửa lại tệp Excel trên máy và tải lên lại (All-or-Nothing).
              </span>
              <span *ngIf="currentBatch()?.status === 'Committed'" class="text-success font-bold">
                Lô nhập liệu đã được Commit thành công! Các hồ sơ triển khai đã được tạo ở trạng thái Bản nháp.
              </span>
            </div>

            <div class="btn-group">
              <button
                class="btn-validate"
                (click)="revalidateBatch()"
                *ngIf="currentBatch()?.status === 'FailedValidation' || currentBatch()?.status === 'Pending'"
                [disabled]="isValidating()"
              >
                Kiểm tra lại
              </button>

              <button
                class="btn-commit"
                (click)="commitBatch()"
                [disabled]="currentBatch()?.status !== 'ReadyToCommit' || isCommitting()"
              >
                <span *ngIf="!isCommitting()">Cam kết nhập dữ liệu (Commit)</span>
                <span *ngIf="isCommitting()">Đang ghi dữ liệu bản nháp...</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Tab Content: EXPORT -->
      <div class="tab-content" *ngIf="activeTab() === 'export'">
        <div class="export-card">
          <div class="card-header">
            <h3>Cấu hình Bộ lọc Xuất Báo cáo Triển khai</h3>
            <p class="subtitle">Tiến trình xuất file diễn ra ngầm dưới nền, tự động ẩn cột tài chính nếu thiếu quyền và gắn hạn lưu 24 giờ</p>
          </div>

          <div class="filter-form">
            <div class="form-grid">
              <div class="form-group">
                <label>Lọc theo Phần mềm</label>
                <select [(ngModel)]="exportFilter.softwareId" class="form-control">
                  <option [ngValue]="null">-- Tất cả phần mềm --</option>
                  <option *ngFor="let s of softwareList()" [value]="s.id">{{ s.name }} ({{ s.code }})</option>
                </select>
              </div>

              <div class="form-group">
                <label>Lọc theo Đơn vị</label>
                <select [(ngModel)]="exportFilter.organizationId" class="form-control">
                  <option [ngValue]="null">-- Tất cả đơn vị --</option>
                  <option *ngFor="let o of organizationList()" [value]="o.id">{{ o.name }} ({{ o.code }})</option>
                </select>
              </div>

              <div class="form-group">
                <label>Trạng thái vận hành</label>
                <select [(ngModel)]="exportFilter.status" class="form-control">
                  <option [ngValue]="null">-- Tất cả trạng thái --</option>
                  <option value="Active">Đang hoạt động (Active)</option>
                  <option value="NotInUse">Chưa sử dụng (NotInUse)</option>
                  <option value="Suspended">Tạm ngưng (Suspended)</option>
                  <option value="Retired">Đã ngừng (Retired)</option>
                </select>
              </div>

              <div class="form-group">
                <label>Từ khóa tìm kiếm</label>
                <input
                  type="text"
                  [(ngModel)]="exportFilter.searchTerm"
                  placeholder="Nhập tên đơn vị, phần mềm..."
                  class="form-control"
                />
              </div>
            </div>

            <div class="form-actions">
              <button class="btn-request-export" (click)="requestExport()" [disabled]="isRequestingExport()">
                <span *ngIf="!isRequestingExport()">Gửi Yêu cầu Xuất Tệp Excel</span>
                <span *ngIf="isRequestingExport()">Đang gửi yêu cầu...</span>
              </button>
            </div>
          </div>

          <!-- Current Export Job Status -->
          <div class="export-status-panel" *ngIf="currentExport()">
            <div class="status-box">
              <div class="status-left">
                <div>
                  <h4>Yêu cầu Xuất: {{ currentExport()?.fileName || 'BaoCao_TrienKhai.xlsx' }}</h4>
                  <p class="job-meta">
                    Trạng thái: <strong>{{ currentExport()?.status }}</strong> |
                    Tạo lúc: {{ currentExport()?.createdAt | date:'HH:mm:ss dd/MM/yyyy' }}
                    <span *ngIf="currentExport()?.expiresAt"> | Hết hạn lúc: {{ currentExport()?.expiresAt | date:'HH:mm:ss dd/MM/yyyy' }}</span>
                  </p>
                </div>
              </div>

              <div class="status-right">
                <button
                  class="btn-download-export"
                  *ngIf="currentExport()?.status === 'Completed'"
                  (click)="downloadExportFile()"
                >
                  Tải Tệp Kết quả (.xlsx)
                </button>

                <div class="processing-spinner" *ngIf="currentExport()?.status === 'Queued' || currentExport()?.status === 'Processing'">
                  <div class="spinner-small"></div>
                  <span>Tiến trình nền đang xử lý...</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .wizard-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
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

    .tabs-nav {
      display: flex;
      gap: 8px;
      border-bottom: 2px solid #e2e8f0;

      .tab-btn {
        background: none;
        border: none;
        padding: 12px 20px;
        font-size: 14px;
        font-weight: 600;
        color: #64748b;
        cursor: pointer;
        position: relative;
        transition: color 0.2s ease;

        &:hover { color: #0f172a; }

        &.active {
          color: #0284c7;
          &::after {
            content: '';
            position: absolute;
            bottom: -2px;
            left: 0;
            right: 0;
            height: 2px;
            background: #0284c7;
          }
        }
      }
    }

    .guide-card {
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      border-radius: 12px;
      padding: 20px;
      display: flex;
      gap: 16px;
      align-items: flex-start;

      .guide-icon { font-size: 28px; }
      .guide-content {
        flex: 1;
        h4 { margin: 0 0 6px; font-size: 15px; font-weight: 700; color: #1e3a8a; }
        p { margin: 0 0 14px; font-size: 13.5px; color: #1e40af; line-height: 1.5; }
      }

      .btn-download-template {
        background: #1d4ed8;
        color: #ffffff;
        border: none;
        padding: 9px 18px;
        border-radius: 6px;
        font-weight: 600;
        font-size: 13px;
        cursor: pointer;
        &:hover { background: #1e40af; }
        &:disabled { background: #94a3b8; }
      }
    }

    .upload-section {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 32px;
    }

    .dropzone {
      border: 2px dashed #cbd5e1;
      border-radius: 12px;
      padding: 48px 24px;
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
      transition: all 0.2s ease;
      background: #f8fafc;

      &.dragging {
        border-color: #0284c7;
        background: #f0f9ff;
      }

      .dropzone-icon { font-size: 48px; }
      .dropzone-text {
        h3 { margin: 0 0 4px; font-size: 16px; font-weight: 700; color: #1e293b; }
        p { margin: 0; font-size: 13px; color: #64748b; }
      }

      .btn-select-file {
        background: #0284c7;
        color: #ffffff;
        border: none;
        padding: 10px 24px;
        border-radius: 8px;
        font-weight: 600;
        font-size: 13.5px;
        cursor: pointer;
        &:hover { background: #0369a1; }
        &:disabled { background: #94a3b8; }
      }
    }

    .batch-results-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 20px;

      .results-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;

        h3 { margin: 0; font-size: 16px; font-weight: 700; color: #0f172a; }
        .batch-meta { font-size: 12.5px; color: #64748b; margin: 4px 0 0; }
      }
    }

    .batch-metrics {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
      gap: 16px;

      .metric-item {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 4px;

        .m-label { font-size: 12px; font-weight: 600; color: #64748b; }
        .m-val { font-size: 24px; font-weight: 800; color: #0f172a; }

        &.valid { border-color: #bbf7d0; background: #f0fdf4; .m-val { color: #16a34a; } }
        &.error { border-color: #fecaca; background: #fef2f2; .m-val { color: #dc2626; } }
      }
    }

    .error-table-wrapper {
      h4 { margin: 0 0 12px; font-size: 14px; font-weight: 700; color: #991b1b; }

      .col-tag { background: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-weight: 600; font-size: 12px; }
      .code-tag { background: #fee2e2; color: #991b1b; padding: 2px 6px; border-radius: 4px; font-weight: 700; font-size: 11px; }
      .error-text { color: #b91c1c; font-weight: 500; }
    }

    .commit-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 16px;
      border-top: 1px solid #e2e8f0;

      .btn-group {
        display: flex;
        gap: 12px;
      }

      .btn-validate {
        background: #f1f5f9;
        border: 1px solid #cbd5e1;
        padding: 9px 18px;
        border-radius: 6px;
        font-weight: 600;
        font-size: 13px;
        cursor: pointer;
      }

      .btn-commit {
        background: #16a34a;
        color: #ffffff;
        border: none;
        padding: 10px 24px;
        border-radius: 6px;
        font-weight: 700;
        font-size: 13.5px;
        cursor: pointer;
        &:hover { background: #15803d; }
        &:disabled { background: #94a3b8; cursor: not-allowed; }
      }
    }

    /* Export Styles */
    .export-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .filter-form {
      .form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 16px;
      }

      .form-group {
        display: flex;
        flex-direction: column;
        gap: 6px;

        label { font-size: 13px; font-weight: 600; color: #334155; }
        .form-control {
          padding: 9px 12px;
          border: 1px solid #cbd5e1;
          border-radius: 6px;
          font-size: 13.5px;
          outline: none;
        }
      }

      .form-actions {
        margin-top: 20px;
        display: flex;
        justify-content: flex-end;

        .btn-request-export {
          background: #0284c7;
          color: #ffffff;
          border: none;
          padding: 10px 24px;
          border-radius: 6px;
          font-weight: 700;
          font-size: 13.5px;
          cursor: pointer;
          &:hover { background: #0369a1; }
          &:disabled { background: #94a3b8; }
        }
      }
    }

    .export-status-panel {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 20px;

      .status-box {
        display: flex;
        justify-content: space-between;
        align-items: center;

        .status-left {
          display: flex;
          align-items: center;
          gap: 16px;

          .job-icon { font-size: 32px; }
          h4 { margin: 0; font-size: 15px; font-weight: 700; color: #0f172a; }
          .job-meta { margin: 4px 0 0; font-size: 13px; color: #64748b; }
        }

        .btn-download-export {
          background: #10b981;
          color: #ffffff;
          border: none;
          padding: 10px 20px;
          border-radius: 6px;
          font-weight: 700;
          font-size: 13px;
          cursor: pointer;
          &:hover { background: #059669; }
        }

        .processing-spinner {
          display: flex;
          align-items: center;
          gap: 8px;
          color: #0284c7;
          font-weight: 600;
          font-size: 13px;

          .spinner-small {
            width: 18px;
            height: 18px;
            border: 2px solid #e2e8f0;
            border-top-color: #0284c7;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
          }
        }
      }
    }

    .badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 9999px;
      font-size: 12px;
      font-weight: 700;

      &.pending { background: #fef3c7; color: #92400e; }
      &.validating { background: #e0f2fe; color: #0369a1; }
      &.readytocommit { background: #dcfce7; color: #15803d; }
      &.failedvalidation { background: #fee2e2; color: #b91c1c; }
      &.committed { background: #d1fae5; color: #065f46; }
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 13px;

      th { background: #f8fafc; padding: 10px 12px; border-bottom: 2px solid #e2e8f0; text-align: left; }
      td { padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }
      .text-center { text-align: center; }
      .font-bold { font-weight: 700; }
    }

    .alert {
      padding: 12px 16px;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 500;
      &.alert-danger { background: #fef2f2; border: 1px solid #fecaca; color: #991b1b; }
    }
  `]
})
export class ExcelWizardComponent implements OnInit {
  private readonly excelService = inject(ExcelService);
  private readonly softwareService = inject(SoftwareService);
  private readonly orgService = inject(OrganizationService);

  activeTab = signal<'import' | 'export'>('import');

  // Import state
  isDragging = signal<boolean>(false);
  isUploading = signal<boolean>(false);
  isValidating = signal<boolean>(false);
  isCommitting = signal<boolean>(false);
  isDownloadingTemplate = signal<boolean>(false);
  currentBatch = signal<ImportBatchDto | null>(null);

  // Export state
  isRequestingExport = signal<boolean>(false);
  currentExport = signal<ExportRequestDto | null>(null);
  exportFilter: ExportFilterDto = {
    softwareId: null,
    organizationId: null,
    status: null,
    searchTerm: null
  };

  softwareList = signal<SoftwareDto[]>([]);
  organizationList = signal<OrganizationDto[]>([]);

  ngOnInit(): void {
    this.loadReferenceData();
  }

  loadReferenceData(): void {
    this.softwareService.getSoftware({ page: 1, pageSize: 100 }).subscribe(res => {
      this.softwareList.set(res.items);
    });

    this.orgService.getOrganizations({ isActive: true }).subscribe(res => {
      this.organizationList.set(res.items);
    });
  }

  downloadTemplate(): void {
    this.isDownloadingTemplate.set(true);
    this.excelService.downloadTemplate().subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'Mau_Nhap_Lieu_Trien_Khai.xlsx';
        a.click();
        window.URL.revokeObjectURL(url);
        this.isDownloadingTemplate.set(false);
      },
      error: () => this.isDownloadingTemplate.set(false)
    });
  }

  onDragOver(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(false);
  }

  onFileDrop(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(false);
    if (e.dataTransfer && e.dataTransfer.files.length > 0) {
      this.uploadFile(e.dataTransfer.files[0]);
    }
  }

  onFileSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile(input.files[0]);
    }
  }

  uploadFile(file: File): void {
    this.isUploading.set(true);
    this.excelService.uploadImport(file).subscribe({
      next: batch => {
        this.currentBatch.set(batch);
        this.isUploading.set(false);
        // Automatically trigger validation
        this.revalidateBatch();
      },
      error: () => this.isUploading.set(false)
    });
  }

  revalidateBatch(): void {
    const batch = this.currentBatch();
    if (!batch) return;

    this.isValidating.set(true);
    this.excelService.validateImportBatch(batch.id).subscribe({
      next: res => {
        this.currentBatch.set(res.batch);
        this.isValidating.set(false);
      },
      error: () => this.isValidating.set(false)
    });
  }

  commitBatch(): void {
    const batch = this.currentBatch();
    if (!batch) return;

    this.isCommitting.set(true);
    this.excelService.commitImportBatch(batch.id).subscribe({
      next: res => {
        this.isCommitting.set(false);
        // Refresh batch details
        this.excelService.getImportBatch(batch.id).subscribe(b => this.currentBatch.set(b));
      },
      error: () => this.isCommitting.set(false)
    });
  }

  requestExport(): void {
    this.isRequestingExport.set(true);
    this.excelService.requestExport(this.exportFilter).subscribe({
      next: req => {
        this.currentExport.set(req);
        this.isRequestingExport.set(false);
        this.pollExportStatus(req.id);
      },
      error: () => this.isRequestingExport.set(false)
    });
  }

  pollExportStatus(exportId: string): void {
    const interval = setInterval(() => {
      this.excelService.getExportStatus(exportId).subscribe({
        next: req => {
          this.currentExport.set(req);
          if (req.status === 'Completed' || req.status === 'Failed') {
            clearInterval(interval);
          }
        },
        error: () => clearInterval(interval)
      });
    }, 2000);
  }

  downloadExportFile(): void {
    const exp = this.currentExport();
    if (!exp) return;

    this.excelService.downloadExport(exp.id).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = exp.fileName || 'BaoCao_TrienKhai.xlsx';
        a.click();
        window.URL.revokeObjectURL(url);
      }
    });
  }

  getBatchStatusLabel(status?: string): string {
    switch (status) {
      case 'Pending': return 'Đang chờ xử lý';
      case 'Validating': return 'Đang kiểm tra tệp...';
      case 'ReadyToCommit': return 'Hợp lệ 100% (Sẵn sàng Commit)';
      case 'FailedValidation': return 'Phát hiện lỗi dữ liệu';
      case 'Committed': return 'Đã hoàn tất Commit';
      default: return status || 'Không rõ';
    }
  }
}
