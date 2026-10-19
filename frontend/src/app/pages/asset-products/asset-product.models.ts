export interface AssetProduct {
  productId: number;
  categoryId: number;
  categoryName: string;
  productName: string;
  brand: string;
  model: string | null;
  trackingType: 'Individual' | 'Bulk';
  minimumStock: number;
  isActive: boolean;
  createdBy: string | null;
  createdOn: string;
  modifiedBy: string | null;
  modifiedOn: string | null;
}

export interface SaveAssetProductRequest {
  categoryId: number;
  productName: string;
  brand: string;
  model: string | null;
  trackingType: 'Individual' | 'Bulk';
  minimumStock: number;
  isActive: boolean;
}
