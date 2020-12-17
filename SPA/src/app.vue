<template>
  <div id="app">
    <v-app>
      <v-app-bar app class="headerbar">
        <v-container fluid>
          <v-row no-gutters>
            <v-col class="text-left titleAndLogo" align-self="center">
               <v-icon>mdi-virus</v-icon><h2>COVID Tracker</h2>
            </v-col>
            <v-col v-if="localUser" class="text-right" align-self="center">
              <v-icon v-popover:subscriber.left large>mdi-account-circle</v-icon>
              <popover name="subscriber" class="subscriberPop">
                <span class="handle">{{localUser}}</span>
                <a v-if="$route.name !== 'Counties'" href="#" @click.prevent="$router.push({name: 'Counties'})">Manage <v-icon>mdi-cog</v-icon></a>
                <a href="#" @click="logoutSubscriber">Logout <v-icon color="blue">mdi-logout</v-icon></a>
                <a href="#" class="removeAccount" @click.prevent="dialogAccountDelete = true">Unsubscribe <v-icon>mdi-account-remove</v-icon></a>
              </popover>
            </v-col>
          </v-row>
        </v-container>
      </v-app-bar>
      <v-main>
        <v-container class="breadcrumb">
              <v-container>
                <p class="text-uppercase subtitle">{{$route.meta}}</p>
              </v-container>
        </v-container>
        <v-container>
        <v-dialog v-model="dialogAccountDelete" max-width="300px">
            <v-card>
              <v-card-title class="headline">Are you sure?</v-card-title>
              <v-card-subtitle><br/>Removing your account is permanent. Select "OK" to remove your subscription or "Cancel" to keep your account.</v-card-subtitle>
              <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn color="blue darken-1" text @click="closeAccountDelete">Cancel</v-btn>
                <v-btn color="blue darken-1" text @click="deleteAccountConfirm">OK</v-btn>
                <v-spacer></v-spacer>
              </v-card-actions>
            </v-card>
          </v-dialog>
          <router-view/>
        </v-container>
      </v-main>
      <v-footer>
        <v-row>
        <v-col class="text-center" cols="12">
          <img src="/i/uvmExt.jpg"/>
      </v-col>
        </v-row>
                <v-row>
            <v-col class="body-1">
                <p>The updates provided by COVID Tracker are based on a <a href="https://dfr.vermont.gov/sites/finreg/files/doc_library/dfr-travel-map-methodology-071620.pdf">model</a> developed by the <a href="https://dfr.vermont.gov/about-us/covid-19/modeling">Vermont Department of Financial Regulation</a>. The COVID Tracker is a project of <a href="https://blog.uvm.edu/cwcallah/">UVM Extension Ag Engineering</a> and WalkerWorks. This project was made possible by support from the <a href="https://www.uvm.edu/engagement">UVM Office of Engagement</a> and <a href="https://accd.vermont.gov/covid-19">Vermont Agency of Commerce and Community Development</a>.</p>
                <p>Issued in furtherance of Cooperative Extension work, Acts of May 8 and June 30, 1914, in cooperation with the United States Department of Agriculture. University of Vermont Extension, Burlington, Vermont. University of Vermont Extension, and U.S. Department of Agriculture, cooperating, offer education and employment to everyone without regard to race, color, national origin, gender, religion, age, disability, political beliefs, sexual orientation, and marital or familial status. Any reference to commercial products, trade names, or brand names is for information only, and no endorsement or approval is intended.</p>
            </v-col>
          </v-row>
    </v-footer>
    </v-app>
  </div>
</template>
<script>
import { getLocalUser,logout} from '~/modules/utils/session';
import axios from 'axios';
export default {
  data() {
    return {
      localUser:null,
      dialogAccountDelete: false,
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
    },
    async deleteAccountConfirm () {
      try{
        this.dialogAccountDelete = false;
        var result = await axios.post('/api/unsubscribe/');
        if(result.data.success) {
          logout();
          return;
        }
      }
      catch(err){console.log(err);}
      this.accountDeleteError = 'There was an error unsubscribing you. Please try again later';
    },
    closeAccountDelete () {
      this.dialogAccountDelete = false;
    },
  },
  created() {
    this.$eventHub.$on('logout', this.refreshLocalUser);
    this.refreshLocalUser();
  },
  beforeDestroy() {
    this.$eventHub.$off('logout', this.refreshLocalUser);
  }
};
</script>
