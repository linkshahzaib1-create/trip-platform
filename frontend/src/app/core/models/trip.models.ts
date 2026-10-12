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
  estimatedCost: CostRange;
  travelTime: string;
  travelModes: string;
  budgetFit: string;
  suitableFor: string;
  bucket: string;
  reasons: string[];
  score: number;
}

export interface RecommendResponse {
  origin: string;
  recommendations: Recommendation[];
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
