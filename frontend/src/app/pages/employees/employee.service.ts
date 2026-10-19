import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Employee,
  EmployeeOption,
  EmployeePage,
  EmployeeQuery,
  SaveEmployeeRequest,
} from './employee.models';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/employees';

  getPage(query: EmployeeQuery): Observable<EmployeePage> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.department) params = params.set('department', query.department);
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    return this.http.get<EmployeePage>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }

  getById(employeeId: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.endpoint}/${employeeId}`, {
      withCredentials: true,
    });
  }

  getActive(): Observable<EmployeeOption[]> {
    return this.http.get<EmployeeOption[]>(`${this.endpoint}/active`, {
      withCredentials: true,
    });
  }

  create(request: SaveEmployeeRequest): Observable<Employee> {
    return this.http.post<Employee>(this.endpoint, request, { withCredentials: true });
  }

  update(employeeId: number, request: SaveEmployeeRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.endpoint}/${employeeId}`, request, {
      withCredentials: true,
    });
  }

  setStatus(employeeId: number, isActive: boolean): Observable<Employee> {
    return this.http.patch<Employee>(
      `${this.endpoint}/${employeeId}/status`,
      { isActive },
      { withCredentials: true },
    );
  }
}
