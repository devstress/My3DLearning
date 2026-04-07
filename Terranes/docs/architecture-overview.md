# Architecture Overview

## System Context

Terranes is an immersive 3D property platform that acts as a broker and facilitator, connecting home buyers with builders, landscapers, interior designers, furniture suppliers, smart home providers, solicitors, and real estate agents.

```
                              Terranes Platform
┌──────────────────────────────────────────────────────────────────────────┐
│                                                                          │
│  ┌──────────────┐    ┌───────────────┐    ┌──────────────────────────┐  │
│  │   Buyers     │───▶│  Platform.Api  │───▶│   3D Village Engine     │  │
│  │  (Web/App)   │    │  (REST + WS)   │    │ (Models, Placement, Tour)│  │
│  └──────────────┘    └───────────────┘    └────────────┬─────────────┘  │
│                              │                          │                │
│                              ▼                          ▼                │
│  ┌──────────────┐    ┌───────────────┐    ┌──────────────────────────┐  │
│  │   Partners   │◀──▶│ Quoting Engine │    │   Land Data Service      │  │
│  │ (Builders,   │    │ (Aggregation)  │    │ (Gov Data, Zoning,       │  │
│  │  Landscapers,│    └───────────────┘    │  Compliance)             │  │
│  │  Furniture,  │                          └──────────────────────────┘  │
│  │  Smart Home) │                                                        │
│  └──────────────┘    ┌───────────────┐    ┌──────────────────────────┐  │
│                      │  Marketplace   │    │   Compliance Engine      │  │
│  ┌──────────────┐    │ (Listings,     │    │ (AI Regulation Checks)   │  │
│  │ Real Estate  │◀──▶│  Search)       │    └──────────────────────────┘  │
│  │ Agents       │    └───────────────┘                                   │
│  └──────────────┘                                                        │
│                                                                          │
│  ┌──────────────┐    ┌───────────────┐                                   │
│  │ Solicitors   │    │   Storage     │                                   │
│  └──────────────┘    │ (Designs,     │                                   │
│                      │  Quotes, Users)│                                   │
│  ┌──────────────┐    └───────────────┘                                   │
│  │ Land         │                                                        │
│  │ Providers    │                                                        │
│  └──────────────┘                                                        │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## Core Workflows

### 1. Explore & Walk Through

Buyer enters the virtual village → selects a home → walks through in immersive 3D with furniture and landscaping → experiences the home before committing.

### 2. Test-Fit on Land

Buyer selects a design → enters their land address → platform pulls government land data → 3D model is placed on the real block dimensions → buyer sees the home on their land.

### 3. Real-Time Modification

Buyer modifies the design in real-time → changes layout, rooms, materials → sees updated 3D model instantly on their land.

### 4. End-to-End Quoting

Buyer requests a quote → platform aggregates quotes from builder, landscaper, furniture, and smart home suppliers → buyer sees total indicative cost breakdown.

### 5. Partner Referral

Qualified buyer (visually committed, budget-aware, site-considered) is referred to partners → partners receive a high-quality lead with full context.

## Key Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Platform type | Broker/facilitator | No direct construction — connect clients with partners |
| 3D technology | Server-side model processing + client-side rendering | Supports web, mobile, and VR clients |
| Data source for land | Government land data APIs | Accurate, authoritative block dimensions and zoning |
| Quote model | Aggregated indicative quotes | Not binding — partners provide final quotes |
| Contract model | Direct client-to-partner contracts | Platform is facilitator, not a party to contracts |

See [`docs/adr/`](adr/) for full Architecture Decision Records.
