<template>
  <div>
        <v-container>
          <v-row>
            <v-col>
              <h2> Automated Notifications for Your County</h2>
              <p class="subtitle-1">
                Receive daily, weekly or monthly updates on your selected US Counties.
              </p>
              <v-divider></v-divider>
              <p class="body-1">
                You can log-in or sign up with either an email address for email notifications or a full 10 digit phone number if you'd prefer updates via text-message (smart-phone required). We will send a link to the address/phone you enter to log you in.<br/>
              </p>
              <p class="body-1 " v-if=showButton>
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
                >Send my log-in link
                </v-btn>
              </v-form>
              </p>
              <p class="body-1 success--text" v-else>
                {{validationResultTxt}}
              </p>
              <p class="red--text" v-if="Feedback">{{Feedback}}</p>
              <v-divider></v-divider>
            </v-col>
          </v-row>
        </v-container>
  </div>
</template>
<script>
import axios from 'axios';
export default {
  data() {
    return {
      showButton:true,
      validationResultTxt:'',
      Feedback:'',
      handleErrors:'',
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
        return 'E-mail or 10-digit phone number is required';
      }
      if(val.includes('@')) { /* propably intending to enter an email */
        if(!(/^\w+([\\.-]?\w+)*@\w+([\\.-]?\w+)*(\.\w{2,3})+$/.test(val))) { /* is valid email format? */
          this.valid = false;
          return 'E-mail must be valid';
        }
      }
      else {
        if(val.length >=6) { /* min length for an email */
          if((val.replace(/[^0-9]/g,'').length !== 10)) { /* has exactly 10 digits? */
            this.valid = false;
            return 'Not a valid 10-digit phone number or e-mail address';
          }
          else {
            /* isn't an email, does have 10 digits, IS valid */
            this.handleHint = this.formatPhoneNumber(val);
          }
        }
        else {
          /* too short for anything valid */
          this.valid = false;
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