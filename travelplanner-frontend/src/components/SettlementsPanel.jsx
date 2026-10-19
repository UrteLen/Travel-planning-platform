import { useEffect, useState } from 'react';
import { readError } from '../apiError';

const eur = value => `€${Number(value || 0).toFixed(2)}`;

export default function SettlementsPanel({ apiBase, trip }) {
  const [payments, setPayments] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    fetch(`${apiBase}/budget/settlement?tripCode=${encodeURIComponent(trip.inviteCode)}`)
      .then(async res => {
        if (!res.ok) throw new Error(await readError(res));
        return res.json();
      })
      .then(data => { if (!cancelled) setPayments(data); })
      .catch(err => { if (!cancelled) setError(err.message); });
    return () => { cancelled = true; };
  }, [apiBase, trip.inviteCode]);

    return (
    <div>
      <h2>Balances & Settlements</h2>
      {error && <p className="panel-error">{error}</p>}
      {payments === null && !error && <p className="text-muted">Calculating payments...</p>}
      {payments !== null && payments.length === 0 && <p className="text-muted">Everyone is settled up.</p>}
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
  );
}