import './css/app.css';
import Vue from 'vue';
import App from './app.vue';
import router from './router';
import globalMixins from './mixins/global-mixins';
import vuetify from './plugins/vuetify';
import Popover from 'vue-js-popover';

Vue.use(Popover);
Vue.mixin(globalMixins);

// Add a global event bus for components to use to communicate
import { eventHub } from '~/modules/eventHub';
Vue.prototype.$eventHub = eventHub;

new Vue({
  router,
  vuetify,
  render: h => h(App)
}).$mount('#app');

window.$eventHub = eventHub;


