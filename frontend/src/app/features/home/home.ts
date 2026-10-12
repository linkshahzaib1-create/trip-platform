import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarDays,
  lucideCircleAlert,
  lucideHeart,
  lucideLoaderCircle,
  lucideMapPin,
  lucideSearch,
  lucideSparkles,
  lucideUsers,
  lucideWallet,
  lucidePlane,
  lucideClock3,
  lucideArrowRight,
  lucideCompass,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import { Recommendation } from '../../core/models/trip.models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, NgIcon],
  providers: [
    provideIcons({
      lucideCalendarDays,
      lucideCircleAlert,
      lucideHeart,
      lucideLoaderCircle,
      lucideMapPin,
      lucideSearch,
      lucideSparkles,
      lucideUsers,
      lucideWallet,
      lucidePlane,
      lucideClock3,
      lucideArrowRight,
      lucideCompass,
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
  budget = 2000;
  travelerCategory = 'Dating & Couples';
  subcategory = 'FirstTripTogether';
  priorities = 'Romance,Culture,Comfort';

  loading = signal(false);
  error = signal<string | null>(null);
  recommendations = signal<Recommendation[]>([]);
  originLabel = signal('Lahore');

  discover(): void {
    this.loading.set(true);
    this.error.set(null);
    const travelers = Array.from({ length: this.adults }, () => ({ age: 29 }));
    this.api
      .recommend({
        origin: this.origin,
        startDate: this.startDate,
        endDate: this.endDate,
        travelers,
        budget: this.budget,
        currency: 'USD',
        travelerCategory: this.travelerCategory,
        subcategory: this.subcategory,
        priorities: this.priorities.split(',').map((p) => p.trim()).filter(Boolean),
      })
      .subscribe({
        next: (res) => {
          this.originLabel.set(res.origin);
          this.recommendations.set(res.recommendations);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.title ?? err?.message ?? 'Unable to load recommendations');
          this.loading.set(false);
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
        category: this.travelerCategory,
        subcategory: this.subcategory,
        priorities: this.priorities,
      },
    });
  }

  groupedBuckets(): { bucket: string; label: string; items: Recommendation[] }[] {
    const map = new Map<string, Recommendation[]>();
    for (const r of this.recommendations()) {
      const key = r.bucket || 'Other';
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(r);
    }
    return Array.from(map.entries()).map(([bucket, items]) => ({
      bucket,
      label: this.bucketLabel(bucket),
      items,
    }));
  }

  bucketLabel(bucket: string): string {
    const labels: Record<string, string> = {
      Nearby: 'Nearby getaways',
      Domestic: 'Across Pakistan',
      InternationalBudget: 'International · Budget',
      InternationalMid: 'International · Mid-range',
      InternationalPremium: 'International · Premium',
    };
    return labels[bucket] ?? bucket;
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
