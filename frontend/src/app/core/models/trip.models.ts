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
    entryCostTypical: number;
    familyScore: number;
    coupleScore: number;
  }[];
  activities: string[];
  inclusions: DestinationInclusions;
  sampleItinerary: string[];
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
}
