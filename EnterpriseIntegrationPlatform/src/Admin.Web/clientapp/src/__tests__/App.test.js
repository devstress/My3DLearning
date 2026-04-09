import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../App.vue'

describe('App', () => {
  it('renders the sidebar with all navigation items', () => {
    const wrapper = mount(App, { shallow: true })
    const navLinks = wrapper.findAll('nav a')
    expect(navLinks).toHaveLength(12)
    expect(wrapper.find('[data-nav="dashboard"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="flow"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="messages"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="dlq"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="testmsg"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="throttle"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="ratelimit"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="config"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="features"]').exists()).toBe(true)
    expect(wrapper.find('[data-nav="tenants"]').exists()).toBe(true)
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
})
