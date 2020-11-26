using Microsoft.Extensions.Configuration;

namespace CovidTrackUS_Core.Models
{
    public class EmailSettings
    {

        /// <summary>
        /// The Sender email address for notification emails
        /// </summary>
        public string NotifyAddress { get; set; }

        /// <summary>
        /// The Sender email address for login emails
        /// </summary>
        public string LoginAddress { get; set; }

        /// <summary>
        /// Email Sender API template ID for notifications
        /// </summary>
        public string NotificationTemplateID { get; set; }


        /// <summary>
        /// Email Sender API template ID for login links
        /// </summary>
        public string LoginKeyTemplateID { get; set; }

        /// <summary>
        /// The API key for our 3rd party Emailer
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Our friendly "from" name for our notification emails
        /// </summary>
        public string FriendlyNotify { get; set; }
        /// <summary>
        /// Our friendly "from" name for our login emails
        /// </summary>
        public string FriendlyLogin { get; set; }
        /// <summary>
        /// Environment specific Host
        /// </summary>
        public string Host { get; set; }
    }
}
