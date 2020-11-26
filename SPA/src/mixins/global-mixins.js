/**
 * Global mixins to be added to the application for use everywhere
 */
export default {
  data() {
    return {

    };
  },
  methods : {
    formatPhoneNumber(phoneNumberString) {
      var cleaned = (`${  phoneNumberString}`).replace(/\D/g, '');
      var match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/);
      if (match) {
        return `(${  match[1]  }) ${  match[2]  }-${  match[3]}`;
      }
      return null;
    },
  }
};