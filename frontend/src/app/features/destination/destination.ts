import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowLeft,
  lucideBedDouble,
  lucideCalendarDays,
  lucideCamera,
  lucideCheck,
  lucideCircleAlert,
  lucideClipboardList,
  lucideInfo,
  lucideListChecks,
  lucideMap,
  lucideMapPin,
  lucideRoute,
  lucideStar,
  lucideX,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import { DestinationProfile } from '../../core/models/trip.models';

type DestTab =
  | 'overview'
  | 'itinerary'
  | 'map'
  | 'stays'
  | 'inclusions'
  | 'pricing'
  | 'extra'
  | 'photos'
  | 'reviews';

@Component({
  selector: 'app-destination',
  standalone: true,
  imports: [CommonModule, RouterLink, NgIcon],
  providers: [
    provideIcons({
      lucideArrowLeft,
      lucideBedDouble,
      lucideCalendarDays,
      lucideCamera,
      lucideCheck,
      lucideCircleAlert,
      lucideClipboardList,
      lucideInfo,
      lucideListChecks,
      lucideMap,
      lucideMapPin,
      lucideRoute,
      lucideStar,
      lucideX,
    }),
  ],
  templateUrl: './destination.html',
})
export class DestinationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(TripApiService);

  loading = signal(true);
  error = signal<string | null>(null);
  profile = signal<DestinationProfile | null>(null);
  query = signal<Record<string, string>>({});
  activeTab = signal<DestTab>('inclusions');

  readonly tabs: { id: DestTab; label: string; icon: string }[] = [
    { id: 'overview', label: 'Overview', icon: 'lucideClipboardList' },
    { id: 'itinerary', label: 'Itinerary', icon: 'lucideRoute' },
    { id: 'map', label: 'Map', icon: 'lucideMap' },
    { id: 'stays', label: 'Hotels & Stays', icon: 'lucideBedDouble' },
    { id: 'inclusions', label: 'Inclusions', icon: 'lucideListChecks' },
    { id: 'pricing', label: 'Dates & Pricing', icon: 'lucideCalendarDays' },
    { id: 'extra', label: 'Extra Info', icon: 'lucideInfo' },
    { id: 'photos', label: 'Photos', icon: 'lucideCamera' },
    { id: 'reviews', label: 'Reviews', icon: 'lucideStar' },
  ];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    const q = this.route.snapshot.queryParamMap;
    this.query.set({
      origin: q.get('origin') ?? 'Lahore',
      startDate: q.get('startDate') ?? '',
      endDate: q.get('endDate') ?? '',
      budget: q.get('budget') ?? '2000',
      adults: q.get('adults') ?? '2',
      children: q.get('children') ?? '0',
      childAges: q.get('childAges') ?? '',
    });

    this.api.getDestination(id).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Destination not found');
        this.loading.set(false);
      },
    });
  }

  setTab(tab: DestTab): void {
    this.activeTab.set(tab);
  }

  estimateLink(profile: DestinationProfile) {
    return {
      origin: this.query()['origin'],
      destination: profile.name,
      startDate: this.query()['startDate'],
      endDate: this.query()['endDate'],
      budget: this.query()['budget'],
      adults: this.query()['adults'],
      children: this.query()['children'],
      childAges: this.query()['childAges'],
      category: Number(this.query()['children'] || 0) > 0 ? 'Families' : 'Dating & Couples',
      priorities: Number(this.query()['children'] || 0) > 0 ? 'Family,Culture,Comfort' : 'Romance,Culture,Comfort',
    };
  }
}
