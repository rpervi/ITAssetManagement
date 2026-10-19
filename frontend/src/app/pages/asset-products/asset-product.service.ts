import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AssetProduct, SaveAssetProductRequest } from './asset-product.models';

@Injectable({ providedIn: 'root' })
export class AssetProductService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/assetproducts';

  getAll(): Observable<AssetProduct[]> {
    return this.http.get<AssetProduct[]>(this.endpoint, { withCredentials: true });
  }

  getById(productId: number): Observable<AssetProduct> {
    return this.http.get<AssetProduct>(`${this.endpoint}/${productId}`, {
      withCredentials: true,
    });
  }

  create(request: SaveAssetProductRequest): Observable<AssetProduct> {
    return this.http.post<AssetProduct>(this.endpoint, request, { withCredentials: true });
  }

  update(productId: number, request: SaveAssetProductRequest): Observable<AssetProduct> {
    return this.http.put<AssetProduct>(`${this.endpoint}/${productId}`, request, {
      withCredentials: true,
    });
  }

  setStatus(productId: number, isActive: boolean): Observable<AssetProduct> {
    return this.http.patch<AssetProduct>(
      `${this.endpoint}/${productId}/status`,
      { isActive },
      { withCredentials: true },
    );
  }
}
