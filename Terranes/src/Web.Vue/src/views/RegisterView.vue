<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuth } from '../composables/useAuth';
import { useValidation, required } from '../composables/useValidation';
import ErrorAlert from '../components/ErrorAlert.vue';
import ActionButton from '../components/ActionButton.vue';

const router = useRouter();
const { register } = useAuth();

const email = ref('');
const displayName = ref('');
const password = ref('');
const confirmPassword = ref('');
const loading = ref(false);
const errorMessage = ref<string | null>(null);

const emailValidation = useValidation();
const nameValidation = useValidation();
const passwordValidation = useValidation();
const confirmValidation = useValidation();

async function handleRegister() {
  const emailOk = emailValidation.validate(email.value, [required('Email is required')]);
  const nameOk = nameValidation.validate(displayName.value, [required('Display name is required')]);
  const passOk = passwordValidation.validate(password.value, [required('Password is required')]);

  const matchRule = {
    validate: () => password.value === confirmPassword.value,
    message: 'Passwords must match',
  };
  const confirmOk = confirmValidation.validate(confirmPassword.value, [required('Please confirm your password'), matchRule]);

  if (!emailOk || !nameOk || !passOk || !confirmOk) return;

  loading.value = true;
  errorMessage.value = null;
  try {
    await register(email.value, displayName.value, password.value);
    router.push('/dashboard');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Registration failed';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="container">
    <div class="row justify-content-center">
      <div class="col-md-6 col-lg-4">
        <h2 class="mb-4 text-center">📝 Register</h2>

        <ErrorAlert :message="errorMessage" />

        <form @submit.prevent="handleRegister">
          <div class="mb-3">
            <label for="email" class="form-label">Email</label>
            <input
              id="email"
              type="email"
              class="form-control"
              placeholder="you@example.com"
              v-model="email"
            />
            <div v-if="emailValidation.errors.value.length" class="text-danger small mt-1">
              {{ emailValidation.errors.value[0] }}
            </div>
          </div>
          <div class="mb-3">
            <label for="displayName" class="form-label">Display Name</label>
            <input
              id="displayName"
              type="text"
              class="form-control"
              placeholder="Your name"
              v-model="displayName"
            />
            <div v-if="nameValidation.errors.value.length" class="text-danger small mt-1">
              {{ nameValidation.errors.value[0] }}
            </div>
          </div>
          <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input
              id="password"
              type="password"
              class="form-control"
              placeholder="Password"
              v-model="password"
            />
            <div v-if="passwordValidation.errors.value.length" class="text-danger small mt-1">
              {{ passwordValidation.errors.value[0] }}
            </div>
          </div>
          <div class="mb-3">
            <label for="confirmPassword" class="form-label">Confirm Password</label>
            <input
              id="confirmPassword"
              type="password"
              class="form-control"
              placeholder="Confirm password"
              v-model="confirmPassword"
            />
            <div v-if="confirmValidation.errors.value.length" class="text-danger small mt-1">
              {{ confirmValidation.errors.value[0] }}
            </div>
          </div>
          <ActionButton :loading="loading" variant="primary" class="w-100" loading-text="Registering..." @click="handleRegister">Register</ActionButton>
        </form>

        <p class="mt-3 text-center">
          Already have an account? <RouterLink to="/login">Login</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>
