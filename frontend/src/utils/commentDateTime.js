/**
 * Parse datetime from API (PascalCase JSON) or SignalR. ISO without timezone is treated as UTC
 * (matches server storing DateTime.UtcNow with Unspecified kind from SQL).
 */
export function parseCommentDateTime(value) {
  if (value == null || value === '') return null;
  if (typeof value === 'number' && Number.isFinite(value)) {
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? null : d;
  }
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }
  const s = String(value).trim();
  if (!s) return null;

  let d = new Date(s);
  if (!Number.isNaN(d.getTime())) return d;

  const isoLocalForm = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$/;
  if (isoLocalForm.test(s) && !/[zZ]$|[+-]\d{2}:?\d{2}$/.test(s)) {
    // SQL / older API may omit "Z"; assume UTC. Trim sub-ms fractional digits for JS Date.
    const trimmedFrac = s.replace(/(\.\d{3})\d*$/, '$1');
    d = new Date(`${trimmedFrac}Z`);
    if (!Number.isNaN(d.getTime())) return d;
  }

  return null;
}

export function formatCommentDateTime(value) {
  const d = parseCommentDateTime(value);
  if (!d) return '';

  return d.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

/** Store comments with a single ISO UTC string so every client formats the same instant. */
export function toIsoUtcString(value) {
  const d = parseCommentDateTime(value);
  return d ? d.toISOString() : '';
}
