namespace CovidTrackUS_Core.Models
{
    public class Settings
    {

        /// <summary>
        /// SQL Connection String
        /// </summary>
        public string DataConnectionString { get; set; }

        /// <summary>
        /// The cookie Domain
        /// </summary>
        public string CookieDomain { get; set; }

        /// <summary>
        /// Cookie Name
        /// </summary>
        public string CookieName { get; set; }

        /// <summary>
        /// Cookie Timeout value
        /// </summary>
        public int CookieTimeout { get; set; }
    }
}
