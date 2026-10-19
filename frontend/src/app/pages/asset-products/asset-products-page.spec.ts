import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { AssetCategoryService } from '../asset-categories/asset-category.service';
import { AssetProductService } from './asset-product.service';
import { AssetProductsPage } from './asset-products-page';

describe('AssetProductsPage', () => {
  let fixture: ComponentFixture<AssetProductsPage>;
  let dialogOpen: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    dialogOpen = vi.fn().mockReturnValue({ afterClosed: () => of(undefined) });
    TestBed.configureTestingModule({
      imports: [AssetProductsPage],
      providers: [
        {
          provide: AssetProductService,
          useValue: {
            getAll: () =>
              of([
                {
                  productId: 1,
                  categoryId: 2,
                  categoryName: 'Hardware',
                  productName: 'Laptop',
                  brand: 'Acme',
                  model: 'L1',
                  trackingType: 'Individual',
                  minimumStock: 0,
                  isActive: true,
                  createdBy: null,
                  createdOn: '2026-01-01T00:00:00Z',
                  modifiedBy: null,
                  modifiedOn: null,
                },
              ]),
          },
        },
        {
          provide: AssetCategoryService,
          useValue: { getAll: () => of([]) },
        },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
      ],
    });
    TestBed.overrideProvider(MatDialog, { useValue: { open: dialogOpen } });
    await TestBed.compileComponents();

    fixture = TestBed.createComponent(AssetProductsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('loads and displays products from the product service', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Laptop');
  });

  it('opens the create product dialog', () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('.create-button')!
      .click();

    expect(dialogOpen).toHaveBeenCalledWith(
      expect.any(Function),
      expect.objectContaining({
        data: expect.objectContaining({ mode: 'create' }),
      }),
    );
  });
});
