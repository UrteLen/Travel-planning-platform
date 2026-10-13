import { useEffect, useMemo, useState } from 'react';
import './CalendarPanel.css';

const API_BASE = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL.replace(/\/$/, '')}/api`
  : 'http://localhost:5154/api';

function dayFrom(value) { return value ? String(value).slice(0, 10) : ''; }
function moveDay(day, amount) {
  const date = new Date(`${day}T12:00:00`);
  date.setDate(date.getDate() + amount);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
function friendlyDate(day) {
  return new Intl.DateTimeFormat('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(`${day}T12:00:00`));
}
function timeFrom(value) {
  if (!value) return '—';
  const found = String(value).match(/T(\d\d:\d\d)/);
  return found?.[1] ?? '—';
}
function formatDuration(value) {
  if (!value) return '';
  const [h, m] = String(value).split(':').map(Number);
  return h ? `${h}h${m ? ` ${m}m` : ''}` : `${m}m`;
}
async function apiError(response) {
  const body = await response.text();
  let message = body || `Request failed (${response.status}).`;
  try {
    const json = JSON.parse(body);
    message = json.detail || json.title || json.message || message;
  } catch { /* Some endpoints return plain text rather than JSON. */ }
  throw new Error(message);
}

export default function CalendarPanel({ tripId, startDate, endDate, focusDate, plannedVisits = [], revision = 0 }) {
  const firstDay = dayFrom(startDate);
  const lastDay = dayFrom(endDate);
  const [day, setDay] = useState(() => focusDate || firstDay || dayFrom(new Date().toISOString()));
  const [scheduled, setScheduled] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [refresh, setRefresh] = useState(0);
  const [editingId, setEditingId] = useState('');
  const [editStart, setEditStart] = useState('');
  const [editSpeed, setEditSpeed] = useState('40');
  const [busyId, setBusyId] = useState('');

  useEffect(() => {
    if (!tripId || !day) return undefined;
    const controller = new AbortController();
    async function fetchSchedule() {
      setLoading(true);
      setError('');
      setScheduled([]);
      try {
        const res = await fetch(`${API_BASE}/trips/${tripId}/scheduled-activities?date=${encodeURIComponent(day)}`, {
          signal: controller.signal
        });
        if (!res.ok) await apiError(res);
        const data = await res.json();
        if (!controller.signal.aborted) setScheduled(data);
      } catch (err) {
        if (!controller.signal.aborted) {
          setError(err.message === 'Failed to fetch' ? 'Cannot reach the backend. Is it running?' : err.message);
        }
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }
    fetchSchedule();
    return () => controller.abort();
  }, [tripId, day, revision, refresh]);

  const generated = useMemo(() => plannedVisits
    .filter(item => dayFrom(item.arrivalTime) === day)
    .sort((a, b) => String(a.arrivalTime).localeCompare(String(b.arrivalTime))), [day, plannedVisits]);

  function beginEdit(entry) {
    setEditingId(entry.id);
    setEditStart(String(entry.startTime).slice(0, 16));
    setEditSpeed('40'); // Travel speed is used for validation; it isn't stored on ScheduledActivity.
    setError('');
    setMessage('');
  }

  async function saveEdit(event) {
    event.preventDefault();
    const speedKmh = Number(editSpeed);
    if (!editStart || editSpeed.trim() === '' || !Number.isFinite(speedKmh) || speedKmh <= 0) {
      setError('Enter a date/time and a positive travel speed.');
      return;
    }
    if ((firstDay && dayFrom(editStart) < firstDay) || (lastDay && dayFrom(editStart) > lastDay)) {
      setError('Choose a date within the trip.');
      return;
    }
    setBusyId(editingId);
    setError('');
    setMessage('');
    try {
      const res = await fetch(`${API_BASE}/trips/${tripId}/scheduled-activities/${editingId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ startTime: editStart, speedKmh })
      });
      if (!res.ok) await apiError(res);
      const updated = await res.json();
      setEditingId('');
      setMessage('Activity time updated.');
      const updatedDay = dayFrom(updated.startTime);
      if (updatedDay && updatedDay !== day) setDay(updatedDay);
      else setRefresh(value => value + 1);
    } catch (err) {
      setError(err.message === 'Failed to fetch' ? 'Cannot reach the backend. Is it running?' : err.message);
    } finally {
      setBusyId('');
    }
  }

  async function removeActivity(entry) {
    if (!window.confirm(`Remove "${entry.activity?.name || 'Activity'}" from the calendar?`)) return;
    setBusyId(entry.id);
    setError('');
    setMessage('');
    try {
      const res = await fetch(`${API_BASE}/trips/${tripId}/scheduled-activities/${entry.id}`, { method: 'DELETE' });
      if (!res.ok) await apiError(res);
      if (editingId === entry.id) setEditingId('');
      setMessage('Activity removed from calendar.');
      setRefresh(value => value + 1);
    } catch (err) {
      setError(err.message === 'Failed to fetch' ? 'Cannot reach the backend. Is it running?' : err.message);
    } finally {
      setBusyId('');
    }
  }

  return (
    <section className="cp-shell" aria-label="Trip calendar">
      <h2>Trip Calendar</h2>

      <div className="cp-date-controls">
        <button type="button" className="btn-secondary" onClick={() => { setDay(moveDay(day, -1)); setEditingId(''); }}
                disabled={Boolean(busyId) || !day || Boolean(firstDay && day <= firstDay)}>Previous</button>
        <input type="date" aria-label="Select day" value={day}
               min={firstDay || undefined} max={lastDay || undefined} disabled={Boolean(busyId)}
               onChange={event => { setDay(event.target.value); setEditingId(''); }} />
        <button type="button" className="btn-secondary" onClick={() => { setDay(moveDay(day, 1)); setEditingId(''); }}
                disabled={Boolean(busyId) || !day || Boolean(lastDay && day >= lastDay)}>Next</button>
      </div>
      <p className="text-muted cp-current-day">{day ? friendlyDate(day) : 'Select a day'}</p>

      {error && <div className="error-banner" role="alert">{error}</div>}
      {message && <p className="text-muted cp-message" role="status">{message}</p>}
      {loading ? <p className="text-muted cp-empty">Loading calendar...</p> : (
        <>
          <h3>Scheduled Activities ({scheduled.length})</h3>
          {scheduled.length === 0 ? <p className="text-muted cp-empty">No activities scheduled for this day.</p> : (
            <ul className="list">
              {scheduled.map(entry => (
                <li className="list-item cp-row" key={entry.id}>
                  <div className="cp-row-details">
                    <strong>{entry.activity?.name || 'Activity'}</strong>
                    <div className="text-muted">
                      {entry.activity?.category || 'Activity'} · {formatDuration(entry.activity?.duration)} · €{Number(entry.activity?.estimatedCost || 0).toFixed(2)}
                    </div>
                  </div>
                  <div className="cp-row-actions">
                    <span className="cp-time">{timeFrom(entry.startTime)} – {timeFrom(entry.endTime)}</span>
                    <button type="button" className="btn-secondary" disabled={Boolean(busyId)}
                            onClick={() => editingId === entry.id ? setEditingId('') : beginEdit(entry)}>
                      {editingId === entry.id ? 'Cancel' : 'Edit'}
                    </button>
                    <button type="button" className="btn-secondary" disabled={Boolean(busyId)}
                            onClick={() => removeActivity(entry)}>
                      {busyId === entry.id ? 'Working...' : 'Remove'}
                    </button>
                  </div>
                  {editingId === entry.id && (
                    <form className="form-grid cp-edit-form" onSubmit={saveEdit}>
                      <label>New date and time
                        <input type="datetime-local" value={editStart} required
                               min={firstDay ? `${firstDay}T00:00` : undefined}
                               max={lastDay ? `${lastDay}T23:59` : undefined}
                               disabled={Boolean(busyId)} onChange={event => setEditStart(event.target.value)} />
                      </label>
                      <label>Travel speed (km/h)
                        <input type="number" min="1" step="any" value={editSpeed} required
                               disabled={Boolean(busyId)} onChange={event => setEditSpeed(event.target.value)} />
                      </label>
                      <div className="cp-edit-buttons">
                        <button type="submit" className="btn-primary" disabled={Boolean(busyId)}>
                          {busyId === entry.id ? 'Saving...' : 'Save Changes'}
                        </button>
                        <button type="button" className="btn-secondary" disabled={Boolean(busyId)}
                                onClick={() => setEditingId('')}>Cancel</button>
                      </div>
                    </form>
                  )}
                </li>
              ))}
            </ul>
          )}

          <h3>Generated Route ({generated.length})</h3>
          {generated.length === 0 ? <p className="text-muted cp-empty">No generated route visits for this day.</p> : (
            <ul className="list">
              {generated.map((visit, i) => (
                <li className="list-item cp-row" key={`${visit.name}-${i}`}>
                  <strong>{visit.name}</strong>
                  <span className="cp-time">{timeFrom(visit.arrivalTime)} – {timeFrom(visit.departureTime)}</span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}
