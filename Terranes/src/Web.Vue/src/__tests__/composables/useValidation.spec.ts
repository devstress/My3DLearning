import { describe, it, expect } from 'vitest';
import { required, minValue, maxValue, pattern, useValidation } from '../../composables/useValidation';

describe('useValidation', () => {
  it('required rule fails for empty string', () => {
    const rule = required();
    expect(rule.validate('')).toBe(false);
    expect(rule.validate('hello')).toBe(true);
  });

  it('minValue rule validates correctly', () => {
    const rule = minValue(5);
    expect(rule.validate(3)).toBe(false);
    expect(rule.validate(5)).toBe(true);
    expect(rule.validate(10)).toBe(true);
  });

  it('maxValue rule validates correctly', () => {
    const rule = maxValue(10);
    expect(rule.validate(5)).toBe(true);
    expect(rule.validate(10)).toBe(true);
    expect(rule.validate(15)).toBe(false);
  });

  it('pattern rule validates regex', () => {
    const rule = pattern(/^\d{3}$/);
    expect(rule.validate('123')).toBe(true);
    expect(rule.validate('12')).toBe(false);
    expect(rule.validate('abcd')).toBe(false);
  });

  it('useValidation returns errors for failing rules', () => {
    const { errors, validate } = useValidation();
    const valid = validate('', [required(), minValue(1)]);
    expect(valid).toBe(false);
    expect(errors.value.length).toBeGreaterThan(0);
    expect(errors.value).toContain('This field is required');
  });
});
