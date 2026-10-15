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
  lucideUtensils,
  lucideShoppingBag,
  lucidePlane,
  lucideCar,
  lucideRefreshCw,
  lucideTicket,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import {
  DestinationActivity,
  DestinationDiscovery,
  DestinationProfile,
  PlaceCard,
} from '../../core/models/trip.models';

type DestTab =
  | 'overview'
  | 'itinerary'
  | 'discovery'
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
      lucideUtensils,
      lucideShoppingBag,
      lucidePlane,
      lucideCar,
      lucideRefreshCw,
      lucideTicket,
    }),
  ],
  templateUrl: './destination.html',
})
export class DestinationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(TripApiService);

  loading = signal(true);
  discoveryLoading = signal(false);
  error = signal<string | null>(null);
  profile = signal<DestinationProfile | null>(null);
  discovery = signal<DestinationDiscovery | null>(null);
  query = signal<Record<string, string>>({});
  activeTab = signal<DestTab>('discovery');
  selectedActivity = signal<DestinationActivity | null>(null);
  selectedPlace = signal<PlaceCard | null>(null);

  readonly tabs: { id: DestTab; label: string; icon: string }[] = [
    { id: 'overview', label: 'Overview', icon: 'lucideClipboardList' },
    { id: 'itinerary', label: 'Itinerary', icon: 'lucideRoute' },
    { id: 'discovery', label: 'Local discovery', icon: 'lucideMapPin' },
    { id: 'stays', label: 'Hotels & Flights', icon: 'lucideBedDouble' },
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
      destination: q.get('destination') ?? '',
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
        this.loadDiscovery(id, false);
      },
      error: () => {
        this.error.set('Destination not found');
        this.loading.set(false);
      },
    });
  }

  loadDiscovery(id: number, refresh: boolean): void {
    this.discoveryLoading.set(true);
    this.api.getDiscovery(id, refresh).subscribe({
      next: (d) => {
        this.discovery.set(d);
        this.discoveryLoading.set(false);
      },
      error: () => {
        this.discoveryLoading.set(false);
      },
    });
  }

  setTab(tab: DestTab): void {
    this.activeTab.set(tab);
  }

  openActivity(activity: DestinationActivity): void {
    this.selectedPlace.set(null);
    this.selectedActivity.set(activity);
  }

  openPlace(place: PlaceCard): void {
    this.selectedActivity.set(null);
    this.selectedPlace.set(place);
  }

  closeCostPopup(): void {
    this.selectedActivity.set(null);
    this.selectedPlace.set(null);
  }

  /** Cost shown in popup — updates whenever selection changes. */
  popupCostLabel(): string {
    const a = this.selectedActivity();
    if (a) {
      return `$${a.costMin} – $${a.costMax} ${a.currency} (typical $${a.costTypical})`;
    }
    const p = this.selectedPlace();
    if (p && p.priceTypical != null) {
      const min = p.priceMin ?? p.priceTypical;
      const max = p.priceMax ?? p.priceTypical;
      return `$${min} – $${max} ${p.currency} (typical $${p.priceTypical})`;
    }
    return 'Price on request';
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
