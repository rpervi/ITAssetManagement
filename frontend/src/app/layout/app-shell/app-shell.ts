import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../../auth/auth.service';

interface NavigationItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  selector: 'app-shell',
  styleUrl: './app-shell.css',
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe('(max-width: 760px)').pipe(map(({ matches }) => matches)),
    { initialValue: false },
  );
  protected readonly mobileMenuOpen = signal(false);
  protected readonly sidebarCollapsed = signal(false);
  protected readonly searchQuery = signal('');
  protected readonly logoutError = signal('');
  protected readonly user = this.authService.currentUser;

  protected readonly navigationItems: NavigationItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'dashboard' },
    { label: 'Asset Categories', path: '/asset-categories', icon: 'category' },
    { label: 'Asset Products', path: '/asset-products', icon: 'devices_other' },
    { label: 'Receive Stock', path: '/receive-stock', icon: 'move_to_inbox' },
    { label: 'Inventory', path: '/inventory', icon: 'inventory_2' },
    { label: 'Barcode Labels', path: '/generate-barcodes', icon: 'qr_code_2' },
    { label: 'Scan Asset', path: '/scan-assets', icon: 'qr_code_scanner' },
    { label: 'Employees', path: '/employees', icon: 'groups' },
    { label: 'Assign Assets', path: '/assign-assets', icon: 'assignment_ind' },
    { label: 'Return Assets', path: '/return-assets', icon: 'keyboard_return' },
    { label: 'History', path: '/history', icon: 'history' },
    { label: 'Reports', path: '/reports', icon: 'bar_chart' },
  ];

  protected readonly filteredNavigationItems = computed(() => {
    const query = this.searchQuery().trim().toLocaleLowerCase();
    if (!query) return [];
    return this.navigationItems.filter((item) =>
      item.label.toLocaleLowerCase().includes(query),
    );
  });

  protected toggleSidebar(): void {
    if (this.isMobile()) {
      this.toggleMobileMenu();
      return;
    }

    this.sidebarCollapsed.update((collapsed) => !collapsed);
  }

  protected updateSearch(value: string): void {
    this.searchQuery.set(value);
  }

  protected closeSearch(): void {
    this.searchQuery.set('');
  }

  protected toggleMobileMenu(): void {
    this.mobileMenuOpen.update((isOpen) => !isOpen);
  }

  protected closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  protected onDrawerOpenedChange(isOpen: boolean): void {
    if (this.isMobile()) {
      this.mobileMenuOpen.set(isOpen);
    }
  }

  protected logout(): void {
    this.logoutError.set('');
    this.authService.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => this.logoutError.set('Could not sign out. Please try again.'),
    });
  }

  protected initials(username: string | undefined): string {
    return username?.slice(0, 2).toUpperCase() ?? 'IT';
  }
}
