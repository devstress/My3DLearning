<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { Report, ComplianceResult } from '../types';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import EmptyState from '../components/EmptyState.vue';
import ActionButton from '../components/ActionButton.vue';
import { useToast } from '../composables/useToast';

const { showSuccess, showError } = useToast();

const DEMO_USER_ID = '00000000-0000-0000-0000-000000000001';
const DEMO_TENANT_ID = '00000000-0000-0000-0000-000000000001';

const activeTab = ref<'reports' | 'compliance'>('reports');

// Reports state
const reportTypes = ref<string[]>([]);
const reports = ref<Report[] | null>(null);
const selectedReport = ref<Report | null>(null);
const reportTitle = ref('');
const reportType = ref('');
const generatingReport = ref(false);

// Compliance state
const complianceResults = ref<ComplianceResult[] | null>(null);
const checkPlacementId = ref('');
const checkJurisdiction = ref('');
const runningCheck = ref(false);

async function loadReportTypes() {
  try {
    reportTypes.value = await api.getReportTypes();
    if (reportTypes.value.length > 0) reportType.value = reportTypes.value[0];
  } catch {
    reportTypes.value = ['Summary', 'Financial', 'Compliance', 'Design'];
    reportType.value = reportTypes.value[0];
  }
}

async function loadReports() {
  try {
    reports.value = await api.getTenantReports(DEMO_TENANT_ID);
  } catch {
    reports.value = [];
  }
}

async function generateReport() {
  if (!reportTitle.value.trim() || !reportType.value) return;
  generatingReport.value = true;
  try {
    const report = await api.generateReport(reportType.value, reportTitle.value.trim(), DEMO_USER_ID, DEMO_TENANT_ID);
    showSuccess('Report generated successfully!');
    if (!reports.value) reports.value = [];
    reports.value.unshift(report);
    reportTitle.value = '';
  } catch {
    showError('Failed to generate report.');
  } finally {
    generatingReport.value = false;
  }
}

function viewReport(report: Report) {
  selectedReport.value = report;
}

function closeModal() {
  selectedReport.value = null;
}

async function loadComplianceResults() {
  try {
    complianceResults.value = await api.getComplianceByPlacement(DEMO_USER_ID);
  } catch {
    complianceResults.value = [];
  }
}

async function runComplianceCheck() {
  if (!checkPlacementId.value.trim() || !checkJurisdiction.value.trim()) return;
  runningCheck.value = true;
  try {
    const result = await api.runComplianceCheck(checkPlacementId.value.trim(), checkJurisdiction.value.trim());
    showSuccess('Compliance check completed!');
    if (!complianceResults.value) complianceResults.value = [];
    complianceResults.value.unshift(result);
    checkPlacementId.value = '';
    checkJurisdiction.value = '';
  } catch {
    showError('Failed to run compliance check.');
  } finally {
    runningCheck.value = false;
  }
}

onMounted(() => {
  loadReportTypes();
  loadReports();
  loadComplianceResults();
});
</script>

