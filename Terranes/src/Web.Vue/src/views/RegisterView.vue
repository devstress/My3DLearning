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

const displayName = ref('');
const email = ref('');
const password = ref('');
const confirmPassword = ref('');
const isLoading = ref(false);
const errorMessage = ref<string | null>(null);

async function handleRegister() {
  errorMessage.value = null;

  if (!displayName.value || !email.value || !password.value) {
    errorMessage.value = 'All fields are required.';
    return;
  }
  if (password.value !== confirmPassword.value) {
    errorMessage.value = 'Passwords do not match.';
    return;
  }
  if (password.value.length < 6) {
    errorMessage.value = 'Password must be at least 6 characters.';
    return;
  }

  isLoading.value = true;
  try {
    const user = await api.register(
      {
        email: email.value,
        displayName: displayName.value,
        role: 'Buyer',
        isActive: true,
      },
      password.value,
    );
    setUser(user);
    showSuccess(`Welcome, ${user.displayName}! Account created.`);
    router.push('/dashboard');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Registration failed';
    showError('Registration failed. Please try again.');
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="container" style="max-width: 480px;">
    <h2 class="mb-4 text-center">📝 Register</h2>
    <p class="text-muted text-center">Create your Terranes account to start your journey.</p>

    <form @submit.prevent="handleRegister" novalidate>
      <div class="mb-3">
        <label for="reg-name" class="form-label">Display Name</label>
        <input
          id="reg-name"
          v-model="displayName"
          type="text"
          class="form-control"
          placeholder="Your name"
          autocomplete="name"
          required
        />
      </div>
      <div class="mb-3">
        <label for="reg-email" class="form-label">Email</label>
        <input
          id="reg-email"
          v-model="email"
          type="email"
          class="form-control"
          placeholder="you@example.com"
          autocomplete="email"
          required
        />
      </div>
      <div class="mb-3">
        <label for="reg-password" class="form-label">Password</label>
        <input
          id="reg-password"
          v-model="password"
          type="password"
          class="form-control"
          placeholder="At least 6 characters"
          autocomplete="new-password"
          required
        />
      </div>
      <div class="mb-3">
        <label for="reg-confirm" class="form-label">Confirm Password</label>
        <input
          id="reg-confirm"
          v-model="confirmPassword"
          type="password"
          class="form-control"
          placeholder="Repeat password"
          autocomplete="new-password"
          required
        />
      </div>

      <ErrorAlert :message="errorMessage" />

      <ActionButton
        :loading="isLoading"
        variant="primary"
        class="w-100"
        loading-text="Creating account..."
        type="submit"
      >
        Create Account
      </ActionButton>
    </form>

    <p class="mt-3 text-center text-muted">
      Already have an account?
      <RouterLink to="/login">Sign in</RouterLink>
    </p>
  </div>
</template>
