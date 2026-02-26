<template>
  <RouterView v-slot="{ Component }">
    <AppShell no-padding>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>
