<script setup lang="ts">
withDefaults(defineProps<{
  rows?: number;
  cols?: number;
}>(), {
  rows: 5,
  cols: 4,
});
</script>

<template>
  <div class="table-responsive" aria-hidden="true">
    <table class="table placeholder-glow">
      <thead>
        <tr>
          <th v-for="c in cols" :key="c">
            <span class="placeholder col-8"></span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r">
          <td v-for="c in cols" :key="c">
            <span class="placeholder" :class="`col-${4 + (c % 5)}`"></span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
