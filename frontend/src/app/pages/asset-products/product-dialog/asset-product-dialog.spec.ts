import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AssetProductService } from '../asset-product.service';
import { AssetProductDialog } from './asset-product-dialog';

describe('AssetProductDialog', () => {
  let fixture: ComponentFixture<AssetProductDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssetProductDialog],
      providers: [
        {
          provide: MAT_DIALOG_DATA,
          useValue: { mode: 'create', categories: [] },
        },
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
        {
          provide: AssetProductService,
          useValue: { create: vi.fn(), update: vi.fn() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetProductDialog);
    fixture.detectChanges();
  });

  it('shows required validation before submitting an empty form', () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('button[type="submit"]')!
      .click();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Product name is required.');
    expect(text).toContain('Brand is required.');
    expect(text).toContain('An active category is required.');
  });
});
