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
