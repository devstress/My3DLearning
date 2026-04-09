# Terranes – Completion Log

> Detailed record of completed chunks. Each entry includes: date, goal, architecture, files created/modified, test counts.

---

## Phase 1 — Foundation

### Chunk 001 — Repository Scaffold (2026-04-07)

**Goal:** Create the Terranes folder with rules, docs, solution, and initial project structure mirroring EnterpriseIntegrationPlatform conventions.

**Files created:**
- `Terranes/.editorconfig`
- `Terranes/.gitignore`
- `Terranes/global.json`
- `Terranes/Directory.Build.props`
- `Terranes/Directory.Packages.props`
- `Terranes/Terranes.sln`
- `Terranes/README.md`
- `Terranes/rules/` — All 7 rule files
- `Terranes/docs/` — architecture-overview.md, developer-setup.md
- `Terranes/src/Contracts/` — Core DTOs and interfaces
- `Terranes/tests/UnitTests/` — Initial unit test project

**Tests:** Initial unit tests for Contracts project.

---

## Phase 2 — Core Platform Services

### Chunk 002 — 3D Model Service (2026-04-07)

**Goal:** Build in-memory 3D model service with upload, storage, metadata, format validation, and search.

**Files created:**
- `src/Models3D/Models3D.csproj`
- `src/Models3D/HomeModelService.cs` — Full IHomeModelService implementation with ConcurrentDictionary store, format/size/bedrooms/area validation
- `src/Models3D/ServiceCollectionExtensions.cs` — DI registration
- `tests/UnitTests/Services/HomeModelServiceTests.cs` — 15 tests (creation, retrieval, search, validation)

### Chunk 003 — Land Data Service (2026-04-07)

**Goal:** Build land block service for government land data integration, block analysis, and zoning lookup.

**Files created:**
- `src/Land/Land.csproj`
- `src/Land/LandBlockService.cs` — Full ILandBlockService implementation with address/state lookup, suburb/area search
- `src/Land/ServiceCollectionExtensions.cs` — DI registration
- `tests/UnitTests/Services/LandBlockServiceTests.cs` — 13 tests (creation, retrieval, address lookup, search)

### Chunk 004 — Site Placement Engine (2026-04-07)

**Goal:** Build site placement service to test-fit 3D models onto land blocks using real dimensions.

**Files created:**
- `src/SitePlacement/SitePlacement.csproj`
- `src/SitePlacement/SitePlacementService.cs` — Full ISitePlacementService implementation with fit validation using footprint, setback, offset, and scale
- `src/SitePlacement/ServiceCollectionExtensions.cs` — DI registration
- `tests/UnitTests/Services/SitePlacementServiceTests.cs` — 12 tests (placement, retrieval, fit validation with various scenarios)

### Chunk 005 — Quoting Engine (2026-04-07)

**Goal:** Build quoting engine to aggregate end-to-end quotes from partners.

**Files created:**
- `src/Quoting/Quoting.csproj`
- `src/Quoting/QuotingService.cs` — Full IQuotingService implementation with request lifecycle (Pending→InProgress→Completed), line item management
- `src/Quoting/ServiceCollectionExtensions.cs` — DI registration
- `tests/UnitTests/Services/QuotingServiceTests.cs` — 14 tests (request, line items, completion, lifecycle)

### Chunk 006 — Marketplace Service (2026-04-07)

**Goal:** Build marketplace service for home listings, search, filtering, and status management.

**Files created:**
- `src/Marketplace/Marketplace.csproj`
- `src/Marketplace/MarketplaceService.cs` — Full IMarketplaceService implementation with listing lifecycle (Draft→Active→UnderOffer→Sold/Withdrawn), search by suburb/price/status
- `src/Marketplace/ServiceCollectionExtensions.cs` — DI registration
- `tests/UnitTests/Services/MarketplaceServiceTests.cs` — 16 tests (creation, retrieval, search, status transitions, lifecycle)

### Chunk 007 — Compliance Engine (2026-04-07)

**Goal:** Build compliance engine for building regulation checks per jurisdiction.

**Files created:**
- `src/Contracts/Enums/ComplianceOutcome.cs` — New enum (Compliant, NonCompliant, ConditionallyCompliant, RequiresReview)
- `src/Contracts/Models/ComplianceResult.cs` — New model record
- `src/Contracts/Models/ComplianceViolation.cs` — New model record
- `src/Contracts/Abstractions/IComplianceService.cs` — New interface
- `src/Compliance/Compliance.csproj`
- `src/Compliance/ComplianceService.cs` — Full IComplianceService implementation with 4 compliance rules (site coverage, setback, lot size, frontage) varying by zoning type
- `src/Compliance/ServiceCollectionExtensions.cs` — DI registration
- `tests/UnitTests/Services/ComplianceServiceTests.cs` — 13 tests (compliant, violations, retrieval, error handling)

### Platform.Api — REST API (2026-04-07)

**Goal:** Build minimal API REST layer wiring all services for UAT.

**Files created:**
- `src/Platform.Api/Platform.Api.csproj`
- `src/Platform.Api/Program.cs` — Minimal API with DI registration, health check
- `src/Platform.Api/appsettings.json`
- `src/Platform.Api/appsettings.Development.json`
- `src/Platform.Api/Properties/launchSettings.json`
- `src/Platform.Api/Endpoints/HomeModelEndpoints.cs` — POST/GET/Search
- `src/Platform.Api/Endpoints/LandBlockEndpoints.cs` — POST/GET/Lookup/Search
- `src/Platform.Api/Endpoints/SitePlacementEndpoints.cs` — POST/GET/Validate
- `src/Platform.Api/Endpoints/QuotingEndpoints.cs` — POST/GET/LineItems/Complete
- `src/Platform.Api/Endpoints/MarketplaceEndpoints.cs` — POST/GET/Search/UpdateStatus
- `src/Platform.Api/Endpoints/ComplianceEndpoints.cs` — POST Check/GET/GetByPlacement

**Tests:** 104 total (21 Contracts + 83 Services). All pass.

**Architecture:**
- 8 src projects (Contracts, Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance, Platform.Api)
- All services use ConcurrentDictionary for thread-safe in-memory storage
- All services have full input validation with descriptive error messages
- DI registration via AddXxx() extension methods per project
- REST API at `http://localhost:5200` with 19 endpoints across 6 resource groups

---

## Phase 3 — Partner Integration

