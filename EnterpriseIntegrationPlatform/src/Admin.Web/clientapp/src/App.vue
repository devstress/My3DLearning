<template>
  <div class="sidebar">
    <h1>⚙️ EIP Admin</h1>
    <nav>
      <a :class="{ active: currentPage === 'dashboard' }" @click="currentPage = 'dashboard'" data-nav="dashboard">📊 Dashboard</a>
      <a :class="{ active: currentPage === 'flow' }" @click="currentPage = 'flow'" data-nav="flow">🔀 Message Flow</a>
      <a :class="{ active: currentPage === 'messages' }" @click="currentPage = 'messages'" data-nav="messages">🔍 Messages</a>
      <a :class="{ active: currentPage === 'dlq' }" @click="currentPage = 'dlq'" data-nav="dlq">📬 DLQ</a>
      <a :class="{ active: currentPage === 'testmsg' }" @click="currentPage = 'testmsg'" data-nav="testmsg">🧪 Test Messages</a>
      <a :class="{ active: currentPage === 'throttle' }" @click="currentPage = 'throttle'" data-nav="throttle">🔧 Throttle</a>
      <a :class="{ active: currentPage === 'ratelimit' }" @click="currentPage = 'ratelimit'" data-nav="ratelimit">🚦 Rate Limiting</a>
      <a :class="{ active: currentPage === 'config' }" @click="currentPage = 'config'" data-nav="config">⚡ Config</a>
      <a :class="{ active: currentPage === 'features' }" @click="currentPage = 'features'" data-nav="features">🚩 Feature Flags</a>
      <a :class="{ active: currentPage === 'tenants' }" @click="currentPage = 'tenants'" data-nav="tenants">🏢 Tenants</a>
      <a :class="{ active: currentPage === 'dr' }" @click="currentPage = 'dr'" data-nav="dr">🛡️ DR Drills</a>
      <a :class="{ active: currentPage === 'profiling' }" @click="currentPage = 'profiling'" data-nav="profiling">📈 Profiling</a>
    </nav>
  </div>
  <div class="main">
    <header>
      <h2>{{ pageTitle }}</h2>
    </header>
    <div class="content">
      <DashboardPage v-if="currentPage === 'dashboard'" />
      <MessageFlowPage v-if="currentPage === 'flow'" />
      <MessagesPage v-if="currentPage === 'messages'" />
      <DlqPage v-if="currentPage === 'dlq'" />
      <TestMessagesPage v-if="currentPage === 'testmsg'" />
      <ThrottlePage v-if="currentPage === 'throttle'" />
      <RateLimitPage v-if="currentPage === 'ratelimit'" />
      <ConfigPage v-if="currentPage === 'config'" />
      <FeatureFlagsPage v-if="currentPage === 'features'" />
      <TenantsPage v-if="currentPage === 'tenants'" />
      <DrDrillsPage v-if="currentPage === 'dr'" />
      <ProfilingPage v-if="currentPage === 'profiling'" />
    </div>
  </div>
</template>

<script>
import DashboardPage from './components/DashboardPage.vue'
import MessageFlowPage from './components/MessageFlowPage.vue'
import MessagesPage from './components/MessagesPage.vue'
import DlqPage from './components/DlqPage.vue'
import TestMessagesPage from './components/TestMessagesPage.vue'
import ThrottlePage from './components/ThrottlePage.vue'
import RateLimitPage from './components/RateLimitPage.vue'
import ConfigPage from './components/ConfigPage.vue'
import FeatureFlagsPage from './components/FeatureFlagsPage.vue'
import TenantsPage from './components/TenantsPage.vue'
import DrDrillsPage from './components/DrDrillsPage.vue'
import ProfilingPage from './components/ProfilingPage.vue'

export default {
  name: 'App',
  components: {
    DashboardPage,
    MessageFlowPage,
    MessagesPage,
    DlqPage,
    TestMessagesPage,
    ThrottlePage,
    RateLimitPage,
    ConfigPage,
    FeatureFlagsPage,
    TenantsPage,
    DrDrillsPage,
    ProfilingPage,
  },
  data() {
    return {
      currentPage: 'dashboard',
    }
  },
  computed: {
    pageTitle() {
      const titles = {
        dashboard: '📊 Platform Dashboard',
        flow: '🔀 Message Flow Timeline',
        messages: '🔍 Message Inspector',
        dlq: '📬 DLQ Management',
        testmsg: '🧪 Test Message Generator',
        throttle: '🔧 Throttle Policies',
        ratelimit: '🚦 Rate Limiting',
        config: '⚡ Configuration',
        features: '🚩 Feature Flags',
        tenants: '🏢 Tenant Management',
        dr: '🛡️ DR Drills',
        profiling: '📈 Performance Profiling',
      }
      return titles[this.currentPage] || 'Dashboard'
    },
  },
}
</script>
