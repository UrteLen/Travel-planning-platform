import { useCallback, useEffect, useState } from 'react';
import { readError } from '../apiError';

export default function HistoryPanel({ apiBase, tripId, changedBy, onRestored }) {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busyVersion, setBusyVersion] = useState(null);
  const [message, setMessage] = useState('');

  const load = useCallback(async () => {
    try {
      const res = await fetch(`${apiBase}/plan/${tripId}/history`);
      if (!res.ok) throw new Error(await readError(res));
      setHistory(await res.json());
      setMessage('');
    } catch (err) {
      setMessage(err.message);
    } finally {
      setLoading(false);
    }
  }, [apiBase, tripId]);

  useEffect(() => {
    load();
  }, [load]);

  async function restore(versionNumber) {
    const ok = window.confirm(
      `Restore version ${versionNumber}? This creates a new version, so no history is deleted.`
    );
    if (!ok) return;

    setBusyVersion(versionNumber);
    try {
      const res = await fetch(
        `${apiBase}/plan/${tripId}/restore/${versionNumber}?changedBy=${encodeURIComponent(changedBy)}`,
        { method: 'POST' }
      );
      if (!res.ok) throw new Error(await readError(res));
      if (onRestored) await onRestored();
      await load();
    } catch (err) {
      setMessage(err.message);
    } finally {
      setBusyVersion(null);
    }
  }

  const rows = [...history].sort((a, b) => b.versionNumber - a.versionNumber);

  return (
    <div>
      <h2>Version History</h2>
      <p className="text-muted">
        A version is saved whenever a plan is generated, an expense is added or category limits are saved.
        Restoring a version brings back its budget, plan and activity list as a new version.
      </p>

      {message && <p className="panel-error">{message}</p>}

      {loading ? (
        <p className="text-muted">Loading history...</p>
      ) : rows.length === 0 ? (
        <p className="text-muted">No versions yet.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Version</th>
              <th>When</th>
              <th>Changed by</th>
              <th>Note</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((v, index) => (
              <tr key={v.versionNumber}>
                <td>
                  <strong>v{v.versionNumber}</strong>
                  {index === 0 && <span className="badge ok" style={{ marginLeft: 8 }}>latest</span>}
                </td>
                <td>{new Date(v.timestamp).toLocaleString()}</td>
                <td>{v.changedBy}</td>
                <td>{v.restoredFromVersion ? `Restored from v${v.restoredFromVersion}` : ''}</td>
                <td>
                  {index !== 0 && (
                    <button
                      className="btn-secondary"
                      disabled={busyVersion !== null}
                      onClick={() => restore(v.versionNumber)}
                    >
                      {busyVersion === v.versionNumber ? 'Restoring...' : 'Restore'}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}