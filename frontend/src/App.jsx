import { useEffect, useMemo, useState } from 'react'
import './App.css'

const apiBase = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? ''

const statusOptions = ['Active', 'Cancelled']

const shiftHours = (hours) => {
  const date = new Date()
  date.setHours(date.getHours() + hours)
  return date
}

const toInputValue = (date) => {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

const formatDateTime = (value) =>
  new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value))

const formatBookingDate = (value) =>
  new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  }).format(new Date(value))

const formatBookingTimeRange = (startDateTime, endDateTime) => {
  const start = new Date(startDateTime)
  const end = new Date(endDateTime)

  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  })

  return `${timeFormatter.format(start)} - ${timeFormatter.format(end)}`
}

const createDemoBookings = () => [
  {
    id: 'booking-1001',
    resourceId: 'court-01',
    userId: 'user-204',
    startDateTime: shiftHours(-3).toISOString(),
    endDateTime: shiftHours(-2).toISOString(),
    status: 'Active',
    createdAt: shiftHours(-10).toISOString(),
    cancelledAt: null,
  },
  {
    id: 'booking-1002',
    resourceId: 'court-01',
    userId: 'user-118',
    startDateTime: shiftHours(2).toISOString(),
    endDateTime: shiftHours(3).toISOString(),
    status: 'Active',
    createdAt: shiftHours(-5).toISOString(),
    cancelledAt: null,
  },
  {
    id: 'booking-1003',
    resourceId: 'court-02',
    userId: 'user-411',
    startDateTime: shiftHours(5).toISOString(),
    endDateTime: shiftHours(6).toISOString(),
    status: 'Cancelled',
    createdAt: shiftHours(-8).toISOString(),
    cancelledAt: shiftHours(-4).toISOString(),
  },
  {
    id: 'booking-1004',
    resourceId: 'room-a',
    userId: 'user-082',
    startDateTime: shiftHours(9).toISOString(),
    endDateTime: shiftHours(10.5).toISOString(),
    status: 'Active',
    createdAt: shiftHours(-2).toISOString(),
    cancelledAt: null,
  },
]

const normalizeBooking = (booking) => {
  const statusValue = booking.status ?? booking.Status ?? 'Active'
  const normalizedStatus =
    typeof statusValue === 'number'
      ? statusValue === 1
        ? 'Cancelled'
        : 'Active'
      : String(statusValue)

  return {
    id: booking.id ?? booking.Id,
    resourceId: booking.resourceId ?? booking.ResourceId,
    userId: booking.userId ?? booking.UserId,
    startDateTime: booking.startDateTime ?? booking.StartDateTime,
    endDateTime: booking.endDateTime ?? booking.EndDateTime,
    status: normalizedStatus,
    createdAt: booking.createdAt ?? booking.CreatedAt ?? null,
    cancelledAt: booking.cancelledAt ?? booking.CancelledAt ?? null,
  }
}

const buildQuery = (filters) => {
  const params = new URLSearchParams()
  params.set('resourceId', filters.resourceId)
  params.set('from', new Date(filters.from).toISOString())
  params.set('to', new Date(filters.to).toISOString())
  params.set('page', '1')
  params.set('pageSize', '100')
  return params
}

