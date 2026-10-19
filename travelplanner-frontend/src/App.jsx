import { useState, useRef } from 'react';
import './App.css';
import ActivitiesPanel from './components/ActivitiesPanel';
import CalendarPanel from './components/CalendarPanel';
import HistoryPanel from './components/HistoryPanel';
import SummaryPanel from './components/SummaryPanel';
import BudgetAlerts from './components/BudgetAlerts';
import { readError } from './apiError';

const API_BASE = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL.replace(/\/$/, '')}/api`
  : 'http://localhost:5154/api';

export default function App() {
  const [currentTrip, setCurrentTrip] = useState(null);
  const [activeTab, setActiveTab] = useState('overview');
  const [activeParticipantId, setActiveParticipantId] = useState('');
  const [scheduleRevision, setScheduleRevision] = useState(0);
  const [calendarFocus, setCalendarFocus] = useState('');

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  const [createForm, setCreateForm] = useState({ name: '', destination: '', startDate: '', endDate: '', organizerName: '' });
  const [joinForm, setJoinForm] = useState({ inviteCode: '', participantName: '' });
  
  const [summary, setSummary] = useState(null);
  const [expenseForm, setExpenseForm] = useState({ amount: '', category: 'Food', participantId: '' });
  
  const [categoryLimits, setCategoryLimits] = useState({ Food: 100, Transport: 100, Activities: 100, Accommodation: 100 });
  const [newParticipantName, setNewParticipantName] = useState('');
  
  const [planForm, setPlanForm] = useState({ startLat: 54.6872, startLon: 25.2797, startTime: new Date().toISOString().slice(0, 16), speedKmh: 50, changedBy: 'Organizer' });
  const [planResult, setPlanResult] = useState([]);

  const requestIdRef = useRef(0);

  function handleTripLoaded(tripData, preferredParticipantName = '') {
    const people = Object.values(tripData?.participants || []);

    const preferred = preferredParticipantName
        ? people.filter(
            person => person.name.toLowerCase() === preferredParticipantName.toLowerCase()
        ).at(-1)
        : null;

    const selected =
        preferred ||
        people.find(person => person.id === activeParticipantId) ||
        people[0];

    setActiveParticipantId(selected?.id || '');
    setCurrentTrip(tripData);
    setCalendarFocus('');
    setErrorMessage('');
    
    if (tripData?.budget?.categoryLimits) {
      setCategoryLimits(prev => ({
        ...prev,
        ...tripData.budget.categoryLimits
      }));
    }

    if (tripData?.id && tripData?.inviteCode) {
      fetchTripDetails(tripData.id, tripData.inviteCode);
    }
  }
    async function handleVersionRestored() {
      if (!currentTrip?.id) return;
      try {
        const res = await fetch(`${API_BASE}/trip/${currentTrip.id}`);
        if (!res.ok) throw new Error(await readError(res));
        const freshTrip = await res.json();
        setCategoryLimits({ Food: 100, Transport: 100, Activities: 100, Accommodation: 100 });
        handleTripLoaded(freshTrip);
      } catch (err) {
        setErrorMessage('Failed to reload the restored trip: ' + err.message);
      }
      setActiveTab('overview');
    }

  async function handleCreateTrip(e) {
    e.preventDefault();
    const name = createForm.name.trim();
    const destination = createForm.destination.trim();
    const organizerName = createForm.organizerName.trim();

    if (!name || !destination || !organizerName || !createForm.startDate || !createForm.endDate) {
      setErrorMessage('Please fill out all required trip fields.');
      return;
    }

    const startUtc = new Date(`${createForm.startDate}T00:00:00Z`).getTime();
    const endUtc = new Date(`${createForm.endDate}T00:00:00Z`).getTime();
    if (isNaN(startUtc) || isNaN(endUtc) || endUtc < startUtc) {
      setErrorMessage('End date cannot be earlier than start date.');
      return;
    }

    setLoading(true);
    setErrorMessage('');

    try {
      const res = await fetch(`${API_BASE}/trip/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...createForm, name, destination, organizerName })
      });
      if (!res.ok) throw new Error(await readError(res));
      const data = await res.json();
      handleTripLoaded(data, organizerName);
      setCreateForm({ name: '', destination: '', startDate: '', endDate: '', organizerName: '' });
    } catch (err) {
      setErrorMessage(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleJoinTrip(e) {
    e.preventDefault();
    const inviteCode = joinForm.inviteCode.trim();
    const participantName = joinForm.participantName.trim();

    if (!inviteCode || inviteCode.length < 3) {
      setErrorMessage('Please provide a valid invite code.');
      return;
    }
    if (!participantName) {
      setErrorMessage('Please enter your participant name.');
      return;
    }

    setLoading(true);
    setErrorMessage('');

    try {
      const res = await fetch(`${API_BASE}/trip/join`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ inviteCode, participantName })
      });
      if (!res.ok) throw new Error(await readError(res));
      const data = await res.json();
      handleTripLoaded(data, participantName);
      setJoinForm({ inviteCode: '', participantName: '' });
    } catch (err) {
      setErrorMessage(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function fetchTripDetails(tripId, tripCode) {
    if (!tripId || !tripCode) return;

    const currentRequestId = ++requestIdRef.current;
    setErrorMessage('');

    try {
      const results = await Promise.allSettled([
        fetch(`${API_BASE}/budget/summary?tripCode=${tripCode}`).then(async res => {
          if (!res.ok) throw new Error(await readError(res));
          return res.json();
        }),
        fetch(`${API_BASE}/plan/${tripId}`).then(async res => {
          if (!res.ok) throw new Error(await readError(res));
          return res.json();
        })
      ]);

      if (currentRequestId !== requestIdRef.current) return;

      if (results[0].status === 'fulfilled') setSummary(results[0].value);
      if (results[1].status === 'fulfilled') setPlanResult(results[1].value);

      const failedCount = results.filter(r => r.status === 'rejected').length;
      if (failedCount > 0) {
        setErrorMessage('Warning: Some trip details failed to synchronize from the server.');
      }
    } catch (err) {
      if (currentRequestId === requestIdRef.current) {
        setErrorMessage('Failed to load trip details: ' + err.message);
      }
    }
  }

 async function handleAddExpense(e) {
  e.preventDefault();
  if (!currentTrip?.inviteCode || !currentTrip?.id) {
    setErrorMessage('No active trip selected.');
    return;
  }

  const amountText = String(expenseForm.amount).trim();
  if (!/^\d+(\.\d{1,2})?$/.test(amountText) || parseFloat(amountText) <= 0) {
    setErrorMessage('Enter an amount greater than 0 with at most 2 decimal places.');
    return;
  }
  const amountNum = parseFloat(amountText);

  if (!expenseForm.participantId) {
    setErrorMessage('Please select who paid for the expense.');
    return;
  }

  // limits are only known once the summary has loaded
  const limitMissing = Boolean(summary?.results) &&
    !summary.results.some(r => r.category === expenseForm.category);

  setLoading(true);
  setErrorMessage('');

  try {
    const res = await fetch(`${API_BASE}/budget/expense`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        tripCode: currentTrip.inviteCode,
        amount: amountNum,
        category: expenseForm.category,
        participantId: expenseForm.participantId
      })
    });
    if (!res.ok) throw new Error(await readError(res));

    const newExpense = {
      amount: amountNum,
      category: expenseForm.category,
      date: new Date().toISOString(),
      participant: currentTrip.participants[expenseForm.participantId] || { name: 'Unknown' }
    };

    setCurrentTrip(prev => ({
      ...prev,
      budget: {
        ...prev.budget,
        expenses: [...(prev.budget?.expenses || []), newExpense]
      }
    }));

    setExpenseForm(prev => ({ ...prev, amount: '' }));
    await fetchTripDetails(currentTrip.id, currentTrip.inviteCode);

    
    if (limitMissing) {
      setErrorMessage(`Warning: the expense was saved, but ${expenseForm.category} has no limit set, so it will not show in the budget status. Set a limit on the Categories tab.`);
    }
  } catch (err) {
    setErrorMessage(err.message);
  } finally {
    setLoading(false);
  }
}

  async function handleSetLimits(e) {
    e.preventDefault();
    if (!currentTrip?.inviteCode || !currentTrip?.id) return;

    const sanitizedLimits = {};
    for (const [cat, val] of Object.entries(categoryLimits)) {
      if (val === '' || val === null || isNaN(val) || Number(val) < 0) {
        setErrorMessage(`Invalid limit for category ${cat}. Limits must be numeric and non-negative.`);
        return;
      }
      sanitizedLimits[cat] = Number(val);
    }

    setLoading(true);
    setErrorMessage('');

    try {
      const res = await fetch(`${API_BASE}/budget/limits`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tripCode: currentTrip.inviteCode,
          categoryLimits: sanitizedLimits
        })
      });
      if (!res.ok) throw new Error(await readError(res));

      await fetchTripDetails(currentTrip.id, currentTrip.inviteCode);
    } catch (err) {
      setErrorMessage(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleAddParticipant(e) {
    e.preventDefault();
    if (!currentTrip?.inviteCode) return;
    const participantName = newParticipantName.trim();
    if (!participantName) {
      setErrorMessage('Please provide a valid participant name.');
      return;
    }

    setLoading(true);
    setErrorMessage('');

    try {
      const res = await fetch(`${API_BASE}/trip/join`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          inviteCode: currentTrip.inviteCode,
          participantName
        })
      });
      if (!res.ok) throw new Error(await readError(res));
      const data = await res.json();
      handleTripLoaded(data);
      setNewParticipantName('');
    } catch (err) {
      setErrorMessage(err.message);
    } finally {
      setLoading(false);
    }
  }

