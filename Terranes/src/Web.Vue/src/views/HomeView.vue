<script setup lang="ts">
import { ref } from 'vue';

const testimonialIndex = ref(0);

const testimonials = [
  { quote: 'Terranes helped us find our dream home and understand the budget before committing. Amazing experience!', author: 'Sarah M., First Home Buyer' },
  { quote: 'The 3D village walkthrough was incredible. We felt like we were already living there.', author: 'James & Priya K., Upgraders' },
  { quote: 'Getting an indicative quote so early in the process saved us months of uncertainty.', author: 'David L., Investor' },
];

function scrollTo(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
}

function prevTestimonial() {
  testimonialIndex.value = (testimonialIndex.value - 1 + testimonials.length) % testimonials.length;
}

function nextTestimonial() {
  testimonialIndex.value = (testimonialIndex.value + 1) % testimonials.length;
}
</script>

<template>
  <div class="container">
    <section id="hero" class="hero text-center text-white py-5 mb-5 rounded-4">
      <h1 class="display-4 fw-bold">🏠 Welcome to Terranes</h1>
      <p class="lead mx-auto" style="max-width: 600px;">
        An immersive 3D property platform where buyers explore virtual villages,
        walk through fully designed homes, test-fit designs onto real land,
        and receive end-to-end indicative quotes — all before committing.
      </p>
      <a href="#how-it-works" class="btn btn-light btn-lg mt-3" @click.prevent="scrollTo('how-it-works')">Explore How It Works</a>
    </section>

    <section id="how-it-works" class="mb-5">
      <h2 class="text-center mb-4">How It Works</h2>
      <div class="row g-4 text-center">
        <div class="col-12 col-md-3">
          <div class="step-icon mb-2 fs-1">🔍</div>
          <h5>Browse</h5>
          <p class="text-muted">Explore virtual villages and home designs in immersive 3D.</p>
        </div>
        <div class="col-12 col-md-3">
          <div class="step-icon mb-2 fs-1">🏡</div>
          <h5>Select</h5>
          <p class="text-muted">Pick your favourite home design and land block.</p>
        </div>
        <div class="col-12 col-md-3">
          <div class="step-icon mb-2 fs-1">🎨</div>
          <h5>Customise</h5>
          <p class="text-muted">Personalise your home with finishes and layouts.</p>
        </div>
        <div class="col-12 col-md-3">
          <div class="step-icon mb-2 fs-1">💰</div>
          <h5>Quote</h5>
          <p class="text-muted">Get an indicative quote from our partner network.</p>
        </div>
      </div>
    </section>

    <div class="row g-4 mb-5">
      <div class="col-12 col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🏘️ Virtual Villages</h3>
            <p class="card-text">Explore 3D neighbourhoods of fully designed homes with furniture and landscaping.</p>
            <router-link to="/villages" class="btn btn-primary">Explore Villages</router-link>
          </div>
        </div>
      </div>

      <div class="col-12 col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🏡 Home Designs</h3>
            <p class="card-text">Browse our gallery of 3D home models — filter by bedrooms, bathrooms, and more.</p>
            <router-link to="/home-models" class="btn btn-primary">Browse Designs</router-link>
          </div>
        </div>
      </div>

      <div class="col-12 col-md-4">
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
      <div class="col-12 col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🏬 Marketplace</h3>
            <p class="card-text">Browse property listings from agents, builders, and homeowners.</p>
            <router-link to="/marketplace" class="btn btn-outline-primary">View Listings</router-link>
          </div>
        </div>
      </div>

      <div class="col-12 col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>🚀 Start Your Journey</h3>
            <p class="card-text">From browsing to quoting — experience the full buyer journey end-to-end.</p>
            <router-link to="/journey" class="btn btn-outline-primary">Begin Journey</router-link>
          </div>
        </div>
      </div>

      <div class="col-12 col-md-4">
        <div class="card h-100 shadow-sm">
          <div class="card-body text-center">
            <h3>📊 Dashboard</h3>
            <p class="card-text">View your journeys, notifications, and platform analytics.</p>
            <router-link to="/dashboard" class="btn btn-outline-primary">Open Dashboard</router-link>
          </div>
        </div>
      </div>
    </div>

    <section class="testimonials text-center mb-5">
      <h2 class="mb-4">What Our Users Say</h2>
      <div class="mx-auto" style="max-width: 600px;">
        <blockquote class="blockquote">
          <p>&ldquo;{{ testimonials[testimonialIndex].quote }}&rdquo;</p>
          <footer class="blockquote-footer">{{ testimonials[testimonialIndex].author }}</footer>
        </blockquote>
        <div class="mt-3">
          <button class="btn btn-outline-secondary me-2" aria-label="Previous testimonial" @click="prevTestimonial">← Prev</button>
          <button class="btn btn-outline-secondary" aria-label="Next testimonial" @click="nextTestimonial">Next →</button>
        </div>
      </div>
    </section>

    <div class="text-center text-muted mb-4">
      <p>
        <strong>Think CanIBuild + Envis + Matterport</strong> in one integrated platform.
        Instead of imagining the home — you <em>experience</em> it.
        Instead of guessing the budget — you <em>understand</em> it early.
      </p>
    </div>

    <footer class="site-footer text-center py-4 border-top text-muted">
      <p class="mb-1">&copy; {{ new Date().getFullYear() }} Terranes. All rights reserved.</p>
      <small>Built with Vue 3, TypeScript, and Bootstrap 5</small>
    </footer>
  </div>
</template>

<style scoped>
.hero {
  background: linear-gradient(135deg, #052767 0%, #3a0647 50%, #052767 100%);
  background-size: 200% 200%;
  animation: heroGradient 8s ease infinite;
}

@keyframes heroGradient {
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
}

@media (prefers-reduced-motion: reduce) {
  .hero {
    animation: none;
  }
}
</style>
