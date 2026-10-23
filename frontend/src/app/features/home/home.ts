import { AfterViewInit, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
  lucideShield,
  lucideHeartHandshake,
  lucideStar,
  lucideLocateFixed,
  lucideNavigation,
} from '@ng-icons/lucide';
import { TripApiService } from '../../core/services/trip-api.service';
import {
  PlanningMode,
  Recommendation,
  TopDestinationCard,
  TopDestinationsResponse,
  TravelerCategory,
} from '../../core/models/trip.models';

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
      lucideShield,
      lucideHeartHandshake,
      lucideStar,
      lucideLocateFixed,
      lucideNavigation,
    }),
  ],
  templateUrl: './home.html',
})
export class HomeComponent implements OnInit, AfterViewInit {
  private readonly api = inject(TripApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  origin = 'Lahore';
  destination = '';
  startDate = '2026-12-10';
  endDate = '2026-12-15';
  adults = 2;
  children = 0;
  childAges: number[] = [];
  budget = 2000;
  priorities = 'Culture,Comfort,Family';

  loading = signal(false);
  locating = signal(false);
  locationNote = signal<string | null>(null);
  error = signal<string | null>(null);
  recommendations = signal<Recommendation[]>([]);
  originLabel = signal('Lahore');
  partySummary = signal('');
  topDest = signal<TopDestinationsResponse | null>(null);
  topLoading = signal(true);
  categories = signal<TravelerCategory[]>([]);
  selectedCategory = signal<TravelerCategory | null>(null);
  selectedSubcategoryCode = signal<string | null>(null);
  planningMode = signal<PlanningMode>('AiBudget');

  readonly regions = [
    {
      title: 'Gilgit-Baltistan',
      places: ['Hunza', 'Skardu-style mountain escapes', 'Scenic valleys'],
    },
    {
      title: 'Hill stations',
      places: ['Murree', 'Islamabad day trips', 'Cool-weather weekends'],
    },
    {
      title: 'International',
      places: ['Dubai', 'Istanbul', 'Bangkok', 'Paris', 'Maldives'],
    },
  ];

  ngOnInit(): void {
    this.detectLocation(false);
    this.loadTopDestinations();
    this.api.getTravelerCategories().subscribe({
      next: (cats) => {
        this.categories.set(cats);
        const initial =
          cats.find((c) => c.code === 'COUPLES') ?? cats[0] ?? null;
        if (initial) {
          this.selectedCategory.set(initial);
          this.selectedSubcategoryCode.set(initial.subcategories[0]?.code ?? null);
        }
      },
    });
  }

  selectCategory(cat: TravelerCategory): void {
    this.selectedCategory.set(cat);
    this.selectedSubcategoryCode.set(cat.subcategories[0]?.code ?? null);
    if (cat.code === 'FAMILIES' && this.children === 0) {
      this.children = 1;
      this.syncChildren();
    }
  }

  startWizard(): void {
    const cat = this.selectedCategory();
    void this.router.navigate(['/plan'], {
      queryParams: {
        origin: this.origin,
        destination: this.destination || undefined,
        startDate: this.startDate,
        endDate: this.endDate,
        budget: this.budget,
        adults: this.adults,
        children: this.children,
        childAges: this.childAges.join(','),
        category: cat?.name,
        categoryCode: cat?.code,
        subcategory: this.selectedSubcategoryCode() ?? undefined,
        priorities: this.priorities,
        mode: this.planningMode(),
      },
    });
  }

  ngAfterViewInit(): void {
    const fragment = this.route.snapshot.fragment;
    if (fragment) {
      setTimeout(() => document.getElementById(fragment)?.scrollIntoView({ behavior: 'smooth' }), 50);
    }
  }

  detectLocation(userInitiated: boolean): void {
    if (!navigator.geolocation) {
      if (userInitiated) this.locationNote.set('Geolocation is not supported in this browser.');
      return;
    }

    this.locating.set(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.api.reverseGeocode(pos.coords.latitude, pos.coords.longitude).subscribe({
          next: (geo) => {
            const city = geo.matchedDestination?.name ?? geo.city;
            this.origin = city;
            this.locationNote.set(`Using your current city: ${city}`);
            this.locating.set(false);
            this.loadTopDestinations(pos.coords.latitude, pos.coords.longitude);
          },
          error: () => {
            this.locating.set(false);
            if (userInitiated) this.locationNote.set('Could not resolve your city from coordinates.');
            this.loadTopDestinations(pos.coords.latitude, pos.coords.longitude);
          },
        });
      },
      () => {
        this.locating.set(false);
        if (userInitiated) {
          this.locationNote.set('Location permission denied — enter your city in From.');
        }
        this.loadTopDestinations();
      },
      { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 },
    );
  }

