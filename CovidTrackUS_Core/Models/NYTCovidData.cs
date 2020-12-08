using System;

namespace CovidTrackUS_Core.Models
{
    /// <summary>
    /// Strongly typed class for deserializing NYTimes Data
    /// </summary>
   public class NYTCovidData
    {
        public DateTime date { get; set; }
        public string county { get; set; }
        public string fips { get; set; }
        public int cases { get; set; }
    }
}