function App() {
  const [demoBookings] = useState(() => createDemoBookings())
  const [now] = useState(() => new Date())

  const [bookings, setBookings] = useState(demoBookings)
  const [sampleMode, setSampleMode] = useState(true)
  const [loading, setLoading] = useState(false)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')
  const [filters, setFilters] = useState({
    resourceId: 'court-01',
    from: toInputValue(now),
    to: toInputValue(shiftHours(72)),
    status: 'Active',
  })
  const [bookingForm, setBookingForm] = useState({
    resourceId: 'court-01',
    userId: 'user-009',
    startDateTime: toInputValue(shiftHours(4)),
    endDateTime: toInputValue(shiftHours(5)),
  })

  const sortedBookings = useMemo(
    () =>
      [...bookings].sort(
        (left, right) => new Date(left.startDateTime) - new Date(right.startDateTime),
      ),
    [bookings],
  )

  const visibleBookings = useMemo(() => {
    const activeFilter = filters.status === 'All'

    return sortedBookings.filter((booking) => {
      const matchesResource = booking.resourceId
        .toLowerCase()
        .includes(filters.resourceId.toLowerCase())
      const matchesStatus = activeFilter || booking.status === filters.status

      return matchesResource && matchesStatus
    })
  }, [filters.resourceId, filters.status, sortedBookings])

  const summary = useMemo(() => {
    const active = bookings.filter((booking) => booking.status === 'Active')
    const cancelled = bookings.filter((booking) => booking.status === 'Cancelled')
    const upcoming = active.filter((booking) => new Date(booking.startDateTime) > new Date())
    const nextReservation = [...active].sort(
      (left, right) => new Date(left.startDateTime) - new Date(right.startDateTime),
    )[0]
    const utilization = bookings.length
      ? Math.round((active.length / bookings.length) * 100)
      : 0

    return {
      active: active.length,
      cancelled: cancelled.length,
      upcoming: upcoming.length,
      utilization,
      nextReservation,
    }
  }, [bookings])

  const syncFromApi = async (nextFilters = filters) => {
    setLoading(true)
    setError('')

    try {
      const statuses = nextFilters.status === 'All' ? statusOptions : [nextFilters.status]
      const requests = statuses.map(async (status) => {
        const params = buildQuery({ ...nextFilters, status })
        params.set('status', status)

        const response = await fetch(`${apiBase}/api/bookings?${params.toString()}`)

        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const payload = await response.json()
        return Array.isArray(payload) ? payload.map(normalizeBooking) : []
      })

      const results = await Promise.all(requests)
      const merged = results.flat()

      setBookings(merged.length ? merged : [])
      setSampleMode(false)
      setNotice('Live bookings loaded from the API.')
    } catch (fetchError) {
      const fallback = demoBookings.filter((booking) => {
        const matchesResource = booking.resourceId
          .toLowerCase()
          .includes(nextFilters.resourceId.toLowerCase())
        const matchesStatus =
          nextFilters.status === 'All' || booking.status === nextFilters.status

        return matchesResource && matchesStatus
      })

      setBookings(fallback)
      setSampleMode(true)
      setNotice('Showing sample bookings. Set VITE_API_BASE_URL to connect live data.')
      setError(fetchError.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void (async () => {
      await syncFromApi(filters)
    })()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleFilterChange = (event) => {
    const { name, value } = event.target
    setFilters((current) => ({ ...current, [name]: value }))
  }

  const handleFormChange = (event) => {
    const { name, value } = event.target
    setBookingForm((current) => ({ ...current, [name]: value }))
  }

  const handleSearch = async (event) => {
    event.preventDefault()
    await syncFromApi(filters)
  }

  const handleCreateBooking = async (event) => {
    event.preventDefault()
    setError('')

    const payload = {
      resourceId: bookingForm.resourceId.trim(),
      userId: bookingForm.userId.trim(),
      startDateTime: new Date(bookingForm.startDateTime).toISOString(),
      endDateTime: new Date(bookingForm.endDateTime).toISOString(),
    }

    try {
      const response = await fetch(`${apiBase}/api/bookings`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        throw new Error(`Create failed with status ${response.status}`)
      }

      const created = normalizeBooking(await response.json())
      setBookings((current) => [created, ...current])
      setSampleMode(false)
      setNotice(`Created booking for ${created.resourceId}.`)
    } catch (createError) {
      const fallbackBooking = {
        id: `local-${Date.now()}`,
        ...payload,
        status: 'Active',
        createdAt: new Date().toISOString(),
        cancelledAt: null,
      }

      setBookings((current) => [fallbackBooking, ...current])
      setSampleMode(true)
      setNotice('Saved locally. Connect the API to persist bookings server-side.')
      setError(createError.message)
    }
  }

  const handleCancelBooking = async (bookingId) => {
    setError('')

    try {
      const response = await fetch(`${apiBase}/api/bookings/${bookingId}/cancel`, {
        method: 'POST',
      })

      if (!response.ok && response.status !== 204) {
        throw new Error(`Cancel failed with status ${response.status}`)
      }

      setBookings((current) =>
        current.map((booking) =>
          booking.id === bookingId
            ? { ...booking, status: 'Cancelled', cancelledAt: new Date().toISOString() }
            : booking,
        ),
      )
      setSampleMode(false)
      setNotice('Booking cancelled.')
    } catch (cancelError) {
      setBookings((current) =>
        current.map((booking) =>
          booking.id === bookingId
            ? { ...booking, status: 'Cancelled', cancelledAt: new Date().toISOString() }
            : booking,
        ),
      )
      setSampleMode(true)
      setNotice('Updated locally. The API endpoint was unavailable.')
      setError(cancelError.message)
    }
  }

  return (
    <div className="app-shell">
      <header className="hero-panel panel">
        <div className="hero-copy">
          <p className="eyebrow">Booking Management Service</p>
          <h1>Plan, protect, and publish every reservation from one control room.</h1>
          <p className="hero-text">
            Create bookings, inspect upcoming reservations, and cancel conflicts with a UI
            aligned to the backend domain.
          </p>
          <div className="hero-actions">
            <button className="primary-button" type="button" onClick={() => syncFromApi(filters)}>
              {loading ? 'Refreshing...' : 'Refresh data'}
            </button>
            <span className="status-pill">{sampleMode ? 'Sample data' : 'Live API'}</span>
          </div>
        </div>

        <div className="hero-metrics">
          <div className="metric-card metric-card-prominent">
            <span>Active reservations</span>
            <strong>{summary.active}</strong>
            <small>{summary.utilization}% of the current set remains active</small>
          </div>
          <div className="metric-grid">
            <div className="metric-card">
              <span>Upcoming</span>
              <strong>{summary.upcoming}</strong>
            </div>
            <div className="metric-card">
              <span>Cancelled</span>
              <strong>{summary.cancelled}</strong>
            </div>
          </div>
        </div>
      </header>

      <section className="dashboard-grid">
        <section className="panel form-panel">
          <div className="panel-heading">
            <div>
              <p className="section-label">Create booking</p>
              <h2>Capture a new reservation</h2>
            </div>
            <span className="panel-chip">POST /api/bookings</span>
          </div>

          <form className="booking-form" onSubmit={handleCreateBooking}>
            <label>
              Resource ID
              <input
                name="resourceId"
                value={bookingForm.resourceId}
                onChange={handleFormChange}
                placeholder="court-01"
              />
            </label>
            <label>
              User ID
              <input
                name="userId"
                value={bookingForm.userId}
                onChange={handleFormChange}
                placeholder="user-009"
              />
            </label>
            <label>
              Start time
              <input
                type="datetime-local"
                name="startDateTime"
                value={bookingForm.startDateTime}
                onChange={handleFormChange}
              />
            </label>
            <label>
              End time
              <input
                type="datetime-local"
                name="endDateTime"
                value={bookingForm.endDateTime}
                onChange={handleFormChange}
              />
            </label>
            <button className="primary-button full-width" type="submit">
              Create booking
            </button>
          </form>
        </section>

        <section className="panel filters-panel">
          <div className="panel-heading">
            <div>
              <p className="section-label">Search</p>
              <h2>Filter the schedule</h2>
            </div>
            <span className="panel-chip">GET /api/bookings</span>
          </div>

          <form className="filters-form" onSubmit={handleSearch}>
            <label>
              Resource
              <input
                name="resourceId"
                value={filters.resourceId}
                onChange={handleFilterChange}
                placeholder="court-01"
              />
            </label>
            <label>
              From
              <input type="datetime-local" name="from" value={filters.from} onChange={handleFilterChange} />
            </label>
            <label>
              To
              <input type="datetime-local" name="to" value={filters.to} onChange={handleFilterChange} />
            </label>
            <label>
              Status
              <select name="status" value={filters.status} onChange={handleFilterChange}>
                <option value="All">All</option>
                {statusOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <button className="secondary-button full-width" type="submit">
              Search bookings
            </button>
          </form>
          <p className="helper-text">
            The API requires a resource and a date window, so the filters stay aligned to the
            service contract.
          </p>
        </section>

        <section className="panel list-panel">
          <div className="panel-heading">
            <div>
              <p className="section-label">Reservations</p>
              <h2>Schedule feed</h2>
            </div>
            <span className="panel-chip">{visibleBookings.length} items</span>
          </div>

          {notice ? <div className="notice success">{notice}</div> : null}
          {error ? <div className="notice error">{error}</div> : null}

          <div className="booking-list">
            {visibleBookings.map((booking) => (
              <article key={booking.id} className="booking-row">
                <div className="booking-main">
                  <div className="booking-topline">
                    <div className="booking-title-group">
                      <span className="booking-label">Resource</span>
                      <h3>{booking.resourceId}</h3>
                    </div>
                    <span className={`status-badge status-${booking.status.toLowerCase()}`}>
                      {booking.status}
                    </span>
                  </div>

                  <div className="booking-lines">
                    <div className="booking-line">
                      <span className="booking-label">Guest</span>
                      <strong>{booking.userId}</strong>
                    </div>
                    <div className="booking-line">
                      <span className="booking-label">Date</span>
                      <strong>{formatBookingDate(booking.startDateTime)}</strong>
                    </div>
                    <div className="booking-line">
                      <span className="booking-label">Time</span>
                      <strong>{formatBookingTimeRange(booking.startDateTime, booking.endDateTime)}</strong>
                    </div>
                    <div className="booking-line">
                      <span className="booking-label">Created</span>
                      <strong>
                        {booking.createdAt ? formatDateTime(booking.createdAt) : 'just now'}
                      </strong>
                    </div>
                    {booking.cancelledAt ? (
                      <div className="booking-line">
                        <span className="booking-label">Cancelled</span>
                        <strong>{formatDateTime(booking.cancelledAt)}</strong>
                      </div>
                    ) : null}
                  </div>
                </div>

                <div className="booking-actions">
                  {booking.status === 'Active' ? (
                    <button
                      className="text-button"
                      type="button"
                      onClick={() => handleCancelBooking(booking.id)}
                    >
                      Cancel booking
                    </button>
                  ) : null}
                </div>
              </article>
            ))}

            {visibleBookings.length === 0 ? (
              <div className="empty-state">
                <strong>No bookings match the current filter.</strong>
                <p>Broaden the resource or date window, or create a new reservation above.</p>
              </div>
            ) : null}
          </div>
        </section>

        <aside className="panel side-panel">
          <div className="panel-heading">
            <div>
              <p className="section-label">Operations</p>
              <h2>At a glance</h2>
            </div>
          </div>

          <div className="side-stack">
            <div className="side-card">
              <span>Next reservation</span>
              <strong>
                {summary.nextReservation
                  ? formatDateTime(summary.nextReservation.startDateTime)
                  : 'None scheduled'}
              </strong>
              <p>
                {summary.nextReservation
                  ? `${summary.nextReservation.resourceId} · ${summary.nextReservation.userId}`
                  : 'Create a booking to populate the queue.'}
              </p>
            </div>

            <div className="side-card accent-card">
              <span>Connectivity</span>
              <strong>{sampleMode ? 'Local fallback' : 'API connected'}</strong>
              <p>
                {sampleMode
                  ? 'The UI is running on seeded data until the API responds.'
                  : 'Bookings are synced from the backend and can be cancelled live.'}
              </p>
            </div>

            <div className="side-card timeline-card">
              <span>Recent activity</span>
              <ul>
                {sortedBookings.slice(0, 3).map((booking) => (
                  <li key={booking.id}>
                    <strong>{booking.resourceId}</strong>
                    <span>{booking.status}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </aside>
      </section>
    </div>
  )
}

export default App
