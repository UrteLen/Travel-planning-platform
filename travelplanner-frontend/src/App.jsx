import { useState } from 'react';
import './App.css';

const API_BASE = 'http://localhost:5154/api';

export default function App() {
  const [currentTrip, setCurrentTrip] = useState(null);
  const [activeTab, setActiveTab] = useState('overview');

  const [createForm, setCreateForm] = useState({ name: '', destination: '', startDate: '', endDate: '', organizerName: '' });
  const [joinForm, setJoinForm] = useState({ inviteCode: '', participantName: '' });
  
  const [balances, setBalances] = useState({});
  const [settlements, setSettlements] = useState([]);
  const [expenseForm, setExpenseForm] = useState({ amount: '', category: 'Food', participantId: '' });
  const [categoryLimits, setCategoryLimits] = useState({ Food: 100, Transport: 100, Activities: 100 });
  const [newParticipantName, setNewParticipantName] = useState('');
  
  const [planForm, setPlanForm] = useState({ startLat: 54.6872, startLon: 25.2797, startTime: new Date().toISOString().slice(0, 16), speedKmh: 50, changedBy: 'Organizer' });
  const [planResult, setPlanResult] = useState([]);

  function handleCreateTrip(e) {
    e.preventDefault();
    fetch(`${API_BASE}/trip/create`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(createForm)
    })
      .then(res => res.json())
      .then(data => {
        setCurrentTrip(data);
        fetchTripDetails(data.id);
      })
      .catch(err => alert('Failed to create trip: ' + err.message));
  }

  function handleJoinTrip(e) {
    e.preventDefault();
    fetch(`${API_BASE}/trip/join`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(joinForm)
    })
      .then(res => {
        if (!res.ok) throw new Error('Trip not found or invalid code');
        return res.json();
      })
      .then(data => {
        setCurrentTrip(data);
        fetchTripDetails(data.id);
      })
      .catch(err => alert(err.message));
  }

  function fetchTripDetails(tripId) {
    fetch(`${API_BASE}/budget/${tripId}/balances`)
      .then(res => res.json())
      .then(data => setBalances(data))
      .catch(err => console.error('Error fetching balances:', err));

    fetch(`${API_BASE}/budget/${tripId}/settlements`)
      .then(res => res.json())
      .then(data => setSettlements(data))
      .catch(err => console.error('Error fetching settlements:', err));
      
    fetch(`${API_BASE}/plan/${tripId}`)
      .then(res => res.json())
      .then(data => setPlanResult(data))
      .catch(err => console.error('Error fetching plan:', err));
  }

  function handleAddExpense(e) {
    e.preventDefault();
    if (!expenseForm.participantId) {
      alert('Please select a participant who paid.');
      return;
    }

    fetch(`${API_BASE}/budget/expense`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        tripCode: currentTrip.inviteCode,
        amount: parseFloat(expenseForm.amount),
        category: expenseForm.category,
        participantId: expenseForm.participantId
      })
    })
      .then(async res => {
        if (!res.ok) throw new Error('Failed to add expense');
        
        const newExpense = {
          amount: parseFloat(expenseForm.amount),
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
        fetchTripDetails(currentTrip.id);
      })
      .catch(err => alert(err.message));
  }

  function handleSetLimits(e) {
    e.preventDefault();
    fetch(`${API_BASE}/budget/limits`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        tripCode: currentTrip.inviteCode,
        categoryLimits
      })
    })
      .then(() => {
        fetchTripDetails(currentTrip.id);
        alert('Category limits updated!');
      })
      .catch(err => alert('Failed to update limits: ' + err.message));
  }

  function handleAddParticipant(e) {
    e.preventDefault();
    fetch(`${API_BASE}/trip/join`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        inviteCode: currentTrip.inviteCode,
        participantName: newParticipantName
      })
    })
      .then(res => res.json())
      .then(data => {
        setCurrentTrip(data);
        setNewParticipantName('');
        fetchTripDetails(data.id);
      })
      .catch(err => alert('Failed to add participant: ' + err.message));
  }

  function handleGeneratePlan(e) {
    e.preventDefault();
    fetch(`${API_BASE}/plan/${currentTrip.id}/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        startLatitude: parseFloat(planForm.startLat),
        startLongitude: parseFloat(planForm.startLon),
        startTime: planForm.startTime,
        speedKmh: parseFloat(planForm.speedKmh),
        changedBy: planForm.changedBy
      })
    })
      .then(res => res.json())
      .then(data => {
        setPlanResult(data);
        fetchTripDetails(currentTrip.id);
      })
      .catch(err => alert('Failed to generate plan: ' + err.message));
  }

  // Calculate category spending locally to ensure instant rendering without API blocking
  const expenses = currentTrip?.budget?.expenses || [];
  const categoryResults = Object.keys(categoryLimits).map(cat => {
    const actualSpent = expenses
      .filter(exp => exp.category.toLowerCase() === cat.toLowerCase())
      .reduce((sum, exp) => sum + Number(exp.amount), 0);
    const limit = categoryLimits[cat] || 0;
    
    let status = 'Ok';
    if (actualSpent > limit) {
      status = 'Exceeded';
    } else if (actualSpent >= limit * 0.9 && limit > 0) {
      status = 'Warning';
    }

    return { category: cat, limit, actualSpent, status };
  });

  const totalSpent = categoryResults.reduce((acc, r) => acc + r.actualSpent, 0);
  const totalPlanned = categoryResults.reduce((acc, r) => acc + r.limit, 0);
  const participantCount = Object.keys(currentTrip?.participants || {}).length;
  const costPerPerson = participantCount > 0 ? totalSpent / participantCount : 0;

  if (!currentTrip) {
    return (
      <div className="container">
        <h1>Travel Planner</h1>
        <p className="subtitle">Organize group trips, track expenses, and build optimized itineraries easily.</p>
        
        <div className="card">
          <h2>Create New Trip</h2>
          <form onSubmit={handleCreateTrip} className="form-grid">
            <input placeholder="Trip Name" value={createForm.name} onChange={e => setCreateForm({...createForm, name: e.target.value})} required />
            <input placeholder="Destination" value={createForm.destination} onChange={e => setCreateForm({...createForm, destination: e.target.value})} required />
            <input type="date" value={createForm.startDate} onChange={e => setCreateForm({...createForm, startDate: e.target.value})} required />
            <input type="date" value={createForm.endDate} onChange={e => setCreateForm({...createForm, endDate: e.target.value})} required />
            <input placeholder="Your Name (Organizer)" value={createForm.organizerName} onChange={e => setCreateForm({...createForm, organizerName: e.target.value})} required />
            <button type="submit" className="btn-primary">Create Trip</button>
          </form>
        </div>

        <div className="card">
          <h2>Join Existing Trip</h2>
          <form onSubmit={handleJoinTrip} className="form-grid">
            <input placeholder="6-Digit Invite Code" value={joinForm.inviteCode} onChange={e => setJoinForm({...joinForm, inviteCode: e.target.value})} required />
            <input placeholder="Your Name" value={joinForm.participantName} onChange={e => setJoinForm({...joinForm, participantName: e.target.value})} required />
            <button type="submit" className="btn-secondary">Join Trip</button>
          </form>
        </div>
      </div>
    );
  }

  const participantsList = Object.values(currentTrip.participants || {});

  return (
    <div className="container">
      <div className="header-bar">
        <div>
          <h1>{currentTrip.name}</h1>
          <p className="trip-meta">Destination: <strong>{currentTrip.destination}</strong> | Invite Code: <span className="code-badge">{currentTrip.inviteCode}</span></p>
        </div>
        <button onClick={() => setCurrentTrip(null)} className="btn-secondary">Switch Trip</button>
      </div>

      <div className="tabs">
        {['overview', 'participants', 'categories', 'expenses', 'itinerary', 'calendar', 'settlements'].map(tab => (
          <button 
            key={tab} 
            onClick={() => setActiveTab(tab)}
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
              <button type="submit" className="btn-primary">Add Participant</button>
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
                    onChange={e => setCategoryLimits({...categoryLimits, [cat]: parseFloat(e.target.value) || 0})} 
                  />
                </div>
              ))}
              <button type="submit" className="btn-primary">Save Limits</button>
            </form>

            <div>
              <h3>Spent vs Limit & Status</h3>
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
            </div>
          </div>
        )}

        {activeTab === 'expenses' && (
          <div>
            <h2>Record an Expense</h2>
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
              </select>
              <select value={expenseForm.participantId} onChange={e => setExpenseForm({...expenseForm, participantId: e.target.value})} required>
                <option value="">Select Who Paid</option>
                {participantsList.map(p => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
              <button type="submit" className="btn-primary">Add Expense</button>
            </form>

            <div style={{ marginBottom: '30px' }}>
              <h3>Category Spending Summary</h3>
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
            </div>

            <h3>Expense History</h3>
            {expenses.length > 0 ? (
              <ul className="list">
                {expenses.map((exp, idx) => (
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
            <form onSubmit={handleGeneratePlan} className="form-grid" style={{ marginBottom: '20px' }}>
              <input type="number" step="any" placeholder="Start Latitude" value={planForm.startLat} onChange={e => setPlanForm({...planForm, startLat: e.target.value})} required />
              <input type="number" step="any" placeholder="Start Longitude" value={planForm.startLon} onChange={e => setPlanForm({...planForm, startLon: e.target.value})} required />
              <input type="datetime-local" value={planForm.startTime} onChange={e => setPlanForm({...planForm, startTime: e.target.value})} required />
              <input type="number" placeholder="Speed (km/h)" value={planForm.speedKmh} onChange={e => setPlanForm({...planForm, speedKmh: e.target.value})} required />
              <button type="submit" className="btn-primary" style={{ gridColumn: '1 / -1' }}>Generate Optimized Plan</button>
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
          </div>
        )}

        {activeTab === 'calendar' && (
          <div>
            <h2>Trip Calendar View</h2>
            <p className="text-muted" style={{ marginBottom: '20px' }}>Schedule overview from {new Date(currentTrip.startDate).toLocaleDateString()} to {new Date(currentTrip.endDate).toLocaleDateString()}</p>
            
            <div className="calendar-box" style={{ background: '#f1f5f9', padding: '20px', borderRadius: '8px', border: '1px solid #cbd5e1' }}>
              <h3>Planned Timeline</h3>
              {planResult.length > 0 ? (
                <ul className="list" style={{ marginTop: '10px' }}>
                  {planResult.map((visit, idx) => (
                    <li key={idx} className="list-item" style={{ background: '#fff', marginBottom: '8px', borderRadius: '6px' }}>
                      <div>
                        <strong>{visit.name}</strong>
                        <div className="text-muted">{new Date(visit.arrivalTime).toLocaleString()}</div>
                      </div>
                      <span className="badge ok">Scheduled</span>
                    </li>
                  ))}
                </ul>
              ) : <p className="text-muted">No calendar events scheduled. Generate an itinerary plan first.</p>}
            </div>
          </div>
        )}

        {activeTab === 'settlements' && (
          <div>
            <h2>Balances & Settlements</h2>
            
            <h3>Individual Balances</h3>
            <ul className="list" style={{ marginBottom: '25px' }}>
              {Object.keys(balances).length > 0 ? Object.entries(balances).map(([id, balance]) => {
                const participant = currentTrip.participants[id];
                return (
                  <li key={id} className="list-item">
                    <span>{participant ? participant.name : 'Unknown'}</span>
                    <strong style={{ color: balance >= 0 ? '#10b981' : '#ef4444' }}>
                      {balance >= 0 ? `+€${balance.toFixed(2)}` : `-€${Math.abs(balance).toFixed(2)}`}
                    </strong>
                  </li>
                );
              }) : <p className="text-muted">No balances calculated yet. Add some expenses!</p>}
            </ul>

            <h3>Suggested Debt Settlements</h3>
            {settlements.length > 0 ? (
              <ul className="list">
                {settlements.map((s, idx) => {
                  const fromName = currentTrip.participants[s.fromParticipantId]?.name || 'Someone';
                  const toName = currentTrip.participants[s.toParticipantId]?.name || 'Someone';
                  return (
                    <li key={idx} className="list-item settlement-item">
                      <span><strong>{fromName}</strong> pays <strong>{toName}</strong></span>
                      <span className="settlement-amount">€{s.amount.toFixed(2)}</span>
                    </li>
                  );
                })}
              </ul>
            ) : <p className="text-muted">All debts are settled or no expenses logged yet!</p>}
          </div>
        )}
      </div>
    </div>
  );
}