export interface CostRange {
  min: number;
  typical: number;
  max: number;
  currency: string;
}

export interface DestinationMatch {
  id: number;
  name: string;
  country: string;
  type: string;
  slug: string;
}

export interface Recommendation {
  destination: string;
  slug: string;
  destinationId: number;
  country: string;
  description: string;
  estimatedCost: CostRange;
  travelTime: string;
  travelModes: string;
  budgetFit: string;
  suitableFor: string;
  bucket: string;
  familyFriendly: boolean;
  familyScore: number;
  highlights: string[];
  reasons: string[];
  score: number;
}

export interface RecommendResponse {
  origin: string;
  destination?: string | null;
  adults: number;
  children: number;
  childAges: number[];
  recommendations: Recommendation[];
}

export interface InclusionItem {
  title: string;
  details?: string | null;
  isCritical: boolean;
  sortOrder: number;
}

export interface InclusionGroup {
  packageTier: string;
  packageLabel: string;
  items: InclusionItem[];
}

export interface DestinationInclusions {
  included: InclusionGroup[];
  notIncluded: InclusionGroup[];
}

export interface DestinationActivity {
  id: number;
  name: string;
  category: string;
  durationMinutes: number;
  costMin: number;
  costTypical: number;
  costMax: number;
  currency: string;
  budgetLevel: string;
  description?: string | null;
  familyScore: number;
  coupleScore: number;
}

export interface DestinationProfile {
  id: number;
  name: string;
  slug: string;
  country: string;
  region?: string;
  description?: string;
  familyFriendlyScore: number;
  coupleScore: number;
  romanceScore: number;
  cultureScore: number;
  adventureScore: number;
  highlights: string[];
  attractions: {
    name: string;
    category: string;
    durationMinutes: number;
    entryCostMin: number;
    entryCostTypical: number;
    entryCostMax: number;
    currency: string;
    openingHours?: string | null;
    familyScore: number;
    coupleScore: number;
  }[];
  activities: DestinationActivity[];
  inclusions: DestinationInclusions;
  sampleItinerary: string[];
  photos: string[];
  heroImageUrl?: string | null;
}

export interface TravelerSubcategory {
  id: number;
  name: string;
  code: string;
}

export interface TravelerCategory {
  id: number;
  name: string;
  code: string;
  subcategories: TravelerSubcategory[];
}

export type PlanningMode = 'AiBudget' | 'Custom';

export interface TopDestinationCard {
  id: number;
  name: string;
  country: string;
  region?: string | null;
  slug: string;
  blurb: string;
  bucket: string;
  familyScore: number;
  distanceKm?: number | null;
}

export interface TopDestinationsResponse {
  originLabel: string;
  originCountry?: string | null;
  nearYou: TopDestinationCard[];
  outsidePakistan: TopDestinationCard[];
  international: TopDestinationCard[];
}

export interface PlaceCard {
  id: number;
  listingType: string;
  name: string;
  description?: string | null;
  neighborhood?: string | null;
  priceMin?: number | null;
  priceTypical?: number | null;
  priceMax?: number | null;
  currency: string;
  rating?: number | null;
  starRating?: number | null;
  amenities?: string | null;
  cuisine?: string | null;
  openingHours?: string | null;
  imageUrl?: string | null;
  source: string;
  fetchedAtUtc?: string | null;
}

export interface FlightCard {
  id: number;
  from: string;
  to: string;
  airline: string;
  cabinClass: string;
  durationMinutes: number;
  priceMin: number;
  priceTypical: number;
  priceMax: number;
  currency: string;
  baggageNotes?: string | null;
  scheduleNotes?: string | null;
  fetchedAtUtc: string;
}

export interface TransportCard {
  id: number;
  mode: string;
  title: string;
  details?: string | null;
  typicalMinutes?: number | null;
  priceMin: number;
  priceTypical: number;
  priceMax: number;
  currency: string;
  fetchedAtUtc: string;
}

export interface DestinationDiscovery {
  destinationId: number;
  destinationName: string;
  hotels: PlaceCard[];
  apartments: PlaceCard[];
  homes: PlaceCard[];
  restaurants: PlaceCard[];
  attractions: PlaceCard[];
  shopping: PlaceCard[];
  nightlife: PlaceCard[];
  flights: FlightCard[];
  transport: TransportCard[];
  activities: DestinationActivity[];
  lastSyncedUtc?: string | null;
}

export interface ReverseGeocodeResponse {
  city: string;
  country?: string | null;
  countryCode?: string | null;
  latitude: number;
  longitude: number;
  displayName: string;
  matchedDestination?: {
    id: number;
    name: string;
    country: string;
    slug: string;
  } | null;
}

export interface EstimateLine {
  category: string;
  min: number;
  typical: number;
  max: number;
  notes?: string;
}

export interface ItineraryItem {
  dayNumber: number;
  title: string;
  category: string;
  durationMinutes: number;
  estimatedCost: number;
  isOptional: boolean;
}

export interface EstimateResponse {
  tripPlanId: number;
  origin: string;
  destination: string;
  days: number;
  nights: number;
  total: CostRange;
  budgetFit: string;
  breakdown: EstimateLine[];
  itinerary: ItineraryItem[];
  assumptions: string[];
  reasons: string[];
  planningMode?: string | null;
}
