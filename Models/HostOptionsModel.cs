using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTrackUS_Web.Models
{
    public class HostOptions
    {
        public string CookieDomain { get; set; }

        public string OriginUrl { get; set; }

        public string CookieName { get; set; }

        /// <summary>
        /// Cookie expiration in seconds
        /// </summary>
        public int CookieTimeout { get; set; }

        public CookieProtectionOptions CookieProtection { get; set; }
    }
}
