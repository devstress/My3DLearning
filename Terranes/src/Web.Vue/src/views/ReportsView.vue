<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { Report } from '../types';
import ActionButton from '../components/ActionButton.vue';
import StatusBadge from '../components/StatusBadge.vue';
import EmptyState from '../components/EmptyState.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import { useToast } from '../composables/useToast';

const DEMO_TENANT_ID = '00000000-0000-0000-0000-000000000001';
const DEMO_USER_ID = '00000000-0000-0000-0000-000000000001';

const { showSuccess, showError } = useToast();

const reports = ref<Report[] | null>(null);
const reportTypes = ref<string[]>([]);
const selectedType = ref('');
const reportTitle = ref('');
const isGenerating = ref(false);
const selectedReport = ref<Report | null>(null);

async function loadReports() {
  try {
    reports.value = await api.getTenantReports(DEMO_TENANT_ID);
  } catch {
    reports.value = [];
  }
}

async function loadTypes() {
  try {
    reportTypes.value = await api.getReportTypes();
    if (reportTypes.value.length > 0 && !selectedType.value) {
      selectedType.value = reportTypes.value[0];
    }
  } catch {
    reportTypes.value = ['UserActivity', 'PropertySummary', 'PartnerPerformance', 'FinancialOverview'];
    selectedType.value = reportTypes.value[0];
  }
}

async function generateReport() {
  if (!selectedType.value || !reportTitle.value) return;
  isGenerating.value = true;
  try {
    const report = await api.generateReport(selectedType.value, reportTitle.value, DEMO_USER_ID, DEMO_TENANT_ID);
    reports.value = [report, ...(reports.value ?? [])];
    reportTitle.value = '';
    showSuccess('Report generated successfully!');
  } catch (err: unknown) {
    showError(err instanceof Error ? err.message : 'Failed to generate report');
  } finally {
    isGenerating.value = false;
  }
}

function viewReport(report: Report) {
  selectedReport.value = report;
}

onMounted(async () => {
  await Promise.all([loadReports(), loadTypes()]);
});
</script>

<template>
  <div class="container">
    <h2 class="mb-4">📊 Reports</h2>
    <p class="text-muted">Generate and view analytics reports for your tenant.</p>

    <div class="card shadow-sm mb-4">
      <div class="card-header"><strong>Generate New Report</strong></div>
      <div class="card-body">
        <form class="row g-3 align-items-end" @submit.prevent="generateReport">
          <div class="col-md-4">
            <label class="form-label">Report Type</label>
            <select class="form-select" v-model="selectedType" aria-label="Report type">
              <option v-for="rt in reportTypes" :key="rt" :value="rt">{{ rt }}</option>
            </select>
          </div>
          <div class="col-md-5">
            <label class="form-label">Title</label>
            <input v-model="reportTitle" class="form-control" placeholder="e.g. Q1 2026 Summary" required />
          </div>
          <div class="col-md-3">
            <ActionButton
              :loading="isGenerating"
              variant="primary"
              class="w-100"
              loading-text="Generating..."
              type="submit"
            >
              📄 Generate
            </ActionButton>
          </div>
        </form>
      </div>
    </div>

    <div class="row">
      <div class="col-12" :class="{ 'col-md-6': selectedReport }">
        <h5>Reports ({{ reports?.length ?? 0 }})</h5>

        <SkeletonTable v-if="reports === null" :rows="5" :cols="4" />
        <EmptyState v-else-if="reports.length === 0" title="No reports yet" message="Generate your first report above." icon="report" />

        <div v-else class="table-responsive">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>Title</th>
                <th>Type</th>
                <th>Generated</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="report in reports"
                :key="report.id"
                :class="{ 'table-active': selectedReport?.id === report.id }"
              >
                <td>{{ report.title }}</td>
                <td><StatusBadge :status="report.reportType" /></td>
                <td>{{ new Date(report.generatedUtc).toLocaleString() }}</td>
                <td>
                  <button class="btn btn-sm btn-outline-primary" @click="viewReport(report)">View</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="selectedReport" class="col-12 col-md-6">
        <div class="card shadow-sm">
          <div class="card-header d-flex justify-content-between align-items-center">
            <strong>{{ selectedReport.title }}</strong>
            <button class="btn-close" aria-label="Close report preview" @click="selectedReport = null"></button>
          </div>
          <div class="card-body">
            <table class="table table-sm mb-3">
              <tbody>
                <tr><th>Type</th><td><StatusBadge :status="selectedReport.reportType" /></td></tr>
                <tr><th>Generated</th><td>{{ new Date(selectedReport.generatedUtc).toLocaleString() }}</td></tr>
                <tr><th>ID</th><td><code>{{ selectedReport.id }}</code></td></tr>
              </tbody>
            </table>
            <h6>Content</h6>
            <div class="bg-light p-3 rounded" style="white-space: pre-wrap; font-family: monospace; font-size: 0.85rem;">{{ selectedReport.contentMarkdown }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
