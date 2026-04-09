<template>
  <div class="sidebar" :class="{ collapsed: sidebarCollapsed }">
    <div class="sidebar-header">
      <h1 v-if="!sidebarCollapsed">⚙️ EIP Admin</h1>
      <h1 v-else>⚙️</h1>
      <button class="btn-collapse" @click="sidebarCollapsed = !sidebarCollapsed" id="btn-toggle-sidebar" :title="sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'">
        {{ sidebarCollapsed ? '▶' : '◀' }}
      </button>
    </div>
    <div class="sidebar-section" v-if="!sidebarCollapsed"><span class="section-label">Monitoring</span></div>
    <nav>
      <a :class="{ active: currentPage === 'dashboard' }" @click="currentPage = 'dashboard'" data-nav="dashboard" :title="sidebarCollapsed ? 'Dashboard' : ''">📊 <span v-if="!sidebarCollapsed">Dashboard</span></a>
      <a :class="{ active: currentPage === 'flow' }" @click="currentPage = 'flow'" data-nav="flow" :title="sidebarCollapsed ? 'Message Flow' : ''">🔀 <span v-if="!sidebarCollapsed">Message Flow</span></a>
      <a :class="{ active: currentPage === 'messages' }" @click="currentPage = 'messages'" data-nav="messages" :title="sidebarCollapsed ? 'Messages' : ''">🔍 <span v-if="!sidebarCollapsed">Messages</span></a>
      <a :class="{ active: currentPage === 'inflight' }" @click="currentPage = 'inflight'" data-nav="inflight" :title="sidebarCollapsed ? 'In-Flight' : ''">⚡ <span v-if="!sidebarCollapsed">In-Flight</span></a>
      <a :class="{ active: currentPage === 'subscriptions' }" @click="currentPage = 'subscriptions'" data-nav="subscriptions" :title="sidebarCollapsed ? 'Subscriptions' : ''">📡 <span v-if="!sidebarCollapsed">Subscriptions</span></a>
      <a :class="{ active: currentPage === 'connectors' }" @click="currentPage = 'connectors'" data-nav="connectors" :title="sidebarCollapsed ? 'Connectors' : ''">🔌 <span v-if="!sidebarCollapsed">Connectors</span></a>
      <a :class="{ active: currentPage === 'events' }" @click="currentPage = 'events'" data-nav="events" :title="sidebarCollapsed ? 'Event Store' : ''">📚 <span v-if="!sidebarCollapsed">Event Store</span></a>
    </nav>
    <div class="sidebar-section" v-if="!sidebarCollapsed"><span class="section-label">Operations</span></div>
    <nav>
      <a :class="{ active: currentPage === 'dlq' }" @click="currentPage = 'dlq'" data-nav="dlq" :title="sidebarCollapsed ? 'DLQ' : ''">📬 <span v-if="!sidebarCollapsed">DLQ</span></a>
      <a :class="{ active: currentPage === 'replay' }" @click="currentPage = 'replay'" data-nav="replay" :title="sidebarCollapsed ? 'Replay' : ''">⏪ <span v-if="!sidebarCollapsed">Replay</span></a>
      <a :class="{ active: currentPage === 'testmsg' }" @click="currentPage = 'testmsg'" data-nav="testmsg" :title="sidebarCollapsed ? 'Test Messages' : ''">🧪 <span v-if="!sidebarCollapsed">Test Messages</span></a>
      <a :class="{ active: currentPage === 'controlbus' }" @click="currentPage = 'controlbus'" data-nav="controlbus" :title="sidebarCollapsed ? 'Control Bus' : ''">🎛️ <span v-if="!sidebarCollapsed">Control Bus</span></a>
    </nav>
    <div class="sidebar-section" v-if="!sidebarCollapsed"><span class="section-label">Configuration</span></div>
    <nav>
      <a :class="{ active: currentPage === 'throttle' }" @click="currentPage = 'throttle'" data-nav="throttle" :title="sidebarCollapsed ? 'Throttle' : ''">🔧 <span v-if="!sidebarCollapsed">Throttle</span></a>
      <a :class="{ active: currentPage === 'ratelimit' }" @click="currentPage = 'ratelimit'" data-nav="ratelimit" :title="sidebarCollapsed ? 'Rate Limiting' : ''">🚦 <span v-if="!sidebarCollapsed">Rate Limiting</span></a>
      <a :class="{ active: currentPage === 'config' }" @click="currentPage = 'config'" data-nav="config" :title="sidebarCollapsed ? 'Config' : ''">⚙️ <span v-if="!sidebarCollapsed">Config</span></a>
      <a :class="{ active: currentPage === 'features' }" @click="currentPage = 'features'" data-nav="features" :title="sidebarCollapsed ? 'Feature Flags' : ''">🚩 <span v-if="!sidebarCollapsed">Feature Flags</span></a>
      <a :class="{ active: currentPage === 'tenants' }" @click="currentPage = 'tenants'" data-nav="tenants" :title="sidebarCollapsed ? 'Tenants' : ''">🏢 <span v-if="!sidebarCollapsed">Tenants</span></a>
    </nav>
    <div class="sidebar-section" v-if="!sidebarCollapsed"><span class="section-label">System</span></div>
    <nav>
      <a :class="{ active: currentPage === 'auditlog' }" @click="currentPage = 'auditlog'" data-nav="auditlog" :title="sidebarCollapsed ? 'Audit Log' : ''">📋 <span v-if="!sidebarCollapsed">Audit Log</span></a>
      <a :class="{ active: currentPage === 'dr' }" @click="currentPage = 'dr'" data-nav="dr" :title="sidebarCollapsed ? 'DR Drills' : ''">🛡️ <span v-if="!sidebarCollapsed">DR Drills</span></a>
      <a :class="{ active: currentPage === 'profiling' }" @click="currentPage = 'profiling'" data-nav="profiling" :title="sidebarCollapsed ? 'Profiling' : ''">📈 <span v-if="!sidebarCollapsed">Profiling</span></a>
    </nav>
    <div class="sidebar-footer" v-if="!sidebarCollapsed">
      <button class="btn-theme" @click="toggleTheme" id="btn-toggle-theme" :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'">
        {{ isDark ? '☀️ Light' : '🌙 Dark' }}
      </button>
    </div>
  </div>
  <div class="main">
    <header>
      <h2>{{ pageTitle }}</h2>
    </header>
    <div class="content">
      <DashboardPage v-if="currentPage === 'dashboard'" />
      <MessageFlowPage v-if="currentPage === 'flow'" />
      <MessagesPage v-if="currentPage === 'messages'" />
      <InFlightPage v-if="currentPage === 'inflight'" />
      <SubscriptionsPage v-if="currentPage === 'subscriptions'" />
      <ConnectorsPage v-if="currentPage === 'connectors'" />
      <EventStorePage v-if="currentPage === 'events'" />
      <DlqPage v-if="currentPage === 'dlq'" />
      <ReplayPage v-if="currentPage === 'replay'" />
      <TestMessagesPage v-if="currentPage === 'testmsg'" />
      <ControlBusPage v-if="currentPage === 'controlbus'" />
      <ThrottlePage v-if="currentPage === 'throttle'" />
      <RateLimitPage v-if="currentPage === 'ratelimit'" />
      <ConfigPage v-if="currentPage === 'config'" />
      <FeatureFlagsPage v-if="currentPage === 'features'" />
      <TenantsPage v-if="currentPage === 'tenants'" />
      <AuditLogPage v-if="currentPage === 'auditlog'" />
      <DrDrillsPage v-if="currentPage === 'dr'" />
      <ProfilingPage v-if="currentPage === 'profiling'" />
    </div>
  </div>
  <!-- Toast container -->
  <div class="toast-container" id="toast-container">
    <div v-for="(toast, i) in toasts" :key="toast.id" class="toast" :class="'toast-' + toast.type">
      <span>{{ toast.message }}</span>
      <button class="toast-close" @click="removeToast(i)">✕</button>
    </div>
  </div>
