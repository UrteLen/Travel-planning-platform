import { useEffect, useState } from 'react';
import { readError } from '../apiError';

const CATEGORIES = ['Documents', 'Clothes', 'Health', 'Other'];

export default function ChecklistPanel({ apiBase, trip }) {
  const [checklist, setChecklist] = useState(null);
  const [error, setError] = useState('');
  const [text, setText] = useState('');
  const [category, setCategory] = useState('Other');
  const [busy, setBusy] = useState(false);
  const [reload, setReload] = useState(0);

  const url = `${apiBase}/trips/${trip.id}/checklist`;

  useEffect(() => {
    let cancelled = false;
    fetch(url)
      .then(async res => {
        if (!res.ok) throw new Error(await readError(res));
        return res.json();
      })
      .then(data => {
        if (!cancelled) {
          setChecklist(data);
          setError('');
        }
      })
      .catch(err => { if (!cancelled) setError(err.message); });
    return () => { cancelled = true; };
  }, [url, reload]);

  async function send(path, method, body) {
    setBusy(true);
    try {
      const res = await fetch(`${url}${path}`, {
        method,
        headers: body ? { 'Content-Type': 'application/json' } : undefined,
        body: body ? JSON.stringify(body) : undefined
      });
      if (!res.ok) throw new Error(await readError(res));
      setError('');
      setReload(count => count + 1);
      return true;
    } catch (err) {
      setError(err.message);
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function addItem(e) {
    e.preventDefault();
    if (await send('', 'POST', { text, category })) {
      setText('');
    }
  }

  function toggleItem(item) {
    return send(`/${item.id}/done`, 'PUT', { isDone: !item.isDone });
  }

  function removeItem(item) {
    return send(`/${item.id}`, 'DELETE');
  }

  return (
    <div>
      <h2>My checklist</h2>
      {error && <p className="panel-error">{error}</p>}

      <form onSubmit={addItem} className="form-grid">
        <label className="form-field">Item
          <input value={text} maxLength={100} onChange={e => setText(e.target.value)} required />
        </label>
        <label className="form-field">Category
          <select value={category} onChange={e => setCategory(e.target.value)}>
            {CATEGORIES.map(name => <option key={name} value={name}>{name}</option>)}
          </select>
        </label>
        <button type="submit" className="btn-primary" disabled={busy}>Add</button>
      </form>

      {checklist === null && !error && <p className="text-muted">Loading checklist...</p>}

      {checklist !== null && (
        <>
          <p>{checklist.doneCount} of {checklist.totalCount} ready ({checklist.percentDone}%)</p>
          <progress value={checklist.doneCount} max={Math.max(checklist.totalCount, 1)} />

          {checklist.totalCount === 0 && (
            <p className="text-muted">Your checklist is empty. Add your first item above.</p>
          )}

          {checklist.groups.map(group => (
            <div key={group.category}>
              <h3>{group.category}</h3>
              <ul className="list">
                {group.items.map(item => (
                  <li key={item.id} className="list-item">
                    <label>
                      <input
                        type="checkbox"
                        checked={item.isDone}
                        disabled={busy}
                        onChange={() => toggleItem(item)}
                      />{' '}
                      <span style={item.isDone ? { textDecoration: 'line-through', opacity: 0.6 } : undefined}>
                        {item.text}
                      </span>
                    </label>
                    <button
                      type="button"
                      className="btn-secondary"
                      disabled={busy}
                      onClick={() => removeItem(item)}
                    >
                      Delete
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </>
      )}
    </div>
  );
}