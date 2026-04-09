import { ref } from 'vue';

export interface ValidationRule {
  validate: (value: unknown) => boolean;
  message: string;
}

export function required(msg = 'This field is required'): ValidationRule {
  return { validate: (v) => v !== null && v !== undefined && v !== '', message: msg };
}

export function minValue(min: number, msg?: string): ValidationRule {
  return { validate: (v) => typeof v === 'number' && v >= min, message: msg ?? `Must be at least ${min}` };
}

export function maxValue(max: number, msg?: string): ValidationRule {
  return { validate: (v) => typeof v === 'number' && v <= max, message: msg ?? `Must be at most ${max}` };
}

export function pattern(regex: RegExp, msg = 'Invalid format'): ValidationRule {
  return { validate: (v) => typeof v === 'string' && regex.test(v), message: msg };
}

export function useValidation() {
  const errors = ref<string[]>([]);
  function validate(value: unknown, rules: ValidationRule[]): boolean {
    errors.value = rules.filter(r => !r.validate(value)).map(r => r.message);
    return errors.value.length === 0;
  }
  function clearErrors() { errors.value = []; }
  return { errors, validate, clearErrors };
}
