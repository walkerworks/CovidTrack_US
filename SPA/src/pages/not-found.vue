<template>
  <div class="page" data-name="not-found">
    <div class="fab fab-container-btm">
      <a href="#" class="backAction" @click.prevent="$router.go(-1)">
        <font-awesome-icon :style="{ color: 'white' }" size="2x" transform="shrink-2" :icon="['fal', 'arrow-left']" />
      </a>
    </div>
    <div class="page-content">
      <nav-standard pageId="not-found"></nav-standard>
      <div class="page-title-bar">
        <h2>Not Found</h2>
      </div>
      <div class="block">
        <p>Sorry!</p>
      </div>
      <div class="block">
        <p>The requested content not found.</p>
      </div>
    </div>
  </div>
</template>
<script>
export default {};
</script>