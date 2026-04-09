<script setup lang="ts">
import { ref } from 'vue';

const testimonials = [
  { name: 'Sarah M.', location: 'Sydney, NSW', quote: 'Terranes let me walk through my dream home before it was even built. The 3D experience is incredible!' },
  { name: 'James T.', location: 'Melbourne, VIC', quote: 'The test-fit feature saved me months of back-and-forth with builders. I could see exactly how the home fits the block.' },
  { name: 'Priya K.', location: 'Brisbane, QLD', quote: 'Getting an indicative quote so early in the process gave me real confidence to move forward.' },
];

const activeTestimonial = ref(0);

function nextTestimonial() {
  activeTestimonial.value = (activeTestimonial.value + 1) % testimonials.length;
}

function prevTestimonial() {
  activeTestimonial.value = (activeTestimonial.value - 1 + testimonials.length) % testimonials.length;
}
</script>

<template>
  <div>
    <!-- Hero Section -->
    <section class="hero-section text-center text-white py-5 mb-5">
      <div class="container py-4">
        <h1 class="display-4 fw-bold">🏠 Welcome to Terranes</h1>
        <p class="lead mt-3 mb-4">
          An immersive 3D property platform where buyers explore virtual villages,
          walk through fully designed homes, test-fit designs onto real land,
          and receive end-to-end indicative quotes — all before committing.
        </p>
        <div class="d-flex justify-content-center gap-3 flex-wrap">
          <router-link to="/villages" class="btn btn-light btn-lg">Explore Villages</router-link>
          <router-link to="/journey" class="btn btn-outline-light btn-lg">Start Your Journey</router-link>
        </div>
      </div>
    </section>

    <div class="container">
      <!-- How It Works -->
      <section id="how-it-works" class="mb-5">
        <h2 class="text-center mb-4">How It Works</h2>
        <div class="row g-4 text-center">
          <div class="col-6 col-md-3">
            <div class="how-step">
              <div class="step-icon mb-2">🏘️</div>
              <div class="step-number">1</div>
              <h6>Explore Villages</h6>
              <p class="text-muted small">Browse immersive 3D neighbourhoods with fully designed homes.</p>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="how-step">
              <div class="step-icon mb-2">🏡</div>
              <div class="step-number">2</div>
              <h6>Choose a Design</h6>
              <p class="text-muted small">Pick from our gallery of 3D home models that match your style.</p>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="how-step">
              <div class="step-icon mb-2">🗺️</div>
              <div class="step-number">3</div>
              <h6>Test-Fit on Land</h6>
              <p class="text-muted small">Place your chosen design on a real land block to check fit.</p>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="how-step">
              <div class="step-icon mb-2">💰</div>
              <div class="step-number">4</div>
              <h6>Get a Quote</h6>
              <p class="text-muted small">Receive an indicative quote from our partner network instantly.</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Feature Cards -->
      <section class="mb-5">
        <h2 class="text-center mb-4">Platform Features</h2>
        <div class="row g-4">
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

        <div class="row g-4 mt-1">
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
      </section>

      <!-- Testimonials -->
      <section class="mb-5">
        <h2 class="text-center mb-4">What Our Users Say</h2>
        <div class="testimonial-carousel mx-auto" style="max-width: 600px;">
          <div class="card shadow-sm">
            <div class="card-body text-center py-4">
              <p class="mb-3 fst-italic">"{{ testimonials[activeTestimonial].quote }}"</p>
              <strong>{{ testimonials[activeTestimonial].name }}</strong>
              <br />
              <small class="text-muted">{{ testimonials[activeTestimonial].location }}</small>
            </div>
          </div>
          <div class="d-flex justify-content-center gap-3 mt-3">
            <button class="btn btn-sm btn-outline-secondary" aria-label="Previous testimonial" @click="prevTestimonial">← Prev</button>
            <span class="align-self-center text-muted small">
              {{ activeTestimonial + 1 }} / {{ testimonials.length }}
            </span>
            <button class="btn btn-sm btn-outline-secondary" aria-label="Next testimonial" @click="nextTestimonial">Next →</button>
          </div>
        </div>
      </section>

      <!-- Tagline -->
      <div class="text-center text-muted mb-5">
        <p>
          <strong>Think CanIBuild + Envis + Matterport</strong> in one integrated platform.
          Instead of imagining the home — you <em>experience</em> it.
          Instead of guessing the budget — you <em>understand</em> it early.
        </p>
      </div>

      <!-- Footer -->
      <footer class="border-top pt-4 pb-3 text-center text-muted">
        <div class="row">
          <div class="col-12 col-md-4 mb-3">
            <h6>Platform</h6>
            <ul class="list-unstyled">
              <li><router-link to="/villages" class="text-muted text-decoration-none">Villages</router-link></li>
              <li><router-link to="/home-models" class="text-muted text-decoration-none">Home Designs</router-link></li>
              <li><router-link to="/land" class="text-muted text-decoration-none">Land Blocks</router-link></li>
            </ul>
          </div>
          <div class="col-12 col-md-4 mb-3">
            <h6>Services</h6>
            <ul class="list-unstyled">
              <li><router-link to="/marketplace" class="text-muted text-decoration-none">Marketplace</router-link></li>
              <li><router-link to="/journey" class="text-muted text-decoration-none">Buyer Journey</router-link></li>
              <li><router-link to="/dashboard" class="text-muted text-decoration-none">Dashboard</router-link></li>
            </ul>
          </div>
          <div class="col-12 col-md-4 mb-3">
            <h6>About</h6>
            <ul class="list-unstyled">
              <li><a href="https://learn.microsoft.com/aspnet/core/" target="_blank" class="text-muted text-decoration-none">Documentation</a></li>
              <li><span class="text-muted">© 2026 Terranes</span></li>
            </ul>
          </div>
        </div>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.hero-section {
  background: linear-gradient(135deg, #052767 0%, #3a0647 50%, #052767 100%);
  background-size: 200% 200%;
  animation: hero-gradient 8s ease infinite;
  margin: -1.5rem -1.5rem 0;
  padding-left: 1.5rem;
  padding-right: 1.5rem;
}

@keyframes hero-gradient {
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
}

@media (prefers-reduced-motion: reduce) {
  .hero-section {
    animation: none;
  }
}

.how-step {
  position: relative;
  padding: 1rem;
}

.step-icon {
  font-size: 2rem;
}

.step-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  background-color: var(--bs-primary, #0d6efd);
  color: white;
  font-weight: bold;
  font-size: 0.875rem;
  margin-bottom: 0.5rem;
}
</style>
