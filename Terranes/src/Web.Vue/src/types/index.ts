export interface HomeModel {
  id: string;
  name: string;
  description: string;
  bedrooms: number;
  bathrooms: number;
  garageSpaces: number;
  floorAreaSqm: number;
  format: string;
  fileSizeMb: number;
  createdUtc: string;
}

export interface LandBlock {
  id: string;
  address: string;
  suburb: string;
  state: string;
  areaSqm: number;
  frontageMetre: number;
  depthMetre: number;
  zoning: string;
}

export interface SitePlacement {
  id: string;
  landBlockId: string;
  homeModelId: string;
  offsetX: number;
  offsetY: number;
  rotationDegrees: number;
  scaleFactor: number;
  placedUtc: string;
}

export interface PropertyListing {
  id: string;
  title: string;
  description: string;
  homeModelId: string;
  landBlockId?: string;
  askingPriceAud?: number;
  status: string;
  listedUtc: string;
}

export interface VirtualVillage {
  id: string;
  name: string;
  description: string;
  layoutType: string;
  maxLots: number;
  centreLatitude: number;
  centreLongitude: number;
  createdUtc: string;
}

export interface VillageLot {
  id: string;
  villageId: string;
  lotNumber: number;
  status: string;
  positionX: number;
  positionY: number;
}

export interface BuyerJourney {
  id: string;
  buyerId: string;
  villageId?: string;
  homeModelId?: string;
  landBlockId?: string;
  quoteRequestId?: string;
  currentStage: string;
  startedUtc: string;
  lastUpdatedUtc: string;
}

export interface Notification {
  id: string;
  recipientId: string;
  title: string;
  message: string;
  isRead: boolean;
  createdUtc: string;
}

export interface SearchResult {
  entityType: string;
  entityId: string;
  title: string;
  summary: string;
  relevanceScore: number;
}

export interface PlatformUser {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
  createdUtc: string;
}

export interface PartnerProfile {
  partnerId: string;
  companyName: string;
  contactEmail: string;
  partnerType: string;
  isActive: boolean;
}

export interface Walkthrough {
  id: string;
  homeModelId: string;
  sitePlacementId?: string;
  userId: string;
  status: string;
  sceneUrl?: string;
  createdUtc: string;
}

export interface WalkthroughPoi {
  id: string;
  walkthroughId: string;
  room: string;
  label: string;
  positionX: number;
  positionY: number;
  positionZ: number;
}

export interface DesignEdit {
  id: string;
  sitePlacementId: string;
  operation: string;
  targetElement: string;
  previousValue: string;
  newValue: string;
  appliedUtc: string;
}

export interface Report {
  id: string;
  reportType: string;
  title: string;
  generatedByUserId: string;
  tenantId: string;
  contentMarkdown: string;
  generatedUtc: string;
}
