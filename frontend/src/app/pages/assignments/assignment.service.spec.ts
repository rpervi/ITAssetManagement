import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AssignmentService } from './assignment.service';

describe('AssignmentService', () => {
  let service: AssignmentService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AssignmentService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads paged assignment history and sends cookie credentials', () => {
    service.getPage({
      search: 'E-1',
      productId: 4,
      page: 2,
      pageSize: 10,
      sortBy: 'assignedDate',
      sortDirection: 'desc',
    }).subscribe();

    const request = httpTesting.expectOne((candidate) =>
      candidate.url === '/api/assetassignments');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.params.get('search')).toBe('E-1');
    expect(request.request.params.get('productId')).toBe('4');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush({ items: [], totalCount: 0, page: 2, pageSize: 10 });
  });

  it('filters assignments with outstanding returns', () => {
    service.getPage({
      page: 1,
      pageSize: 25,
      returnableOnly: true,
    }).subscribe();

    const request = httpTesting.expectOne((candidate) =>
      candidate.url === '/api/assetassignments');
    expect(request.request.params.get('returnableOnly')).toBe('true');
    request.flush({ items: [], totalCount: 0, page: 1, pageSize: 25 });
  });

  it('loads available products, stock and individual assets', () => {
    service.getActiveProducts().subscribe();
    const products = httpTesting.expectOne('/api/assetassignments/products');
    expect(products.request.withCredentials).toBe(true);
    products.flush([]);

    service.getAvailableUnits(4).subscribe();
    const units = httpTesting.expectOne(
      '/api/assetassignments/products/4/available-units',
    );
    units.flush([]);

    service.getAvailableStock(5).subscribe();
    const stock = httpTesting.expectOne(
      '/api/assetassignments/products/5/available-stock',
    );
    stock.flush({
      productId: 5,
      productName: 'Monitor',
      categoryName: 'Displays',
      brand: 'Example',
      model: null,
      availableQuantity: 3,
      batches: [],
    });
  });

  it('posts the assignment including the idempotency key', () => {
    const body = {
      idempotencyKey: 'e823c802-183a-4f7b-a3e3-aa9a79204c39',
      employeeId: 2,
      productId: 4,
      assetUnitIds: [20],
      quantity: 1,
      assignedDate: '2026-10-19',
      remarks: null,
    };
    service.create(body).subscribe();

    const request = httpTesting.expectOne('/api/assetassignments');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual(body);
    request.flush({ assignmentId: 10 });
  });

  it('posts partial return details to the selected assignment', () => {
    const body = {
      assignmentItemId: 12,
      quantity: 1,
      returnDate: '2026-10-21',
      returnedCondition: 'Good',
      remarks: 'Returned in good condition',
    };
    service.returnAssets(10, body).subscribe();

    const request = httpTesting.expectOne('/api/assetassignments/10/returns');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual(body);
    request.flush({ assignmentId: 10 });
  });
});
