import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DestinationDiscovery,
  DestinationMatch,
  DestinationProfile,
  EstimateResponse,
  RecommendResponse,
  ReverseGeocodeResponse,
  TopDestinationsResponse,
  TravelerCategory,
} from '../models/trip.models';

function apiBase(): string {
  if (typeof window === 'undefined') {
    return '/api/v1';
  }
  const host = window.location.hostname;
  if (host === 'localhost' || host === '127.0.0.1') {
    return 'http://localhost:5280/api/v1';
  }
  return '/api/v1';
}

@Injectable({ providedIn: 'root' })
export class TripApiService {
  private readonly http = inject(HttpClient);
  private readonly api = apiBase();

  searchDestinations(q: string): Observable<{ query: string; matches: DestinationMatch[] }> {
    return this.http.get<{ query: string; matches: DestinationMatch[] }>(
      `${this.api}/destinations/search`,
      { params: { q } },
    );
  }

  getDestination(id: number): Observable<DestinationProfile> {
    return this.http.get<DestinationProfile>(`${this.api}/destinations/${id}`);
  }

  getDiscovery(id: number, refresh = false): Observable<DestinationDiscovery> {
    return this.http.get<DestinationDiscovery>(`${this.api}/destinations/${id}/discovery`, {
      params: refresh ? { refresh: 'true' } : {},
    });
  }

  getTopDestinations(params: {
    origin?: string;
    lat?: number;
    lng?: number;
  }): Observable<TopDestinationsResponse> {
    const query: Record<string, string> = {};
    if (params.origin) query['origin'] = params.origin;
    if (params.lat != null) query['lat'] = String(params.lat);
    if (params.lng != null) query['lng'] = String(params.lng);
    return this.http.get<TopDestinationsResponse>(`${this.api}/destinations/top`, { params: query });
  }

  reverseGeocode(lat: number, lng: number): Observable<ReverseGeocodeResponse> {
    return this.http.get<ReverseGeocodeResponse>(`${this.api}/geo/reverse`, {
      params: { lat: String(lat), lng: String(lng) },
    });
  }

  recommend(body: unknown): Observable<RecommendResponse> {
    return this.http.post<RecommendResponse>(`${this.api}/destinations/recommend`, body);
  }

  estimate(body: unknown): Observable<EstimateResponse> {
    return this.http.post<EstimateResponse>(`${this.api}/trips/estimate`, body);
  }

  getTravelerCategories(): Observable<TravelerCategory[]> {
    return this.http.get<TravelerCategory[]>(`${this.api}/traveler-categories`);
  }
}
