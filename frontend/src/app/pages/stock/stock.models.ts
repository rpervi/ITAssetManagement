export interface StockProduct {
  productId: number;
  categoryId: number;
  categoryName: string;
  productName: string;
  brand: string;
  model: string | null;
  trackingType: 'Individual' | 'Bulk';
}

export interface IndividualStockAssetRequest {
  serialNumber: string | null;
  purchaseDate: string;
  warrantyExpiryDate: string | null;
  purchaseCost: number;
  condition: 'New' | 'Good' | 'Damaged';
}

export interface ReceiveIndividualStockRequest {
  idempotencyKey: string;
  productId: number;
  assets: IndividualStockAssetRequest[];
  supplier: string | null;
  invoiceNumber: string | null;
  remarks: string | null;
}

export interface ReceiveBulkStockRequest {
  idempotencyKey: string;
  productId: number;
  quantity: number;
  unitPrice: number;
  purchaseDate: string;
  warrantyExpiryDate: string | null;
  condition: 'New' | 'Good' | 'Damaged';
  supplier: string | null;
  invoiceNumber: string | null;
  remarks: string | null;
}

export interface StockAssetUnit {
  assetUnitId: number;
  assetCode: string;
  serialNumber: string | null;
  purchaseDate: string;
  warrantyExpiryDate: string | null;
  purchaseCost: number;
  status: string;
  condition: string;
}

export interface AssetBarcodeDetails {
  assetUnitId: number;
  productId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  categoryName: string;
  manufacturer: string;
  model: string | null;
  serialNumber: string | null;
  assignedEmployee: string | null;
  department: string | null;
  currentLocation: string | null;
  status: string;
  purchaseDate: string;
}

export interface BulkStockConversion {
  productId: number;
  assetsCreated: number;
}

export interface PrintableAsset {
  assetUnitId: number;
  assetCode: string;
  assetName: string;
}

export interface StockReceiptItem {
  receiptItemId: number;
  productId: number;
  productName: string;
  brand: string;
  model: string | null;
  quantity: number;
  unitPrice: number;
  purchaseDate: string;
  warrantyExpiryDate: string | null;
  condition: string;
  assets: StockAssetUnit[];
}

export interface StockReceipt {
  receiptId: number;
  receiptNumber: string;
  receivedDate: string;
  supplier: string | null;
  invoiceNumber: string | null;
  remarks: string | null;
  receivedBy: string | null;
  trackingType: string;
  quantity: number;
  totalAmount: number;
  items: StockReceiptItem[];
}

export interface StockReceiptSummary {
  receiptId: number;
  receiptNumber: string;
  receivedDate: string;
  productName: string;
  trackingType: string;
  quantity: number;
  supplier: string | null;
  invoiceNumber: string | null;
  totalAmount: number;
  receivedBy: string | null;
}

export interface PagedStockReceipts {
  items: StockReceiptSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StockAvailability {
  productId: number;
  productName: string;
  categoryName: string;
  trackingType: 'Individual' | 'Bulk';
  availableQuantity: number;
  assignedQuantity: number;
  unavailableQuantity: number;
  damagedQuantity: number;
  minimumStock: number;
}

export interface StockMovement {
  movementId: number;
  productId: number;
  productName: string;
  receiptItemId: number | null;
  assetUnitId: number | null;
  assetCode: string | null;
  movementType: string;
  quantity: number;
  movementDate: string;
  referenceId: number | null;
  remarks: string | null;
}

export interface PagedStockMovements {
  items: StockMovement[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StockMovementQuery {
  page: number;
  pageSize: number;
  productId?: number;
  movementType?: string;
  startDate?: string;
  endDate?: string;
}