### Chunk 008 — Builder Integration (2026-04-07)

**Goal:** Builder partner registration, profile lookup, matching by bedrooms/floor area/region, and quote request/response workflow.

**Files created:**
- `src/Contracts/Enums/PartnerQuoteStatus.cs` — Requested, Quoted, Declined, Expired, Accepted, Rejected
- `src/Contracts/Enums/BuilderType.cs` — Volume, Custom
- `src/Contracts/Models/PartnerQuoteResponse.cs` — Partner quote response record
- `src/Contracts/Models/BuilderProfile.cs` — Builder-specific profile record
- `src/Contracts/Abstractions/IBuilderService.cs` — Builder service interface
- `src/PartnerIntegration/BuilderService.cs` — Full implementation with registration, search, quote request/response
- `src/Platform.Api/Endpoints/BuilderEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/BuilderServiceTests.cs` — 17 tests

### Chunk 009 — Landscaper Integration (2026-04-07)

**Goal:** Landscaper partner registration, design template management, and quote routing.

**Files created:**
- `src/Contracts/Enums/LandscapeStyle.cs` — Native, Tropical, Modern, Cottage, Minimal, Japanese, Mediterranean
- `src/Contracts/Models/LandscaperProfile.cs` — Landscaper profile record
- `src/Contracts/Models/LandscapeDesign.cs` — Landscape design template record
- `src/Contracts/Abstractions/ILandscaperService.cs` — Landscaper service interface
- `src/PartnerIntegration/LandscaperService.cs` — Full implementation with registration, search, design management, quoting
- `src/Platform.Api/Endpoints/LandscaperEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/LandscaperServiceTests.cs` — 14 tests

### Chunk 010 — Furniture & Interior Integration (2026-04-07)

**Goal:** Furniture supplier catalog, room fitting, and pricing calculation.

**Files created:**
- `src/Contracts/Enums/FurnitureCategory.cs` — LivingRoom, Bedroom, Kitchen, Bathroom, Outdoor, Office, Dining, Lighting, Appliance
- `src/Contracts/Models/FurnitureItem.cs` — Furniture catalog item record
- `src/Contracts/Models/RoomFitting.cs` — Room fitting placement record
- `src/Contracts/Abstractions/IFurnitureService.cs` — Furniture service interface
- `src/PartnerIntegration/FurnitureService.cs` — Full implementation with catalog, room fitting, total pricing
- `src/Platform.Api/Endpoints/FurnitureEndpoints.cs` — 6 REST endpoints
- `tests/UnitTests/Services/FurnitureServiceTests.cs` — 17 tests

### Chunk 011 — Smart Home Integration (2026-04-07)

**Goal:** Smart home device catalog, compatibility checks, and package pricing.

**Files created:**
- `src/Contracts/Enums/SmartHomeCategory.cs` — Security, Lighting, Climate, Entertainment, Energy, Appliance, Network, Blinds
- `src/Contracts/Models/SmartHomeDevice.cs` — Smart home device record
- `src/Contracts/Abstractions/ISmartHomeService.cs` — Smart home service interface
- `src/PartnerIntegration/SmartHomeService.cs` — Full implementation with catalog, compatibility (protocol-based), package pricing
- `src/Platform.Api/Endpoints/SmartHomeEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/SmartHomeServiceTests.cs` — 15 tests

### Chunk 012 — Solicitor Integration (2026-04-07)

**Goal:** Solicitor partner registration, matching by service type, and fixed-fee quoting.

**Files created:**
- `src/Contracts/Models/SolicitorProfile.cs` — Solicitor profile record
- `src/Contracts/Abstractions/ISolicitorService.cs` — Solicitor service interface
- `src/PartnerIntegration/SolicitorService.cs` — Full implementation with registration, search, fixed-fee quoting
- `src/Platform.Api/Endpoints/SolicitorEndpoints.cs` — 4 REST endpoints
- `tests/UnitTests/Services/SolicitorServiceTests.cs` — 12 tests

### Chunk 013 — Real Estate Agent Integration (2026-04-07)

**Goal:** Real estate agent registration, listings sync, and MLS feed integration.

**Files created:**
- `src/Contracts/Models/AgentProfile.cs` — Agent profile record
- `src/Contracts/Abstractions/IRealEstateAgentService.cs` — Agent service interface
- `src/PartnerIntegration/RealEstateAgentService.cs` — Full implementation with registration, search, marketplace listing sync
- `src/Platform.Api/Endpoints/RealEstateAgentEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/RealEstateAgentServiceTests.cs` — 14 tests

### Phase 3 Summary

**Files created:**
- `src/PartnerIntegration/PartnerIntegration.csproj` — New project
- `src/PartnerIntegration/ServiceCollectionExtensions.cs` — AddPartnerIntegration() DI registration

**Tests:** 193 total (104 existing + 89 new partner tests). All pass.

**Architecture:**
- 9 src projects (Contracts, Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance, PartnerIntegration, Platform.Api)
- PartnerIntegration houses all 6 partner services in one project
- All partner services use ConcurrentDictionary for thread-safe in-memory storage
- All services have full input validation with descriptive error messages
- DI registration via single AddPartnerIntegration() extension method
- REST API now has 49 endpoints across 12 resource groups
- RealEstateAgentService integrates with IMarketplaceService for listing sync

---

## Phase 4 — Immersive 3D Experience

### Chunk 014 — Virtual Village (2026-04-07)

**Goal:** 3D neighbourhood scene with lot management, home placement, and village statistics.

**Files created:**
- `src/Contracts/Enums/VillageLayoutType.cs` — Grid, Curved, CulDeSac, MixedUse
- `src/Contracts/Enums/VillageLotStatus.cs` — Vacant, Occupied, Reserved, UnderDesign
- `src/Contracts/Models/VirtualVillage.cs` — Village scene record
- `src/Contracts/Models/VillageLot.cs` — Individual lot record
- `src/Contracts/Abstractions/IVirtualVillageService.cs` — Village service interface
- `src/Immersive3D/VirtualVillageService.cs` — Full implementation with village creation, lot allocation, placement assignment, stats
- `src/Platform.Api/Endpoints/VirtualVillageEndpoints.cs` — 7 REST endpoints
- `tests/UnitTests/Services/VirtualVillageServiceTests.cs` — 15 tests

### Chunk 015 — Home Walkthrough (2026-04-07)

