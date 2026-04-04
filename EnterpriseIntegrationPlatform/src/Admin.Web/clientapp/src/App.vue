<template>
  <div class="sidebar">
    <h1>⚙️ EIP Admin</h1>
    <nav>
      <a :class="{ active: currentPage === 'dashboard' }" @click="currentPage = 'dashboard'" data-nav="dashboard">📊 Dashboard</a>
      <a :class="{ active: currentPage === 'throttle' }" @click="currentPage = 'throttle'" data-nav="throttle">🔧 Throttle</a>
      <a :class="{ active: currentPage === 'ratelimit' }" @click="currentPage = 'ratelimit'" data-nav="ratelimit">🚦 Rate Limiting</a>
      <a :class="{ active: currentPage === 'dlq' }" @click="currentPage = 'dlq'" data-nav="dlq">📬 DLQ</a>
      <a :class="{ active: currentPage === 'messages' }" @click="currentPage = 'messages'" data-nav="messages">🔍 Messages</a>
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
      <ThrottlePage v-if="currentPage === 'throttle'" />
      <RateLimitPage v-if="currentPage === 'ratelimit'" />
      <DlqPage v-if="currentPage === 'dlq'" />
      <MessagesPage v-if="currentPage === 'messages'" />
      <DrDrillsPage v-if="currentPage === 'dr'" />
      <ProfilingPage v-if="currentPage === 'profiling'" />
    </div>
  </div>
</template>

<script>
import DashboardPage from './components/DashboardPage.vue'
import ThrottlePage from './components/ThrottlePage.vue'
import RateLimitPage from './components/RateLimitPage.vue'
import DlqPage from './components/DlqPage.vue'
import MessagesPage from './components/MessagesPage.vue'
import DrDrillsPage from './components/DrDrillsPage.vue'
import ProfilingPage from './components/ProfilingPage.vue'

export default {
  name: 'App',
  components: {
    DashboardPage,
    ThrottlePage,
    RateLimitPage,
    DlqPage,
    MessagesPage,
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
        dashboard: '�� Platform Dashboard',
        throttle: '🔧 Throttle Policies',
        ratelimit: '🚦 Rate Limiting',
        dlq: '📬 DLQ Management',
        messages: '🔍 Message Inspector',
        dr: '🛡️ DR Drills',
        profiling: '📈 Performance Profiling',
      }
      return titles[this.currentPage] || 'Dashboard'
    },
  },
}
</script>
