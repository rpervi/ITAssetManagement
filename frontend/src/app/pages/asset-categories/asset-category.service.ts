import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AssetCategory, SaveAssetCategoryRequest } from './asset-category.models';

@Injectable({ providedIn: 'root' })
export class AssetCategoryService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/assetcategories';

  getAll(): Observable<AssetCategory[]> {
    return this.http.get<AssetCategory[]>(this.endpoint, { withCredentials: true });
  }

  getById(categoryId: number): Observable<AssetCategory> {
    return this.http.get<AssetCategory>(`${this.endpoint}/${categoryId}`, {
      withCredentials: true,
    });
  }

  create(request: SaveAssetCategoryRequest): Observable<AssetCategory> {
    return this.http.post<AssetCategory>(this.endpoint, request, { withCredentials: true });
  }

  update(categoryId: number, request: SaveAssetCategoryRequest): Observable<AssetCategory> {
    return this.http.put<AssetCategory>(`${this.endpoint}/${categoryId}`, request, {
      withCredentials: true,
    });
  }

  setStatus(categoryId: number, isActive: boolean): Observable<AssetCategory> {
    return this.http.patch<AssetCategory>(
      `${this.endpoint}/${categoryId}/status`,
      { isActive },
      { withCredentials: true },
    );
  }
}
