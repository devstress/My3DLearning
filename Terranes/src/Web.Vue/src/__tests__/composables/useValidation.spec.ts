import { describe, it, expect } from 'vitest';
import { ref, nextTick } from 'vue';
import { useValidation, rules } from '../../composables/useValidation';

describe('useValidation', () => {
  it('starts with no error and not touched', () => {
    const source = ref('');
    const { error, touched } = useValidation(source, [rules.required()]);
    expect(error.value).toBeNull();
    expect(touched.value).toBe(false);
  });

  it('validates required field after touch', () => {
    const source = ref('');
    const { error, touch } = useValidation(source, [rules.required()]);
    touch();
    expect(error.value).toBe('This field is required');
  });

  it('clears error when value becomes valid', async () => {
    const source = ref('');
    const { error, touch } = useValidation(source, [rules.required()]);
    touch();
    expect(error.value).toBe('This field is required');
    source.value = 'hello';
    await nextTick();
    expect(error.value).toBeNull();
  });

  it('validates minValue rule', () => {
    const source = ref(0);
    const { error, touch } = useValidation(source, [rules.minValue(1)]);
    touch();
    expect(error.value).toBe('Must be at least 1');
  });

  it('validates maxValue rule', () => {
    const source = ref(100);
    const { error, touch } = useValidation(source, [rules.maxValue(50)]);
    touch();
    expect(error.value).toBe('Must be at most 50');
  });

  it('validates positiveNumber rule', () => {
    const source = ref(-5);
    const { error, touch } = useValidation(source, [rules.positiveNumber()]);
    touch();
    expect(error.value).toBe('Must be a positive number');
  });

  it('positiveNumber allows undefined/null/empty', () => {
    const source = ref<number | undefined>(undefined);
    const { error, touch } = useValidation(source, [rules.positiveNumber()]);
    touch();
    expect(error.value).toBeNull();
  });

  it('reset clears error and touched state', () => {
    const source = ref('');
    const { error, touched, touch, reset } = useValidation(source, [rules.required()]);
    touch();
    expect(error.value).not.toBeNull();
    expect(touched.value).toBe(true);
    reset();
    expect(error.value).toBeNull();
    expect(touched.value).toBe(false);
  });

  it('validate returns false for invalid and true for valid', () => {
    const source = ref('');
    const { validate } = useValidation(source, [rules.required()]);
    expect(validate()).toBe(false);
    source.value = 'valid';
    expect(validate()).toBe(true);
  });
});
