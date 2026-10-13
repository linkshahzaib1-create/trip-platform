import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideBaby,
  lucideCalendarDays,
  lucideCheck,
  lucideCircleAlert,
  lucideMapPin,
  lucideSearch,
  lucideUsers,
  lucideWallet,
  lucidePlus,
  lucideMinus,
  lucidePlane,
  lucideClock3,
  lucideArrowRight,
  lucideSparkles,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import { Recommendation } from '../../core/models/trip.models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, NgIcon],
  providers: [
    provideIcons({
      lucideBaby,
      lucideCalendarDays,
      lucideCheck,
      lucideCircleAlert,
      lucideMapPin,
      lucideSearch,
      lucideUsers,
      lucideWallet,
      lucidePlus,
      lucideMinus,
      lucidePlane,
      lucideClock3,
      lucideArrowRight,
      lucideSparkles,
    }),
  ],
  templateUrl: './home.html',
})
export class HomeComponent {
  private readonly api = inject(TripApiService);
  private readonly router = inject(Router);

  origin = 'Lahore';
  startDate = '2026-12-10';
  endDate = '2026-12-15';
  adults = 2;
  children = 0;
  childAges: number[] = [];
  budget = 2000;
  priorities = 'Culture,Comfort,Family';

  loading = signal(false);
  error = signal<string | null>(null);
  recommendations = signal<Recommendation[]>([]);
  originLabel = signal('Lahore');
  partySummary = signal('');

  syncChildren(): void {
    const count = Math.max(0, Number(this.children) || 0);
    this.children = count;
    while (this.childAges.length < count) this.childAges.push(8);
    while (this.childAges.length > count) this.childAges.pop();
  }

  inc(field: 'adults' | 'children'): void {
    if (field === 'adults') this.adults = Math.min(12, this.adults + 1);
    else {
      this.children = Math.min(8, this.children + 1);
      this.syncChildren();
    }
  }

  dec(field: 'adults' | 'children'): void {
    if (field === 'adults') this.adults = Math.max(1, this.adults - 1);
    else {
      this.children = Math.max(0, this.children - 1);
      this.syncChildren();
    }
  }

  discover(): void {
    this.syncChildren();
    this.loading.set(true);
    this.error.set(null);
    const travelerCategory = this.children > 0 ? 'Families' : 'Dating & Couples';
    this.api
      .recommend({
        origin: this.origin,
        startDate: this.startDate,
        endDate: this.endDate,
        adults: this.adults,
        childAges: this.childAges,
        budget: this.budget,
        currency: 'USD',
        travelerCategory,
        subcategory: this.children > 0 ? 'YoungKids' : 'FirstTripTogether',
        priorities: this.priorities.split(',').map((p) => p.trim()).filter(Boolean),
      })
      .subscribe({
        next: (res) => {
          this.originLabel.set(res.origin);
          this.recommendations.set(res.recommendations);
          this.partySummary.set(
            `${res.adults} adult${res.adults === 1 ? '' : 's'}` +
              (res.children ? `, ${res.children} child${res.children === 1 ? '' : 'ren'}` : ''),
          );
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.title ?? err?.message ?? 'Unable to load recommendations');
          this.loading.set(false);
        },
      });
  }

  openDestination(rec: Recommendation): void {
    void this.router.navigate(['/destinations', rec.destinationId], {
      queryParams: {
        origin: this.originLabel(),
        startDate: this.startDate,
        endDate: this.endDate,
        budget: this.budget,
        adults: this.adults,
        children: this.children,
        childAges: this.childAges.join(','),
      },
    });
  }

  openEstimate(rec: Recommendation): void {
    void this.router.navigate(['/results'], {
      queryParams: {
        origin: this.originLabel(),
        destination: rec.destination,
        startDate: this.startDate,
        endDate: this.endDate,
        budget: this.budget,
        adults: this.adults,
        children: this.children,
        childAges: this.childAges.join(','),
        category: this.children > 0 ? 'Families' : 'Dating & Couples',
        priorities: this.priorities,
      },
    });
  }

  fitClass(fit: string): string {
    switch (fit) {
      case 'Excellent':
        return 'bg-emerald-100 text-emerald-800';
      case 'Good':
        return 'bg-sky-100 text-sky-800';
      case 'Partial':
        return 'bg-amber-100 text-amber-800';
      case 'OverBudget':
        return 'bg-rose-100 text-rose-800';
      default:
        return 'bg-slate-100 text-slate-700';
    }
  }
}
