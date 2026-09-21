import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BackgroundJobDto } from '../models/job.models';
import { PagedResult } from './deployment.service';

@Injectable({
  providedIn: 'root'
})
export class JobAdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/jobs';

  getJobs(
    page = 1,
    pageSize = 20,
    status?: string,
    type?: string
  ): Observable<PagedResult<BackgroundJobDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status) {
      params = params.set('status', status);
    }
    if (type) {
      params = params.set('type', type);
    }

    return this.http.get<PagedResult<BackgroundJobDto>>(this.baseUrl, { params });
  }

  getJobById(id: string): Observable<BackgroundJobDto> {
    return this.http.get<BackgroundJobDto>(`${this.baseUrl}/${id}`);
  }

  retryJob(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/retry`, {});
  }
}
