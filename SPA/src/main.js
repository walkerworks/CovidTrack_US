import './css/app.css';
import Vue from 'vue';
import App from './app.vue';
import router from './router';
import globalMixins from './mixins/global-mixins';
import vuetify from './plugins/vuetify';
import Popover from 'vue-js-popover';
import Modal from './components/modal';
import '../node_modules/vuetify/dist/vuetify.min.css';
import VueAnalytics from 'vue-analytics';
Vue.component('modal', Modal);

Vue.use(Popover);
Vue.mixin(globalMixins);

Vue.use(VueAnalytics, {
  id: 'G-Q0SZL5VSWW',
  router
});

// Add a global event bus for components to use to communicate
import { eventHub } from '~/modules/eventHub';
Vue.prototype.$eventHub = eventHub;

new Vue({
  router,
  vuetify,
  render: h => h(App)
}).$mount('#app');

window.$eventHub = eventHub;


