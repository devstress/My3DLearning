<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { api } from '../api/client';
import ActionButton from '../components/ActionButton.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
import { useAuth } from '../composables/useAuth';
import { useToast } from '../composables/useToast';

const router = useRouter();
const { setUser } = useAuth();
const { showSuccess, showError } = useToast();

const email = ref('');
const password = ref('');
const isLoading = ref(false);
const errorMessage = ref<string | null>(null);

async function handleLogin() {
  if (!email.value || !password.value) {
    errorMessage.value = 'Email and password are required.';
    return;
  }
  isLoading.value = true;
  errorMessage.value = null;
  try {
    const user = await api.login(email.value, password.value);
    setUser(user);
    showSuccess(`Welcome back, ${user.displayName}!`);
    router.push('/dashboard');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Login failed';
    showError('Login failed. Check your credentials.');
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="container" style="max-width: 480px;">
    <h2 class="mb-4 text-center">🔐 Login</h2>
    <p class="text-muted text-center">Sign in to your Terranes account.</p>

    <form @submit.prevent="handleLogin" novalidate>
      <div class="mb-3">
        <label for="login-email" class="form-label">Email</label>
        <input
          id="login-email"
          v-model="email"
          type="email"
          class="form-control"
          placeholder="you@example.com"
          autocomplete="email"
          required
        />
      </div>
      <div class="mb-3">
        <label for="login-password" class="form-label">Password</label>
        <input
          id="login-password"
          v-model="password"
          type="password"
          class="form-control"
          placeholder="Password"
          autocomplete="current-password"
          required
        />
      </div>

      <ErrorAlert :message="errorMessage" />

      <ActionButton
        :loading="isLoading"
        variant="primary"
        class="w-100"
        loading-text="Signing in..."
        type="submit"
      >
        Sign In
      </ActionButton>
    </form>

    <p class="mt-3 text-center text-muted">
      Don't have an account?
      <RouterLink to="/register">Register here</RouterLink>
    </p>
  </div>
</template>
