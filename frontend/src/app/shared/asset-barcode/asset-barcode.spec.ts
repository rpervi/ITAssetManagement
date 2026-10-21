import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssetBarcode } from './asset-barcode';

describe('AssetBarcode', () => {
  let fixture: ComponentFixture<AssetBarcode>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssetBarcode],
    }).compileComponents();
  });

  it('renders a Code 128 barcode for the asset code', () => {
    fixture = TestBed.createComponent(AssetBarcode);
    fixture.componentRef.setInput('value', 'AST-000123');
    fixture.detectChanges();

    const svg = fixture.nativeElement.querySelector('svg') as SVGSVGElement;
    expect(svg.querySelector('rect')).not.toBeNull();
    expect(svg.getAttribute('aria-label')).toBe('Code 128 barcode for AST-000123');
  });

  it('updates the barcode when the asset code changes', () => {
    fixture = TestBed.createComponent(AssetBarcode);
    fixture.componentRef.setInput('value', 'AST-000123');
    fixture.detectChanges();
    fixture.componentRef.setInput('value', 'AST-000456');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('svg').getAttribute('aria-label'))
      .toBe('Code 128 barcode for AST-000456');
  });
});
