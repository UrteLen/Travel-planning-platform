import { useState, useRef } from 'react';
import './App.css';

const API_BASE = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL.replace(/\/$/, '')}/api/budgetapi`
  : 'http://localhost:5154/api/budgetapi';
  
function App() {
  const featuresRef = useRef(null);

  const [participants, setParticipants] = useState(2);
  const [foodLimit, setFoodLimit] = useState(200);
  const [transportLimit, setTransportLimit] = useState(100);
  const [tripCreated, setTripCreated] = useState(false);

  const [expenseAmount, setExpenseAmount] = useState('');
  const [expenseCategory, setExpenseCategory] = useState('Food');
  const [summary, setSummary] = useState(null);

  function scrollToFeatures() {
    featuresRef.current?.scrollIntoView({ behavior: 'smooth' });
  }

  function createTrip() {
    fetch(`${API_BASE}/create`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        participants,
        categoryLimits: { Food: foodLimit, Transport: transportLimit }
      })
    })
      .then(res => res.json())
      .then(() => setTripCreated(true));
  }

  function addExpense() {
    fetch(`${API_BASE}/expense`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        amount: parseFloat(expenseAmount),
        category: expenseCategory
      })
    })
      .then(res => res.json())
      .then(() => {
        setExpenseAmount('');
        fetchSummary();
      });
  }

  function fetchSummary() {
    fetch(`${API_BASE}/summary`)
      .then(res => res.json())
      .then(data => setSummary(data));
  }

  function statusClass(status) {
    if (status === 'Exceeded') return 'status-exceeded';
    if (status === 'Warning') return 'status-warning';
    return 'status-ok';
  }

  return (
    <div>
      <nav className="navbar">
        <div className="nav-links">
          <button className="link" onClick={scrollToFeatures}>Features</button>
          <a href="#">Pricing</a>
          <a href="#">Blog</a>
          <a href="#">About</a>
        </div>
        <div className="nav-right">
          <a className="login-link" href="#">Log in</a>
          <button className="btn-primary" onClick={scrollToFeatures}>Start a Trip</button>
        </div>
      </nav>

      <section className="hero">
        <div>
          <h1>Plan Group Trips<br /><span className="accent">Without the Chaos</span> —</h1>
          <p className="desc">
            Real-time itinerary optimization, automatic expense splitting, schedule
            conflict alerts, and collaborative voting for stress-free group travel.
          </p>
          <div className="hero-buttons">
            <button className="btn-fill" onClick={scrollToFeatures}>Start a New Trip</button>
            <button className="btn-outline">Join Existing Trip</button>
          </div>
          <div className="invite-box">
            <div className="label">🔗 Have an Invite Code?</div>
            <input placeholder="Enter 6-digit trip code..." />
          </div>
        </div>

        <div className="browser-mock">
          <div className="browser-bar">
            <span className="dot-red"></span>
            <span className="dot-yellow"></span>
            <span className="dot-green"></span>
            <span className="browser-url">trips/barcelona-2025</span>
          </div>
          <div className="browser-body">
            <div className="trip-info">
              <h3>Barcelona Summer Trip</h3>
              <p>Jul 14 – 21, 2025 · 5 members</p>
              <div className="day-tabs">
                <span className="day-tab active">Mon 14</span>
                <span className="day-tab">Tue 15</span>
                <span className="day-tab">Wed 16</span>
                <span className="day-tab">Thu 17</span>
              </div>
              <div className="visit-row">
                <span className="visit-time">09:00</span>
                <div>
                  <div className="visit-name">Sagrada Família</div>
                  <div className="visit-tag confirmed">Culture · ✓ Confirmed</div>
                </div>
              </div>
              <div className="visit-row">
                <span className="visit-time">11:30</span>
                <div>
                  <div className="visit-name">La Barceloneta</div>
                  <div className="visit-tag">Leisure · Voting 3/5</div>
                </div>
              </div>
              <div className="visit-row">
                <span className="visit-time">13:00</span>
                <div>
                  <div className="visit-name">La Mar Salada</div>
                  <div className="visit-tag confirmed">Food · ✓ Confirmed</div>
                </div>
              </div>
            </div>
            <div className="budget-panel">
              <div className="label">Budget</div>
              <div className="amount">€2 680</div>
              <div className="label" style={{marginBottom: '1rem'}}>of €3 200 total · 84%</div>

              <div className="bar-row">
                <div className="bar-label"><span>Flights</span><span>€1200</span></div>
                <div className="bar-track"><div className="bar-fill" style={{width: '90%', background: '#60a5fa'}}></div></div>
              </div>
              <div className="bar-row">
                <div className="bar-label"><span>Hotels</span><span>€820</span></div>
                <div className="bar-track"><div className="bar-fill" style={{width: '70%', background: '#2dd4bf'}}></div></div>
              </div>
              <div className="bar-row">
                <div className="bar-label"><span>Food</span><span>€380</span></div>
                <div className="bar-track"><div className="bar-fill" style={{width: '50%', background: '#facc15'}}></div></div>
              </div>
              <div className="bar-row">
                <div className="bar-label"><span>Activities</span><span>€280</span></div>
                <div className="bar-track"><div className="bar-fill" style={{width: '40%', background: '#a78bfa'}}></div></div>
              </div>

              <div className="label" style={{marginTop: '1.5rem'}}>Remaining</div>
              <div className="amount" style={{color: '#2dd4bf', fontSize: '1.3rem'}}>€520</div>
            </div>
          </div>
        </div>
      </section>

      <div className="section-title" ref={featuresRef}>
        <h2>Everything you need</h2>
      </div>

      <div className="feature-grid">
        <div className="feature-card">
          <div className="feature-icon icon-teal">🔀</div>
          <h3>Smart Itinerary & Conflict Checking</h3>
          <div className="fc-desc">Never overlap bookings — automatic travel time and opening hours validation.</div>
          <div className="inner-panel">
            <div className="mock-line"><span>09:00 Sagrada Família</span><span>Confirmed</span></div>
            <div className="mock-line"><span>11:30 La Barceloneta</span><span>45min gap missing</span></div>
          </div>
        </div>

        <div className="feature-card active-card">
          <div className="feature-icon icon-blue">🖨️</div>
          <h3>Fair Expense Splitting</h3>
          <div className="fc-desc">Track group spending, category limits, and cost-per-person calculation.</div>

          <div className="inner-panel">
            {!tripCreated ? (
              <div>
                <div className="field-row">
                  <label>Number of participants</label>
                  <input type="number" value={participants} onChange={e => setParticipants(parseInt(e.target.value))} />
                </div>
                <div className="field-row">
                  <label>Food budget limit (€)</label>
                  <input type="number" value={foodLimit} onChange={e => setFoodLimit(parseFloat(e.target.value))} />
                </div>
                <div className="field-row">
                  <label>Transport budget limit (€)</label>
                  <input type="number" value={transportLimit} onChange={e => setTransportLimit(parseFloat(e.target.value))} />
                </div>
                <button onClick={createTrip}>Create Trip</button>
              </div>
            ) : (
              <div>
                <div className="field-row">
                  <label>Expense amount (€)</label>
                  <input type="number" placeholder="e.g. 45.00" value={expenseAmount} onChange={e => setExpenseAmount(e.target.value)} />
                </div>
                <div className="field-row">
                  <label>Category</label>
                  <select value={expenseCategory} onChange={e => setExpenseCategory(e.target.value)}>
                    <option value="Food">Food</option>
                    <option value="Transport">Transport</option>
                  </select>
                </div>
                <button onClick={addExpense}>Add Expense</button>
                <button onClick={fetchSummary} style={{marginLeft: '0.5rem'}}>Refresh</button>

                {summary && (
                  <div style={{ marginTop: '1rem' }}>
                    <div className="result-header">
                      <span>Category</span>
                      <span>Spent / Limit</span>
                      <span>Status</span>
                    </div>
                    {summary.results.map((r, i) => (
                      <div className="result-line" key={i}>
                        <span>{r.category}</span>
                        <span>€{r.actualSpent} / €{r.limit}</span>
                        <span className={statusClass(r.status)}>{r.status}</span>
                      </div>
                    ))}
                    <div className="result-line">
                      <strong>Cost per person</strong>
                      <strong>€{summary.costPerPerson}</strong>
                      <span></span>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        <div className="feature-card">
          <div className="feature-icon icon-purple">👍</div>
          <h3>Group Voting & Auto-Planning</h3>
          <div className="fc-desc">Propose activities, vote together, and let AI build the optimal daily route.</div>
          <div className="inner-panel">
            <div className="mock-line"><span>Park Güell</span><span>👍 3 · 👎 1</span></div>
          </div>
        </div>

        <div className="feature-card">
          <div className="feature-icon icon-orange">🛡️</div>
          <h3>Role-Based Permissions</h3>
          <div className="fc-desc">Organiser, Participant, or View-Only access controls for seamless collaboration.</div>
          <div className="inner-panel">
            <div className="mock-line"><span>Alice Chen</span><span>Organiser</span></div>
            <div className="mock-line"><span>Bob Martinez</span><span>Participant</span></div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;