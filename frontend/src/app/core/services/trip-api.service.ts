import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DestinationMatch,
  EstimateResponse,
  RecommendResponse,
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

  recommend(body: unknown): Observable<RecommendResponse> {
    return this.http.post<RecommendResponse>(`${this.api}/destinations/recommend`, body);
  }

  estimate(body: unknown): Observable<EstimateResponse> {
    return this.http.post<EstimateResponse>(`${this.api}/trips/estimate`, body);
  }
}
