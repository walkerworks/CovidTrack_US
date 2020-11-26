using System;

namespace CovidTrackUS_Core.Models
{
    /// <summary>
    /// Strongly typed class for deserializing NYC Covid Data from Json
    /// </summary>
   public class NYCCovidData
    {
        public DateTime test_date { get; set; }
        public string county { get; set; }
        public int new_positives { get; set; }
    }
}
