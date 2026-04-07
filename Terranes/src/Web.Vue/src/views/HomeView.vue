<script setup lang="ts">
</script>

<template>
  <div class="container">
    <div class="text-center py-5">
      <h1 class="display-4">🏠 Welcome to Terranes</h1>
      <p class="lead text-muted">
        An immersive 3D property platform where buyers explore virtual villages,
        walk through fully designed homes, test-fit designs onto real land,
        and receive end-to-end indicative quotes — all before committing.
      </p>
    </div>

    <div class="row g-4 mb-5">
      <div class="col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🏘️ Virtual Villages</h3>
            <p class="card-text">Explore 3D neighbourhoods of fully designed homes with furniture and landscaping.</p>
            <router-link to="/villages" class="btn btn-primary">Explore Villages</router-link>
          </div>
        </div>
      </div>

      <div class="col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🏡 Home Designs</h3>
            <p class="card-text">Browse our gallery of 3D home models — filter by bedrooms, bathrooms, and more.</p>
            <router-link to="/home-models" class="btn btn-primary">Browse Designs</router-link>
          </div>
        </div>
      </div>

      <div class="col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🗺️ Find Land</h3>
            <p class="card-text">Search available land blocks by suburb, area, zoning, and frontage.</p>
            <router-link to="/land" class="btn btn-primary">Search Land</router-link>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-4 mb-5">
      <div class="col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🏬 Marketplace</h3>
            <p class="card-text">Browse property listings from agents, builders, and homeowners.</p>
            <router-link to="/marketplace" class="btn btn-outline-primary">View Listings</router-link>
          </div>
        </div>
      </div>

      <div class="col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🚀 Start Your Journey</h3>
            <p class="card-text">From browsing to quoting — experience the full buyer journey end-to-end.</p>
            <router-link to="/journey" class="btn btn-outline-primary">Begin Journey</router-link>
          </div>
        </div>
      </div>

      <div class="col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>📊 Dashboard</h3>
            <p class="card-text">View your journeys, notifications, and platform analytics.</p>
            <router-link to="/dashboard" class="btn btn-outline-primary">Open Dashboard</router-link>
          </div>
        </div>
      </div>
    </div>

    <div class="text-center text-muted">
      <p>
        <strong>Think CanIBuild + Envis + Matterport</strong> in one integrated platform.
        Instead of imagining the home — you <em>experience</em> it.
        Instead of guessing the budget — you <em>understand</em> it early.
      </p>
    </div>
  </div>
</template>
