import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../App.vue'

// Mock localStorage for theme tests
const localStorageMock = {
  store: {},
  getItem: vi.fn((key) => localStorageMock.store[key] ?? null),
  setItem: vi.fn((key, val) => { localStorageMock.store[key] = val }),
  clear: vi.fn(() => { localStorageMock.store = {} }),
}
vi.stubGlobal('localStorage', localStorageMock)

describe('App', () => {
  it('renders the sidebar with all navigation items', () => {
    const wrapper = mount(App, { shallow: true })
    const navLinks = wrapper.findAll('nav a')
    expect(navLinks).toHaveLength(19)
    expect(wrapper.find('[data-nav="dashboard"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="flow"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="messages"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="inflight"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="subscriptions"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="connectors"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="events"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="dlq"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="replay"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="testmsg"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="controlbus"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="throttle"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="ratelimit"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="config"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="features"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="tenants"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="auditlog"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="dr"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="profiling"]').exists()).toBe(true)
  })

  it('sets dashboard as the default active page', () => {
    const wrapper = mount(App, { shallow: true })
    const dashboardLink = wrapper.find('[data-nav="dashboard"]')
    expect(dashboardLink.classes()).toContain('active')
  })

  it('displays the correct page title for dashboard', () => {
    const wrapper = mount(App, { shallow: true })
    expect(wrapper.find('h2').text()).toContain('Platform Dashboard')
  })

  it('switches active page when navigation is clicked', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="throttle"]').trigger('click')
    expect(wrapper.find('[data-nav="throttle"]').classes()).toContain('active')
    expect(wrapper.find('[data-nav="dashboard"]').classes()).not.toContain('active')
    expect(wrapper.find('h2').text()).toContain('Throttle Policies')
  })

  it('switches to DLQ page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="dlq"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('DLQ Management')
  })

  it('switches to DR Drills page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="dr"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('DR Drills')
  })

  it('switches to Profiling page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="profiling"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Performance Profiling')
  })

  it('switches to Message Flow page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="flow"]').trigger('click')
    expect(wrapper.find('[data-nav="flow"]').classes()).toContain('active')
    expect(wrapper.find('h2').text()).toContain('Message Flow Timeline')
  })

  it('switches to Test Messages page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="testmsg"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Test Message Generator')
  })

  it('switches to Config page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="config"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Configuration')
  })

  it('switches to Feature Flags page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="features"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Feature Flags')
  })

  it('switches to Tenants page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="tenants"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Tenant Management')
  })

  it('switches to In-Flight page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="inflight"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('In-Flight Messages')
  })

  it('switches to Subscriptions page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="subscriptions"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Subscription Viewer')
  })

  it('switches to Control Bus page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="controlbus"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Control Bus')
  })

  it('switches to Audit Log page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="auditlog"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Audit Log')
  })

  it('switches to Connectors page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="connectors"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Connector Health')
  })

  it('switches to Event Store page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="events"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Event Store Browser')
  })

  it('switches to Replay page on nav click', async () => {
    const wrapper = mount(App, { shallow: true })
    await wrapper.find('[data-nav="replay"]').trigger('click')
    expect(wrapper.find('h2').text()).toContain('Message Replay')
  })

  it('has a theme toggle button', () => {
    const wrapper = mount(App, { shallow: true })
    expect(wrapper.find('#btn-toggle-theme').exists()).toBe(true)
  })

  it('has a sidebar collapse button', () => {
    const wrapper = mount(App, { shallow: true })
    expect(wrapper.find('#btn-toggle-sidebar').exists()).toBe(true)
  })

  it('collapses sidebar on toggle click', async () => {
    const wrapper = mount(App, { shallow: true })
    expect(wrapper.find('.sidebar').classes()).not.toContain('collapsed')
    await wrapper.find('#btn-toggle-sidebar').trigger('click')
    expect(wrapper.find('.sidebar').classes()).toContain('collapsed')
  })
})
