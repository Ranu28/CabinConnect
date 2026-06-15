export interface CabinDetail {
  cabinId:     string
  name:        string
  description: string
  imageUrl:    string | null
  maxGuests:   number
  amenities:   string[]
  baseRate:    number
  currency:    string
  location:    { lat: number; lng: number } | null
}
