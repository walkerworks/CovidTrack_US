<template>
  <div>
    <v-container>
      <v-row>
        <v-col>
          <p class="body-1">
            Choose up to 5 counties to monitor. Or you may <a href="#" @click.prevent="$router.push({name: 'Counties'})">select from a map</a>.
          </p>
        </v-col>
      </v-row>
      <v-row>
        <v-col>
        <table class="tbl-responsive">
            <thead>
            <tr>
                <th>County</th>
                <th>Frequency</th>
                <th>Remove</th>
            </tr>
            </thead>
            <tbody>
            <tr v-for="(row, x) in selectedCounties" :key="x">
                <td data-th="County">
                      {{ row.name }}, {{ row.state }}
                </td>
                <td data-th="Frequency">
                    <v-select attach :items="frequencyItems" @change="dataIsDirty = true" v-model="row.frequency"></v-select>
                </td>
                <td>
                    <a :alt="'stop monitoring ' + row.name + ' County'" @click="removeData(row)" class="red--text" href="#"><v-icon class="red--text">mdi-delete</v-icon></a>
                </td>
            </tr>
            </tbody>
        </table>
        <template v-if="dataIsDirty || selectedCounties.length < 5">
            <p></p>
            <v-card  flat class="d-flex justify-space-around">
                <v-btn v-if="dataIsDirty"  color="info" outlined text @click="saveData"><v-icon>mdi-content-save-all</v-icon>&nbsp;Save changes</v-btn>
                <v-btn  color="success" outlined v-if="selectedCounties.length < 5"  text @click="openModal"><v-icon>mdi-plus-circle-outline</v-icon>&nbsp;Add County</v-btn>
            </v-card>
        </template>
        <template v-if="dataIsDirty || origSelectedCounties.length > 0">
            <p></p>
            <v-card  flat class="d-flex justify-space-around">
              <v-btn v-if="dataIsDirty" color="gray" outlined text @click="undo"><v-icon>mdi-undo-variant</v-icon>&nbsp;Undo</v-btn>
              <v-btn @click="sendNotification" v-if="origSelectedCounties.length > 0" color="warning" outlined text><v-icon>mdi-send</v-icon>&nbsp;{{alertType}} me now</v-btn>
            </v-card>
        </template>
        <p class="body-1" v-else>
            <br/>
            You are monitoring your maximum five counties.  You must remove a county before you can add another.
        </p>
        </v-col>
      </v-row>
        <modal v-if="showModal">
        <template v-slot:header>
          <h3>Monitor a new county</h3>
        </template>
        <template v-slot:body>
            <p>
                <v-select :items="states" label="Choose state" item-text="name" item-value="abbr" @input="onStateSelect" v-model="newCounty.state"></v-select>
            </p>
            <p>
                <v-select :items="selectCounties" label="Choose county" item-text="name" return-object v-model="newCounty.county"></v-select>
            </p>
            <p>
                <v-select label="Choose Frequency" :items="frequencyItems" v-model="newCounty.frequency"></v-select>
            </p>
        </template>
        <template v-slot:footer>
            <v-card  flat class="d-flex justify-space-around">
            <v-btn v-if="newCounty.county.id" color="info" outlined text @click="saveNew">Save</v-btn>
            <v-btn color="gray" outlined text @click="closeModal">Cancel</v-btn>
          </v-card>
        <p></p>
        </template>
      </modal>
    </v-container>
    <v-snackbar v-model="showSnack" color="success" :timeout="5000">
      <span>{{snackMessage}}</span>
    </v-snackbar>
  </div>
