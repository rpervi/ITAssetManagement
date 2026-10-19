import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EmployeeService } from './employee.service';

describe('EmployeeService', () => {
  let service: EmployeeService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(EmployeeService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads a filtered and paged employee list with cookie credentials', () => {
    service.getPage({
      search: 'E-1',
      department: 'IT',
      isActive: false,
      page: 2,
      pageSize: 25,
      sortBy: 'employeeCode',
      sortDirection: 'desc',
    }).subscribe();

    const request = httpTesting.expectOne((candidate) => candidate.url === '/api/employees');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.params.get('search')).toBe('E-1');
    expect(request.request.params.get('department')).toBe('IT');
    expect(request.request.params.get('isActive')).toBe('false');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ items: [], totalCount: 0, page: 2, pageSize: 25, departments: [] });
  });

  it('loads active employee options without requesting all personal details', () => {
    service.getActive().subscribe((employees) =>
      expect(employees).toEqual([
        { employeeId: 1, employeeCode: 'E-1', employeeName: 'Alex', department: 'IT' },
      ]),
    );

    const request = httpTesting.expectOne('/api/employees/active');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    request.flush([
      { employeeId: 1, employeeCode: 'E-1', employeeName: 'Alex', department: 'IT' },
    ]);
  });

  it('creates, updates, and changes employee status through the API', () => {
    const requestBody = {
      employeeCode: 'E-1',
      employeeName: 'Alex Example',
      email: 'alex@example.com',
      department: 'IT',
      designation: null,
      phone: null,
      location: null,
      joiningDate: null,
      isActive: true,
    };

    service.create(requestBody).subscribe();
    const createRequest = httpTesting.expectOne('/api/employees');
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush({ employeeId: 1 });

    service.update(1, requestBody).subscribe();
    const updateRequest = httpTesting.expectOne('/api/employees/1');
    expect(updateRequest.request.method).toBe('PUT');
    updateRequest.flush({ employeeId: 1 });

    service.setStatus(1, false).subscribe();
    const statusRequest = httpTesting.expectOne('/api/employees/1/status');
    expect(statusRequest.request.method).toBe('PATCH');
    expect(statusRequest.request.body).toEqual({ isActive: false });
    statusRequest.flush({ employeeId: 1, isActive: false });
  });
});