</template>

<script>
import DashboardPage from './components/DashboardPage.vue'
import MessageFlowPage from './components/MessageFlowPage.vue'
import MessagesPage from './components/MessagesPage.vue'
import InFlightPage from './components/InFlightPage.vue'
import SubscriptionsPage from './components/SubscriptionsPage.vue'
import ConnectorsPage from './components/ConnectorsPage.vue'
import EventStorePage from './components/EventStorePage.vue'
import DlqPage from './components/DlqPage.vue'
import ReplayPage from './components/ReplayPage.vue'
import TestMessagesPage from './components/TestMessagesPage.vue'
import ControlBusPage from './components/ControlBusPage.vue'
import ThrottlePage from './components/ThrottlePage.vue'
import RateLimitPage from './components/RateLimitPage.vue'
import ConfigPage from './components/ConfigPage.vue'
import FeatureFlagsPage from './components/FeatureFlagsPage.vue'
import TenantsPage from './components/TenantsPage.vue'
import AuditLogPage from './components/AuditLogPage.vue'
import DrDrillsPage from './components/DrDrillsPage.vue'
import ProfilingPage from './components/ProfilingPage.vue'

export default {
  name: 'App',
  components: {
    DashboardPage,
    MessageFlowPage,
    MessagesPage,
    InFlightPage,
    SubscriptionsPage,
    ConnectorsPage,
    EventStorePage,
    DlqPage,
    ReplayPage,
    TestMessagesPage,
    ControlBusPage,
    ThrottlePage,
    RateLimitPage,
    ConfigPage,
    FeatureFlagsPage,
    TenantsPage,
    AuditLogPage,
    DrDrillsPage,
    ProfilingPage,
  },
  data() {
    return {
      currentPage: 'dashboard',
      sidebarCollapsed: false,
      isDark: true,
      toasts: [],
      toastIdCounter: 0,
    }
  },
  computed: {
    pageTitle() {
      const titles = {
        dashboard: '📊 Platform Dashboard',
        flow: '🔀 Message Flow Timeline',
        messages: '🔍 Message Inspector',
        inflight: '⚡ In-Flight Messages',
        subscriptions: '📡 Subscription Viewer',
        connectors: '🔌 Connector Health',
        events: '📚 Event Store Browser',
        dlq: '📬 DLQ Management',
        replay: '⏪ Message Replay',
        testmsg: '🧪 Test Message Generator',
        controlbus: '🎛️ Control Bus',
        throttle: '🔧 Throttle Policies',
        ratelimit: '🚦 Rate Limiting',
        config: '⚙️ Configuration',
        features: '🚩 Feature Flags',
        tenants: '🏢 Tenant Management',
        auditlog: '📋 Audit Log',
        dr: '🛡️ DR Drills',
        profiling: '📈 Performance Profiling',
      }
      return titles[this.currentPage] || 'Dashboard'
    },
  },
  mounted() {
    const savedTheme = localStorage.getItem('eip-theme')
    if (savedTheme) {
      this.isDark = savedTheme === 'dark'
    }
    this.applyTheme()
  },
  methods: {
    toggleTheme() {
      this.isDark = !this.isDark
      localStorage.setItem('eip-theme', this.isDark ? 'dark' : 'light')
      this.applyTheme()
    },
    applyTheme() {
      document.documentElement.setAttribute('data-theme', this.isDark ? 'dark' : 'light')
    },
    addToast(message, type = 'info') {
      const id = ++this.toastIdCounter
      this.toasts.push({ id, message, type })
      setTimeout(() => {
        const idx = this.toasts.findIndex(t => t.id === id)
        if (idx >= 0) this.toasts.splice(idx, 1)
      }, 4000)
    },
    removeToast(index) {
      this.toasts.splice(index, 1)
    },
  },
}
</script>
