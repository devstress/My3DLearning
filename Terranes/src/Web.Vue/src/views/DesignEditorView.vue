<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { DesignEdit } from '../types';
import ActionButton from '../components/ActionButton.vue';
import EmptyState from '../components/EmptyState.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import { useToast } from '../composables/useToast';

const { showSuccess, showError } = useToast();

const sitePlacementId = ref('');
const operation = ref('Move');
const targetElement = ref('');
const newValue = ref('');
const applying = ref(false);
const undoing = ref(false);

const editHistory = ref<DesignEdit[] | null>(null);
const historyPlacementId = ref('');

const operations = ['Move', 'Rotate', 'Scale', 'ColorChange', 'MaterialChange', 'AddElement', 'RemoveElement'];

async function applyEdit() {
  if (!sitePlacementId.value.trim() || !targetElement.value.trim() || !newValue.value.trim()) return;
  applying.value = true;
  try {
    const edit = await api.applyEdit({
      sitePlacementId: sitePlacementId.value.trim(),
      operation: operation.value,
      targetElement: targetElement.value.trim(),
      newValue: newValue.value.trim(),
    });
    showSuccess('Edit applied successfully!');
    if (!editHistory.value) editHistory.value = [];
    editHistory.value.unshift(edit);
    historyPlacementId.value = sitePlacementId.value.trim();
    targetElement.value = '';
    newValue.value = '';
  } catch {
    showError('Failed to apply edit.');
  } finally {
    applying.value = false;
  }
}

async function loadHistory() {
  if (!historyPlacementId.value.trim()) return;
  try {
    editHistory.value = await api.getEditHistory(historyPlacementId.value.trim());
  } catch {
    editHistory.value = [];
  }
}

async function undoLast() {
  if (!historyPlacementId.value.trim()) return;
  undoing.value = true;
  try {
    await api.undoLastEdit(historyPlacementId.value.trim());
    showSuccess('Last edit undone!');
    await loadHistory();
  } catch {
    showError('Failed to undo last edit.');
  } finally {
    undoing.value = false;
  }
}

onMounted(() => {
  editHistory.value = [];
});
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🎨 Design Editor</h2>
    <p class="text-muted">Customise site placements with design operations. Full 3D editing coming soon.</p>

    <div class="card mb-4">
      <div class="card-body">
        <h5 class="card-title">Apply Design Edit</h5>
        <div class="row g-3">
          <div class="col-md-6">
            <label class="form-label">Site Placement ID</label>
            <input type="text" class="form-control" v-model="sitePlacementId" placeholder="Enter site placement ID" />
          </div>
          <div class="col-md-6">
            <label class="form-label">Operation</label>
            <select class="form-select" v-model="operation">
              <option v-for="op in operations" :key="op" :value="op">{{ op }}</option>
            </select>
          </div>
          <div class="col-md-6">
            <label class="form-label">Target Element</label>
            <input type="text" class="form-control" v-model="targetElement" placeholder="e.g. Wall-North" />
          </div>
          <div class="col-md-6">
            <label class="form-label">New Value</label>
            <input type="text" class="form-control" v-model="newValue" placeholder="e.g. #FF5733" />
          </div>
          <div class="col-12">
            <ActionButton :loading="applying" variant="primary" @click="applyEdit">Apply Edit</ActionButton>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="card-title mb-0">Edit History</h5>
          <div class="d-flex gap-2">
            <input type="text" class="form-control form-control-sm" style="width: 250px;" v-model="historyPlacementId" placeholder="Placement ID to load history" />
            <button class="btn btn-sm btn-outline-secondary" @click="loadHistory">Load</button>
            <ActionButton :loading="undoing" variant="warning" size="sm" @click="undoLast">Undo Last</ActionButton>
          </div>
        </div>

        <SkeletonTable v-if="editHistory === null" :rows="3" :cols="5" />
        <EmptyState v-else-if="editHistory.length === 0" message="No edits yet. Apply a design edit above to get started." />
        <div v-else class="table-responsive">
          <table class="table table-sm table-striped">
            <thead>
              <tr>
                <th>Operation</th>
                <th>Target</th>
                <th>Value</th>
                <th>Applied</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="edit in editHistory" :key="edit.id">
                <td><span class="badge bg-info">{{ edit.operation }}</span></td>
                <td>{{ edit.targetElement }}</td>
                <td><code>{{ edit.newValue }}</code></td>
                <td>{{ new Date(edit.appliedUtc).toLocaleString() }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
