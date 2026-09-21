import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobAdminService } from '@core/services/job-admin.service';
import { BackgroundJobDto } from '@core/models/job.models';

@Component({
  selector: 'app-job-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss']
})
export class JobListComponent implements OnInit {
  private jobService = inject(JobAdminService);

  jobs = signal<BackgroundJobDto[]>([]);
  isLoading = signal<boolean>(false);
  totalCount = signal<number>(0);
  page = signal<number>(1);
  pageSize = signal<number>(20);

  selectedStatus = signal<string>('');
  selectedType = signal<string>('');

  actionMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.isLoading.set(true);
    this.jobService.getJobs(
      this.page(),
      this.pageSize(),
      this.selectedStatus() || undefined,
      this.selectedType() || undefined
    ).subscribe({
      next: (res) => {
        this.jobs.set(res.items || []);
        this.totalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  retry(jobId: string): void {
    this.jobService.retryJob(jobId).subscribe({
      next: (res) => {
        this.actionMessage.set(res.message || 'Đã yêu cầu chạy lại tác vụ.');
        this.loadJobs();
        setTimeout(() => this.actionMessage.set(null), 4000);
      },
      error: () => {}
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Succeeded': return 'badge-success';
      case 'Running': return 'badge-running';
      case 'Queued': return 'badge-queued';
      case 'Failed': return 'badge-failed';
      default: return 'badge-default';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Succeeded': return 'Thành công';
      case 'Running': return 'Đang chạy';
      case 'Queued': return 'Đang chờ';
      case 'Failed': return 'Thất bại';
      default: return status;
    }
  }
}
