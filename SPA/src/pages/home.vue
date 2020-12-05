<template>
  <div> 
        <p class="text-uppercase subtitle-2">Notifications</p>
        <v-container>
          <v-row>
            <v-col>
              <h2> Automated Notifications for Your County</h2>
              <p class="subtitle-1">
                Receive daily, weekly or monthly updates on your selected US Counties.
              </p>
              <p class="body-2">
                Whether you are returning or you are a new subscriber - <strong>the way you sign-up or log-in are the same</strong> (below).<br/><br/>
                Enter either an email address for email notifications or a full 10 digit phone number if you'd prefer updates via text-message (smart-phone required). We will send a link for log-in to the address/phone you enter.<br/>
              </p>
              <p class="body-1 handleForm" v-if=showButton>
              <v-form ref="form" v-model="valid" lazy-validation >
                <v-row>
                  <v-col>
                    <v-text-field
                      v-model="handle"
                      :rules="handleRules"
                      :error-messages="handleErrors"
                      label="Enter an e-mail or phone number"
                      :hint=handleHint
                    ></v-text-field>
                  </v-col>
                </v-row>

                <v-btn
                  :disabled="!valid"
                  color="success"
                  class="mr-4"
                  @click="SendVerificationLink"
                >Send link to my {{handleType}}
                </v-btn>
              </v-form>
              </p>
              <p class="body-1 handleForm" v-else>
                {{validationResultTxt}}
              </p>
              <p class="red--text" v-if="Feedback">{{Feedback}}</p>
            </v-col>
          </v-row>
          <v-row>
            <v-col class="body-2">
                <p>The updates provided by COVID Tracker are based on a <a href="https://dfr.vermont.gov/sites/finreg/files/doc_library/dfr-travel-map-methodology-071620.pdf">model</a> developed by the <a href="https://dfr.vermont.gov/about-us/covid-19/modeling">Vermont Department of Financial Regulation</a>. The COVID Tracker is a project of <a href="https://blog.uvm.edu/cwcallah/">UVM Extension Ag Engineering</a> and WalkerWorks. This project was made possible by support from the <a href="https://www.uvm.edu/engagement">UVM Office of Engagement</a> and <a href="https://accd.vermont.gov/covid-19">Vermont Agency of Commerce and Community Development</a>.</p>
                <p>Issued in furtherance of Cooperative Extension work, Acts of May 8 and June 30, 1914, in cooperation with the United States Department of Agriculture. University of Vermont Extension, Burlington, Vermont. University of Vermont Extension, and U.S. Department of Agriculture, cooperating, offer education and employment to everyone without regard to race, color, national origin, gender, religion, age, disability, political beliefs, sexual orientation, and marital or familial status. Any reference to commercial products, trade names, or brand names is for information only, and no endorsement or approval is intended.</p>
            </v-col>
          </v-row>
        </v-container>
  </div>
</template>
<style scoped>
.handleForm {
    border: 1px solid #333;
    padding: 30px 15px;
    background-color: #efefd0;
    border-radius: 12px;

    text-align: center;
}
</style>
<script>
import axios from 'axios';
export default {
  data() {
    return {
      showButton:true,
      validationResultTxt:'',
      Feedback:'',
      handleErrors:'',
      handleType: '...',
      valid: false,
      handle: '',
      handleHint: '',
      handleRules: [this.emailConfirmationRules],
    };
  },
  methods: {
    validate() {
    },
    emailConfirmationRules(val) {
      this.handleHint = '';
      if(!val) {
        this.valid = false;
        this.handleType = '...';
        return 'E-mail or 10-digit phone number is required';
      }
      if(val.includes('@')) { /* propably intending to enter an email */
        if(!(/^\w+([\\.-]?\w+)*@\w+([\\.-]?\w+)*(\.\w{2,3})+$/.test(val))) { /* is valid email format? */
          this.valid = false;
          this.handleType = '';
          return 'E-mail must be valid';
        }
        else {
          this.handleType = 'Email';
        }
      }
      else {
        if(val.length >=6) { /* min length for an email */
          if((val.replace(/[^0-9]/g,'').length !== 10)) { /* has exactly 10 digits? */
            this.valid = false;
            this.handleType = '...';
            return 'Not a valid 10-digit phone number or e-mail address';
          }
          else {
            /* isn't an email, does have 10 digits, IS valid */
            this.handleHint = this.formatPhoneNumber(val);
            this.handleType = 'Phone Number';
          }
        }
        else {
          /* too short for anything valid */
          this.valid = false;
          this.handleType = '...';
          return 'E-mail or 10-digit phone number is required';
        }
      }
      return true;
    },
    async SendVerificationLink() {
      try{
        const response = await axios.post('/api/confirm-user-send-login/', `handle=${this.handle}`);
        this.showButton = response.data.sent = '';
        this.validationResultTxt = response.data.message;
      }
      catch(ex) {
        this.Feedback = 'There was ab error saving the form.';
        console.log(ex);
      }
    }
  }
};
</script>