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
              :headers="headers"
              :items="selectedCounties"
              item-key="name"
              disable-sort
              hide-default-footer>
              <template v-slot:top>
                <v-toolbar flat>
                  <v-toolbar-title>Selected Counties ({{selectedCounties.length}} of 5)</v-toolbar-title>
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
      <v-snackbar v-model="showSaveConfirmed" color="success" :timeout="5000">
        <span>Changes saved!</span>
      </v-snackbar>
  </div>
</template>