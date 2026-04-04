import { describe, it, expect } from 'vitest'
import { formatDuration, formatDate } from '../api.js'

describe('formatDuration', () => {
  it('returns dash for null/undefined', () => {
    expect(formatDuration(null)).toBe('—')
    expect(formatDuration(undefined)).toBe('—')
  })

  it('formats millisecond durations from .NET TimeSpan', () => {
    expect(formatDuration('00:00:00.1234567')).toBe('123ms')
  })

  it('formats second durations from .NET TimeSpan', () => {
    expect(formatDuration('00:00:02.5000000')).toBe('2.50s')
  })

  it('returns string representation for non-TimeSpan values', () => {
    expect(formatDuration(42)).toBe('42')
  })
})

describe('formatDate', () => {
  it('returns dash for null/undefined', () => {
    expect(formatDate(null)).toBe('—')
    expect(formatDate(undefined)).toBe('—')
  })

  it('formats valid ISO date strings', () => {
    const result = formatDate('2026-01-15T10:30:00Z')
    expect(result).toBeTruthy()
    expect(result).not.toBe('—')
  })

  it('returns the original value for unparseable dates', () => {
    expect(formatDate('not-a-date')).toBe('Invalid Date')
  })
})
