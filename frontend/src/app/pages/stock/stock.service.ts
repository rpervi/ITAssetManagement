import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  PagedStockMovements,
  PagedStockReceipts,
  ReceiveBulkStockRequest,
  ReceiveIndividualStockRequest,
  StockAssetUnit,
  StockAvailability,
  StockProduct,
  StockReceipt,
} from './stock.models';

export interface StockReceiptQuery {
  search?: string;
  productId?: number;
  startDate?: string;
  endDate?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: string;
}

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/stock';

  getProducts(): Observable<StockProduct[]> {
    return this.http.get<StockProduct[]>(`${this.endpoint}/products`, {
      withCredentials: true,
    });
  }

  receiveIndividual(request: ReceiveIndividualStockRequest): Observable<StockReceipt> {
    return this.http.post<StockReceipt>(
      `${this.endpoint}/receive/individual`,
      request,
      { withCredentials: true },
    );
  }

  receiveBulk(request: ReceiveBulkStockRequest): Observable<StockReceipt> {
    return this.http.post<StockReceipt>(`${this.endpoint}/receive/bulk`, request, {
      withCredentials: true,
    });
  }

  getReceipts(query: StockReceiptQuery): Observable<PagedStockReceipts> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.productId) params = params.set('productId', query.productId);
    if (query.startDate) params = params.set('startDate', query.startDate);
    if (query.endDate) params = params.set('endDate', query.endDate);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    return this.http.get<PagedStockReceipts>(`${this.endpoint}/receipts`, {
      params,
      withCredentials: true,
    });
  }

  getReceipt(receiptId: number): Observable<StockReceipt> {
    return this.http.get<StockReceipt>(`${this.endpoint}/receipts/${receiptId}`, {
      withCredentials: true,
    });
  }

  getAvailability(): Observable<StockAvailability[]> {
    return this.http.get<StockAvailability[]>(`${this.endpoint}/availability`, {
      withCredentials: true,
    });
  }

  getAvailabilityForProduct(productId: number): Observable<StockAvailability> {
    return this.http.get<StockAvailability>(
      `${this.endpoint}/availability/${productId}`,
      { withCredentials: true },
    );
  }

  getUnits(productId: number): Observable<StockAssetUnit[]> {
    return this.http.get<StockAssetUnit[]>(
      `${this.endpoint}/availability/${productId}/units`,
      { withCredentials: true },
    );
  }

  getMovements(page = 1, pageSize = 50, productId?: number): Observable<PagedStockMovements> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (productId) params = params.set('productId', productId);
    return this.http.get<PagedStockMovements>(`${this.endpoint}/movements`, {
      params,
      withCredentials: true,
    });
  }
}
