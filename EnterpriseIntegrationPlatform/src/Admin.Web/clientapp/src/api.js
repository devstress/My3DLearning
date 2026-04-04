/**
 * Shared API fetch utility for the Admin Dashboard.
 * Wraps fetch with JSON parsing and error handling.
 * All requests go to the local proxy endpoints which forward to Admin.Api.
 */
export async function apiFetch(url, options = {}) {
  const res = await fetch(url, options)
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `HTTP ${res.status}`)
  }
  const text = await res.text()
  return text ? JSON.parse(text) : null
}

/**
 * Formats a .NET TimeSpan duration string (HH:MM:SS.fff) to a human-readable value.
 */
export function formatDuration(d) {
  if (!d) return '—'
  if (typeof d === 'string') {
    const parts = d.split(':')
    if (parts.length === 3) {
      const secs = parseFloat(parts[2])
      return secs < 1 ? `${(secs * 1000).toFixed(0)}ms` : `${secs.toFixed(2)}s`
    }
  }
  return String(d)
}

/**
 * Formats an ISO date string to a locale-specific display format.
 */
export function formatDate(d) {
  if (!d) return '—'
  try {
    return new Date(d).toLocaleString()
  } catch {
    return d
  }
}
