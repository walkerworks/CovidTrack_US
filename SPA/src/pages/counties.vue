<template>
  <div>
        <v-container>
          <v-row>
            <v-col>
              <p class="body-1">
                Use the map below to select up to 5 counties. Or you may <a href="#">Add/Remove counties manually</a>.
              </p>
              <div id="map" ref="leafletMap"></div>
            </v-col>
          </v-row>
          <modal v-if="showModal">
            <template v-slot:header>
              <h3>{{selectedCounty.name}} County</h3>
            </template>
            <template v-slot:body>
              <template v-if="!selectedCounty.alreadySelected && selectedCounties.length >= 5">
                <p class="red--text">Sorry, you have already selected five counties.  You must remove one before you can add any more.</p>
              </template>
              <template v-else>
                <p class="subtitle-1">Frequency</p>
                <v-divider></v-divider>
                <v-radio-group v-model="selectedCounty.frequency">
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
              </template>
              <v-divider></v-divider>
            </template>
            <template v-slot:footer>
              <v-btn v-if="selectedCounty.alreadySelected || selectedCounties.length < 5" color="blue darken-1" text @click="handleSave">Save</v-btn>
              <v-btn v-if="selectedCounty.alreadySelected" color="red darken-1" text @click="removeData">Remove</v-btn>
              <v-btn color="gray darken-1" text @click="closeModal">Cancel</v-btn>
            </template>
          </modal>
        </v-container>
      <v-snackbar v-model="showSaveConfirmed" color="success" :timeout="5000">
        <span>Changes saved!</span>
      </v-snackbar>
  </div>