**Goal:** Immersive 3D tour with room navigation and point-of-interest markers.

**Files created:**
- `src/Contracts/Enums/WalkthroughPoiType.cs` — Room, Feature, Measurement, Fixture, Outdoor
- `src/Contracts/Enums/WalkthroughStatus.cs` — Generating, Ready, Failed
- `src/Contracts/Models/HomeWalkthrough.cs` — Walkthrough session record
- `src/Contracts/Models/WalkthroughPoi.cs` — Point of interest record
- `src/Contracts/Abstractions/IWalkthroughService.cs` — Walkthrough service interface
- `src/Immersive3D/WalkthroughService.cs` — Full implementation with generation, POI management, room filtering
- `src/Platform.Api/Endpoints/WalkthroughEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/WalkthroughServiceTests.cs` — 12 tests

### Chunk 016 — Real-Time 3D Editor (2026-04-07)

**Goal:** Modify home design on-block in real time with material/position/rotation edits and undo support.

**Files created:**
- `src/Contracts/Enums/EditOperationType.cs` — Move, Rotate, Scale, MaterialChange, ComponentSwap, ColourChange
- `src/Contracts/Models/DesignEdit.cs` — Edit operation record
- `src/Contracts/Abstractions/IDesignEditorService.cs` — Design editor interface
- `src/Immersive3D/DesignEditorService.cs` — Full implementation with edit apply, history, undo, filter by type, reset
- `src/Platform.Api/Endpoints/DesignEditorEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/DesignEditorServiceTests.cs` — 11 tests

### Chunk 017 — AI Video-to-3D (2026-04-07)

**Goal:** Video upload and processing pipeline for AI-driven 3D mesh generation.

**Files created:**
- `src/Contracts/Enums/VideoProcessingStatus.cs` — Queued, Analysing, GeneratingMesh, Completed, Failed
- `src/Contracts/Models/VideoToModelJob.cs` — Video conversion job record
- `src/Contracts/Abstractions/IVideoToModelService.cs` — Video-to-3D service interface
- `src/Immersive3D/VideoToModelService.cs` — Full implementation with upload validation, stage progression, failure handling
- `src/Platform.Api/Endpoints/VideoToModelEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/VideoToModelServiceTests.cs` — 13 tests

### Chunk 018 — User-Generated Content (2026-04-07)

**Goal:** User home posts, galleries, publishing, and community ratings.

**Files created:**
- `src/Contracts/Enums/ContentPostStatus.cs` — Draft, Published, UnderReview, Removed, Archived
- `src/Contracts/Models/ContentPost.cs` — Content post record with images and rating aggregation
- `src/Contracts/Models/ContentRating.cs` — Community rating record
- `src/Contracts/Abstractions/IContentService.cs` — Content service interface
- `src/Immersive3D/ContentService.cs` — Full implementation with post lifecycle, publishing, ratings with average calculation, duplicate prevention
- `src/Platform.Api/Endpoints/ContentEndpoints.cs` — 6 REST endpoints
- `tests/UnitTests/Services/ContentServiceTests.cs` — 15 tests

### Phase 4 Summary

**Files created:**
- `src/Immersive3D/Immersive3D.csproj` — New project
- `src/Immersive3D/ServiceCollectionExtensions.cs` — AddImmersive3D() DI registration

**Tests:** 254 total (193 existing + 61 new immersive tests). All pass.

**Architecture:**
- 10 src projects (Contracts, Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance, PartnerIntegration, Immersive3D, Platform.Api)
- Immersive3D houses all 5 immersive experience services in one project
- All services use ConcurrentDictionary for thread-safe in-memory storage
- DI registration via single AddImmersive3D() extension method
- REST API now has 77 endpoints across 17 resource groups

---

## Phase 5 — Platform Infrastructure

### Chunk 019 — Authentication & Authorization (2026-04-07)

**Goal:** User registration, login with hashed passwords, role-based access control.

**Files created:**
- `src/Contracts/Enums/UserRole.cs` — Buyer, Partner, Agent, Admin
- `src/Contracts/Models/PlatformUser.cs` — Platform user record with auth and tenant info
- `src/Contracts/Abstractions/IAuthService.cs` — Auth service interface
- `src/Infrastructure/AuthService.cs` — Full implementation with SHA256 password hashing, email uniqueness, role management, deactivation
- `src/Platform.Api/Endpoints/AuthEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/AuthServiceTests.cs` — 14 tests

### Chunk 020 — Observability (2026-04-07)

**Goal:** Structured audit logging, health checks for all services, custom metrics recording.

**Files created:**
- `src/Contracts/Enums/HealthStatus.cs` — Healthy, Degraded, Unhealthy
- `src/Contracts/Models/HealthCheckResult.cs` — Health check result record
- `src/Contracts/Models/AuditLogEntry.cs` — Structured audit log entry record
- `src/Contracts/Abstractions/IObservabilityService.cs` — Observability service interface
- `src/Infrastructure/ObservabilityService.cs` — Full implementation with audit log, 10-component health checks, metrics store
- `src/Platform.Api/Endpoints/ObservabilityEndpoints.cs` — 6 REST endpoints
- `tests/UnitTests/Services/ObservabilityServiceTests.cs` — 12 tests

### Chunk 021 — Multi-Tenancy (2026-04-07)

**Goal:** Tenant creation, slug-based lookup, tenant isolation, user-tenant assignment.

**Files created:**
- `src/Contracts/Models/Tenant.cs` — Tenant record with slug and active state
- `src/Contracts/Abstractions/ITenantService.cs` — Tenant service interface
- `src/Infrastructure/TenantService.cs` — Full implementation with slug uniqueness, tenant lifecycle, user listing
- `src/Platform.Api/Endpoints/TenantEndpoints.cs` — 6 REST endpoints
- `tests/UnitTests/Services/TenantServiceTests.cs` — 10 tests

### Chunk 022 — Deployment (covered by Platform.Api wiring)

Deployment manifests are not applicable to the in-memory demo platform. All services are wired end-to-end through Platform.Api with `dotnet run --project src/Platform.Api`.

### Phase 5 Summary

**Files created:**
- `src/Infrastructure/Infrastructure.csproj` — New project
- `src/Infrastructure/ServiceCollectionExtensions.cs` — AddInfrastructure() DI registration

**Tests:** 290 total (254 existing + 36 new infrastructure tests). All pass.

