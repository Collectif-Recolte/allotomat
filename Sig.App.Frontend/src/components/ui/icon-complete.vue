<template>
  <Transition v-bind="completeTransition" appear>
    <svg
      class="h-20 w-20 absolute pointer-events-none -translate-x-1/2 left-1/2 -top-10"
      width="75"
      height="75"
      viewBox="0 0 75 75"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <path
        d="M7.26799 32.3613L7.26794 32.3613C4.43509 35.1942 4.43512 39.8027 7.26792 42.6356L7.26794 42.6356L32.3612 67.7288C33.7797 69.1473 35.6419 69.8541 37.4996 69.8541C39.3582 69.8541 41.2169 69.1467 42.6368 67.73L42.6379 67.7288L67.7312 42.6356C70.564 39.8027 70.564 35.1942 67.7312 32.3613L66.6705 33.422L67.7312 32.3613L42.6381 7.26806C39.8027 4.43269 35.1993 4.4327 32.3638 7.268C32.3638 7.26801 32.3638 7.26803 32.3638 7.26804L7.26799 32.3613ZM49.3172 30.0547C49.5159 30.2516 49.5252 30.5789 49.3196 30.7923L49.3145 30.7974L35.1542 44.9577C35.0556 45.0563 34.9221 45.1125 34.7778 45.1125C34.6336 45.1125 34.5001 45.0563 34.4015 44.9578C34.4015 44.9578 34.4015 44.9577 34.4014 44.9577L25.6824 36.2361L25.6823 36.236C25.4733 36.027 25.4733 35.6945 25.6823 35.4856C25.8912 35.2767 26.2235 35.2766 26.4325 35.4854C26.4325 35.4855 26.4326 35.4855 26.4327 35.4856L33.717 42.7725L34.7777 43.8336L35.8385 42.7727L48.5616 30.0496C48.7706 29.8407 49.1031 29.8407 49.312 30.0496L49.3172 30.0547Z"
        fill="#71AB79"
        stroke="white"
        stroke-width="3" />
    </svg>
  </Transition>
</template>

<script setup>
// Style de transitions
const completeTransition = {
  enterActiveClass: "transition ease-out duration-500 delay-200",
  enterFromClass: "scale-0 motion-reduce:scale-1",
  enterToClass: "scale-1",
  leaveActiveClass: "transition duration-700 ease-[cubic-bezier(1,0.5,0.8,1)]",
  leaveFromClass: "scale-1",
  leaveToClass: "scale-0 motion-reduce:scale-1",
  mode: "out-in"
};
</script>