async function handleGeneratePlan(e) {
  e.preventDefault();
  if (!currentTrip?.id || !currentTrip?.inviteCode) return;

  const lat = parseFloat(planForm.startLat);
  const lon = parseFloat(planForm.startLon);
  const speed = parseFloat(planForm.speedKmh);

  if (isNaN(lat) || lat < -90 || lat > 90) {
    setErrorMessage('Latitude must be between -90 and 90.');
    return;
  }
  if (isNaN(lon) || lon < -180 || lon > 180) {
    setErrorMessage('Longitude must be between -180 and 180.');
    return;
  }
  if (isNaN(speed) || speed <= 0) {
    setErrorMessage('Speed must be greater than 0 km/h.');
    return;
  }
  if (!planForm.startTime) {
    setErrorMessage('Start time is required.');
    return;
  }

  setLoading(true);
  setErrorMessage('');

  try {
    const res = await fetch(`${API_BASE}/plan/${currentTrip.id}/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        startLatitude: lat,
        startLongitude: lon,
        startTime: planForm.startTime,
        speedKmh: speed,
        changedBy: Object.values(currentTrip.participants || {}).find(p => p.id === activeParticipantId)?.name || planForm.changedBy
      })
    });
    if (!res.ok) throw new Error(await readError(res));
    const data = await res.json();
    setPlanResult(data);
    await fetchTripDetails(currentTrip.id, currentTrip.inviteCode);

    if (data.length === 0) {
      setErrorMessage('Warning: no visits could be scheduled. Add activities first, or check their opening hours and the start time.');
    }
  } catch (err) {
    setErrorMessage(err.message);
  } finally {
    setLoading(false);
  }
}

  const participantsList = Object.values(currentTrip?.participants || {});
  const expensesList = currentTrip?.budget?.expenses || [];
  const categoryResults = summary?.results || null;
  const totalSpent = summary?.totalSpent ?? 0;
  const totalPlanned = categoryResults ? categoryResults.reduce((acc, r) => acc + r.limit, 0) : Object.values(categoryLimits).reduce((a, b) => a + Number(b), 0);
  const costPerPerson = summary?.costPerPerson ?? 0;

  if (!currentTrip) {
    return (
      <div className="container">
        <h1>Travel Planner</h1>
        <p className="subtitle">Organize group trips, track expenses, and build optimized itineraries easily.</p>
        
           {errorMessage && (
     <div className={`error-banner ${errorMessage.startsWith('Warning') ? 'is-warning' : ''}`} role="alert">
       {errorMessage}
     </div>
   )}
        <div className="card">
          <h2>Create New Trip</h2>
          <form onSubmit={handleCreateTrip} className="form-grid">
            <input placeholder="Trip Name" value={createForm.name} onChange={e => setCreateForm({...createForm, name: e.target.value})} required />
            <input placeholder="Destination" value={createForm.destination} onChange={e => setCreateForm({...createForm, destination: e.target.value})} required />
            <input type="date" value={createForm.startDate} onChange={e => setCreateForm({...createForm, startDate: e.target.value})} required />
            <input type="date" value={createForm.endDate} onChange={e => setCreateForm({...createForm, endDate: e.target.value})} required />
            <input placeholder="Your Name (Organizer)" value={createForm.organizerName} onChange={e => setCreateForm({...createForm, organizerName: e.target.value})} required />
            <button type="submit" className="btn-primary" disabled={loading}>{loading ? 'Creating...' : 'Create Trip'}</button>
          </form>
        </div>

        <div className="card">
          <h2>Join Existing Trip</h2>
          <form onSubmit={handleJoinTrip} className="form-grid">
            <input placeholder="6-Digit Invite Code" value={joinForm.inviteCode} onChange={e => setJoinForm({...joinForm, inviteCode: e.target.value})} required />
            <input placeholder="Your Name" value={joinForm.participantName} onChange={e => setJoinForm({...joinForm, participantName: e.target.value})} required />
            <button type="submit" className="btn-secondary" disabled={loading}>{loading ? 'Joining...' : 'Join Trip'}</button>
          </form>
        </div>
      </div>
    );
  }



  return (
    <div className="container">
      <div className="header-bar">
        <div>
          <h1>{currentTrip.name}</h1>
          <p className="trip-meta">Destination: <strong>{currentTrip.destination}</strong> | Invite Code: <span className="code-badge">{currentTrip.inviteCode}</span></p>
        </div>
        <button onClick={() => setCurrentTrip(null)} className="btn-secondary">Switch Trip</button>
      </div>

      {errorMessage && <div className="error-banner">{errorMessage}</div>}

      <div className="tabs">
        {['overview', 'participants', 'categories', 'expenses', 'itinerary', 'calendar', 'settlements', 'history', 'summary'].map(tab => (
          <button 
            key={tab} 
            onClick={() => { setActiveTab(tab); setErrorMessage(''); }}
            className={`tab-btn ${activeTab === tab ? 'active' : ''}`}
          >
            {tab}
          </button>
        ))}
      </div>

      <div className="card content-card">
        {activeTab === 'overview' && (
          <div>
            <h2>Trip Overview</h2>
            <BudgetAlerts results={categoryResults} expenses={expensesList} />
              {summary ? (
              <div className="stats-grid">
                <div className="stat-box">
                  <h3>Total Planned Budget</h3>
                  <p className="stat-value">€{totalPlanned.toFixed(2)}</p>
                </div>
                <div className="stat-box">
                  <h3>Total Actual Spend</h3>
                  <p className="stat-value">€{totalSpent.toFixed(2)}</p>
                </div>
                <div className="stat-box">
                  <h3>Cost Per Person</h3>
                  <p className="stat-value">€{costPerPerson.toFixed(2)}</p>
                </div>
              </div>
            ) : <p className="text-muted">Loading authoritative budget summary from server...</p>}
          </div>
        )}

        {activeTab === 'participants' && (
          <div>
            <h2>Participants ({participantsList.length})</h2>
            <ul className="list">
              {participantsList.map(p => (
                <li key={p.id} className="list-item">
                  <span><strong>{p.name}</strong> ({p.role})</span>
                  <span className="text-muted">{p.email || 'No email provided'}</span>
                </li>
              ))}
            </ul>
            <form onSubmit={handleAddParticipant} className="inline-form" style={{ marginTop: '20px', display: 'flex', gap: '10px' }}>
              <input placeholder="New Participant Name" value={newParticipantName} onChange={e => setNewParticipantName(e.target.value)} required />
              <button type="submit" className="btn-primary" disabled={loading}>{loading ? 'Adding...' : 'Add Participant'}</button>
            </form>
          </div>
        )}

        {activeTab === 'categories' && (
          <div>
            <h2>Category Limits & Budget Status</h2>
            <form onSubmit={handleSetLimits} className="form-grid" style={{ marginBottom: '30px' }}>
              {Object.keys(categoryLimits).map(cat => (
                <div key={cat} className="input-group">
                  <label>{cat} Limit (€):</label>
                  <input 
                    type="number" 
                    value={categoryLimits[cat]} 
                    onChange={e => setCategoryLimits({...categoryLimits, [cat]: e.target.value})} 
                  />
                </div>
              ))}
              <button type="submit" className="btn-primary" disabled={loading}>{loading ? 'Saving...' : 'Save Limits'}</button>
            </form>

            <div>
              <h3>Spent vs Limit & Status</h3>
              {categoryResults ? (
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Category</th>
                      <th>Limit</th>
                      <th>Spent</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {categoryResults.map(r => (
                      <tr key={r.category}>
                        <td><strong>{r.category}</strong></td>
                        <td>€{r.limit.toFixed(2)}</td>
                        <td>€{r.actualSpent.toFixed(2)}</td>
                        <td>
                          <span className={`badge ${r.status.toLowerCase()}`}>
                            {r.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : <p className="text-muted">Loading authoritative category status...</p>}
            </div>
          </div>
        )}

        {activeTab === 'expenses' && (
  <div>
    <h2>Record an Expense</h2>
    <BudgetAlerts results={categoryResults} expenses={expensesList} />
    <form onSubmit={handleAddExpense} className="form-grid" style={{ marginBottom: '30px' }}>
              <input 
                type="number" 
                step="0.01" 
                placeholder="Amount (€)" 
                value={expenseForm.amount} 
                onChange={e => setExpenseForm({...expenseForm, amount: e.target.value})} 
                required 
              />
              <select value={expenseForm.category} onChange={e => setExpenseForm({...expenseForm, category: e.target.value})}>
                <option value="Food">Food</option>
                <option value="Transport">Transport</option>
                <option value="Activities">Activities</option>
                <option value="Accommodation">Accommodation</option>
              </select>
              <select value={expenseForm.participantId} onChange={e => setExpenseForm({...expenseForm, participantId: e.target.value})} required>
                <option value="">Select Who Paid</option>
                {participantsList.map(p => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
              <button type="submit" className="btn-primary" disabled={loading}>{loading ? 'Adding...' : 'Add Expense'}</button>
            </form>

            <div style={{ marginBottom: '30px' }}>
              <h3>Category Spending Summary</h3>
              {categoryResults ? (
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Category</th>
                      <th>Limit</th>
                      <th>Total Spent</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {categoryResults.map(r => (
                      <tr key={r.category}>
                        <td><strong>{r.category}</strong></td>
                        <td>€{r.limit.toFixed(2)}</td>
                        <td>€{r.actualSpent.toFixed(2)}</td>
                        <td>
                          <span className={`badge ${r.status.toLowerCase()}`}>
                            {r.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : <p className="text-muted">Loading authoritative category summary...</p>}
            </div>

            <h3>Expense History</h3>
            {expensesList.length > 0 ? (
              <ul className="list">
                {expensesList.map((exp, idx) => (
                  <li key={idx} className="list-item">
                    <span><strong>€{Number(exp.amount).toFixed(2)}</strong> ({exp.category}) paid by <em>{exp.participant?.name || 'Unknown'}</em></span>
                    <span className="text-muted">{new Date(exp.date).toLocaleDateString()}</span>
                  </li>
                ))}
              </ul>
            ) : <p className="text-muted">No expenses recorded yet.</p>}
          </div>
        )}

        {activeTab === 'itinerary' && (
          <div>
            <h2>Itinerary & Plan Generator</h2>
            <ActivitiesPanel
                tripId={currentTrip.id}
                participants={participantsList}
                participantId={activeParticipantId}
                onParticipantChange={setActiveParticipantId}
                tripStartDate={currentTrip.startDate}
                tripEndDate={currentTrip.endDate}
                defaultLatitude={planForm.startLat}
                defaultLongitude={planForm.startLon}
                onScheduleChange={() => setScheduleRevision(value => value + 1)}
                onOpenCalendar={date => {
                  setCalendarFocus(date || currentTrip.startDate.slice(0, 10));
                  setActiveTab('calendar');
                }}
            />
            <details style={{ marginTop: '30px' }}>
              <summary style={{ cursor: 'pointer', fontWeight: '600', marginBottom: '15px' }}>
                Generate Optimized Route
              </summary>
            <form onSubmit={handleGeneratePlan} className="form-grid" style={{ marginBottom: '20px' }}>
              <input type="number" step="any" placeholder="Start Latitude" value={planForm.startLat} onChange={e => setPlanForm({...planForm, startLat: e.target.value})} required />
              <input type="number" step="any" placeholder="Start Longitude" value={planForm.startLon} onChange={e => setPlanForm({...planForm, startLon: e.target.value})} required />
              <input type="datetime-local" value={planForm.startTime} onChange={e => setPlanForm({...planForm, startTime: e.target.value})} required />
              <input type="number" placeholder="Speed (km/h)" value={planForm.speedKmh} onChange={e => setPlanForm({...planForm, speedKmh: e.target.value})} required />
              <button type="submit" className="btn-primary" style={{ gridColumn: '1 / -1' }} disabled={loading}>{loading ? 'Generating...' : 'Generate Optimized Plan'}</button>
            </form>

            <h3>Scheduled Visits</h3>
            {planResult.length > 0 ? (
              <ul className="list">
                {planResult.map((visit, idx) => (
                  <li key={idx} className="list-item">
                    <strong>{visit.name}</strong> 
                    <span>{new Date(visit.arrivalTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})} – {new Date(visit.departureTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
                  </li>
                ))}
              </ul>
            ) : <p className="text-muted">No itinerary planned yet. Add activities and generate a plan!</p>}
            </details>
          </div>
        )}

        {activeTab === 'calendar' && (
          <div>
            <CalendarPanel
                key={currentTrip.id}
                tripId={currentTrip.id}
                startDate={currentTrip.startDate}
                endDate={currentTrip.endDate}
                focusDate={calendarFocus}
                plannedVisits={planResult}
                revision={scheduleRevision}
            />
          </div>
        )}
        {activeTab === 'history' && (
          <HistoryPanel
            apiBase={API_BASE}
            tripId={currentTrip.id}
            changedBy={participantsList.find(p => p.id === activeParticipantId)?.name || 'Organizer'}
            onRestored={handleVersionRestored}
          />
        )}

        {activeTab === 'summary' && (
          <SummaryPanel
            apiBase={API_BASE}
            trip={currentTrip}
            summary={summary}
            plan={planResult}
            expenses={expensesList}
          />
        )}

        {activeTab === 'settlements' && (
          <div>
            <h2>Balances & Settlements</h2>
            <div className="card" style={{ background: '#f8fafc', border: '1px dashed #cbd5e1', textAlign: 'center', padding: '40px 20px' }}>
              <h3>Settlements Feature Coming Soon</h3>
              <p className="text-muted">Debt calculation and settlement engine is currently being integrated.</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}