</template>
<script>
import axios from 'axios';
import { logout, getLocalUser } from '~/modules/utils/session';
export default {
  data() {
    return {
      showModal:false,
      newCounty: {
        state: null,
        county: null,
        frequency: 'Daily'
      },
      alertType: '',
      selectCounties: [],
      dataIsDirty:false,
      snackMessage: 'Changes saved!',
      showSnack: false,
      countyData: null,
      selectedCounties: [],
      origSelectedCounties: [],
      frequencyItems: ['Daily','Weekly','Monthly'],
      states: [
        { name: 'ALABAMA', abbr: 'AL', fips: '01'},
        { name: 'ALASKA', abbr: 'AK', fips: '02'},
        { name: 'ARIZONA', abbr: 'AZ', fips: '04'},
        { name: 'ARKANSAS', abbr: 'AR', fips: '05'},
        { name: 'CALIFORNIA', abbr: 'CA', fips: '06'},
        { name: 'COLORADO', abbr: 'CO', fips: '08'},
        { name: 'CONNECTICUT', abbr: 'CT', fips: '09'},
        { name: 'DELAWARE', abbr: 'DE', fips: '10'},
        { name: 'DISTRICT OF COLUMBIA', abbr: 'DC', fips: '11'},
        { name: 'FLORIDA', abbr: 'FL', fips: '12'},
        { name: 'GEORGIA', abbr: 'GA', fips: '13'},
        { name: 'HAWAII', abbr: 'HI', fips: '15'},
        { name: 'IDAHO', abbr: 'ID', fips: '16'},
        { name: 'ILLINOIS', abbr: 'IL', fips: '17'},
        { name: 'INDIANA', abbr: 'IN', fips: '18'},
        { name: 'IOWA', abbr: 'IA', fips: '19'},
        { name: 'KANSAS', abbr: 'KS', fips: '20'},
        { name: 'KENTUCKY', abbr: 'KY', fips: '21'},
        { name: 'LOUISIANA', abbr: 'LA', fips: '22'},
        { name: 'MAINE', abbr: 'ME', fips: '23'},
        { name: 'MARYLAND', abbr: 'MD', fips: '24'},
        { name: 'MASSACHUSETTS', abbr: 'MA', fips: '25'},
        { name: 'MICHIGAN', abbr: 'MI', fips: '26'},
        { name: 'MINNESOTA', abbr: 'MN', fips: '27'},
        { name: 'MISSISSIPPI', abbr: 'MS', fips: '28'},
        { name: 'MISSOURI', abbr: 'MO', fips: '29'},
        { name: 'MONTANA', abbr: 'MT', fips: '30'},
        { name: 'NEBRASKA', abbr: 'NE', fips: '31'},
        { name: 'NEVADA', abbr: 'NV', fips: '32'},
        { name: 'NEW HAMPSHIRE', abbr: 'NH', fips: '33'},
        { name: 'NEW JERSEY', abbr: 'NJ', fips: '34'},
        { name: 'NEW MEXICO', abbr: 'NM', fips: '35'},
        { name: 'NEW YORK', abbr: 'NY', fips: '36'},
        { name: 'NORTH CAROLINA', abbr: 'NC', fips: '37'},
        { name: 'NORTH DAKOTA', abbr: 'ND', fips: '38'},
        { name: 'OHIO', abbr: 'OH', fips: '39'},
        { name: 'OKLAHOMA', abbr: 'OK', fips: '40'},
        { name: 'OREGON', abbr: 'OR', fips: '41'},
        { name: 'PENNSYLVANIA', abbr: 'PA', fips: '42'},
        { name: 'RHODE ISLAND', abbr: 'RI', fips: '44'},
        { name: 'SOUTH CAROLINA', abbr: 'SC', fips: '45'},
        { name: 'SOUTH DAKOTA', abbr: 'SD', fips: '46'},
        { name: 'TENNESSEE', abbr: 'TN', fips: '47'},
        { name: 'TEXAS', abbr: 'TX', fips: '48'},
        { name: 'UTAH', abbr: 'UT', fips: '49'},
        { name: 'VERMONT', abbr: 'VT', fips: '50'},
        { name: 'VIRGINIA', abbr: 'VA', fips: '51'},
        { name: 'WASHINGTON', abbr: 'WA', fips: '53'},
        { name: 'WEST VIRGINIA', abbr: 'WV', fips: '54'},
        { name: 'WISCONSIN', abbr: 'WI', fips: '55'},
        { name: 'WYOMING', abbr: 'WY', fips: '56'},
      ]
    };
  },
  async mounted() {
    /* Load current user's selections from DB */
    await this.loadData();

    /* Load county data for clickable data retrieval */
    this.countyData = (await axios.get('/js/counties.json')).data;

    this.alertType = getLocalUser().handle.includes('@') ? 'Email' : 'Text';
  },
  methods: {
    /* Triggers a notification for the currently logged in user */
    async sendNotification() {
      try{
        const response = await axios.post('/api/notify');
        if(response && response.data) {
          if(response.data.success) {
            this.showSnackbar('Message sent!');
          }
          else {
            this.showSnackbar(response.data.error);
          }
        }
      }
      catch(ex) {
        this.showSnackbar('uh oh, that didn\'t work');
      }
    },
    onStateSelect(){
      let selectedState = this.states.find(s => s.abbr === this.newCounty.state);
      if(selectedState) {
        let stateFeatures = this.countyData.features.filter(f => f.properties.GEOID.startsWith(selectedState.fips));
        this.selectCounties =  stateFeatures.map(s => {let obj = {}; obj.name = s.properties.NAME; obj.id = s.properties.GEOID; return obj;});
        this.selectCounties.sort((a, b) =>  {
          if (a.name < b.name) {
            return -1;
          }
          if (a.name >  b.name) {
            return 1;
          }
          return 0;
        });
      }
    },
    /*
      Loads the counties the current user has already saved to monitor
    */
    async loadData() {
      try{
        var response = await axios.get('/api/get-county-data/');
        if(response.data && response.data.loggedIn === false){
          logout();
        }
        this.origSelectedCounties = response.data;
        this.selectedCounties = JSON.parse(JSON.stringify(this.origSelectedCounties));
        this.sortCountiesForDisplay();
      }
      catch(ex) {
        console.log(ex);
      }
    },
    sortCountiesForDisplay() {
      let cmp = (a, b) => (a > b) - (a < b);
      this.selectedCounties = this.selectedCounties.sort((a, b) => cmp(a.state,b.state) || cmp(a.name,b.name));
    },
    showSnackbar(msg) {
      this.snackMessage = msg;
      this.showSnack = true;
    },
    /*
    Hits the API to persist the users SelectedCounties information
    */
    async saveData(){
      await axios.post('/api/save-counties/',this.selectedCounties);
      this.showSnackbar('Changes saved!');
      this.origSelectedCounties = JSON.parse(JSON.stringify(this.selectedCounties));
      this.dataIsDirty = false;
    },
    async saveNew() {
      /* Doublecheck it's not already a selected county */
      let existing = this.selectedCounties.find(c => c.id === this.newCounty.id);
      if(!existing){
        /* update the SelectedCounties global array */
        this.selectedCounties.push({id:this.newCounty.county.id, state: this.newCounty.state, name: this.newCounty.county.name, frequency: this.newCounty.frequency});
        this.dataIsDirty = true;
        this.sortCountiesForDisplay();
        this.closeModal();
      }
    },
    openModal() {
      this.newCounty = {frequency:'Daily',state: '', county: ''};
      this.showModal = true;
    },
    closeModal() {
      this.newCounty = {frequency:'Daily'};
      this.showModal = false;
    },
    async removeData(county) {
      /* Doublecheck it's already a selected county */
      let existing = this.selectedCounties.find(c => c.id === county.id);
      if(existing){
        /* update the SelectedCounties global array */
        this.selectedCounties.splice(this.selectedCounties.indexOf(existing), 1);
        this.dataIsDirty = true;
        this.sortCountiesForDisplay();
      }
    },
    undo() {
      this.selectedCounties = JSON.parse(JSON.stringify(this.origSelectedCounties));
      this.sortCountiesForDisplay();
      this.dataIsDirty = false;
    }
  }
};
</script>