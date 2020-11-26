<template>
  <div id="app">
    <v-app>
      <v-app-bar app class="headerbar">
        <v-container  fill-height fluid>
          <v-row no-gutters>
            <v-col class="text-left" align-self="center">
               <h2><v-icon>mdi-virus</v-icon> Covid Tracker</h2>
            </v-col>
            <v-col v-if="localUser" class="text-right" align-self="center">
              <v-icon v-popover:subscriber.left large>mdi-account-circle</v-icon>
              <popover name="subscriber" class="subscriberPop" >
                <span class="handle">{{localUser}}</span>
                <a href="#" @click.prevent="$router.push({name: 'Counties'})">Manage <v-icon>mdi-cog</v-icon></a>
                <a href="#" @click="logoutSubscriber">Logout <v-icon color="blue">mdi-logout</v-icon></a>
              </popover>
            </v-col>
          </v-row>
        </v-container>
      </v-app-bar>
      <v-main>
        <v-container>
          <router-view/>
        </v-container>
      </v-main>
    </v-app>
  </div>
</template>
<script>
import { getLocalUser,logout} from '~/modules/utils/session';
export default {
  data() {
    return {
      localUser:null
    };
  },
  methods: {
    logoutSubscriber() {
      logout();
    },
    refreshLocalUser() {
      let loc = getLocalUser();
      if(loc){
        if(loc.handle.includes('@')){
          this.localUser = loc.handle;
        }
        else {
          this.localUser = this.formatPhoneNumber(loc.handle);
        }
      }
      else {
        this.localUser = null;
      }
    }
  },
  created() {
    this.$eventHub.$on('logout', this.refreshLocalUser);
    this.refreshLocalUser();
  },
  beforeDestroy() {
    this.$eventHub.$off('logout', this.refreshLocalUser);
  },
};
</script>
