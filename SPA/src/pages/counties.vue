<template>
  <div>
        <v-container>
          <v-row>
            <v-col>
              <h2>Choose counties to monitor</h2>
              <p class="subtitle-1">
                Click a state to zoom in. Select up to 5 counties for COVID-19 updates.  Alerts are sent at 7:30am ET.
              </p>
              <div id="chartdiv" ref="chartdiv"></div>
            </v-col>
          </v-row>
          <v-row>
            <v-col>
              <v-data-table
              :headers="headers"
              :items="selectedCounties"
              item-key="name"
              disable-sort
              hide-default-footer>
              <template v-slot:top>
                <v-toolbar flat>
                  <v-toolbar-title>Selected Counties ({{selectedCounties.length}} of 5)</v-toolbar-title>
                    <v-dialog v-model="dialog" max-width="500px">
                      <v-card>
                        <v-card-title>
                          <h3>Frequency</h3>
                        </v-card-title>
                        <v-divider></v-divider>
                        <v-card-subtitle>
                          <span class="subtitle-1">Select frequency for <strong>{{editedItem.name}}, {{editedItem.state}}</strong> notifications</span>
                        </v-card-subtitle>
                        <v-card-text>
                          <v-container>
                              <v-radio-group v-model="editedItem.frequency">
                                <v-radio value="Daily">
                                  <template v-slot:label>
                                    <div><strong>Daily</strong></div>
                                  </template>
                                </v-radio>
                                <v-radio value="Weekly">
                                  <template v-slot:label>
                                    <div><strong>Weekly</strong></div>
                                  </template>
                                </v-radio>
                                <v-radio value="Monthly">
                                  <template v-slot:label>
                                    <div><strong>Monthly</strong></div>
                                  </template>
                                </v-radio>
                              </v-radio-group>
                          </v-container>
                        </v-card-text>
                        <v-card-actions>
                          <v-btn outlined color="primary" text @click="save">OK</v-btn>
                          <v-btn outlined color="gray" text @click="close">Cancel</v-btn>
                        </v-card-actions>
                      </v-card>
                  </v-dialog>
                  <v-dialog v-model="dialogDelete" max-width="500px">
                    <v-card>
                      <v-card-title class="headline">Stop following this county?</v-card-title>
                      <v-card-actions>
                        <v-btn outlined color="primary" text @click="deleteItemConfirm">OK</v-btn>
                        <v-btn outlined color="gray" text @click="closeDelete">Cancel</v-btn>
                      </v-card-actions>
                    </v-card>
                  </v-dialog>
                </v-toolbar>
              </template>
              <template slot="items" slot-scope="props">
                <td class="text-xs-right">{{ props.item.name }}</td>
                <td class="text-xs-right">{{ props.item.state }}</td>
                <td slot="item.data.expand" class="text-xs-right">{{ props.item.frequency }}</td>
              </template>
            <template v-slot:item.actions="{ item }">
              <v-icon small class="mr-2" @click="editItem(item)" >mdi-pencil</v-icon>
                  &nbsp;&nbsp;&nbsp;
                  <v-icon small @click="deleteItem(item)">
                    mdi-delete
                  </v-icon>
              </template>
              </v-data-table>
            </v-col>
          </v-row>
          <v-row>
            <v-col>
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
              <p class="subtitle-1">
                <a href="#" class="removeAccount red--text" @click.prevent="dialogAccountDelete = true"> <v-icon large>mdi-account-remove</v-icon> Unsubscribe and remove your account</a>
                <span class="red--text" v-if="accountDeleteError"><br/>{{accountDeleteError}}</span>
              </p>
            </v-col>
          </v-row>
        </v-container>
      <v-snackbar v-model="snackbar.show" :color="snackbar.color" :vertical="vertical" :timeout="snackbar.timeout">
        <a v-if="snackbar.mode === 'SAVE'" @click="SaveData">{{snackbar.saveText}}</a>
        <span v-else>{{snackbar.confirmedText}}</span>
      </v-snackbar>
  </div>
</template>
<style scoped>
 .radio-group-full-width >>>.v-input__control {
     width: 100%
 }
