import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowLeft,
  lucideArrowRight,
  lucideBedDouble,
  lucideCheck,
  lucideCircleAlert,
  lucideHome,
  lucidePlane,
  lucideSparkles,
  lucideTicket,
  lucideUsers,
  lucideWallet,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import {
  DestinationActivity,
  DestinationDiscovery,
  FlightCard,
  PlaceCard,
  PlanningMode,
  TravelerCategory,
} from '../../core/models/trip.models';

type Step = 1 | 2 | 3 | 4 | 5 | 6 | 7;

@Component({
  selector: 'app-plan',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NgIcon],
  providers: [
    provideIcons({
      lucideArrowLeft,
      lucideArrowRight,
      lucideBedDouble,
      lucideCheck,
      lucideCircleAlert,
      lucideHome,
      lucidePlane,
      lucideSparkles,
      lucideTicket,
      lucideUsers,
      lucideWallet,
    }),
  ],
  templateUrl: './plan.html',
})
export class PlanComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(TripApiService);

  step = signal<Step>(1);
  loading = signal(false);
  error = signal<string | null>(null);

  origin = 'Lahore';
  destination = 'Dubai';
  startDate = '2026-12-10';
  endDate = '2026-12-20';
  adults = 2;
  children = 0;
  childAges: number[] = [];
  budget = 2500;
  priorities = 'Culture,Comfort';

  categories = signal<TravelerCategory[]>([]);
  selectedCategory = signal<TravelerCategory | null>(null);
  selectedSubcategoryCode = signal<string | null>(null);
  planningMode = signal<PlanningMode>('AiBudget');

  discovery = signal<DestinationDiscovery | null>(null);
  destinationId = signal<number | null>(null);

  selectedFlightId = signal<number | null>(null);
  stayType = signal<'Hotel' | 'Apartment' | 'Home'>('Hotel');
  selectedListingId = signal<number | null>(null);
  selectedActivityIds = signal<number[]>([]);

  stepLabel = computed(() => {
    const labels: Record<Step, string> = {
      1: 'Trip basics',
      2: 'Who is traveling',
      3: 'Planning mode',
      4: 'Flights',
      5: 'Stays',
      6: 'Activities',
      7: 'Summary',
    };
    return labels[this.step()];
  });

  stayOptions = computed(() => {
    const d = this.discovery();
    if (!d) return [] as PlaceCard[];
    switch (this.stayType()) {
      case 'Apartment':
        return d.apartments?.length ? d.apartments : d.hotels;
      case 'Home':
        return d.homes?.length ? d.homes : d.hotels;
      default:
        return d.hotels;
    }
  });

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.origin = q.get('origin') ?? this.origin;
    this.destination = q.get('destination') ?? this.destination;
    this.startDate = q.get('startDate') ?? this.startDate;
    this.endDate = q.get('endDate') ?? this.endDate;
    this.budget = Number(q.get('budget') ?? this.budget);
    this.adults = Number(q.get('adults') ?? this.adults);
    this.children = Number(q.get('children') ?? 0);
    this.syncChildren();
    const ages = (q.get('childAges') ?? '')
      .split(',')
      .map((x) => Number(x.trim()))
      .filter((n) => !Number.isNaN(n));
    if (ages.length) this.childAges = ages;
    this.priorities = q.get('priorities') ?? this.priorities;
    const mode = q.get('mode');
    if (mode === 'Custom' || mode === 'AiBudget') this.planningMode.set(mode);

    this.api.getTravelerCategories().subscribe({
      next: (cats) => {
        this.categories.set(cats);
        const code = q.get('categoryCode');
        const match =
          cats.find((c) => c.code === code) ??
          cats.find((c) => c.name === q.get('category')) ??
          (this.children > 0
            ? cats.find((c) => c.code === 'FAMILIES')
            : cats.find((c) => c.code === 'COUPLES')) ??
          cats[0];
        if (match) {
          this.selectedCategory.set(match);
          const sub = q.get('subcategory') ?? match.subcategories[0]?.code ?? null;
          this.selectedSubcategoryCode.set(sub);
        }
      },
      error: () => this.error.set('Could not load traveler categories'),
    });
  }

  syncChildren(): void {
    const count = Math.max(0, this.children);
    while (this.childAges.length < count) this.childAges.push(8);
    while (this.childAges.length > count) this.childAges.pop();
  }

  selectCategory(cat: TravelerCategory): void {
    this.selectedCategory.set(cat);
    this.selectedSubcategoryCode.set(cat.subcategories[0]?.code ?? null);
  }

  next(): void {
    const s = this.step();
    if (s === 3) {
      this.loadDiscoveryThen(() => this.step.set(4));
      return;
    }
    if (s < 7) this.step.set((s + 1) as Step);
  }

  back(): void {
    const s = this.step();
    if (s > 1) this.step.set((s - 1) as Step);
  }

  private loadDiscoveryThen(done: () => void): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.searchDestinations(this.destination).subscribe({
      next: (res) => {
        const match = res.matches[0];
        if (!match) {
          this.loading.set(false);
          this.error.set(`Destination "${this.destination}" not found. Try Dubai, Murree, Paris…`);
          return;
        }
        this.destinationId.set(match.id);
        this.api.getDiscovery(match.id, false).subscribe({
          next: (d) => {
            this.discovery.set(d);
            if (!this.selectedFlightId() && d.flights[0]) this.selectedFlightId.set(d.flights[0].id);
            const stays = this.stayOptions();
            if (!this.selectedListingId() && stays[0]) this.selectedListingId.set(stays[0].id);
            if (this.planningMode() === 'AiBudget' && this.selectedActivityIds().length === 0) {
              // Pre-select cheaper activities for budget mode
              const picks = [...d.activities].sort((a, b) => a.costTypical - b.costTypical).slice(0, 3).map((a) => a.id);
              this.selectedActivityIds.set(picks);
            }
            this.loading.set(false);
            done();
          },
          error: () => {
            this.loading.set(false);
            this.error.set('Could not load flights & stays for this destination');
          },
        });
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Destination search failed');
      },
    });
  }

  pickFlight(f: FlightCard): void {
    this.selectedFlightId.set(f.id);
  }

  pickStay(p: PlaceCard): void {
    this.selectedListingId.set(p.id);
  }

  onStayTypeChange(type: 'Hotel' | 'Apartment' | 'Home'): void {
    this.stayType.set(type);
    const first = this.stayOptions()[0];
    this.selectedListingId.set(first?.id ?? null);
  }

  toggleActivity(a: DestinationActivity): void {
    const cur = this.selectedActivityIds();
    if (cur.includes(a.id)) {
      this.selectedActivityIds.set(cur.filter((id) => id !== a.id));
    } else {
      this.selectedActivityIds.set([...cur, a.id]);
    }
  }

  isActivitySelected(id: number): boolean {
    return this.selectedActivityIds().includes(id);
  }

  finish(): void {
    this.syncChildren();
    const cat = this.selectedCategory();
    void this.router.navigate(['/results'], {
      queryParams: {
        origin: this.origin,
        destination: this.destination,
        startDate: this.startDate,
        endDate: this.endDate,
        budget: this.budget,
        adults: this.adults,
        children: this.children,
        childAges: this.childAges.join(','),
        category: cat?.name ?? 'Dating & Couples',
        subcategory: this.selectedSubcategoryCode() ?? '',
        priorities: this.priorities,
        mode: this.planningMode(),
        stayType: this.stayType(),
        flightQuoteId: this.selectedFlightId() ?? '',
        listingId: this.selectedListingId() ?? '',
        activityIds: this.selectedActivityIds().join(','),
        budgetMode: this.planningMode() === 'AiBudget' ? 'Budget' : 'Balanced',
      },
    });
  }

  liveFlightUrl(): string {
    return `https://www.google.com/travel/flights?q=Flights%20to%20${encodeURIComponent(this.destination)}%20from%20${encodeURIComponent(this.origin)}`;
  }

  liveStayUrl(): string {
    return `https://www.booking.com/searchresults.html?ss=${encodeURIComponent(this.destination)}`;
  }
}
