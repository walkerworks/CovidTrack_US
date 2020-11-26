using Dapper.Contrib.Extensions;
using System;

namespace CovidTrackUS_Core.Models.Data
{
    /// <summary>
    /// Simply object to record feedback from subscribers to the nofitication service
    /// </summary>
    [Table("SubscriberFeedback")]
    public class SubscriberFeedback : CovidTrackDO
    {
        public int SubscriberID { get; set; }
        public bool IsPositive { get; set; }

        public string Feedback { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
