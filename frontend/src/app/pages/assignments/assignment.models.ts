export interface AssignmentProduct {
  productId: number;
  productName: string;
  categoryId: number;
  categoryName: string;
  brand: string;
  model: string | null;
  trackingType: 'Individual' | 'Bulk';
}

export interface AvailableAssignmentUnit {
  assetUnitId: number;
  assetCode: string;
  serialNumber: string | null;
  warrantyExpiryDate: string | null;
  condition: string;
}

export interface AvailableAssignmentBatch {
  receiptItemId: number;
  receiptNumber: string;
  receivedDate: string;
  purchaseDate: string;
  warrantyExpiryDate: string | null;
  unitPrice: number;
  condition: string;
  availableQuantity: number;
}

export interface AssignmentBatchAllocation {
  receiptItemId: number;
  receiptNumber: string;
  receivedDate: string;
  purchaseDate: string;
  warrantyExpiryDate: string | null;
  unitPrice: number;
  condition: string;
  quantity: number;
}

export interface AvailableAssignmentStock {
  productId: number;
  productName: string;
  categoryName: string;
  brand: string;
  model: string | null;
  availableQuantity: number;
  batches: AvailableAssignmentBatch[];
}

export interface AssignmentItem {
  assignmentItemId: number;
  productId: number;
  productName: string;
  categoryName: string;
  brand: string;
  model: string | null;
  trackingType: 'Individual' | 'Bulk' | string;
  quantity: number;
  assetCode: string | null;
  serialNumber: string | null;
  warrantyExpiryDate: string | null;
  batchAllocations: AssignmentBatchAllocation[];
  returnedQuantity: number;
  outstandingQuantity: number;
}

export interface AssetAssignment {
  assignmentId: number;
  assignmentNumber: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  department: string;
  assignedDate: string;
  assignedBy: string;
  remarks: string | null;
  issuedQuantity: number;
  status: string;
  items: AssignmentItem[];
}

export interface AssignmentPage {
  items: AssetAssignment[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AssignmentQuery {
  search?: string;
  employeeId?: number;
  productId?: number;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreateAssignmentRequest {
  idempotencyKey: string;
  employeeId: number;
  productId: number;
  assetUnitIds: number[] | null;
  quantity: number;
  assignedDate: string;
  remarks: string | null;
}
