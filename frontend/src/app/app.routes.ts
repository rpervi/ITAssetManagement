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
          import('./pages/placeholder-page/placeholder-page').then(
            (page) => page.PlaceholderPage,
          ),
        data: { title: 'Dashboard' },
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
      ...[
        ['return-assets', 'Return Assets'],
        ['history', 'History'],
        ['reports', 'Reports'],
      ].map(([path, title]) => ({
        path,
        loadComponent: () =>
          import('./pages/placeholder-page/placeholder-page').then(
            (page) => page.PlaceholderPage,
          ),
        data: { title },
      })),
    ],
  },
  { path: '**', redirectTo: 'login' },
];
