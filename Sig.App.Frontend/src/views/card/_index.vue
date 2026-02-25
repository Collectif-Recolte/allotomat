<template>
  <RouterView v-slot="{ Component }">
    <AppShell no-padding>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup></script>
