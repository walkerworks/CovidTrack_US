<template>
  <div>
        <v-container>
          <v-row>
            <v-col>
              <h2>Choose counties to monitor</h2>
              <p class="subtitle-1">
               Add up to 5 counties for updates.
              </p>
            </v-col>
          </v-row>
          <v-row>
            <v-col>
              <v-data-table
              class="manualCounties"
              :headers="headers"
              :items="selectedCounties"
              item-key="name"
              disable-sort
              hide-default-footer>
              <template slot="items" slot-scope="props">
                <td class="text-xs-right">{{ props.item.name }}</td>
                <td class="text-xs-right">{{ props.item.state }}</td>
              </template>
              <template v-slot:item.frequency="{item}">
                <td class="text-xs-right">
                    <v-select attach :items="frequencyItems" v-model="item.frequency"></v-select>
                </td>
              </template>
                <template v-slot:item.actions="{ item }">
                    <v-icon small @click="deleteItem(item)">
                        mdi-delete
                    </v-icon>
                </template>
              </v-data-table>
            </v-col>
          </v-row>
        </v-container>
      <v-snackbar v-model="showSaveConfirmed" color="success" :timeout="5000">
        <span>Changes saved!</span>
      </v-snackbar>
  </div>
</template>
<script>
import axios from 'axios';
import { logout} from '~/modules/utils/session';
export default {
  data() {
    return {
      showSaveConfirmed: false,
      countyData: null,
      selectedCounties: [],
      origSelectedCounties: [],
      frequencyItems: ['Daily','Weekly','Monthly'],
      headers: [
        { text: 'County', value: 'name' },
        { text: 'State', value: 'state' },
        { text: 'Frequency', value: 'frequency' },
        { text: 'Actions', value: 'actions' },
      ]
    };
  },
  async mounted() {
    /* Load current user's selections from DB */
    await this.loadData();

    /* Load county data for clickable data retrieval */
    this.countyData = (await axios.get('/js/counties.json')).data;
  },
  methods: {
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
      }
      catch(ex) {
        console.log(ex);
      }
    }
  }
};
</script>