</template>
<script>
import axios from 'axios';
import { logout} from '~/modules/utils/session';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import 'leaflet-spin';
import centroid from '@turf/centroid';
export default {
  data() {
    return {
      showSaveConfirmed: false,
      showModal: false,
      lastCountyClick: 0,
      modalOpendedAt: 0,
      countyLayer: null,
      countyData: null,
      mymap: null,
      bounds: null,
      featureClickEvents: [],
      dialog: false,
      dialogDelete: false,
      selectedCounty: {},
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
  async mounted() {
    /* Load current user's selections from DB */
    await this.loadData();

    /* Load county data for clickable data retrieval */
    this.countyData = (await axios.get('/js/counties.json')).data;

    /* Zoom to current location & set USA map boundaries add event to keep user within bounaries */
    this.mymap = L.map('map');

    /* Start Spinner */
    this.mymap.spin(true);

    /* Setup Leaflet map with MapBox tile layers */
    L.tileLayer('https://api.mapbox.com/styles/v1/walkerworks/ckiotv7id20z217nv8jnwx9ga/tiles/256/{z}/{x}/{y}@2x?access_token=pk.eyJ1Ijoid2Fsa2Vyd29ya3MiLCJhIjoiY2tpb3M2YWoyMGo4MTJybXYzcGhjOGFobSJ9.iEmT9oelguTy6h4v0v5tbw',{
      attribution: 'Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
    }).addTo(this.mymap);

    /* Create a layer container for the GEOJson Counties */
    this.countyLayer = L.geoJSON(null,{
      onEachFeature: this.onEachFeature
    }).addTo(this.mymap);

    /* Put any preselected counties on the map */
    this.addSelectedCountiesToMap();

    /* Precalculate all the centers of the counties */
    this.precalculateCountyCentroids();

    /* GeoLocate the browser (if possible) */
    this.mymap.locate({setView: true,maxZoom:8});

    /* Set the map boundaries so the user can't go to, like, Europe */
    this.setMapBoundaries();

    /* Hook up event handlers for Map & Layers */
    this.addEventHandlers();
  },
  beforeDestroy() {
    if(this.mymap) {
      /* Cleanup MAP Event Handlers */
      this.mymap.off('load', this.mapLoaded);
      this.mymap.off('locationfound', this.locationFound);
      this.mymap.off('locationerror',this.locationError );
      this.mymap.off('moveend', this.moveend);
    }
    /* Cleanup GEO Layer Event Handlers */
    if(this.countyLayer) {
      this.countyLayer.eachLayer(l => {
        l.off('click',this.countyClick);
      });
    }
  },
  watch: {
    dialog (val) {
      val || this.close();
    },
    dialogDelete (val) {
      val || this.closeDelete();
    },
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
        this.isDirty = false;
        this.origSelectedCounties = response.data;
        this.selectedCounties = JSON.parse(JSON.stringify(this.origSelectedCounties));
      }
      catch(ex) {
        console.log(ex);
      }
    },
    /*
      Executes whenever a feautre is added to the county layer
      Hooks up click handler and determines outline color.
    */
    onEachFeature(feature, layer) {
      layer.on('click',this.countyClick);
      if(this.selectedCounties.find(c => c.id === feature.properties.GEOID)) {
        layer.setStyle({color: 'red'});
      }
      else {
        layer.setStyle({color: 'transparent'});
      }
    },
    /*
    Handles when a user clicks a county
    */
    countyClick(ev) {
      /* This fires twice in rapid succession.  We need to only handle one of those firings -
      this is a hack to ignore a second trigger within milliseconds */
      if(this.lastCountyClick !== 0) {
        let sinceLastClick = Date.now()-this.lastCountyClick;
        if(sinceLastClick < 100) {
          return;
        }
      }
      this.lastCountyClick = Date.now();
      this.openModal(ev.target.feature.properties);
    },
    /*
    Fires when the map has finished loading (stop the spinner!)
    */
    mapLoaded() {
      this.mymap.spin(false);
    },
    /*
    Fires when the user has finished moving the map viewport
    We'll need to recalculate what it visible
    */
    moveend() {
      /* If the user is zoomed in at 7 or greater - we need to show counties */
      if(this.mymap.getZoom() >= 7) {
        /* Remove click events and clear the existing counties on the map first */
        this.countyLayer.eachLayer(l => {
          l.off('click',this.countyClick);
        });
        this.countyLayer.clearLayers();
        /* Get the current viewport boundaries from Map (visible map) */
        let mapBounds = this.mymap.getBounds();
        /* Go through our counties and see which ones are visible */
        this.countyData.features.forEach(feature => {
          /* If it's already a selected county - re-add it to map (in red) */
          if(this.selectedCounties.find(c => c.id === feature.properties.GEOID)) {
            this.countyLayer.addData(feature);
          }
          /* Otherwise, only add it to map if it's in the visible viewport */
          else if(mapBounds.contains(feature.center)){
            this.countyLayer.addData(feature);
          }
        });
      }
    },
    /*
    Hooks up the various event handlers on the Leaflet map.
    */
    addEventHandlers() {
      this.mymap.on('locationfound', this.locationFound);
      this.mymap.on('locationerror',this.locationError );
      this.mymap.on('load',this.mapLoaded);
      this.mymap.on('moveend', this.moveend);
      this.countyLayer.on('click',this.countyClick);
    },
    /*
    Makes sure the map stays within the United States (Roughly)
    */
    setMapBoundaries() {
      this.mymap.options.minZoom = 5;
      this.mymap.options.maxZoom = 11;
      this.bounds = L.latLngBounds(L.latLng(5.499550, -167.276413),  L.latLng(83.162102, -66.233040));
      this.mymap.setMaxBounds(this.bounds);
    },
    /*
    Adds the County Data in the SelectedCounties array to the
    map as data features
    */
    addSelectedCountiesToMap() {
      if(this.selectedCounties.length > 0) {
        this.selectedCounties.forEach(county => {
          let featureToAdd = this.countyData.features.find(feature => county.id === feature.properties.GEOID);
          if(featureToAdd) {
            this.countyLayer.addData(featureToAdd)
              .on('click',this.countyClick)
              .setStyle({color: 'red'});
          }
        });
      }
    },
    /*
    Find the center of all the County Polygons
    so we don't have to look them up on the fly
    */
    precalculateCountyCentroids() {
      this.countyData.features.forEach(feature => {
        let cntrd = centroid(feature);
        feature.center = L.latLng([cntrd.geometry.coordinates[1],cntrd.geometry.coordinates[0]]);
      });
    },
    /*
    Handles an inability to GeoLocate the user.
    Will default the map to the NorthEast.
    */
    locationError() {
      this.mymap.setView([42, -73], 7);
    },
    /*
    Handles the successful GeoLocation of a user.
    Ensures it's not in the middle of nowhere (Ocean or Mexico)
    */
    locationFound(ev) {
      let toTheRightOfFlorida = (ev.latlng.lat < 24.2 && ev.latlng.lng < -79.8);
      let totheLeftOfCaliforniaButNotHawaiiOrAlaska = ((ev.latlng.lat < 51 && ev.latlng.lat > 22) && ev.latlng.lng < -124.7);
      if( toTheRightOfFlorida || totheLeftOfCaliforniaButNotHawaiiOrAlaska){
        this.mymap.setView([37, -94], 4);
      }
    },
    /*
    Handles the saving of an individual county via the modal
    */
    async handleSave() {
      if ((Date.now()-this.modalOpenedAt) < 100) { return; }
      try{
        let existing = this.selectedCounties.find(c => c.id === this.selectedCounty.id);
        if(existing){
          existing.frequency = this.selectedCounty.frequency;
        }
        else {
          if(this.selectedCounties.length === 5) {
            return false;
          }
          else {
            this.selectedCounty.alreadySelected = true; // Keeps screen from flashing the "already selected warning"
            this.selectedCounties.push({id:this.selectedCounty.id, name: this.selectedCounty.name, frequency: this.selectedCounty.frequency});
            this.countyLayer.eachLayer(layer => {
              if(layer.feature && layer.feature.properties.GEOID === this.selectedCounty.id) {
                layer.setStyle({color: 'red'});
                return;
              }
            });
          }
        }
        await this.saveData();
        this.showModal = false;
      }
      catch(ex) {
        console.log(ex);
      }
    },
    /*
    Hits the API to persist the users SelectedCounties information
    */
    async saveData(){
      await axios.post('/api/save-counties/',this.selectedCounties);
      this.showSaveConfirmed = true;
      this.origSelectedCounties = JSON.parse(JSON.stringify(this.selectedCounties));
    },
    async removeData() {
      if ((Date.now()-this.modalOpenedAt) < 100) { return; }
      /* Doublecheck it's already a selected county */
      let existing = this.selectedCounties.find(c => c.id === this.selectedCounty.id);
      if(existing){
        /* update the SelectedCounties global array */
        this.selectedCounties.splice(this.selectedCounties.indexOf(existing), 1);
        /* Update the map UI to unselect the county from the map */
        this.countyLayer.eachLayer(layer => {
          if(layer.feature && layer.feature.properties.GEOID === existing.id) {
            layer.setStyle({color: 'transparent'});
          }
        });
        /* Save the data */
        await this.saveData();
        this.showModal = false;
      }
    },
    /*
    Helper to determine if two arrays are the same (datawise)
    */
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
    /*
    Close the modal and reset the SelectedCounty object
    */
    closeModal() {
      if ((Date.now()-this.modalOpenedAt) < 100) { return; }
      this.showModal = false;
      this.selectedCounty = null;
    },
    /*
    Open the modal and initialize the county data options
    */
    openModal(info) {
      let alreadySelected = this.selectedCounties.find(c => c.id === info.GEOID);
      this.selectedCounty = {
        id: alreadySelected ? alreadySelected.id : info.GEOID,
        name: alreadySelected ? alreadySelected.name : info.NAME,
        frequency: alreadySelected ? alreadySelected.frequency : 'Daily'
      };
      this.selectedCounty.alreadySelected = alreadySelected ? true : false;
      this.modalOpenedAt = Date.now();
      this.showModal = true;
    },
  }
};
</script>