import { Route, Routes } from 'react-router-dom'
import { NavBar } from './components/nav-bar'
import { SearchResultsView } from './features/search/search-results-view'
import { LoginPage } from './features/auth/login-page'
import { RegisterPage } from './features/auth/register-page'
import { CabinDetailPage } from './features/cabins/cabin-detail-page'
import { CheckoutPage } from './features/booking/checkout-page'
import { BookingConfirmedPage } from './features/booking/booking-confirmed-page'
import { MyBookingsPage } from './features/booking/my-bookings-page'
import { HostBookingsPage } from './features/host/host-bookings-page'

function App() {
  return (
    <>
      <NavBar />
      <Routes>
        <Route path="/" element={
          <main style={{ maxWidth: 900, margin: '0 auto', padding: '2rem 1rem' }}>
            <SearchResultsView />
          </main>
        } />
        <Route path="/login"              element={<LoginPage />} />
        <Route path="/register"           element={<RegisterPage />} />
        <Route path="/cabins/:id"         element={<CabinDetailPage />} />
        <Route path="/checkout"           element={<CheckoutPage />} />
        <Route path="/booking-confirmed"  element={<BookingConfirmedPage />} />
        <Route path="/my-bookings"        element={<MyBookingsPage />} />
        <Route path="/host/bookings"      element={<HostBookingsPage />} />
      </Routes>
    </>
  )
}

export default App
