export async function readError(res) {
  try {
    const text = await res.text();
    if (!text) return `Request failed (${res.status})`;
    try {
      const data = JSON.parse(text);
      if (data && data.errors) {
        const messages = Object.values(data.errors).flat();
        if (messages.length > 0) return messages.join(' ');
      }
      return (data && (data.error || data.title)) || text;
    } catch {
      return text;
    }
  } catch {
    return `Request failed (${res.status})`;
  }
}
