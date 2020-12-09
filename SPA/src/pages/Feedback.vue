<template>
  <div>
        <v-container>
          <v-row>
            <v-col>
              <h2>Thank you</h2>
              <p class="subtitle-1">
                We appreciate the feedback.
              </p>
              <p class="body-2" v-if=preSubmit>
                Is there anything you would like to share?
              </p>
              <div class="body-1 feedbackForm" v-if=preSubmit>
                <v-form ref="form" lazy-validation >
                  <v-row>
                    <v-col>
                      <v-text-field
                        v-model="feedback"
                        label="Enter any additional comments here"
                      ></v-text-field>
                    </v-col>
                  </v-row>
                  <v-btn color="success" class="mr-4" @click="SaveFeedback">Send feedback</v-btn>
                </v-form>
              </div>
              <p class="red--text" v-if="ErrorMsg">{{ErrorMsg}}</p>
              <p class="body-1 feedbackForm" v-else>
                {{validationResultTxt}}
              </p>
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
      preSubmit:'true',
      validationResultTxt:'',
      feedback:null,
      feedbackId:this.$route.params.feedbackId,
      subscriberId:this.$route.params.subscriberId,
      ErrorMsg: null,
    };
  },
  methods: {
    async SaveFeedback() {
      try{
        let form = {
          feedbackId: this.feedbackId,
          subscriberId: this.subscriberId,
          feedback: this.feedback
        };
        let result = await axios.post('/api/feedback/add',form);
        if(result.data.error) {
          this.ErrorMsg = result.data.error;
        }
        else{
          this.preSubmit = false;
          this.validationResultTxt = 'Thank you, your comments have been received.';
        }
      }
      catch(ex) {
        this.ErrorMsg = 'There was an error saving the form.';
        console.log(ex);
      }
    }
  }
};
</script>