**Architecture:**
- 11 src projects (Contracts, Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance, PartnerIntegration, Immersive3D, Infrastructure, Platform.Api)
- Infrastructure houses auth, observability, and tenant services
- AuthService uses SHA256 password hashing with email index for uniqueness
- TenantService depends on IAuthService for user lookups
- DI registration via single AddInfrastructure() extension method
- REST API now has 94 endpoints across 20 resource groups
- Run: `dotnet run --project src/Platform.Api` → http://localhost:5200

---

## Phase 6 — Buyer Journey Orchestration

### Chunk 023 — Buyer Journey Service (2026-04-07)

**Goal:** Orchestrate the complete buyer experience from village browsing through to partner referral.

**Files created:**
- `src/Contracts/Enums/JourneyStage.cs` — Browsing, DesignSelected, PlacedOnLand, Customising, QuoteRequested, QuoteReceived, Referred, Completed, Abandoned
- `src/Contracts/Models/BuyerJourney.cs` — Full journey record with entity linking
- `src/Contracts/Abstractions/IBuyerJourneyService.cs` — Journey service interface
- `src/Journey/BuyerJourneyService.cs` — Full implementation with lifecycle, stage validation, entity linking
- `src/Platform.Api/Endpoints/BuyerJourneyEndpoints.cs` — 6 REST endpoints
- `tests/UnitTests/Services/BuyerJourneyServiceTests.cs` — 14 tests

### Chunk 024 — Quote Aggregator (2026-04-07)

**Goal:** Aggregate cost estimates from multiple partner categories into a unified quote.

**Files created:**
- `src/Contracts/Models/AggregatedQuote.cs` — Aggregated quote record with per-category breakdowns
- `src/Contracts/Abstractions/IQuoteAggregatorService.cs` — Aggregator service interface
- `src/Journey/QuoteAggregatorService.cs` — Full implementation with realistic estimate ranges
- `src/Platform.Api/Endpoints/QuoteAggregatorEndpoints.cs` — 3 REST endpoints
- `tests/UnitTests/Services/QuoteAggregatorServiceTests.cs` — 9 tests

### Chunk 025 — Referral Pipeline (2026-04-07)

**Goal:** Generate qualified leads and route to partners with accept/decline workflow.

**Files created:**
- `src/Contracts/Enums/ReferralStatus.cs` — Pending, Sent, Accepted, Declined, Expired
- `src/Contracts/Models/PartnerReferral.cs` — Referral record with response tracking
- `src/Contracts/Abstractions/IReferralService.cs` — Referral service interface
- `src/Journey/ReferralService.cs` — Full implementation with status management and response timestamps
- `src/Platform.Api/Endpoints/ReferralEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/ReferralServiceTests.cs` — 13 tests

### Phase 6 Summary

**Files created:**
- `src/Journey/Journey.csproj` — New project
- `src/Journey/ServiceCollectionExtensions.cs` — AddJourney() DI registration

**Tests:** 326 total (290 existing + 36 new journey tests). All pass.

**Architecture:**
- 12 src projects (+ Journey)
- Journey houses buyer journey, quote aggregator, and referral services
- QuoteAggregatorService depends on IQuotingService for aggregation
- DI registration via single AddJourney() extension method
- REST API now has 108 endpoints across 23 resource groups

---

## Phase 7 — Notifications & Events

### Chunk 026 — Notification Service (2026-04-07)

**Goal:** Notification creation, delivery tracking, and read status management.

**Files created:**
- `src/Contracts/Enums/NotificationType.cs` — QuoteReady, ReferralCreated, ReferralAccepted, DesignSaved, VideoProcessingComplete, ContentPublished, SystemAlert, Info
- `src/Contracts/Enums/NotificationStatus.cs` — Queued, Delivered, Read, Failed
- `src/Contracts/Models/Notification.cs` — Notification record
- `src/Contracts/Abstractions/INotificationService.cs` — Notification service interface
- `src/Notifications/NotificationService.cs` — Full implementation with delivery and read tracking
- `src/Platform.Api/Endpoints/NotificationEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/NotificationServiceTests.cs` — 12 tests

### Chunk 027 — Event Bus (2026-04-07)

**Goal:** In-memory pub/sub event system for cross-service communication.

**Files created:**
- `src/Contracts/Models/PlatformEvent.cs` — Event record with topic and correlation
- `src/Contracts/Abstractions/IEventBusService.cs` — Event bus interface
- `src/Notifications/EventBusService.cs` — Full implementation with topic index and correlation tracking
- `src/Platform.Api/Endpoints/EventBusEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/EventBusServiceTests.cs` — 11 tests

### Chunk 028 — Webhook Service (2026-04-07)

**Goal:** Webhook registration and simulated delivery for partner integrations.

**Files created:**
- `src/Contracts/Enums/WebhookDeliveryStatus.cs` — Pending, Delivered, Failed, Retrying
- `src/Contracts/Models/WebhookRegistration.cs` — Webhook registration record
- `src/Contracts/Models/WebhookDelivery.cs` — Webhook delivery record
- `src/Contracts/Abstractions/IWebhookService.cs` — Webhook service interface
- `src/Notifications/WebhookService.cs` — Full implementation with topic matching, deactivation, delivery history
- `src/Platform.Api/Endpoints/WebhookEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/WebhookServiceTests.cs` — 13 tests

### Phase 7 Summary

**Files created:**
- `src/Notifications/Notifications.csproj` — New project
- `src/Notifications/ServiceCollectionExtensions.cs` — AddNotifications() DI registration

**Tests:** 362 total (326 existing + 36 new notification tests). All pass.

**Architecture:**
- 13 src projects (+ Notifications)
- Notifications houses notification, event bus, and webhook services
- EventBusService supports case-insensitive topic matching
- WebhookService matches events to registered webhooks by topic
- DI registration via single AddNotifications() extension method
- REST API now has 123 endpoints across 26 resource groups

---

## Phase 8 — Search & Analytics

### Chunk 029 — Search Service (2026-04-07)

**Goal:** Cross-entity full-text search with relevance scoring.

**Files created:**
- `src/Contracts/Models/SearchResult.cs` — Search result record with relevance score
- `src/Contracts/Abstractions/ISearchService.cs` — Search service interface
- `src/Analytics/SearchService.cs` — Full implementation with substring matching, word-level scoring, type filtering
- `src/Platform.Api/Endpoints/SearchEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/SearchServiceTests.cs` — 12 tests

