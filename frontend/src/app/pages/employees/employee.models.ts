export interface Employee {
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  email: string;
  department: string;
  designation: string | null;
  phone: string | null;
  location: string | null;
  joiningDate: string | null;
  isActive: boolean;
  createdBy: string | null;
  createdOn: string;
  modifiedBy: string | null;
  modifiedOn: string | null;
}

export interface EmployeeOption {
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  department: string;
}

export interface SaveEmployeeRequest {
  employeeCode: string;
  employeeName: string;
  email: string;
  department: string;
  designation: string | null;
  phone: string | null;
  location: string | null;
  joiningDate: string | null;
  isActive: boolean;
}

export interface EmployeePage {
  items: Employee[];
  totalCount: number;
  page: number;
  pageSize: number;
  departments: string[];
}

export interface EmployeeQuery {
  search?: string;
  department?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}
