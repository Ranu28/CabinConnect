export interface SearchParams {
  checkIn: string;
  checkOut: string;
  guests?: number;
  amenities?: string[];
  minPrice?: number;
  maxPrice?: number;
}

// API response types — matches CSA-002 contract exactly
export interface RateLineItem {
  label: string;          // e.g. "4 nights × $120 (Base Rate)"
  nights: number;
  ratePerNight: number;
  rateType: 'BaseRate' | 'SeasonalRate';
  subtotal: number;
}

export interface CabinSearchResult {
  cabinId: string;
  name: string;
  description: string;
  imageUrl: string | null;
  maxGuests: number;
  amenities: string[];
  location: { lat: number; lng: number } | null;
  priceBreakdown: RateLineItem[];
  totalPrice: number;
  currency: string;
}

export const AMENITY_SLUGS = [
  'wifi',
  'parking',
  'hot-tub',
  'pet-friendly',
  'fireplace',
  'kitchen',
] as const;

export type AmenitySlug = (typeof AMENITY_SLUGS)[number];