### Chunk 030 — Analytics Service (2026-04-07)

**Goal:** User engagement tracking with aggregated summaries and popular entity discovery.

**Files created:**
- `src/Contracts/Enums/AnalyticsEventType.cs` — VillageView, HomeModelView, WalkthroughStarted, DesignPlaced, DesignEdited, QuoteRequested, Referral, Search, Registration, Login
- `src/Contracts/Models/AnalyticsEvent.cs` — Analytics event record
- `src/Contracts/Models/AnalyticsSummary.cs` — Aggregated summary record
- `src/Contracts/Abstractions/IAnalyticsService.cs` — Analytics service interface
- `src/Analytics/AnalyticsService.cs` — Full implementation with user events, summaries, popular entities
- `src/Platform.Api/Endpoints/AnalyticsEndpoints.cs` — 5 REST endpoints
- `tests/UnitTests/Services/AnalyticsServiceTests.cs` — 12 tests

### Chunk 031 — Reporting Service (2026-04-07)

**Goal:** Report generation with multiple report types and markdown content.

**Files created:**
- `src/Contracts/Models/Report.cs` — Report record
- `src/Contracts/Abstractions/IReportingService.cs` — Reporting service interface
- `src/Analytics/ReportingService.cs` — Full implementation with 5 report types (PlatformOverview, JourneySummary, PartnerActivity, EngagementReport, QuoteSummary)
- `src/Platform.Api/Endpoints/ReportingEndpoints.cs` — 4 REST endpoints
- `tests/UnitTests/Services/ReportingServiceTests.cs` — 12 tests

### Phase 8 Summary

**Files created:**
- `src/Analytics/Analytics.csproj` — New project
- `src/Analytics/ServiceCollectionExtensions.cs` — AddAnalytics() DI registration

**Tests:** 390 total (362 existing + 28 new analytics tests). All pass. *Correction: 100 new tests across all 3 phases for a total of 390.*

**Architecture:**
- 14 src projects (Contracts, Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance, PartnerIntegration, Immersive3D, Infrastructure, Journey, Notifications, Analytics, Platform.Api)
- Analytics houses search, analytics, and reporting services
- SearchService uses simple substring matching with word-level relevance scoring
- ReportingService depends on IAnalyticsService for event counts
- DI registration via single AddAnalytics() extension method
- REST API now has 137 endpoints across 29 resource groups
- Run: `dotnet run --project src/Platform.Api` → http://localhost:5200

---

## Phase 9 — Integration Tests

### Chunk 032 — IntegrationTests (2026-04-07)

**Goal:** Create WebApplicationFactory-based integration tests that exercise every API endpoint group end-to-end through the HTTP pipeline. Fix pre-existing multi-body parameter binding bugs discovered during integration testing.

**Files created:**
- `tests/IntegrationTests/IntegrationTests.csproj` — Test project with Microsoft.AspNetCore.Mvc.Testing
- `tests/IntegrationTests/IntegrationTestBase.cs` — Shared base class with WebApplicationFactory, HttpClient, helper methods
- `tests/IntegrationTests/CoreServiceIntegrationTests.cs` — 14 tests: HomeModel, LandBlock, SitePlacement, Quoting, Marketplace, Compliance, Health
- `tests/IntegrationTests/PartnerIntegrationTests.cs` — 10 tests: Furniture, SmartHome CRUD/search/compatibility
- `tests/IntegrationTests/Immersive3DIntegrationTests.cs` — 10 tests: Village, Walkthrough, DesignEditor, VideoToModel, Content
- `tests/IntegrationTests/InfrastructureIntegrationTests.cs` — 10 tests: Auth, Observability, Tenant
- `tests/IntegrationTests/JourneyNotificationAnalyticsIntegrationTests.cs` — 12 tests: Journey, Aggregator, Referral, Notification, EventBus, Webhook, Search, Analytics, Reporting

**Files modified:**
- `Terranes.slnx` — Added IntegrationTests project
- `Directory.Packages.props` — Added Microsoft.AspNetCore.Mvc.Testing 10.0.5
- 5 endpoint files fixed: Builder, Landscaper, Solicitor, RealEstateAgent, Webhook — multi-body params wrapped into request DTOs

**Tests:** 446 total (390 unit + 56 integration), all passing

---

## Phase 10 — Blazor Web UI

### Chunk 033 — Blazor Server Scaffold + All Pages (2026-04-07)

**Goal:** Build a Blazor Server web UI that wires all 29 backend services directly via DI, providing a full buyer-facing experience with 7 pages covering the entire platform.

**Files created:**
- `src/Web/Web.csproj` — Blazor Server project referencing all 12 service projects
- `src/Web/Program.cs` — Registers all Terranes services + Blazor components
- `src/Web/Components/_Imports.razor` — Global Razor imports including Terranes namespaces
- `src/Web/Components/Layout/NavMenu.razor` — Navigation with Terranes pages
- `src/Web/Components/Pages/Home.razor` — Landing page with platform overview and feature cards
- `src/Web/Components/Pages/Villages.razor` — Virtual village browser with search, layout filter, lot details
- `src/Web/Components/Pages/HomeModels.razor` — Home design gallery with bedroom/format filters, detail modal
- `src/Web/Components/Pages/LandBlocks.razor` — Land block search with suburb/state filters, test-fit functionality
- `src/Web/Components/Pages/Marketplace.razor` — Property listing browser with suburb/price/status filters
- `src/Web/Components/Pages/Journey.razor` — Guided buyer journey (Browsing → DesignSelected → PlacedOnLand → Customising → QuoteRequested → QuoteReceived → Completed)
- `src/Web/Components/Pages/Dashboard.razor` — Platform dashboard with active journeys, notifications, stats, recent designs

**Files modified:**
- `Terranes.slnx` — Added Web project

**Architecture:**
- 15 src projects (14 existing + Web)
- Blazor Server Interactive with all 29 services wired via DI
- 7 interactive pages covering: home, villages, designs, land, marketplace, journey, dashboard
- Journey page implements full buyer flow with stage-specific UI and progress bar
- Dashboard aggregates data from Journey, Notification, Analytics, HomeModel, and Marketplace services
- Run: `dotnet run --project src/Web` → http://localhost:5000 (Blazor UI)
- Run: `dotnet run --project src/Platform.Api` → http://localhost:5200 (REST API)

