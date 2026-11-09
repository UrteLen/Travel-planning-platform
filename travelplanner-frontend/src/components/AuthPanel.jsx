import { useState } from 'react';
import { readError } from '../apiError';

export default function AuthPanel({ apiBase, onAuth }) {
  const [mode, setMode] = useState('login');
  const [form, setForm] = useState({ email: '', displayName: '', password: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      const body = mode === 'login'
        ? { email: form.email, password: form.password }
        : form;
      const res = await fetch(`${apiBase}/auth/${mode}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (!res.ok) throw new Error(await readError(res));
      onAuth(await res.json());
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="container">
      <h1>Travel Planner</h1>
      <div className="card">
        <h2>{mode === 'login' ? 'Log in' : 'Create account'}</h2>
        {error && <div className="error-banner" role="alert">{error}</div>}
        <form onSubmit={submit} className="form-grid">
          <label className="form-field">Email
            <input type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} required />
          </label>
          {mode === 'register' && (
            <label className="form-field">Display name
              <input value={form.displayName} onChange={e => setForm({ ...form, displayName: e.target.value })} required />
            </label>
          )}
          <label className="form-field">Password (min. 8 characters)
            <input type="password" minLength={8} value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} required />
          </label>
          <button type="submit" className="btn-primary" disabled={busy}>
            {busy ? 'Please wait...' : mode === 'login' ? 'Log in' : 'Register'}
          </button>
          <button type="button" className="btn-secondary" onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setError(''); }}>
            {mode === 'login' ? 'Need an account? Register' : 'Have an account? Log in'}
          </button>
        </form>
      </div>
    </div>
  );
}