import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';

@Component({
  imports: [MatCardModule, MatIconModule],
  selector: 'app-placeholder-page',
  styleUrl: './placeholder-page.css',
  templateUrl: './placeholder-page.html',
})
export class PlaceholderPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly title = toSignal(
    this.route.data.pipe(map((data) => data['title'] as string)),
    { initialValue: this.route.snapshot.data['title'] as string },
  );
}
