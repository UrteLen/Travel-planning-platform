import { useEffect, useState } from 'react';
import { readError } from '../apiError';

const eur = value => `€${Number(value || 0).toFixed(2)}`;

export default function SummaryPanel({ apiBase, trip, summary, plan, expenses }) {
    const [exporting, setExporting] = useState(false);
    const [exportError, setExportError] = useState('');
    const [payments, setPayments] = useState(null);
    const [paymentsError, setPaymentsError] = useState('');

  useEffect(() => {
    let cancelled = false;
    fetch(`${apiBase}/budget/settlement?tripCode=${encodeURIComponent(trip.inviteCode)}`)
      .then(async res => {
        if (!res.ok) throw new Error(await readError(res));
        return res.json();
      })
      .then(data => { if (!cancelled) setPayments(data); })
      .catch(err => { if (!cancelled) setPaymentsError(err.message); });
    return () => { cancelled = true; };
  }, [apiBase, trip.inviteCode]);

  async function exportPdf() {
     setExporting(true);
     setExportError('');
     try {
       const res = await fetch(`${apiBase}/budget/export/${trip.id}`);
       if (!res.ok) throw new Error(await readError(res));
       const blob = await res.blob();
       const url = URL.createObjectURL(blob);
       const link = document.createElement('a');
       link.href = url;
       link.download = `trip-summary-${trip.name || trip.id}.pdf`;
       document.body.appendChild(link);
       link.click();
       link.remove();
       URL.revokeObjectURL(url);
     } catch (err) {
       setExportError(err.message);
     } finally {
       setExporting(false);
     }
  }

  const results = summary?.results || [];
  const totalPlanned = results.reduce((sum, r) => sum + r.limit, 0);
  const totalSpent = results.reduce((sum, r) => sum + r.actualSpent, 0);
  const warnings = [];
  if (summary && results.length === 0) warnings.push('No category limits are set, so the budget part of the PDF will be empty.');
  if (plan.length === 0) warnings.push('No itinerary has been generated, so the PDF itinerary will be empty.');
  if (expenses.length === 0) warnings.push('No expenses are recorded yet.');

  return (
    <div>
      <div className="summary-header">
        <div>
          <h2>Trip Summary</h2>
          <p className="text-muted">{trip.name} · {trip.destination}</p>
        </div>
        <button className="btn-primary" onClick={exportPdf} disabled={exporting}>
          {exporting ? 'Exporting...' : 'Export PDF'}
        </button>
      </div>

      {exportError && <p className="panel-error">{exportError}</p>}
      {warnings.length > 0 && (
        <div className="alert-list">
          {warnings.map(w => <div key={w} className="alert-box warning">{w}</div>)}
        </div>
      )}

      {!summary ? (
        <p className="text-muted">Loading budget summary...</p>
      ) : (
        <>
          <div className="stats-grid">
            <div className="stat-box"><h3>Planned budget</h3><p className="stat-value">{eur(totalPlanned)}</p></div>
            <div className="stat-box"><h3>Actual spend</h3><p className="stat-value">{eur(totalSpent)}</p></div>
            <div className="stat-box"><h3>Cost per person</h3><p className="stat-value">{eur(summary.costPerPerson)}</p></div>
          </div>

          <div className="summary-section">
            <h3>Budget by category</h3>
            <table className="data-table">
              <thead><tr><th>Category</th><th>Limit</th><th>Spent</th><th>Status</th></tr></thead>
              <tbody>
                {results.map(r => (
                  <tr key={r.category}>
                    <td><strong>{r.category}</strong></td>
                    <td>{eur(r.limit)}</td>
                    <td>{eur(r.actualSpent)}</td>
                    <td><span className={`badge ${r.status.toLowerCase()}`}>{r.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <div className="summary-section">
        <h3>Timetable</h3>
        {plan.length > 0 ? (
          <ul className="list">
            {plan.map((v, i) => (
              <li key={i} className="list-item">
                <strong>{v.name}</strong>
                <span>
                  {new Date(v.arrivalTime).toLocaleString([], { weekday: 'short', hour: '2-digit', minute: '2-digit' })}
                  {' – '}
                  {new Date(v.departureTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
              </li>
            ))}
          </ul>
        ) : <p className="text-muted">No itinerary generated yet.</p>}
      </div>

      <div className="summary-section">
        <h3>Expenses</h3>
        {expenses.length > 0 ? (
          <ul className="list">
            {expenses.map((exp, i) => (
              <li key={i} className="list-item">
                <span><strong>{eur(exp.amount)}</strong> ({exp.category}) paid by <em>{exp.participant?.name || 'Unknown'}</em></span>
                <span className="text-muted">{new Date(exp.date).toLocaleDateString()}</span>
              </li>
            ))}
          </ul>
        ) : <p className="text-muted">No expenses recorded yet.</p>}
      </div>

      <div className="summary-section">
        <h3>Payments</h3>
        {paymentsError && <p className="panel-error">{paymentsError}</p>}
        {payments === null && !paymentsError && <p className="text-muted">Calculating payments...</p>}
        {payments !== null && payments.length === 0 && <p className="text-muted">No payments needed.</p>}
        {payments !== null && payments.length > 0 && (
          <ul className="list">
            {payments.map((p, i) => (
              <li key={i} className="list-item settlement-item">
                <span><strong>{p.from}</strong> pays <strong>{p.to}</strong></span>
                <span className="settlement-amount">{eur(p.amount)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}