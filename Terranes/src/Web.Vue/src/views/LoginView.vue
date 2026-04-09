<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuth } from '../composables/useAuth';
import { useValidation, required } from '../composables/useValidation';
import ErrorAlert from '../components/ErrorAlert.vue';
import ActionButton from '../components/ActionButton.vue';

const router = useRouter();
const { login } = useAuth();

const email = ref('');
const password = ref('');
const loading = ref(false);
const errorMessage = ref<string | null>(null);

const emailValidation = useValidation();
const passwordValidation = useValidation();

async function handleLogin() {
  const emailOk = emailValidation.validate(email.value, [required('Email is required')]);
  const passOk = passwordValidation.validate(password.value, [required('Password is required')]);
  if (!emailOk || !passOk) return;

  loading.value = true;
  errorMessage.value = null;
  try {
    await login(email.value, password.value);
    router.push('/dashboard');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Login failed';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="container">
    <div class="row justify-content-center">
      <div class="col-md-6 col-lg-4">
        <h2 class="mb-4 text-center">🔐 Login</h2>

        <ErrorAlert :message="errorMessage" />

        <form @submit.prevent="handleLogin">
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
          <ActionButton :loading="loading" variant="primary" class="w-100" loading-text="Logging in..." @click="handleLogin">Login</ActionButton>
        </form>

        <p class="mt-3 text-center">
          Don't have an account? <RouterLink to="/register">Register</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>
