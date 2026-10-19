export interface AssetCategory {
  categoryId: number;
  categoryName: string;
  description: string | null;
  isActive: boolean;
  createdBy: string | null;
  createdOn: string;
  modifiedBy: string | null;
  modifiedOn: string | null;
}

export interface SaveAssetCategoryRequest {
  categoryName: string;
  description: string | null;
  isActive: boolean;
}
