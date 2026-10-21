import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AssignmentPage,
  AssignmentProduct,
  AssignmentQuery,
  AssetAssignment,
  AvailableAssignmentStock,
  AvailableAssignmentUnit,
  CreateAssignmentRequest,
  ReturnAssignmentRequest,
} from './assignment.models';

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/assetassignments';

  getPage(query: AssignmentQuery): Observable<AssignmentPage> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.employeeId) params = params.set('employeeId', query.employeeId);
    if (query.productId) params = params.set('productId', query.productId);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.returnableOnly) params = params.set('returnableOnly', true);
    if (query.startDate) params = params.set('startDate', query.startDate);
    if (query.endDate) params = params.set('endDate', query.endDate);
    return this.http.get<AssignmentPage>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }

  getById(assignmentId: number): Observable<AssetAssignment> {
    return this.http.get<AssetAssignment>(`${this.endpoint}/${assignmentId}`, {
      withCredentials: true,
    });
  }

  getActiveProducts(): Observable<AssignmentProduct[]> {
    return this.http.get<AssignmentProduct[]>(`${this.endpoint}/products`, {
      withCredentials: true,
    });
  }

  getAvailableUnits(productId: number): Observable<AvailableAssignmentUnit[]> {
    return this.http.get<AvailableAssignmentUnit[]>(
      `${this.endpoint}/products/${productId}/available-units`,
      { withCredentials: true },
    );
  }

  getAvailableStock(productId: number): Observable<AvailableAssignmentStock> {
    return this.http.get<AvailableAssignmentStock>(
      `${this.endpoint}/products/${productId}/available-stock`,
      { withCredentials: true },
    );
  }

  create(request: CreateAssignmentRequest): Observable<AssetAssignment> {
    return this.http.post<AssetAssignment>(this.endpoint, request, {
      withCredentials: true,
    });
  }

  returnAssets(
    assignmentId: number,
    request: ReturnAssignmentRequest,
  ): Observable<AssetAssignment> {
    return this.http.post<AssetAssignment>(
      `${this.endpoint}/${assignmentId}/returns`,
      request,
      { withCredentials: true },
    );
  }
}