**Tests:** 446 total (390 unit + 56 integration), all passing

---

## Phase 11 — Vue 3 + Vite Frontend & Aspire Orchestration

### Chunk 048 — Vue 3 + Vite + Aspire Migration (2026-04-07)

**Goal:** Replace Blazor Server UI with a Vue 3 + Vite + TypeScript frontend and add .NET Aspire orchestration to run the frontend and backend together.

**Files created:**
- `src/Web.Vue/` — Full Vue 3 + Vite + TypeScript frontend project
  - `package.json` — Dependencies: vue 3.5, vue-router 4.5, vite 8, typescript
  - `vite.config.ts` — Vite config with API proxy (supports Aspire service discovery)
  - `index.html` — Entry HTML with Bootstrap 5 CDN
  - `src/main.ts` — Vue app entry point with router
  - `src/App.vue` — Root layout with sidebar navigation
  - `src/style.css` — Global layout styles
  - `src/types/index.ts` — TypeScript interfaces for all domain entities
  - `src/api/client.ts` — Fetch-based API client for all endpoints
  - `src/router/index.ts` — Vue Router with 7 lazy-loaded routes
  - `src/views/HomeView.vue` — Landing page with 6 feature cards
  - `src/views/HomeModelsView.vue` — Home design gallery with filters + detail modal
  - `src/views/VillagesView.vue` — Virtual village browser with lots table
  - `src/views/LandBlocksView.vue` — Land search + test-fit placement modal
  - `src/views/MarketplaceView.vue` — Property listings with status badges
  - `src/views/JourneyView.vue` — 7-stage buyer journey with progress bar
  - `src/views/DashboardView.vue` — Stats cards, journeys, notifications, designs
- `src/AppHost/` — .NET Aspire AppHost project
  - `AppHost.csproj` — Aspire SDK 13.2.1, Aspire.Hosting.JavaScript
  - `Program.cs` — Orchestrates Platform.Api + Vue frontend
  - `Properties/launchSettings.json` — Dashboard URLs
  - `appsettings.json`, `appsettings.Development.json`
- `src/ServiceDefaults/` — .NET Aspire ServiceDefaults project
  - `ServiceDefaults.csproj` — OpenTelemetry, service discovery, resilience
  - `Extensions.cs` — AddServiceDefaults(), ConfigureOpenTelemetry(), MapDefaultEndpoints()

**Files modified:**
- `Terranes.slnx` — Replaced Web with AppHost + ServiceDefaults (17 src projects)
- `Directory.Packages.props` — Added Aspire and OpenTelemetry package versions
- `src/Platform.Api/Platform.Api.csproj` — Added ServiceDefaults reference
- `src/Platform.Api/Program.cs` — Added AddServiceDefaults(), CORS, MapDefaultEndpoints()
- `.gitignore` — Added dist/ for Vue build output
- `README.md` — Updated tech stack, run instructions, project structure

**Architecture:**
- 17 src projects (14 existing domain + Platform.Api + AppHost + ServiceDefaults) + Vue 3 frontend
- Vue 3 + Vite + TypeScript frontend replaces Blazor Server
- .NET Aspire orchestrates both frontend and backend
- ServiceDefaults adds OpenTelemetry, health checks, service discovery, HTTP resilience
- Vite dev proxy forwards /api → Platform.Api (supports Aspire service references)
- Run: `dotnet run --project src/AppHost` → Aspire dashboard + both services
- Run: `cd src/Web.Vue && npm run dev` → Vue frontend standalone
- Run: `dotnet run --project src/Platform.Api` → REST API standalone

**Tests:** 446 total (390 unit + 56 integration), all passing — no regressions

---

## Phase 12 — Vue Frontend Cleanup, Components & Testing

### Chunk 049 — Cleanup, Reusable Components & Vitest Tests (2026-04-07)

**Goal:** Remove old Blazor Web project, extract reusable Vue components, write comprehensive Vitest component tests, and fix HTML validation warnings.

**Files deleted:**
- `src/Web/` — Entire old Blazor Server Web project removed from filesystem

**Files created:**
- `src/Web.Vue/src/components/LoadingSpinner.vue` — Reusable loading indicator with customizable message
- `src/Web.Vue/src/components/StatusBadge.vue` — Color-mapped status badge supporting 16 status values
- `src/Web.Vue/src/components/DetailModal.vue` — Reusable Bootstrap modal wrapper with slot
- `src/Web.Vue/src/components/ErrorAlert.vue` — Conditional error alert display
- `src/Web.Vue/src/__tests__/HomeView.spec.ts` — 6 tests: page title, feature cards, router links
- `src/Web.Vue/src/__tests__/HomeModelsView.spec.ts` — 6 tests: loading, cards, details modal, empty state
- `src/Web.Vue/src/__tests__/VillagesView.spec.ts` — 5 tests: loading, cards, detail modal, lots table
- `src/Web.Vue/src/__tests__/LandBlocksView.spec.ts` — 5 tests: loading, table, test-fit modal, error handling
- `src/Web.Vue/src/__tests__/MarketplaceView.spec.ts` — 5 tests: loading, cards, status badges, price formatting
- `src/Web.Vue/src/__tests__/JourneyView.spec.ts` — 6 tests: begin button, progress bar, stages, errors, completion
- `src/Web.Vue/src/__tests__/DashboardView.spec.ts` — 5 tests: stat cards, journeys, notifications, designs
- `src/Web.Vue/src/__tests__/components/StatusBadge.spec.ts` — 4 tests: text, classes, fallback, custom map
- `src/Web.Vue/src/__tests__/components/DetailModal.spec.ts` — 3 tests: hidden, visible, close emit
- `src/Web.Vue/src/__tests__/components/LoadingSpinner.spec.ts` — 2 tests: default message, custom message
- `src/Web.Vue/src/__tests__/components/ErrorAlert.spec.ts` — 2 tests: hidden when null, visible when set

