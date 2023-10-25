<template>
  <ConfirmView :token-type="TOKEN_TYPE_MERCHANT_INVITATION" />
</template>

<script setup>
import { TOKEN_TYPE_MERCHANT_INVITATION } from "@/lib/consts/tokens";
import ConfirmView from "@/views/account/_ConfirmView";
</script>
