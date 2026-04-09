<script setup lang="ts">
import { ref } from 'vue';
import { api } from '../api/client';
import type { DesignEdit } from '../types';
import ActionButton from '../components/ActionButton.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
import EmptyState from '../components/EmptyState.vue';
import StatusBadge from '../components/StatusBadge.vue';
import ConfirmDialog from '../components/ConfirmDialog.vue';
import { useToast } from '../composables/useToast';

const { showSuccess, showError, showInfo } = useToast();

const placementId = ref('');
const editHistory = ref<DesignEdit[] | null>(null);
const errorMessage = ref<string | null>(null);
const isLoading = ref(false);
const showResetConfirm = ref(false);

// New edit form
const newEdit = ref({
  operation: 'Move',
  targetElement: '',
  previousValue: '',
  newValue: '',
});
const operations = ['Move', 'Rotate', 'Scale', 'Recolor', 'Swap', 'Remove', 'Add'];

async function loadHistory() {
  if (!placementId.value) return;
  isLoading.value = true;
  errorMessage.value = null;
  try {
    editHistory.value = await api.getEditHistory(placementId.value);
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Failed to load history';
    editHistory.value = [];
  } finally {
    isLoading.value = false;
  }
}

async function applyEdit() {
  if (!placementId.value || !newEdit.value.targetElement) return;
  isLoading.value = true;
  errorMessage.value = null;
  try {
    const edit = await api.applyEdit({
      sitePlacementId: placementId.value,
      operation: newEdit.value.operation,
      targetElement: newEdit.value.targetElement,
      previousValue: newEdit.value.previousValue,
      newValue: newEdit.value.newValue,
    });
    editHistory.value = [edit, ...(editHistory.value ?? [])];
    newEdit.value = { operation: 'Move', targetElement: '', previousValue: '', newValue: '' };
    showSuccess('Design edit applied!');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Failed to apply edit';
    showError('Failed to apply edit.');
  } finally {
    isLoading.value = false;
  }
}

async function undoLast() {
  if (!placementId.value) return;
  isLoading.value = true;
  try {
    await api.undoLastEdit(placementId.value);
    await loadHistory();
    showInfo('Last edit undone.');
  } catch (err: unknown) {
    showError(err instanceof Error ? err.message : 'Undo failed');
  } finally {
    isLoading.value = false;
  }
}

async function resetAll() {
  showResetConfirm.value = false;
  if (!placementId.value) return;
  isLoading.value = true;
  try {
    const result = await api.resetEdits(placementId.value);
    editHistory.value = [];
    showSuccess(`Reset complete — ${result.removedEdits} edit(s) removed.`);
  } catch (err: unknown) {
    showError(err instanceof Error ? err.message : 'Reset failed');
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🎨 Design Editor</h2>
    <p class="text-muted">Customise placed home designs — move, rotate, scale, and swap elements.</p>

    <div class="row mb-4">
      <div class="col-md-6">
        <label for="placement-id" class="form-label">Site Placement ID</label>
        <div class="input-group">
          <input
            id="placement-id"
            v-model="placementId"
            type="text"
            class="form-control"
            placeholder="Enter placement ID..."
          />
          <ActionButton
            :loading="isLoading"
            variant="outline-primary"
            loading-text="Loading..."
            @click="loadHistory"
          >
            Load
          </ActionButton>
        </div>
      </div>
    </div>

    <ErrorAlert :message="errorMessage" />

    <template v-if="editHistory !== null">
      <div class="row">
        <!-- Apply Edit Form -->
        <div class="col-12 col-md-5 mb-4">
          <div class="card shadow-sm">
            <div class="card-header"><strong>Apply New Edit</strong></div>
            <div class="card-body">
              <form @submit.prevent="applyEdit">
                <div class="mb-2">
                  <label class="form-label">Operation</label>
                  <select class="form-select" v-model="newEdit.operation" aria-label="Operation type">
                    <option v-for="op in operations" :key="op" :value="op">{{ op }}</option>
                  </select>
                </div>
                <div class="mb-2">
                  <label class="form-label">Target Element</label>
                  <input v-model="newEdit.targetElement" class="form-control" placeholder="e.g. kitchen-island" required />
                </div>
                <div class="mb-2">
                  <label class="form-label">Previous Value</label>
                  <input v-model="newEdit.previousValue" class="form-control" placeholder="e.g. 0,0" />
                </div>
                <div class="mb-3">
                  <label class="form-label">New Value</label>
                  <input v-model="newEdit.newValue" class="form-control" placeholder="e.g. 2,3" />
                </div>
                <ActionButton :loading="isLoading" variant="primary" class="w-100" type="submit" loading-text="Applying...">
                  Apply Edit
                </ActionButton>
              </form>
            </div>
          </div>
        </div>

        <!-- Edit History -->
        <div class="col-12 col-md-7">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="mb-0">Edit History ({{ editHistory.length }})</h5>
            <div class="d-flex gap-2">
              <button class="btn btn-sm btn-outline-warning" :disabled="editHistory.length === 0" @click="undoLast">↩ Undo Last</button>
              <button class="btn btn-sm btn-outline-danger" :disabled="editHistory.length === 0" @click="showResetConfirm = true">🗑 Reset All</button>
            </div>
          </div>

          <EmptyState
            v-if="editHistory.length === 0"
            title="No edits yet"
            message="Apply your first design edit using the form."
            icon="editor"
          />

          <div v-else class="list-group">
            <div
              v-for="edit in editHistory"
              :key="edit.id"
              class="list-group-item"
            >
              <div class="d-flex justify-content-between align-items-start">
                <div>
                  <StatusBadge :status="edit.operation" />
                  <strong class="ms-2">{{ edit.targetElement }}</strong>
                </div>
                <small class="text-muted">{{ new Date(edit.appliedUtc).toLocaleString() }}</small>
              </div>
              <small class="text-muted">
                {{ edit.previousValue || '(none)' }} → {{ edit.newValue || '(none)' }}
              </small>
            </div>
          </div>
        </div>
      </div>
    </template>

    <ConfirmDialog
      :show="showResetConfirm"
      title="Reset All Edits"
      message="Are you sure you want to remove all design edits? This cannot be undone."
      confirm-text="Reset All"
      cancel-text="Cancel"
      variant="danger"
      @confirm="resetAll"
      @cancel="showResetConfirm = false"
    />
  </div>
</template>