**Files modified:**
- `src/Web.Vue/package.json` — Added test scripts; added vitest, @vue/test-utils, jsdom, @vitest/coverage-v8
- `src/Web.Vue/vite.config.ts` — Added vitest test configuration (globals, jsdom environment)
- `src/Web.Vue/src/views/HomeModelsView.vue` — Use LoadingSpinner, DetailModal; add `<tbody>` to table
- `src/Web.Vue/src/views/VillagesView.vue` — Use LoadingSpinner, DetailModal, StatusBadge; add `<tbody>`
- `src/Web.Vue/src/views/LandBlocksView.vue` — Use LoadingSpinner, DetailModal, ErrorAlert; add `<tbody>`
- `src/Web.Vue/src/views/MarketplaceView.vue` — Use LoadingSpinner, DetailModal, StatusBadge; add `<tbody>`
- `src/Web.Vue/src/views/JourneyView.vue` — Use StatusBadge, ErrorAlert; removed getStageBadge()
- `src/Web.Vue/src/views/DashboardView.vue` — Use LoadingSpinner, StatusBadge; removed getStageBadge()
- `rules/milestones.md` — Updated for Phases 11, 12
- `rules/completion-log.md` — This entry

**Architecture:**
- 17 src projects (AppHost, ServiceDefaults, 14 domain + Platform.Api) + Vue 3 frontend
- 4 shared components reduce code duplication across 6 views
- 49 Vitest component tests with full API mocking via vi.mock
- All `<table>` elements use proper `<tbody>` wrappers (no HTML validation warnings)
- Vue tests: `npm test` or `npx vitest run` in `src/Web.Vue/`
- .NET tests: `dotnet test` in Terranes/

**Tests:** 495 total (390 NUnit unit + 56 NUnit integration + 49 Vitest component), all passing

---

## Phase 13 — UX & UI Polish (AI-Driven)

### Chunk 050 — Toast Notifications & Action Feedback (2026-04-07)

**Goal:** Add user-facing feedback for every async action. Create toast notification system and loading-disabled buttons so users always know what's happening.

**Files created:**
- `rules/ux-rules.md` — AI agent rule for UX/UI implementation (component conventions, design principles, forbidden patterns)
- `src/Web.Vue/src/composables/useToast.ts` — Global toast state composable: showSuccess, showError, showInfo, removeToast. Auto-dismiss after 5s for success/info, manual dismiss for errors.
- `src/Web.Vue/src/composables/useAsyncAction.ts` — Loading + toast wrapper for async actions
- `src/Web.Vue/src/components/ToastContainer.vue` — Bottom-right stacked toast display with enter/leave transitions, aria-live polite region
- `src/Web.Vue/src/components/ActionButton.vue` — Button with spinner, disabled state, aria-busy during loading
- `src/Web.Vue/src/__tests__/composables/useToast.spec.ts` — 7 tests: add/remove, auto-dismiss, persistence
- `src/Web.Vue/src/__tests__/composables/useAsyncAction.spec.ts` — 6 tests: loading state, success/error toasts, result handling
- `src/Web.Vue/src/__tests__/components/ToastContainer.spec.ts` — 5 tests: empty, success, error, dismiss, stacking
- `src/Web.Vue/src/__tests__/components/ActionButton.spec.ts` — 8 tests: slots, loading, disabled, variant, size, click, aria-busy

**Files modified:**
- `src/Web.Vue/src/App.vue` — Added ToastContainer mount
- `src/Web.Vue/src/views/JourneyView.vue` — All 7 async actions now show toast feedback + ActionButton with loading states
- `src/Web.Vue/src/views/LandBlocksView.vue` — Test-fit success/error toasts added
- `src/Web.Vue/package.json` — Added vitest, @vue/test-utils, jsdom devDeps; added test/test:watch scripts
- `src/Web.Vue/tsconfig.app.json` — Excluded test files from app build
- `src/Web.Vue/.gitignore` — Added vue-tsc build artifact patterns
- `rules/milestones.md` — Added Phase 13 with 13 UX chunks; marked Chunk 050 done

**Tests:** 521 total (390 NUnit unit + 56 NUnit integration + 75 Vitest component), all passing

### Chunk 051 — Skeleton Loaders & Smooth Transitions (2026-04-07)

**Goal:** Replace text-based loading indicators with skeleton placeholders that match the final layout shape. Add smooth fade transitions between routes.

**Files created:**
- `src/Web.Vue/src/components/SkeletonCard.vue` — Configurable card-grid skeleton with count/columns props. Uses Bootstrap's placeholder-glow animation. aria-hidden for screen readers.
- `src/Web.Vue/src/components/SkeletonTable.vue` — Configurable table skeleton with rows/cols props. Animated placeholder cells.
- `src/Web.Vue/src/__tests__/components/SkeletonCard.spec.ts` — 5 tests: default count, custom count, column class, animation, accessibility
- `src/Web.Vue/src/__tests__/components/SkeletonTable.spec.ts` — 3 tests: default rows/cols, custom rows/cols, animation

**Files modified:**
- `src/Web.Vue/src/App.vue` — Added `<Transition name="fade" mode="out-in">` on RouterView for smooth route transitions
- `src/Web.Vue/src/style.css` — Added fade transition CSS with prefers-reduced-motion respect
- `src/Web.Vue/src/views/VillagesView.vue` — Replaced LoadingSpinner with SkeletonCard (3 cards, 3 columns)
- `src/Web.Vue/src/views/HomeModelsView.vue` — Replaced LoadingSpinner with SkeletonCard (3 cards, 3 columns)
- `src/Web.Vue/src/views/MarketplaceView.vue` — Replaced LoadingSpinner with SkeletonCard (2 cards, 2 columns)
- `src/Web.Vue/src/views/LandBlocksView.vue` — Replaced LoadingSpinner with SkeletonTable (5 rows, 8 columns)
- Updated 4 view tests to check for placeholder-glow instead of text-based loading messages

**Tests:** 529 total (390 NUnit unit + 56 NUnit integration + 83 Vitest component), all passing

### Chunk 063 — Playwright Multi-Browser E2E Tests (2026-04-07)

**Goal:** Add Playwright E2E cross-browser testing infrastructure with Chromium, Firefox, WebKit + mobile and tablet viewports. Create AI agent rule for Playwright conventions. 29 E2E tests across 5 spec files covering navigation, home page, responsive layout, views smoke, and UX feedback/accessibility.

