using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTrackUS_Web.Models
{
    public class CookieProtectionOptions
    {
        /// <summary>
        /// Path to a public certificate to encrypt the data protection keys at rest.
        /// The pfx file is adequate for the job.
        /// </summary>
        public string CertificatePath { get; set; }
    }
}