  loadTopDestinations(lat?: number, lng?: number): void {
    this.topLoading.set(true);
    this.api
      .getTopDestinations({
        origin: this.origin || undefined,
        lat,
        lng,
      })
      .subscribe({
        next: (res) => {
          this.topDest.set(res);
          this.topLoading.set(false);
        },
        error: () => {
          this.topLoading.set(false);
        },
      });
  }

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

  onOriginBlur(): void {
    this.loadTopDestinations();
  }

  discover(): void {
    this.syncChildren();
    this.loading.set(true);
    this.error.set(null);
    const cat = this.selectedCategory();
    const travelerCategory =
      cat?.name ?? (this.children > 0 ? 'Families' : 'Dating & Couples');
    const subcategory =
      this.selectedSubcategoryCode() ??
      (this.children > 0 ? 'YoungKids' : 'FirstTripTogether');
    this.api
      .recommend({
        origin: this.origin,
        destination: this.destination?.trim() || null,
        startDate: this.startDate,
        endDate: this.endDate,
        adults: this.adults,
        childAges: this.childAges,
        budget: this.budget,
        currency: 'USD',
        travelerCategory,
        subcategory,
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
          setTimeout(() => document.getElementById('results')?.scrollIntoView({ behavior: 'smooth' }), 80);
        },
        error: (err) => {
          this.error.set(err?.error?.title ?? err?.message ?? 'Unable to load recommendations');
          this.loading.set(false);
        },
      });
  }

  openFeatured(dest: TopDestinationCard): void {
    this.destination = dest.name;
    void this.router.navigate(['/destinations', dest.id], {
      queryParams: {
        origin: this.origin,
        destination: dest.name,
        startDate: this.startDate,
        endDate: this.endDate,
        budget: this.budget,
        adults: this.adults,
        children: this.children,
        childAges: this.childAges.join(','),
      },
    });
  }

  openDestination(rec: Recommendation): void {
    void this.router.navigate(['/destinations', rec.destinationId], {
      queryParams: {
        origin: this.originLabel(),
        destination: rec.destination,
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
    const cat = this.selectedCategory();
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
        category: cat?.name ?? (this.children > 0 ? 'Families' : 'Dating & Couples'),
        subcategory: this.selectedSubcategoryCode() ?? undefined,
        priorities: this.priorities,
        mode: this.planningMode(),
        budgetMode: this.planningMode() === 'AiBudget' ? 'Budget' : 'Balanced',
      },
    });
  }

  openWizardFor(rec: Recommendation): void {
    const cat = this.selectedCategory();
    void this.router.navigate(['/plan'], {
      queryParams: {
        origin: this.originLabel(),
        destination: rec.destination,
        startDate: this.startDate,
        endDate: this.endDate,
        budget: this.budget,
        adults: this.adults,
        children: this.children,
        childAges: this.childAges.join(','),
        category: cat?.name,
        categoryCode: cat?.code,
        subcategory: this.selectedSubcategoryCode() ?? undefined,
        priorities: this.priorities,
        mode: this.planningMode(),
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
