<template>
  <ConfirmView :token-type="TOKEN_TYPE_PROJECT_MANAGER_INVITATION" />
</template>

<script setup>
import { TOKEN_TYPE_PROJECT_MANAGER_INVITATION } from "@/lib/consts/tokens";
import ConfirmView from "@/views/account/_ConfirmView";
</script>
