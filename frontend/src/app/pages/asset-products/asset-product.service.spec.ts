import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AssetProductService } from './asset-product.service';

describe('AssetProductService', () => {
  let service: AssetProductService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AssetProductService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads products from the API with credentials', () => {
    service.getAll().subscribe((products) => expect(products).toEqual([]));

    const request = httpTesting.expectOne('/api/assetproducts');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    request.flush([]);
  });

  it('creates a product through the API', () => {
    const payload = {
      categoryId: 2,
      productName: 'Laptop',
      brand: 'Acme',
      model: null,
      trackingType: 'Individual' as const,
      minimumStock: 0,
      isActive: true,
    };
    service.create(payload).subscribe();

    const request = httpTesting.expectOne('/api/assetproducts');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({ productId: 3 });
  });

  it('changes product status through the API', () => {
    service.setStatus(3, false).subscribe();

    const request = httpTesting.expectOne('/api/assetproducts/3/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ isActive: false });
    request.flush({ productId: 3, isActive: false });
  });
});
