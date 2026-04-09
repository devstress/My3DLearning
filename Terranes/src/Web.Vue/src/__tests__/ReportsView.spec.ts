import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { Report, ComplianceResult } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getReportTypes: vi.fn(),
    getTenantReports: vi.fn(),
    generateReport: vi.fn(),
    getComplianceByPlacement: vi.fn(),
    runComplianceCheck: vi.fn(),
  },
}));

import { api } from '../api/client';
import ReportsView from '../views/ReportsView.vue';

const mockReports: Report[] = [
  {
    id: 'r1', reportType: 'Summary', title: 'Q1 Summary',
    contentMarkdown: '# Q1 Report\nAll good.', generatedByUserId: 'u1',
    tenantId: 't1', generatedUtc: '2026-03-01T00:00:00Z',
  },
];

const mockCompliance: ComplianceResult[] = [
  {
    id: 'c1', sitePlacementId: 'sp1', jurisdiction: 'NSW',
    isCompliant: true, issues: [], checkedUtc: '2026-03-01T00:00:00Z',
  },
  {
    id: 'c2', sitePlacementId: 'sp1', jurisdiction: 'VIC',
    isCompliant: false, issues: ['Setback too small', 'Missing fire rating'],
    checkedUtc: '2026-03-02T00:00:00Z',
  },
];

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('ReportsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows generate report form', async () => {
    vi.mocked(api.getReportTypes).mockResolvedValue(['Summary', 'Financial']);
    vi.mocked(api.getTenantReports).mockResolvedValue([]);
    vi.mocked(api.getComplianceByPlacement).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(ReportsView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Generate Report');
    expect(wrapper.text()).toContain('Report Type');
    expect(wrapper.text()).toContain('Title');
  });

  it('shows compliance check form', async () => {
    vi.mocked(api.getReportTypes).mockResolvedValue(['Summary']);
    vi.mocked(api.getTenantReports).mockResolvedValue([]);
    vi.mocked(api.getComplianceByPlacement).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(ReportsView, { global: { plugins: [router] } });
    await flushPromises();
    // Switch to compliance tab
    const complianceTab = wrapper.findAll('button').find((b) => b.text() === 'Compliance Checks');
    await complianceTab!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Run Compliance Check');
    expect(wrapper.text()).toContain('Site Placement ID');
    expect(wrapper.text()).toContain('Jurisdiction');
  });

  it('shows report cards after loading', async () => {
    vi.mocked(api.getReportTypes).mockResolvedValue(['Summary']);
    vi.mocked(api.getTenantReports).mockResolvedValue(mockReports);
    vi.mocked(api.getComplianceByPlacement).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(ReportsView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Q1 Summary');
    expect(wrapper.text()).toContain('Summary');
  });

  it('shows compliance results', async () => {
    vi.mocked(api.getReportTypes).mockResolvedValue(['Summary']);
    vi.mocked(api.getTenantReports).mockResolvedValue([]);
    vi.mocked(api.getComplianceByPlacement).mockResolvedValue(mockCompliance);
    const router = await createTestRouter();
    const wrapper = mount(ReportsView, { global: { plugins: [router] } });
    await flushPromises();
    // Switch to compliance tab
    const complianceTab = wrapper.findAll('button').find((b) => b.text() === 'Compliance Checks');
    await complianceTab!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('NSW');
    expect(wrapper.text()).toContain('VIC');
    expect(wrapper.text()).toContain('Setback too small');
  });
});
