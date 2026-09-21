import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DeploymentService } from '@core/services/deployment.service';
import { SoftwareService, SoftwareReleaseDto } from '@core/services/software.service';
import { ContractService } from '@core/services/contract.service';
import { DocumentService } from '@core/services/document.service';
import { AuthService } from '@core/services/auth.service';
import { DeploymentDto, DeploymentRevisionDto, ApprovalDecisionDto } from '@core/models/deployment.models';
import { LicenseAllocationDto } from '@core/models/contract.models';
import { DocumentAttachmentDto } from '@core/models/document.models';

@Component({
  selector: 'app-deployment-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './deployment-detail.component.html',
  styleUrls: ['./deployment-detail.component.scss']
})
export class DeploymentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private deploymentService = inject(DeploymentService);
  private softwareService = inject(SoftwareService);
  private contractService = inject(ContractService);
  private documentService = inject(DocumentService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

  deploymentId = signal<string>('');
  deployment = signal<DeploymentDto | null>(null);
  historyRevisions = signal<DeploymentRevisionDto[]>([]);
  releases = signal<SoftwareReleaseDto[]>([]);
  allocations = signal<LicenseAllocationDto[]>([]);
  attachments = signal<DocumentAttachmentDto[]>([]);
  isUploadingDoc = signal<boolean>(false);
  docUploadError = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  actionError = signal<string | null>(null);
  actionSuccess = signal<string | null>(null);
  concurrencyWarning = signal<boolean>(false);

  // Active Tab: 'approved' | 'active' | 'history'
  activeTab = signal<'approved' | 'active' | 'history'>('approved');

  // Reject Modal
  isRejectModalOpen = signal<boolean>(false);
  rejectReason = signal<string>('');
  rejectError = signal<string | null>(null);

  // Edit Draft Form
  draftForm = this.fb.group({
    releaseId: [''],
    operationalStatus: ['NotInUse', [Validators.required]],
    startDate: [''],
    goLiveDate: ['']
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.deploymentId.set(id);
      this.loadDeployment(id);
    }
  }

  loadDeployment(id: string): void {
    this.isLoading.set(true);
    this.concurrencyWarning.set(false);
    this.actionError.set(null);

    this.deploymentService.getDeploymentById(id).subscribe({
      next: (dep) => {
        this.deployment.set(dep);
        this.isLoading.set(false);

        // Load software releases for dropdown
        this.softwareService.getReleases(dep.softwareId).subscribe({
          next: (rels: SoftwareReleaseDto[]) => this.releases.set(rels || []),
          error: () => {}
        });

        // If there's an active revision (Draft or Submitted), show active tab by default
        if (dep.activeRevision) {
          this.activeTab.set('active');
          this.initDraftForm(dep.activeRevision);
        } else {
          this.activeTab.set('approved');
        }

        // Load history, allocations and attachments
        this.loadHistory(id);
        this.loadAllocations(id);
        this.loadAttachments(dep.activeRevision?.id || dep.currentApprovedRevision?.id);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.actionError.set(err.error?.detail || 'Không thể tải thông tin hồ sơ.');
      }
    });
  }

  loadHistory(id: string): void {
    this.deploymentService.getRevisionHistory(id).subscribe({
      next: (history) => this.historyRevisions.set(history || []),
      error: () => {}
    });
  }

  initDraftForm(revision: DeploymentRevisionDto): void {
    this.draftForm.patchValue({
      releaseId: revision.releaseId || '',
      operationalStatus: revision.operationalStatus || 'NotInUse',
      startDate: revision.startDate || '',
      goLiveDate: revision.goLiveDate || ''
    });
  }

  setTab(tab: 'approved' | 'active' | 'history'): void {
    this.activeTab.set(tab);
    this.actionError.set(null);
    this.actionSuccess.set(null);
  }

  saveDraft(): void {
    const dep = this.deployment();
    const activeRev = dep?.activeRevision;
    if (!dep || !activeRev) return;

    if (this.draftForm.invalid) {
      this.draftForm.markAllAsTouched();
      return;
    }

    const val = this.draftForm.value;
    this.actionError.set(null);
    this.actionSuccess.set(null);
    this.concurrencyWarning.set(false);

    this.deploymentService.updateDraftRevision(activeRev.id, {
      releaseId: val.releaseId || undefined,
      operationalStatus: val.operationalStatus!,
      startDate: val.startDate || undefined,
      goLiveDate: val.goLiveDate || undefined,
      responsibleUserId: this.authService.currentUser()?.id
    }, activeRev.version).subscribe({
      next: (updatedRev) => {
        this.actionSuccess.set('Đã lưu bản nháp thành công.');
        this.loadDeployment(dep.id);
      },
      error: (err) => {
        if (err.status === 412) {
          this.concurrencyWarning.set(true);
        } else {
          this.actionError.set(err.error?.detail || err.error?.title || 'Không thể lưu bản nháp.');
        }
      }
    });
  }

  submitRevision(): void {
    const dep = this.deployment();
    const activeRev = dep?.activeRevision;
    if (!dep || !activeRev) return;

    const val = this.draftForm.value;
    if (val.operationalStatus === 'Active' && !val.goLiveDate) {
      this.actionError.set('Ngày đưa vào sử dụng là bắt buộc khi trạng thái là "Đang sử dụng" (Active).');
      return;
    }

    if (!val.releaseId) {
      this.actionError.set('Bắt buộc phải chọn phiên bản phát hành khi gửi duyệt.');
      return;
    }

    // Save changes first then submit, or submit directly
    this.actionError.set(null);
    this.actionSuccess.set(null);
    this.concurrencyWarning.set(false);

    // Update draft first to ensure latest form values are saved
    this.deploymentService.updateDraftRevision(activeRev.id, {
      releaseId: val.releaseId || undefined,
      operationalStatus: val.operationalStatus!,
      startDate: val.startDate || undefined,
      goLiveDate: val.goLiveDate || undefined,
      responsibleUserId: this.authService.currentUser()?.id
    }, activeRev.version).subscribe({
      next: (updatedRev) => {
        // Now submit with the new version
        this.deploymentService.submitRevision(activeRev.id, updatedRev.version).subscribe({
          next: () => {
            this.actionSuccess.set('Hồ sơ đã được gửi duyệt thành công. Thông báo đã gửi tới Lãnh đạo.');
            this.loadDeployment(dep.id);
          },
          error: (submitErr) => {
            this.actionError.set(submitErr.error?.detail || submitErr.error?.title || 'Không thể gửi duyệt hồ sơ.');
          }
        });
      },
      error: (saveErr) => {
        if (saveErr.status === 412) {
          this.concurrencyWarning.set(true);
        } else {
          this.actionError.set(saveErr.error?.detail || 'Lỗi khi cập nhật trước khi gửi duyệt.');
        }
      }
    });
  }

  approveRevision(): void {
    const dep = this.deployment();
    const activeRev = dep?.activeRevision;
    if (!dep || !activeRev) return;

    if (!confirm('Bạn có chắc chắn muốn phê duyệt chính thức hồ sơ triển khai này?')) return;

    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.deploymentService.approveRevision(activeRev.id, activeRev.version).subscribe({
      next: () => {
        this.actionSuccess.set('Đã phê duyệt chính thức hồ sơ triển khai thành công.');
        this.loadDeployment(dep.id);
      },
      error: (err) => {
        if (err.status === 412) {
          this.concurrencyWarning.set(true);
        } else {
          this.actionError.set(err.error?.detail || err.error?.title || 'Không thể phê duyệt hồ sơ.');
        }
      }
    });
  }

  openRejectModal(): void {
    this.rejectReason.set('');
    this.rejectError.set(null);
    this.isRejectModalOpen.set(true);
  }

  closeRejectModal(): void {
    this.isRejectModalOpen.set(false);
    this.rejectReason.set('');
    this.rejectError.set(null);
  }

  confirmReject(): void {
    const reason = this.rejectReason().trim();
    if (!reason) {
      this.rejectError.set('Lý do từ chối hồ sơ là bắt buộc.');
      return;
    }

    const dep = this.deployment();
    const activeRev = dep?.activeRevision;
    if (!dep || !activeRev) return;

    this.deploymentService.rejectRevision(activeRev.id, { reason }, activeRev.version).subscribe({
      next: () => {
        this.closeRejectModal();
        this.actionSuccess.set('Đã từ chối hồ sơ triển khai và gửi thông báo về cán bộ phụ trách.');
        this.loadDeployment(dep.id);
      },
      error: (err) => {
        if (err.status === 412) {
          this.concurrencyWarning.set(true);
          this.closeRejectModal();
        } else {
          this.rejectError.set(err.error?.detail || 'Không thể từ chối hồ sơ.');
        }
      }
    });
  }

  reopenRevision(): void {
    const dep = this.deployment();
    const activeRev = dep?.activeRevision;
    if (!dep || !activeRev) return;

    if (!confirm('Bạn có muốn mở lại hồ sơ bị từ chối thành bản nháp để tiếp tục chỉnh sửa?')) return;

    this.deploymentService.reopenRevision(activeRev.id, activeRev.version).subscribe({
      next: () => {
        this.actionSuccess.set('Hồ sơ đã được chuyển lại về trạng thái Bản nháp (Draft).');
        this.loadDeployment(dep.id);
      },
      error: (err) => {
        this.actionError.set(err.error?.detail || 'Không thể mở lại hồ sơ.');
      }
    });
  }

  createNextRevision(): void {
    const dep = this.deployment();
    if (!dep) return;

    if (!confirm('Tạo revision mới kế tiếp từ bản chính thức hiện tại?')) return;

    this.deploymentService.createNextRevision(dep.id, dep.version).subscribe({
      next: () => {
        this.actionSuccess.set('Đã tạo revision mới thành công.');
        this.loadDeployment(dep.id);
      },
      error: (err) => {
        this.actionError.set(err.error?.detail || 'Không thể tạo revision mới.');
      }
    });
  }

  loadAllocations(deploymentId: string): void {
    this.contractService.getAllocationsByDeployment(deploymentId).subscribe({
      next: (allocs) => this.allocations.set(allocs),
      error: () => {}
    });
  }

  loadAttachments(revisionId?: string): void {
    if (!revisionId) {
      this.attachments.set([]);
      return;
    }
    this.documentService.getAttachments('DeploymentRevision', revisionId).subscribe({
      next: (docs) => this.attachments.set(docs),
      error: () => {}
    });
  }

  onFileSelected(event: Event, revisionId: string): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (file.size > 20 * 1024 * 1024) {
      this.docUploadError.set('Tệp vượt quá kích thước cho phép (tối đa 20 MB).');
      input.value = '';
      return;
    }

    this.isUploadingDoc.set(true);
    this.docUploadError.set(null);

    this.documentService.uploadDocument(file, 'DeploymentRevision', revisionId).subscribe({
      next: () => {
        this.isUploadingDoc.set(false);
        input.value = '';
        this.loadAttachments(revisionId);
      },
      error: (err) => {
        this.isUploadingDoc.set(false);
        this.docUploadError.set(err.error?.detail || 'Tải lên thất bại. Vui lòng kiểm tra định dạng và chính sách an toàn.');
        input.value = '';
      }
    });
  }

  downloadAttachment(doc: DocumentAttachmentDto): void {
    if (doc.scanStatus !== 'Clean') {
      alert('Tệp này chưa được xác nhận Clean hoặc bị từ chối do quét an toàn.');
      return;
    }

    this.documentService.downloadDocument(doc.documentId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.originalName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => alert('Không thể tải tệp. Vui lòng kiểm tra lại quyền truy cập.')
    });
  }

  unlinkAttachment(doc: DocumentAttachmentDto, revisionId: string): void {
    if (!confirm(`Bạn có chắc chắn muốn gỡ tài liệu '${doc.originalName}' khỏi hồ sơ?`)) return;

    this.documentService.unlinkAttachment(doc.attachmentId).subscribe({
      next: () => this.loadAttachments(revisionId),
      error: (err) => alert(err.error?.detail || 'Lỗi gỡ tài liệu')
    });
  }

  isSubmitter(): boolean {
    const currentUserId = this.authService.currentUser()?.id;
    const submitterId = this.deployment()?.activeRevision?.submittedBy;
    return !!currentUserId && !!submitterId && currentUserId === submitterId;
  }
}
