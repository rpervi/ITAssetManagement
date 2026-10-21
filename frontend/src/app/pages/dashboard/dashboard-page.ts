import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AssignmentService } from '../assignments/assignment.service';
import { AssetAssignment } from '../assignments/assignment.models';
import { StockAvailability } from '../stock/stock.models';
import { StockService } from '../stock/stock.service';

@Component({
  imports: [DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule, RouterLink],
  selector: 'app-dashboard-page',
  styleUrl: './dashboard-page.css',
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private readonly assignmentService = inject(AssignmentService);
  private readonly stockService = inject(StockService);

  protected readonly inventory = signal<StockAvailability[]>([]);
  protected readonly recentAssignments = signal<AssetAssignment[]>([]);
  protected readonly activeAssignmentCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly availableQuantity = computed(() =>
    this.inventory().reduce((total, product) => total + product.availableQuantity, 0),
  );
  protected readonly assignedQuantity = computed(() =>
    this.inventory().reduce((total, product) => total + product.assignedQuantity, 0),
  );
  protected readonly lowStockProducts = computed(() =>
    this.inventory().filter(
      (product) => product.minimumStock > 0
        && product.availableQuantity <= product.minimumStock,
    ),
  );
  protected readonly damagedQuantity = computed(() =>
    this.inventory().reduce((total, product) => total + product.damagedQuantity, 0),
  );

  constructor() {
    this.loadData();
  }

  protected loadData(): void {
    this.loading.set(true);
    this.loadError.set('');
    forkJoin({
      inventory: this.stockService.getAvailability(),
      recentAssignments: this.assignmentService.getPage({
        page: 1,
        pageSize: 5,
        sortBy: 'assignedDate',
        sortDirection: 'desc',
      }),
      activeAssignments: this.assignmentService.getPage({
        page: 1,
        pageSize: 1,
        returnableOnly: true,
      }),
    }).subscribe({
      next: ({ inventory, recentAssignments, activeAssignments }) => {
        this.inventory.set(inventory);
        this.recentAssignments.set(recentAssignments.items);
        this.activeAssignmentCount.set(activeAssignments.totalCount);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loadError.set(
          error.error?.detail ?? 'Could not load dashboard information. Please retry.',
        );
        this.loading.set(false);
      },
    });
  }
}
