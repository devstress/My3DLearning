const API_BASE = import.meta.env.VITE_API_URL ?? '/api';
function buildQuery(params) {
    const entries = Object.entries(params).filter(([, v]) => v !== undefined && v !== '');
    if (entries.length === 0)
        return '';
    return '?' + entries.map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`).join('&');
}
async function fetchJson(url, init) {
    const response = await fetch(`${API_BASE}${url}`, {
        headers: { 'Content-Type': 'application/json', ...init?.headers },
        ...init,
    });
    if (!response.ok)
        throw new Error(`API error: ${response.status}`);
    return response.json();
}
export const api = {
    // Home models
    getHomeModels(params) {
        const qs = buildQuery({ minBedrooms: params?.minBedrooms, format: params?.format });
        return fetchJson(`/home-models${qs}`);
    },
    getHomeModel(id) {
        return fetchJson(`/home-models/${id}`);
    },
    // Land blocks
    getLandBlocks(params) {
        const qs = buildQuery({ suburb: params?.suburb, state: params?.state });
        return fetchJson(`/land-blocks${qs}`);
    },
    // Site placements
    createSitePlacement(landBlockId, homeModelId) {
        return fetchJson('/site-placements', {
            method: 'POST',
            body: JSON.stringify({ landBlockId, homeModelId }),
        });
    },
    // Villages
    getVillages(params) {
        const qs = buildQuery({ name: params?.name, layout: params?.layout });
        return fetchJson(`/villages${qs}`);
    },
    getVillageLots(villageId) {
        return fetchJson(`/villages/${villageId}/lots`);
    },
    // Marketplace
    getListings(params) {
        const qs = buildQuery({
            suburb: params?.suburb,
            maxPriceAud: params?.maxPriceAud,
            status: params?.status,
        });
        return fetchJson(`/listings${qs}`);
    },
    // Journey
    createJourney(buyerId, villageId) {
        const qs = buildQuery({ buyerId, villageId });
        return fetchJson(`/journeys${qs}`, { method: 'POST' });
    },
    getBuyerJourneys(buyerId) {
        return fetchJson(`/journeys/buyer/${buyerId}`);
    },
    getJourney(id) {
        return fetchJson(`/journeys/${id}`);
    },
    advanceJourney(id, stage, entityId) {
        const qs = buildQuery({ stage, entityId });
        return fetchJson(`/journeys/${id}/advance${qs}`, { method: 'PUT' });
    },
    // Dashboard
    getNotifications(recipientId) {
        return fetchJson(`/notifications/recipient/${recipientId}`);
    },
    getAnalyticsCount() {
        return fetchJson('/analytics/count');
    },
    // Health
    getHealth() {
        return fetch('/health').then((r) => {
            if (!r.ok)
                throw new Error(`API error: ${r.status}`);
            return r.json();
        });
    },
};
