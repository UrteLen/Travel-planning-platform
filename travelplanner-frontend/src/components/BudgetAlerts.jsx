export default function BudgetAlerts({ results, expenses = [] }) {
  if (!results) return null; // summary not loaded yet

  const notices = [];

  if (results.length === 0) {
    notices.push({
      key: 'no-limits',
      level: 'warning',
      text: "No category limits are saved yet, so planned vs actual spend and limit alerts can't be calculated. Enter limits and press Save Limits."
    });
  } else {
    const limited = new Set(results.map(r => r.category));
    [...new Set(expenses.map(e => e.category))]
      .filter(c => !limited.has(c))
      .forEach(c => notices.push({
        key: `unlimited-${c}`,
        level: 'warning',
        text: `${c} has expenses but no limit, so it is not counted in the budget status. Set a limit for ${c}.`
      }));
  }

  results
    .filter(r => r.status === 'Exceeded' || r.status === 'Warning')
    .forEach(r => notices.push({
      key: r.category,
      level: r.status.toLowerCase(),
      text: r.status === 'Exceeded'
        ? `${r.category} is over its limit by €${(r.actualSpent - r.limit).toFixed(2)} (€${r.actualSpent.toFixed(2)} of €${r.limit.toFixed(2)}).`
        : `${r.category} is close to its limit: €${r.actualSpent.toFixed(2)} of €${r.limit.toFixed(2)} used.`
    }));

  if (notices.length === 0) return null;

  return (
    <div className="alert-list">
      {notices.map(n => (
        <div key={n.key} className={`alert-box ${n.level}`}>{n.text}</div>
      ))}
    </div>
  );
}