<template>
  <div class="container">
    <h2 class="mb-4">📋 Reports &amp; Compliance</h2>
    <p class="text-muted">Generate reports and run compliance checks for your projects.</p>

    <ul class="nav nav-tabs mb-4">
      <li class="nav-item">
        <button class="nav-link" :class="{ active: activeTab === 'reports' }" @click="activeTab = 'reports'">Reports</button>
      </li>
      <li class="nav-item">
        <button class="nav-link" :class="{ active: activeTab === 'compliance' }" @click="activeTab = 'compliance'">Compliance Checks</button>
      </li>
    </ul>

    <!-- Reports Section -->
    <div v-if="activeTab === 'reports'">
      <div class="card mb-4">
        <div class="card-body">
          <h5 class="card-title">Generate Report</h5>
          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Report Type</label>
              <select class="form-select" v-model="reportType">
                <option v-for="t in reportTypes" :key="t" :value="t">{{ t }}</option>
              </select>
            </div>
            <div class="col-md-5">
              <label class="form-label">Title</label>
              <input type="text" class="form-control" v-model="reportTitle" placeholder="Report title..." />
            </div>
            <div class="col-md-3 d-flex align-items-end">
              <ActionButton :loading="generatingReport" variant="primary" @click="generateReport">Generate Report</ActionButton>
            </div>
          </div>
        </div>
      </div>

      <SkeletonCard v-if="reports === null" :count="3" :columns="3" />
      <EmptyState v-else-if="reports.length === 0" message="No reports yet. Generate one above to get started." />
      <div v-else class="row g-4">
        <div class="col-12 col-md-4" v-for="report in reports" :key="report.id">
          <div class="card h-100 shadow-sm">
            <div class="card-body">
              <h5 class="card-title">{{ report.title }}</h5>
              <span class="badge bg-info mb-2">{{ report.reportType }}</span>
              <p class="card-text text-muted">
                Generated {{ new Date(report.generatedUtc).toLocaleDateString() }}
              </p>
            </div>
            <div class="card-footer">
              <button class="btn btn-sm btn-outline-primary" aria-label="View report" @click="viewReport(report)">View</button>
            </div>
          </div>
        </div>
      </div>

      <DetailModal :show="!!selectedReport" :title="selectedReport?.title ?? ''" @close="closeModal">
        <template v-if="selectedReport">
          <table class="table table-sm mb-3">
            <tbody>
              <tr><th>Type</th><td><span class="badge bg-info">{{ selectedReport.reportType }}</span></td></tr>
              <tr><th>Generated</th><td>{{ new Date(selectedReport.generatedUtc).toLocaleString() }}</td></tr>
              <tr><th>Tenant</th><td><code>{{ selectedReport.tenantId }}</code></td></tr>
            </tbody>
          </table>
          <h6>Content</h6>
          <div class="border rounded p-3 bg-light">
            <pre class="mb-0" style="white-space: pre-wrap;">{{ selectedReport.contentMarkdown }}</pre>
          </div>
        </template>
      </DetailModal>
    </div>

    <!-- Compliance Section -->
    <div v-if="activeTab === 'compliance'">
      <div class="card mb-4">
        <div class="card-body">
          <h5 class="card-title">Run Compliance Check</h5>
          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Site Placement ID</label>
              <input type="text" class="form-control" v-model="checkPlacementId" placeholder="Placement ID..." />
            </div>
            <div class="col-md-4">
              <label class="form-label">Jurisdiction</label>
              <input type="text" class="form-control" v-model="checkJurisdiction" placeholder="e.g. NSW, VIC..." />
            </div>
            <div class="col-md-4 d-flex align-items-end">
              <ActionButton :loading="runningCheck" variant="primary" @click="runComplianceCheck">Run Check</ActionButton>
            </div>
          </div>
        </div>
      </div>

      <SkeletonCard v-if="complianceResults === null" :count="2" :columns="3" />
      <EmptyState v-else-if="complianceResults.length === 0" message="No compliance results yet. Run a check above." />
      <div v-else class="row g-4">
        <div class="col-12 col-md-4" v-for="result in complianceResults" :key="result.id">
          <div class="card h-100 shadow-sm">
            <div class="card-body">
              <div class="d-flex justify-content-between align-items-start mb-2">
                <h5 class="card-title mb-0">{{ result.jurisdiction }}</h5>
                <StatusBadge :status="result.isCompliant ? 'Compliant' : 'Non-Compliant'" />
              </div>
              <p class="card-text text-muted">
                Checked {{ new Date(result.checkedUtc).toLocaleDateString() }}
              </p>
              <div v-if="result.issues.length > 0">
                <strong>Issues:</strong>
                <ul class="mb-0">
                  <li v-for="(issue, i) in result.issues" :key="i" class="text-danger">{{ issue }}</li>
                </ul>
              </div>
              <p v-else class="text-success mb-0">✅ No issues found</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