**Files created:**
- `playwright.config.ts` — 6 browser projects (Chromium, Firefox, WebKit, mobile-chrome/Pixel 5, mobile-safari/iPhone 13, tablet/iPad gen 7). Auto-starts Vite dev server. Trace on retry, screenshot on failure.
- `e2e/helpers.ts` — `openSidebarIfMobile()` helper for mobile viewport sidebar toggle
- `e2e/navigation.spec.ts` — 5 tests: home load, sidebar links, sidebar navigation with mobile support, CTA cards, active link highlighting
- `e2e/home.spec.ts` — 4 tests: hero section, feature cards grid, CTA buttons, tagline section
- `e2e/responsive.spec.ts` — 6 tests: desktop sidebar, mobile hidden nav, mobile toggler, tablet cards, mobile stacking, page title
- `e2e/views.spec.ts` — 8 tests: per-view smoke tests for Villages, Home Models, Land Blocks, Marketplace, Journey, Dashboard + search controls
- `e2e/ux-feedback.spec.ts` — 6 tests: route transitions, toast container, skeleton loaders, document structure, button text, link text
- `rules/playwright-rules.md` — AI agent rule: multi-browser projects, run commands, test conventions, selector priority, customer-first philosophy, forbidden patterns

**Files modified:**
- `package.json` — Added `@playwright/test ^1.59.1` devDep, 6 new scripts: `test:e2e`, `test:e2e:chromium`, `test:e2e:firefox`, `test:e2e:webkit`, `test:e2e:mobile`, `test:e2e:report`
- `vite.config.ts` — Added `exclude: ['e2e/**']` to Vitest config to prevent Playwright spec collision
- `.gitignore` — Added `test-results/`, `playwright-report/`, `blob-report/`
- `rules/ux-rules.md` — Added E2E Testing row to tech stack, added Playwright steps to chunk implementation pattern
- `rules/milestones.md` — Added Chunk 063 (done)

**Tests:** 29 Playwright E2E tests × 6 browser projects = 174 cross-browser runs. 83 Vitest component tests. 446 NUnit tests. All passing.

### Chunk 052 — Responsive Layout Overhaul (2026-04-09)

**Goal:** Migrate sidebar breakpoint from 641px to Bootstrap md (768px). Add collapsible sidebar with slide animation. Make all card grids stack to 1-column on mobile. Add responsive scrolling for journey stages. Respect prefers-reduced-motion.

**Files modified:**
- `src/style.css` — Migrated all breakpoints from 641px/640.98px to 768px/767.98px. Replaced `display: none/block` sidebar toggle with `max-height: 0 → 100vh` slide transition. Added `prefers-reduced-motion` media query for sidebar animation.
- `src/views/HomeView.vue` — Added `col-12` to all 6 feature card columns for mobile stacking.
- `src/views/VillagesView.vue` — Added `col-12` to village card columns.
- `src/views/HomeModelsView.vue` — Added `col-12` to model card columns.
- `src/views/MarketplaceView.vue` — Added `col-12` to listing card columns.
- `src/views/DashboardView.vue` — Added `col-6` to stat cards, `col-12` to journey/notification cards and recent models.
- `src/views/JourneyView.vue` — Added `col-12` to design cards. Replaced row/col stage indicators with flex-nowrap scrollable container for mobile.
- `src/components/SkeletonCard.vue` — Added `col-12` for mobile stacking.

**Files created:**
- `src/__tests__/ResponsiveLayout.spec.ts` — 5 tests: breakpoint migration verification, sidebar slide animation CSS, prefers-reduced-motion, HomeView col-12 stacking, SkeletonCard col-12 stacking.

**Tests:** 88 Vitest tests (83 + 5 new). 29 Playwright E2E × 6 browsers. 446 NUnit. All passing.

### Chunk 053 — Accessibility & Keyboard Navigation (2026-04-09)

**Goal:** Add comprehensive accessibility support: ARIA attributes, keyboard navigation, focus trapping, skip-to-content link, and screen-reader-friendly toast announcements.

**Files modified:**
- `src/components/DetailModal.vue` — Added `role="dialog"`, `aria-modal="true"`, `aria-label`, close button `aria-label="Close modal"`. Added Escape-to-close keydown handler. Added Tab focus trap cycling between first/last focusable elements. Added backdrop click-to-close. Auto-focuses modal on open.
- `src/App.vue` — Added skip-to-content link (`<a href="#main-content" class="skip-to-content">`). Added `id="main-content"` on `<main>`. Added `aria-label="Main navigation"` on sidebar nav. Added `aria-label="Toggle navigation"` on navbar toggler.
- `src/style.css` — Added `.skip-to-content` styles: positioned off-screen, shown on focus.
- `src/views/VillagesView.vue` — Added `aria-label="View village details"` on button.
- `src/views/HomeModelsView.vue` — Added `aria-label="View model details"` on button.
- `src/views/MarketplaceView.vue` — Added `aria-label="View listing details"` on button.
- `src/views/LandBlocksView.vue` — Added `aria-label="Test-fit design on block"` on button.

**Files created:**
- `src/__tests__/Accessibility.spec.ts` — 10 tests: dialog role/aria-modal, close button aria-label, Escape-to-close, backdrop click-to-close, skip-to-content link, main#main-content, nav aria-label, toggler aria-label, skip-to-content CSS, toast aria-live verification.

**Tests:** 98 Vitest tests (88 + 10 new). 29 Playwright E2E × 6 browsers. 446 NUnit. All passing.

### Chunk 054 — Dark Mode Support (2026-04-09)

**Goal:** Add dark mode support with system-preference detection, manual toggle, localStorage persistence, and sidebar gradient adaptation.

**Files created:**
- `src/composables/useTheme.ts` — `useTheme()` composable: singleton pattern with `theme` ref, `toggleTheme()`, `setTheme()`. Detects `prefers-color-scheme: dark`. Sets `data-bs-theme` on `<html>`. Persists to `localStorage('terranes-theme')`. Listens for system preference changes when no user override.
- `src/__tests__/composables/useTheme.spec.ts` — 8 tests: default light theme, data-bs-theme attribute, toggle switching, localStorage persistence, read stored preference, explicit setTheme, dark-mode CSS verification (2 tests).

**Files modified:**
- `src/App.vue` — Imported and initialised `useTheme()`. Added theme toggle button in sidebar with 🌙/☀️ emoji and "Dark Mode"/"Light Mode" label.
- `src/style.css` — Migrated `.top-row` background from `#f7f7f7` to `var(--bs-body-bg)` and border from `#d6d5d5` to `var(--bs-border-color)`. Added `[data-bs-theme="dark"] .sidebar` darker gradient. Added `.theme-toggle-btn` styles.

**Tests:** 106 Vitest tests (98 + 8 new). 29 Playwright E2E × 6 browsers. 446 NUnit. All passing.
