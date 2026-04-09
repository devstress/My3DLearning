import type {
  HomeModel,
  LandBlock,
  SitePlacement,
  PropertyListing,
  VirtualVillage,
  VillageLot,
  BuyerJourney,
  Notification,
  SearchResult,
  AggregatedQuote,
  PlatformUser,
} from '../types';

const API_BASE = import.meta.env.VITE_API_URL ?? '/api';

function buildQuery(params: Record<string, string | number | undefined>): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== '',
  );
  if (entries.length === 0) return '';
  return '?' + entries.map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`).join('&');
}

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${url}`, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  });
  if (!response.ok) throw new Error(`API error: ${response.status}`);
  return response.json();
}

export const api = {
  // Home models
  getHomeModels(params?: { minBedrooms?: number; format?: string }) {
    const qs = buildQuery({ minBedrooms: params?.minBedrooms, format: params?.format });
    return fetchJson<HomeModel[]>(`/home-models${qs}`);
  },
  getHomeModel(id: string) {
    return fetchJson<HomeModel>(`/home-models/${id}`);
  },

  // Land blocks
  getLandBlocks(params?: { suburb?: string; state?: string }) {
    const qs = buildQuery({ suburb: params?.suburb, state: params?.state });
    return fetchJson<LandBlock[]>(`/land-blocks${qs}`);
  },

  // Site placements
  createSitePlacement(landBlockId: string, homeModelId: string) {
    return fetchJson<SitePlacement>('/site-placements', {
      method: 'POST',
      body: JSON.stringify({ landBlockId, homeModelId }),
    });
  },

  // Villages
  getVillages(params?: { name?: string; layout?: string }) {
    const qs = buildQuery({ name: params?.name, layout: params?.layout });
    return fetchJson<VirtualVillage[]>(`/villages${qs}`);
  },
  getVillageLots(villageId: string) {
    return fetchJson<VillageLot[]>(`/villages/${villageId}/lots`);
  },

  // Marketplace
  getListings(params?: { suburb?: string; maxPriceAud?: number; status?: string }) {
    const qs = buildQuery({
      suburb: params?.suburb,
      maxPriceAud: params?.maxPriceAud,
      status: params?.status,
    });
    return fetchJson<PropertyListing[]>(`/listings${qs}`);
  },

  // Journey
  createJourney(buyerId: string, villageId?: string) {
    const qs = buildQuery({ buyerId, villageId });
    return fetchJson<BuyerJourney>(`/journeys${qs}`, { method: 'POST' });
  },
  getBuyerJourneys(buyerId: string) {
    return fetchJson<BuyerJourney[]>(`/journeys/buyer/${buyerId}`);
  },
  getJourney(id: string) {
    return fetchJson<BuyerJourney>(`/journeys/${id}`);
  },
  advanceJourney(id: string, stage: string, entityId?: string) {
    const qs = buildQuery({ stage, entityId });
    return fetchJson<BuyerJourney>(`/journeys/${id}/advance${qs}`, { method: 'PUT' });
  },

  // Dashboard
  getNotifications(recipientId: string) {
    return fetchJson<Notification[]>(`/notifications/recipient/${recipientId}`);
  },
  getAnalyticsCount() {
    return fetchJson<number>('/analytics/count');
  },

  // Health
  getHealth() {
    return fetch('/health').then((r) => {
      if (!r.ok) throw new Error(`API error: ${r.status}`);
      return r.json() as Promise<{ status: string; timestamp: string }>;
    });
  },

  // Search
  search(query: string, maxResults?: number) {
    const qs = buildQuery({ query, maxResults });
    return fetchJson<SearchResult[]>(`/search${qs}`);
  },
  searchByType(entityType: string, query: string, maxResults?: number) {
    const qs = buildQuery({ query, maxResults });
    return fetchJson<SearchResult[]>(`/search/${entityType}${qs}`);
  },

  // Aggregated Quotes
  aggregateQuote(journeyId: string) {
    const qs = buildQuery({ journeyId });
    return fetchJson<AggregatedQuote>(`/aggregated-quotes${qs}`, { method: 'POST' });
  },
  getJourneyQuotes(journeyId: string) {
    return fetchJson<AggregatedQuote[]>(`/aggregated-quotes/journey/${journeyId}`);
  },

  // Auth
  login(email: string, password: string) {
    const qs = buildQuery({ email, password });
    return fetchJson<PlatformUser>(`/auth/login${qs}`, { method: 'POST' });
  },
  register(user: { email: string; displayName: string }, password: string) {
    const qs = buildQuery({ password });
    return fetchJson<PlatformUser>(`/auth/register${qs}`, {
      method: 'POST',
      body: JSON.stringify(user),
    });
  },
  getUser(userId: string) {
    return fetchJson<PlatformUser>(`/auth/users/${userId}`);
  },
};
