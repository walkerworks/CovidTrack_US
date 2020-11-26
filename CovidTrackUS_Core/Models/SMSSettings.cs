using Microsoft.Extensions.Configuration;

namespace CovidTrackUS_Core.Models
{
    public class SMSSettings
    {

        /// <summary>
        /// The phone number to use for Verification texts
        /// </summary>
        public string VerificationNumber { get; set; }

        /// <summary>
        /// The phone number to use for Notification texts
        /// </summary>
        public string NotificationNumber { get; set; }

        /// <summary>
        /// The API key for our 3rd party SMS Service
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The Sid for our 3rd party SMS Service
        /// </summary>
        public string Sid { get; set; }

        /// <summary>
        /// The Token key for our a party SMS Service
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Environment specific Host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Short link to https://loc.covid-watch.us/
        /// </summary>
        public string LocalShortLink
        {
            get
            {
                return "https://bit.ly/2UYQ24M";
            }
        }

        /// <summary>
        /// Short link to https://covid-watch.us/
        /// </summary>
        public string ShortLink
        {
            get
            {
                return "https://bit.ly/2J5T84D";
            }
        }
    }
}
