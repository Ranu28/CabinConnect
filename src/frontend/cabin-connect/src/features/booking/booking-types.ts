export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow'

export interface GuestBookingItem {
  bookingId:  string
  cabinId:    string
  cabinName:  string
  checkIn:    string
  checkOut:   string
  totalPrice: number
  currency:   string
  status:     BookingStatus
}

export interface HoldCheckoutState {
  cabinId:    string
  cabinName:  string
  checkIn:    string
  checkOut:   string
  nights:     number
  totalPrice: number
  currency:   string
  expiresAt:  string
}

export interface BookingConfirmState {
  bookingId:  string
  cabinName:  string
  checkIn:    string
  checkOut:   string
  nights:     number
  totalPrice: number
  currency:   string
}
