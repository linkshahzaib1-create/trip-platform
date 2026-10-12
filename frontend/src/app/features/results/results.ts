import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowLeft,
  lucideCircleAlert,
  lucideClipboardList,
  lucideInfo,
  lucideMapPinned,
  lucidePiggyBank,
  lucideRoute,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import { EstimateResponse } from '../../core/models/trip.models';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, RouterLink, NgIcon],
  providers: [
    provideIcons({
      lucideArrowLeft,
      lucideCircleAlert,
      lucideClipboardList,
      lucideInfo,
      lucideMapPinned,
      lucidePiggyBank,
      lucideRoute,
    }),
  ],
  templateUrl: './results.html',
})
export class ResultsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(TripApiService);

  loading = signal(true);
  error = signal<string | null>(null);
  estimate = signal<EstimateResponse | null>(null);

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    const adults = Number(q.get('adults') ?? 2);
    const travelers = Array.from({ length: adults }, () => ({ age: 29 }));
    this.api
      .estimate({
        origin: q.get('origin') ?? 'Lahore',
        destination: q.get('destination') ?? 'Dubai',
        startDate: q.get('startDate') ?? '2026-12-10',
        endDate: q.get('endDate') ?? '2026-12-15',
        travelers,
        budget: Number(q.get('budget') ?? 2000),
        currency: 'USD',
        travelerCategory: q.get('category') ?? 'Dating & Couples',
        subcategory: q.get('subcategory') ?? 'FirstTripTogether',
        priorities: (q.get('priorities') ?? 'Romance,Culture').split(',').map((p) => p.trim()),
        budgetMode: 'Balanced',
      })
      .subscribe({
        next: (res) => {
          this.estimate.set(res);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.message ?? 'Estimate failed');
          this.loading.set(false);
        },
      });
  }

  days(estimate: EstimateResponse): number[] {
    return Array.from(new Set(estimate.itinerary.map((i) => i.dayNumber))).sort((a, b) => a - b);
  }

  dayItems(estimate: EstimateResponse, day: number) {
    return estimate.itinerary.filter((i) => i.dayNumber === day);
  }

  fitBadge(fit: string): string {
    switch (fit) {
      case 'Excellent':
        return 'badge-success';
      case 'Good':
        return 'badge-info';
      case 'Partial':
        return 'badge-warning';
      case 'OverBudget':
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }
}
