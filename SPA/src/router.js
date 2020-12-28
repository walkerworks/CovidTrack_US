import Vue from 'vue';
import Router from 'vue-router';
import Home from './pages/Home.vue';
import Feedback from './pages/Feedback.vue';
import Counties from './pages/Counties.vue';
import CountiesManual from './pages/counties-manual.vue';
import { getLocalUser} from '~/modules/utils/session';


Vue.use(Router);

const router = new Router({
  mode: 'hash',
  base: process.env.BASE_URL,
  routes: [
    {
      path: '/',
      name: 'Home',
      component: Home,
      meta: 'Sign up / Log in',
    },
    {
      path: '/About',
      name: 'About',
      component: Home,
      meta: 'Sign up / Log in',
      props:true,
    },
    {
      path: '/counties',
      name: 'Counties',
      component: Counties,
      meta: 'County Selection & Notifications',
    },
    {
      path: '/counties-manual',
      name: 'CountiesManual',
      component: CountiesManual,
      meta: 'Manual County Selection & Notifications',
    },
    {
      path: '/feedback/:feedbackId/:subscriberId',
      name: 'Feedback',
      component: Feedback,
      props:true,
      meta: 'Feedback',
    },
  ]
});

router.beforeEach((to, from, next) => {
  let user = getLocalUser();
  if(to.name === 'Counties' && !user) next({name: 'Home'});
  else if(to.name === 'Home' && user) next({name: 'Counties'});
  else next();
});

window.$router = router;
export default router;