</style>
<script>
import axios from 'axios';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4maps from '@amcharts/amcharts4/maps';
import usData from '@amcharts/amcharts4-geodata/usaAlbersLow';
import maData from '@amcharts/amcharts4-geodata/region/usa/maLow';
import nhData from '@amcharts/amcharts4-geodata/region/usa/nhLow';
import nyData from '@amcharts/amcharts4-geodata/region/usa/nyLow';
import vtData from '@amcharts/amcharts4-geodata/region/usa/vtLow';
import { logout} from '~/modules/utils/session';
export default {
  data() {
    return {
      data: {usa: usData, vt: vtData, ny: nyData, nh: nhData, ma: maData},
      snackbar: {
        show: false,
        timeout: -1,
        color: 'primary',
        saveText: 'Tap to save your changes',
        mode: 'SAVE',
        confirmedText: 'Saved!'
      },
      dialog: false,
      dialogDelete: false,
      dialogAccountDelete: false,
      editedIndex: -1,
      editedItem: {},
      defaultItem: {},
      selectedCounties: [],
      origSelectedCounties: [],
      accountDeleteError: null,
      headers: [
        { text: 'County', value: 'name' },
        { text: 'State', value: 'state' },
        { text: 'Frequency', value: 'frequency' },
        { text: 'Actions', value: 'actions' },
      ]
    };
  },
  created() {
    this.loadData();
  },
  mounted() {
    // const qry = this.$router.currentRoute.query;
    this.chart = am4core.create(this.$refs.chartdiv, am4maps.MapChart);
    this.chart.responsive.enabled = true;
    // Set map definition
    this.chart.geodata = this.data.usa;

    // Set projection
    this.chart.projection = new am4maps.projections.Albers();

    // Create map polygon series for the USA Map
    this.USASeries = this.chart.series.push(new am4maps.MapPolygonSeries());
    this.USASeries.include = ['US-VT', 'US-MA', 'US-NY', 'US-NH'];
    // Make map load polygon USA data from GeoJSON
    this.USASeries.useGeodata = true;
    // Configure series
    this.USASeries.mapPolygons.template.tooltipText = '{name}';
    this.USASeries.mapPolygons.template.fill = am4core.color('#74B266');
    // Create hover state and set alternative fill color
    var usaHover = this.USASeries.mapPolygons.template.states.create('hover');
    usaHover.properties.fill = am4core.color('#367B25');
    this.stateClickEvent = this.USASeries.mapPolygons.template.events.on('hit',this.stateClick,this);
    this.chart.homeZoomLevel = 0;
    this.chart.homeGeoPoint = {
      latitude: 43.162,
      longitude: -73.072
    };
    this.chart.chartContainer.wheelable = false;
    this.chart.chartContainer.background.events.disableType('doublehit');
    this.chart.seriesContainer.events.disableType('doublehit');

    // Create map polygon series for a specific state / hide by default
    this.StateSeries = this.chart.series.push(new am4maps.MapPolygonSeries());
    this.StateSeries.calculateVisualCenter = true;
    this.StateSeries.mapPolygons.template.tooltipText = '{name}';
    this.StateSeries.mapPolygons.template.fill = am4core.color('#74B266');
    var stateHover = this.StateSeries.mapPolygons.template.states.create('hover');
    stateHover.properties.fill = am4core.color('#367B25');
    this.countyClickEvent = this.StateSeries.mapPolygons.template.events.on('hit',this.countyClick,this);
    this.StateSeries.hide();

    // Add zoomout button
    this.back = this.chart.createChild(am4core.ZoomOutButton);
    this.back.align = 'right';
    this.back.hide();
    this.backClickEvent = this.back.events.on('hit',this.backClick);

    // Add "watching" icons
    this.imageSeries = this.chart.series.push(new am4maps.MapImageSeries());
    var imageSeriesTemplate = this.imageSeries.mapImages.template;
    this.mapItemClickEvent = imageSeriesTemplate.events.on('hit',this.mapItemClick);
    var marker = imageSeriesTemplate.createChild(am4core.Image);
    marker.href = '/i/eye.svg';
    marker.width = 24;
    marker.height = 24;
    marker.nonScaling = true;
    marker.tooltipText = '{title}';
    marker.horizontalCenter = 'middle';
    marker.verticalCenter = 'middle';

    // Set property fields
    imageSeriesTemplate.propertyFields.id = 'countyId';
    imageSeriesTemplate.propertyFields.latitude = 'latitude';
    imageSeriesTemplate.propertyFields.longitude = 'longitude';

    this.imageSeries.data = this.imageData;
  },
  beforeDestroy() {
    if (this.chart) {
      this.chart.dispose();
    }
    if(this.stateClickEvent) {
      this.stateClickEvent.dispose();
    }
    if(this.backClickEvent) {
      this.backClickEvent.dispose();
    }
    if(this.countyClickEvent) {
      this.countyClickEvent.dispose();
    }
    if(this.mapItemClickEvent) {
      this.mapItemClickEvent.dispose();
    }
  },
  watch: {
    selectedCounties : {
      deep: true,
      handler() {
        let show = !this.arraysAreSame(this.selectedCounties,this.origSelectedCounties);
        if(show) {
          this.snackbar.timeout = -1;
          this.snackbar.color = 'primary';
          this.snackbar.mode = 'SAVE';
        }
        this.snackbar.show = show;
      }
    },
    dialog (val) {
      val || this.close();
    },
    dialogDelete (val) {
      val || this.closeDelete();
    },
  },
  methods: {
    async loadData() {
      try{
        var response = await axios.get('/api/get-county-data/');
        if(response.data && response.data.loggedIn === false){
          logout();
        }
        this.isDirty = false;
        this.origSelectedCounties = response.data;
        this.selectedCounties = JSON.parse(JSON.stringify(this.origSelectedCounties));
      }
      catch(ex) {
        console.log(ex);
      }
    },
    arraysAreSame(x, y) {
      if(!x && !y)
        return true;
      if((x && !y) || (y && !x))
        return false;
      if(x.length !== y.length)
        return false;
      var objectsAreSame = true;
      for(var propertyName in x) {
        if(JSON.stringify(x[propertyName]) !== JSON.stringify(y[propertyName])) {
          objectsAreSame = false;
          break;
        }
      }
      return objectsAreSame;
    },
    editItem (item) {
      this.editedIndex = this.selectedCounties.indexOf(item);
      this.editedItem = Object.assign({}, item);
      this.dialog = true;
    },
    deleteItem (item) {
      this.editedIndex = this.selectedCounties.indexOf(item);
      this.editedItem = Object.assign({}, item);
      this.dialogDelete = true;
    },
    deleteItemConfirm () {
      let countyId = this.selectedCounties[this.editedIndex].id;
      if(countyId){
        this.selectedCounties.splice(this.editedIndex, 1);
        let dataItem = this.imageSeries.dataItems.values.find(v => v.dataContext.id === countyId);
        this.imageSeries.dataItems.remove(dataItem);
      }
      this.closeDelete();
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
    save () {
      Object.assign(this.selectedCounties[this.editedIndex], this.editedItem);
      this.close();
    },
    close () {
      this.dialog = false;
      this.$nextTick(() => {
        this.editedItem = Object.assign({}, this.defaultItem);
        this.editedIndex = -1;
      });
    },
    closeDelete () {
      this.dialogDelete = false;
      this.$nextTick(() => {
        this.editedItem = Object.assign({}, this.defaultItem);
        this.editedIndex = -1;
      });
    },
    closeAccountDelete () {
      this.dialogAccountDelete = false;
    },
    backClick() {
      this.USASeries.show();
      this.chart.goHome();
      this.StateSeries.hide();
      this.back.hide();
    },
    mapItemClick(ev) {
      let countyId = ev.target.id;
      let existing = this.selectedCounties.find(c => c.id === countyId);
      if(existing){
        this.selectedCounties.splice(this.selectedCounties.indexOf(existing), 1);
        let dataItem = this.imageSeries.dataItems.values.find(v => v.dataContext.id === countyId);
        this.imageSeries.dataItems.remove(dataItem);
      }
    },
    stateClick(ev) {
      ev.target.series.chart.zoomToMapObject(ev.target);
      var stateAbbrev = ev.target.dataItem.dataContext.id.split('-')[1].toUpperCase();
      this.StateSeries.geodata = this.data[stateAbbrev.toLowerCase()];
      this.StateSeries.geodataSource.updateCurrentData = true;
      this.StateSeries.show();
      this.USASeries.hide();
      this.back.show();
      setTimeout(() => {
        let selectedInThisState = this.selectedCounties.filter(sc => sc.state === stateAbbrev);
        selectedInThisState.forEach(element => {
          let exists = this.imageSeries.dataItems.values.find(v => v.dataContext.id === element.id);
          if(!exists){
            let poly = this.StateSeries.getPolygonById(element.id);
            // Add image
            this.imageSeries.addData({
              'countyId':element.id,
              'id':element.id,
              'latitude':poly.latitude,
              'longitude':poly.longitude,
            },0);
          }
        });
      },1000);
    },
    countyClick(ev) {
      let county = ev.target.dataItem.dataContext;
      let existing = this.selectedCounties.find(c => c.id === county.id );
      if(existing){
        this.selectedCounties.splice(this.selectedCounties.indexOf(existing), 1);
        let dataItem = this.imageSeries.dataItems.values.find(v => v.dataContext.id === county.id );
        this.imageSeries.dataItems.remove(dataItem);
      }
      else {
        if(this.selectedCounties.length === 5) {
          return;
        }
        else {
          this.selectedCounties.push({id:county.id,state:county.STATE, name: county.name, frequency: 'Daily'});
          // Add image
          this.imageSeries.addData({
            'countyId':county.id,
            'id':county.id,
            'latitude':ev.target.dataItem.mapPolygon.latitude,
            'longitude':ev.target.dataItem.mapPolygon.longitude,
          },0);
        }
      }
    },
    async SaveData() {
      try{
        await axios.post('/api/save-counties/',this.selectedCounties);
        this.snackbar.mode = 'CONFIRMED';
        this.snackbar.color = 'success';
        this.snackbar.timeout = 5000;
        this.origSelectedCounties = JSON.parse(JSON.stringify(this.selectedCounties));
      }
      catch(ex) {
        console.log(ex);
      }
    },
  }
};
</script>