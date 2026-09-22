import { useState, useEffect } from 'react';

function App() {
  const [results, setResults] = useState([]);

  useEffect(() => {
    fetch('http://localhost:5000/api/budgetapi/checkall')
      .then(res => res.json())
      .then(data => setResults(data));
  }, []);

  return (
    <div>
      <h1>Budget Check</h1>
      <ul>
        {results.map((r, i) => (
          <li key={i}>{r.category}: {r.actualSpent} / {r.limit} — {r.status}</li>
        ))}
      </ul>
    </div>
  );
}

export default App;