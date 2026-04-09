import { ref, watch, type Ref } from 'vue';

export interface ValidationRule {
  test: (value: unknown) => boolean;
  message: string;
}

export function useValidation<T>(
  source: Ref<T>,
  rules: ValidationRule[],
) {
  const error = ref<string | null>(null);
  const touched = ref(false);

  function validate(): boolean {
    for (const rule of rules) {
      if (!rule.test(source.value)) {
        error.value = rule.message;
        return false;
      }
    }
    error.value = null;
    return true;
  }

  function touch() {
    touched.value = true;
    validate();
  }

  function reset() {
    error.value = null;
    touched.value = false;
  }

  watch(source, () => {
    if (touched.value) {
      validate();
    }
  });

  return { error, touched, validate, touch, reset };
}

/** Common validation rules */
export const rules = {
  required: (message = 'This field is required'): ValidationRule => ({
    test: (v) => v !== null && v !== undefined && v !== '',
    message,
  }),
  minValue: (min: number, message?: string): ValidationRule => ({
    test: (v) => typeof v !== 'number' || v >= min,
    message: message ?? `Must be at least ${min}`,
  }),
  maxValue: (max: number, message?: string): ValidationRule => ({
    test: (v) => typeof v !== 'number' || v <= max,
    message: message ?? `Must be at most ${max}`,
  }),
  positiveNumber: (message = 'Must be a positive number'): ValidationRule => ({
    test: (v) => v === undefined || v === null || v === '' || (typeof v === 'number' && v > 0),
    message,
  }),
};
