import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import './ActivitiesPanel.css';

const API_BASE = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL.replace(/\/$/, '')}/api`
  : 'http://localhost:5154/api';

const CATEGORIES = ['Activities', 'Food', 'Transport', 'Accommodation'];

async function api(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, options);
  if (!response.ok) {
    const body = await response.text();
    let message = body || `Request failed (${response.status}).`;
    try {
      const parsed = JSON.parse(body);
      message = parsed.detail || parsed.title || parsed.error || message;
    } catch { /* The API sometimes returns plain text. */ }
    throw new Error(message);
  }
  if (response.status === 204) return null;
  const body = await response.text();
  return body ? JSON.parse(body) : null;
}

function formatMoney(value) {
  return new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR', maximumFractionDigits: 2 }).format(value || 0);
}

function durationInHours(value) {
  if (!value) return 0;
  const [clock, daysText] = value.includes('.') ? [value.split('.').at(-1), value.split('.')[0]] : [value, '0'];
  const [h = 0, m = 0, s = 0] = clock.split(':').map(Number);
  return Number(daysText) * 24 + h + m / 60 + s / 3600;
}

function formatDuration(value) {
  const total = Math.round(durationInHours(value) * 60);
  const h = Math.floor(total / 60);
  const min = total % 60;
  return h && min ? `${h}h ${min}m` : h ? `${h}h` : `${min}m`;
}

const initialForm = (latitude, longitude) => ({
  name: '', category: 'Activities', durationHours: '2', estimatedCost: '0',
  latitude: String(latitude ?? 54.6872), longitude: String(longitude ?? 25.2797),
  optional: true, openingTime: '', closingTime: ''
});

function initialScheduleTime(startDate, endDate) {
  const today = new Date();
  const yyyy = today.getFullYear();
  const mm = String(today.getMonth() + 1).padStart(2, '0');
  const dd = String(today.getDate()).padStart(2, '0');
  const currentDay = `${yyyy}-${mm}-${dd}`;
  const first = startDate?.slice(0, 10);
  const last = endDate?.slice(0, 10);
  const day = first && last && currentDay >= first && currentDay <= last ? currentDay : (first || currentDay);
  return `${day}T09:00`;
}

export default function ActivitiesPanel({
  tripId, participants = [], participantId, onParticipantChange,
  tripStartDate, tripEndDate, defaultLatitude, defaultLongitude,
  onScheduleChange, onOpenCalendar
}) {
  const [activities, setActivities] = useState([]);
  const [voteResults, setVoteResults] = useState({ key: '', byActivity: {} });
  const currentVoteKey = `${tripId || ''}:${participantId || ''}`;
  const counts = voteResults.key === currentVoteKey ? voteResults.byActivity : {};
  const votesReady = voteResults.key === currentVoteKey;
  const latestLoadId = useRef(0);
  const [loading, setLoading] = useState(true);
  const [busyVote, setBusyVote] = useState('');
  const [adding, setAdding] = useState(false);
  const [importing, setImporting] = useState(false);
  const [importFile, setImportFile] = useState(null);
  const importInput = useRef(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(() => initialForm(defaultLatitude, defaultLongitude));
  const [sort, setSort] = useState('priority');
  const [category, setCategory] = useState('All');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [budget, setBudget] = useState('50');
  const [unlimitedBudget, setUnlimitedBudget] = useState(false);
  const [hours, setHours] = useState('5');
  const [selection, setSelection] = useState(null);
  const [selecting, setSelecting] = useState(false);
  const [scheduleId, setScheduleId] = useState('');
  const [scheduleTime, setScheduleTime] = useState(() => initialScheduleTime(tripStartDate, tripEndDate));
  const [speed, setSpeed] = useState('40');
  const [scheduling, setScheduling] = useState(false);

  const loadActivities = useCallback(async (signal) => {
    const loadId = ++latestLoadId.current;
    try {
      const list = tripId
        ? await api(`/trips/${tripId}/activities/prioritized`, { signal })
        : [];
      const resultPairs = await Promise.all(list.map(async activity => {
        const query = participantId ? `?participantId=${encodeURIComponent(participantId)}` : '';
        const data = await api(`/trips/${tripId}/activities/${activity.id}/votes/results${query}`, { signal });
        return [activity.id, data];
      }));
      if (!signal?.aborted && loadId === latestLoadId.current) {
        setActivities(list);
        setVoteResults({ key: currentVoteKey, byActivity: Object.fromEntries(resultPairs) });
        setError('');
      }
    } catch (err) {
      if (err.name !== 'AbortError' && !signal?.aborted && loadId === latestLoadId.current) {
        setError(err.message === 'Failed to fetch' ? 'Cannot connect to the server. Is the backend running on port 5154?' : err.message);
      }
    } finally {
      if (!signal?.aborted && loadId === latestLoadId.current) setLoading(false);
    }
  }, [tripId, participantId, currentVoteKey]);

  useEffect(() => {
    const controller = new AbortController();
    Promise.resolve().then(() => {
      if (!controller.signal.aborted) return loadActivities(controller.signal);
      return undefined;
    });
    return () => controller.abort();
  }, [loadActivities]);

  const displayed = useMemo(() => {
    const list = activities.filter(activity => category === 'All' || activity.category === category);
    if (sort === 'name') return [...list].sort((a, b) => a.name.localeCompare(b.name));
    if (sort === 'price') return [...list].sort((a, b) => a.estimatedCost - b.estimatedCost);
    return list;
  }, [activities, category, sort]);

  async function addActivity(event) {
    event.preventDefault();
    setError(''); setMessage('');
    const latitude = Number(form.latitude);
    const longitude = Number(form.longitude);
    const cost = Number(form.estimatedCost);
    const duration = Number(form.durationHours);
    if (!form.name.trim() || !Number.isFinite(duration) || duration <= 0 || !Number.isFinite(cost) || cost < 0 ||
        !Number.isFinite(latitude) || latitude < -90 || latitude > 90 ||
        !Number.isFinite(longitude) || longitude < -180 || longitude > 180) {
      setError('Check the name, duration, cost and coordinates.'); return;
    }
    if (Boolean(form.openingTime) !== Boolean(form.closingTime)) {
      setError('Add both opening and closing times, or leave both empty.'); return;
    }
    setAdding(true);
    try {
      await api(`/trips/${tripId}/activities`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name.trim(), category: form.category, durationHours: duration,
          estimatedCost: cost, latitude, longitude, optional: form.optional,
          openingTime: form.openingTime ? `${form.openingTime}:00` : null,
          closingTime: form.closingTime ? `${form.closingTime}:00` : null
        })
      });
      setForm(initialForm(defaultLatitude, defaultLongitude));
      setCreateOpen(false);
      setSelection(null);
      setMessage('Activity added to this trip.');
      await loadActivities();
    } catch (err) { setError(err.message); }
    finally { setAdding(false); }
  }

  async function importActivities(event) {
    event.preventDefault();
    setError(''); setMessage('');
    if (!importFile || !tripId || importing) return;
    if (importFile.size === 0) {
      setError('Choose a JSON file that is not empty.'); return;
    }
    const body = new FormData();
    body.append('file', importFile);
    setImporting(true);
    try {
      const imported = await api(`/trips/${tripId}/activities/import`, {
        method: 'POST', body
      });
      setSelection(null);
      setCategory('All');
      setImportFile(null);
      if (importInput.current) importInput.current.value = '';
      setMessage(imported.length === 0
        ? 'The file contained no activities.'
        : `${imported.length} ${imported.length === 1 ? 'activity imported' : 'activities imported'} into this trip.`);
      await loadActivities();
    } catch (err) {
      setError(err.message === 'Failed to fetch' ? 'Cannot connect to the server. Check that the backend is running.' : err.message);
    } finally { setImporting(false); }
  }

  async function castVote(activityId, voteType) {
    if (!participantId) { setError('Choose a participant before voting.'); return; }
    setBusyVote(activityId); setError(''); setMessage('');
    const previous = counts[activityId]?.participantVote;
    const removing = previous === voteType;
    try {
      await api(`/trips/${tripId}/activities/${activityId}/votes/${participantId}`, removing
        ? { method: 'DELETE' }
        : { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ voteType }) });
      setSelection(null);
      await loadActivities();
    } catch (err) { setError(err.message); }
    finally { setBusyVote(''); }
  }

  async function selectActivities(event) {
    event.preventDefault();
    setError(''); setMessage('');
    const budgetValue = Number(budget);
    const hoursValue = Number(hours);
    if ((!unlimitedBudget && (budget.trim() === '' || !Number.isFinite(budgetValue) || budgetValue < 0)) ||
        hours.trim() === '' || !Number.isFinite(hoursValue) || hoursValue < 0) {
      setError('Enter non-negative hours and a budget, or choose No budget limit.'); return;
    }
    setSelecting(true);
    try {
      const query = new URLSearchParams({ hours: String(hoursValue) });
      if (!unlimitedBudget) query.set('budget', String(budgetValue));
      const result = await api(`/trips/${tripId}/activities/selected?${query}`);
      setSelection(result);
    } catch (err) { setError(err.message); }
    finally { setSelecting(false); }
  }

  async function scheduleActivity(event, activityId) {
    event.preventDefault();
    if (!scheduleTime || !Number.isFinite(Number(speed)) || Number(speed) <= 0) {
      setError('Choose a date/time and a positive travel speed.'); return;
    }
    setScheduling(true); setError(''); setMessage('');
    try {
      await api(`/trips/${tripId}/scheduled-activities`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ activityId, startTime: scheduleTime, speedKmh: Number(speed) })
      });
      setMessage('Activity scheduled successfully.');
      setScheduleId('');
      onScheduleChange?.();
    } catch (err) { setError(err.message); }
    finally { setScheduling(false); }
  }

  const selectedCost = selection?.reduce((sum, activity) => sum + activity.estimatedCost, 0) ?? 0;
  const selectedHours = selection?.reduce((sum, activity) => sum + durationInHours(activity.duration), 0) ?? 0;

  return (
      <section className="ap-shell" aria-label="Activities and voting">
        {error && <div className="error-banner" role="alert">{error}</div>}
      {message && (
        <p className="text-muted" role="status">
          {message}
          {message === 'Activity scheduled successfully.' && onOpenCalendar && (
            <> <button type="button" className="btn-secondary ap-calendar-link"
                        onClick={() => onOpenCalendar(scheduleTime.slice(0, 10))}>Open Calendar</button></>
          )}
        </p>
      )}

      <button type="button" className="btn-primary ap-main-button" onClick={() => setCreateOpen(open => !open)}>
        {createOpen ? 'Close Activity Form' : 'Add Activity'}
      </button>

      {createOpen && (
        <form onSubmit={addActivity} className="form-grid ap-activity-form">
          <input aria-label="Activity name" autoFocus placeholder="Activity Name" value={form.name}
                 onChange={e => setForm(f => ({ ...f, name: e.target.value }))} maxLength={120} required />
          <select aria-label="Activity category" value={form.category}
                  onChange={e => setForm(f => ({ ...f, category: e.target.value }))}>
            {CATEGORIES.map(name => <option value={name} key={name}>{name}</option>)}
          </select>
          <input aria-label="Duration in hours" type="number" min="0.25" max="500" step="0.25"
                 placeholder="Duration (hours)" value={form.durationHours}
                 onChange={e => setForm(f => ({ ...f, durationHours: e.target.value }))} required />
          <input aria-label="Estimated cost in euros" type="number" min="0" step="0.01"
                 placeholder="Estimated Cost (€)" value={form.estimatedCost}
                 onChange={e => setForm(f => ({ ...f, estimatedCost: e.target.value }))} required />
          <label className="ap-checkbox">
            <input type="checkbox" checked={form.optional}
                   onChange={e => setForm(f => ({ ...f, optional: e.target.checked }))} />
            Optional Activity
          </label>
          <details className="ap-advanced">
            <summary>Location and Opening Hours</summary>
            <div className="form-grid ap-advanced-fields">
              <label>Latitude
                <input type="number" min="-90" max="90" step="any" value={form.latitude}
                       onChange={e => setForm(f => ({ ...f, latitude: e.target.value }))} required />
              </label>
              <label>Longitude
                <input type="number" min="-180" max="180" step="any" value={form.longitude}
                       onChange={e => setForm(f => ({ ...f, longitude: e.target.value }))} required />
              </label>
              <label>Opening Time (optional)
                <input type="time" value={form.openingTime}
                       onChange={e => setForm(f => ({ ...f, openingTime: e.target.value }))} />
              </label>
              <label>Closing Time (optional)
                <input type="time" value={form.closingTime}
                       onChange={e => setForm(f => ({ ...f, closingTime: e.target.value }))} />
              </label>
            </div>
          </details>
          <button type="submit" className="btn-primary" disabled={adding || !tripId}>
            {adding ? 'Adding...' : 'Save Activity'}
          </button>
          <button type="button" className="btn-secondary" onClick={() => setCreateOpen(false)}>Cancel</button>
        </form>
      )}

      <details className="ap-import-section">
        <summary>Import Activities from JSON</summary>
      <form onSubmit={importActivities} className="ap-import-form" aria-label="Import activities">
        <label htmlFor="activity-import-file">Import Activities from JSON</label>
        <p id="activity-import-help" className="text-muted">
          Choose a JSON file containing a list of activities. Imported activities are added to this trip.
        </p>
        <div className="ap-import-controls">
          <input id="activity-import-file" ref={importInput} type="file" accept=".json,application/json"
                 aria-describedby="activity-import-help" disabled={importing} required
                 onChange={event => setImportFile(event.target.files?.[0] ?? null)} />
          <button type="submit" className="btn-secondary" disabled={importing || !importFile || !tripId}>
            {importing ? 'Importing...' : 'Import Activities'}
          </button>
        </div>
      </form>
      </details>

      <h3>Suggested Activities ({activities.length})</h3>
      <div className="ap-filter-row">
        <select aria-label="Filter by category" value={category} onChange={e => setCategory(e.target.value)}>
          <option value="All">All Categories</option>
          {CATEGORIES.map(name => <option value={name} key={name}>{name}</option>)}
        </select>
        <select aria-label="Sort activities" value={sort} onChange={e => setSort(e.target.value)}>
          <option value="priority">Sort: Votes</option>
          <option value="name">Sort: Name</option>
          <option value="price">Sort: Cost</option>
        </select>
        {participants.length > 1 && (
          <select aria-label="Vote as participant" value={participantId || ''}
                  onChange={e => onParticipantChange?.(e.target.value)}>
            <option value="">Vote As...</option>
            {participants.map(person => <option key={person.id} value={person.id}>{person.name}</option>)}
          </select>
        )}
      </div>
      {loading && <p className="text-muted ap-empty">Loading activities...</p>}
      {!loading && activities.length === 0 && <p className="text-muted ap-empty">No activities added yet.</p>}
      {!loading && activities.length > 0 && displayed.length === 0 && (
        <p className="text-muted ap-empty">No activities in this category.</p>
      )}

      {!loading && displayed.length > 0 && (
        <ul className="list ap-activities">
          {displayed.map(activity => {
            const up = counts[activity.id]?.upvotes ?? 0;
            const down = counts[activity.id]?.downvotes ?? 0;
            const userVote = counts[activity.id]?.participantVote;
            return (
              <li key={activity.id} className="list-item ap-activity-row">
                <div className="ap-activity-details">
                  <strong>{activity.name}</strong>
                  <div className="text-muted">
                    {activity.category} · {formatDuration(activity.duration)} · {formatMoney(activity.estimatedCost)}
                    {!activity.isOptional && ' · Required'}
                    {activity.openingTime && ` · Open ${activity.openingTime.slice(0, 5)}–${activity.closingTime?.slice(0, 5)}`}
                  </div>
                  <div className="ap-votes" role="group" aria-label={`Votes for ${activity.name}`}>
                    <button type="button" className={`btn-secondary${userVote === 'Upvote' ? ' ap-voted' : ''}`}
                            aria-pressed={userVote === 'Upvote'} disabled={!participantId || !votesReady || busyVote === activity.id}
                            onClick={() => castVote(activity.id, 'Upvote')}>👍 {up}</button>
                    <button type="button" className={`btn-secondary${userVote === 'Downvote' ? ' ap-voted' : ''}`}
                            aria-pressed={userVote === 'Downvote'} disabled={!participantId || !votesReady || busyVote === activity.id}
                            onClick={() => castVote(activity.id, 'Downvote')}>👎 {down}</button>
                  </div>
                </div>
                <button type="button" className="btn-secondary ap-schedule-toggle"
                        onClick={() => setScheduleId(id => id === activity.id ? '' : activity.id)}>
                  {scheduleId === activity.id ? 'Cancel' : 'Schedule'}
                </button>
                {scheduleId === activity.id && (
                  <form onSubmit={event => scheduleActivity(event, activity.id)} className="form-grid ap-schedule-form">
                    <label>Date and Time
                      <input type="datetime-local" value={scheduleTime}
                             min={tripStartDate ? `${tripStartDate.slice(0, 10)}T00:00` : undefined}
                             max={tripEndDate ? `${tripEndDate.slice(0, 10)}T23:59` : undefined}
                             onChange={e => setScheduleTime(e.target.value)} required />
                    </label>
                    <label>Travel Speed (km/h)
                      <input type="number" min="1" step="1" value={speed}
                             onChange={e => setSpeed(e.target.value)} required />
                    </label>
                    <button className="btn-primary" type="submit" disabled={scheduling}>
                      {scheduling ? 'Scheduling...' : 'Add to Calendar'}
                    </button>
                  </form>
                )}
              </li>
            );
          })}
        </ul>
      )}

        <h3>Choose Activities by Budget and Time</h3>

        <form onSubmit={selectActivities} className="form-grid ap-selection-form">
          <label className="ap-checkbox">
            <input type="checkbox" checked={unlimitedBudget}
                   onChange={event => {
                     setUnlimitedBudget(event.target.checked);
                     setSelection(null);
                   }} />
            No budget limit
          </label>
          {unlimitedBudget ? (
            <p className="text-muted" role="status">Only available time limits this selection.</p>
          ) : (
            <>
              <label htmlFor="selection-budget">Maximum Budget (€)</label>
              <input
                  id="selection-budget"
                  type="number"
                  min="0"
                  step="0.01"
                  value={budget}
                  onChange={e => setBudget(e.target.value)}
                  required
              />
            </>
          )}

          <label htmlFor="selection-hours">Available Hours</label>
          <input
              id="selection-hours"
              type="number"
              min="0"
              step="0.5"
              value={hours}
              onChange={e => setHours(e.target.value)}
              required
          />

          <button
              type="submit"
              className="btn-primary"
              disabled={selecting || activities.length === 0}
          >
            {selecting ? 'Selecting...' : 'Find Activities'}
          </button>
        </form>
      {selection && (
        <div className="ap-selection-results">
          <h3>Selected Activities ({selection.length})</h3>
          <p className="text-muted">Total: {formatMoney(selectedCost)} · {selectedHours.toFixed(1)} hours</p>
          {selection.length === 0 ? <p className="text-muted">No activities fit these limits.</p> : (
            <ul className="list">
              {selection.map(activity => (
                <li className="list-item" key={activity.id}>
                  <span>{activity.name}</span>
                  <span className="text-muted">{formatMoney(activity.estimatedCost)} · {formatDuration(activity.duration)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </section>
  );
}
