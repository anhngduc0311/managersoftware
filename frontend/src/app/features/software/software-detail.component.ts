import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SoftwareService, SoftwareDto, SoftwareReleaseDto } from '@core/services/software.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-software-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './software-detail.component.html',
  styleUrls: ['./software-detail.component.scss']
})
export class SoftwareDetailComponent implements OnInit {
  @Input({ required: true }) software!: SoftwareDto;
  @Output() close = new EventEmitter<void>();
  @Output() edit = new EventEmitter<SoftwareDto>();
  @Output() delete = new EventEmitter<SoftwareDto>();
  @Output() reload = new EventEmitter<void>();

  private softwareService = inject(SoftwareService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

  releases = signal<SoftwareReleaseDto[]>([]);
  isAddReleaseModalOpen = signal<boolean>(false);
  modalError = signal<string | null>(null);

  releaseForm = this.fb.group({
    versionName: ['', [Validators.required]],
    releaseDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
    supportEndDate: ['']
  });

  ngOnInit(): void {
    this.loadReleases();
  }

  loadReleases() {
    this.softwareService.getReleases(this.software.id).subscribe({
      next: (data) => this.releases.set(data)
    });
  }

  openAddReleaseModal() {
    this.releaseForm.reset({
      versionName: '',
      releaseDate: new Date().toISOString().substring(0, 10),
      supportEndDate: ''
    });
    this.modalError.set(null);
    this.isAddReleaseModalOpen.set(true);
  }

  closeAddReleaseModal() {
    this.isAddReleaseModalOpen.set(false);
  }

  submitAddRelease() {
    if (this.releaseForm.invalid) {
      this.releaseForm.markAllAsTouched();
      return;
    }

    const val = this.releaseForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.createRelease(this.software.id, {
      versionName: val.versionName!,
      releaseDate: new Date(val.releaseDate!).toISOString(),
      supportEndDate: val.supportEndDate ? new Date(val.supportEndDate).toISOString() : null
    }).subscribe({
      next: () => {
        this.closeAddReleaseModal();
        this.loadReleases();
        this.reload.emit();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể thêm phiên bản phát hành.');
      }
    });
  }

  deleteRelease(rel: SoftwareReleaseDto) {
    if (!confirm(`Bạn có chắc chắn muốn xoá phiên bản "${rel.versionName}"?`)) {
      return;
    }
    this.softwareService.deleteRelease(rel.id).subscribe({
      next: () => {
        this.loadReleases();
        this.reload.emit();
      },
      error: (err) => {
        alert(err.problem?.detail || err.error?.detail || 'Không thể xoá phiên bản này.');
      }
    });
  }
}

