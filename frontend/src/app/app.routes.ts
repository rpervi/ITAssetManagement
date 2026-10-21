import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    loadComponent: () => import('./pages/login-page/login-page').then((page) => page.LoginPage),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password-page/forgot-password-page').then(
        (page) => page.ForgotPasswordPage,
      ),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password-page/reset-password-page').then(
        (page) => page.ResetPasswordPage,
      ),
  },
  {
    path: '',
    loadComponent: () => import('./layout/app-shell/app-shell').then((layout) => layout.AppShell),
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/dashboard-page').then(
            (page) => page.DashboardPage,
          ),
      },
      {
        path: 'asset-categories',
        loadComponent: () =>
          import('./pages/asset-categories/asset-categories-page').then(
            (page) => page.AssetCategoriesPage,
          ),
      },
      {
        path: 'asset-products',
        loadComponent: () =>
          import('./pages/asset-products/asset-products-page').then(
            (page) => page.AssetProductsPage,
          ),
      },
      {
        path: 'receive-stock',
        loadComponent: () =>
          import('./pages/stock/receive-stock-page').then(
            (page) => page.ReceiveStockPage,
          ),
      },
      {
        path: 'stock-receipt-history',
        loadComponent: () =>
          import('./pages/stock/stock-receipt-history-page').then(
            (page) => page.StockReceiptHistoryPage,
          ),
      },
      {
        path: 'inventory',
        loadComponent: () =>
          import('./pages/stock/inventory-page').then(
            (page) => page.InventoryPage,
          ),
      },
      {
        path: 'generate-barcodes',
        loadComponent: () =>
          import('./pages/barcodes/generate-barcodes-page').then(
            (page) => page.GenerateBarcodesPage,
          ),
      },
      {
          path: 'scan-assets',
          loadComponent: () =>
            import('./pages/scan-assets/scan-assets-page').then(
              (page) => page.ScanAssetsPage,
            ),
      },
      {
        path: 'employees',
        loadComponent: () =>
          import('./pages/employees/employees-page').then(
            (page) => page.EmployeesPage,
          ),
      },
      {
        path: 'assign-assets',
        loadComponent: () =>
          import('./pages/assignments/assignments-page').then(
            (page) => page.AssignmentsPage,
          ),
      },
      {
        path: 'return-assets',
        loadComponent: () =>
          import('./pages/returns/return-assets-page').then(
            (page) => page.ReturnAssetsPage,
          ),
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./pages/history/history-page').then(
            (page) => page.HistoryPage,
          ),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./pages/reports/reports-page').then(
            (page) => page.ReportsPage,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
