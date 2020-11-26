using CovidTrackUS_Core.Enums;
using Dapper.Contrib.Extensions;
using System;

namespace CovidTrackUS_Core.Models.Data
{
    /// <summary>
    /// CountySubscriber domain object. Maps a <see cref="Subscriber"/> to a <see cref="County"/>
    /// with details on how often they'd like to be notified of COVID statistics
    /// </summary>
    [Table("CountySubscriber")]
    public class CountySubscriber : CovidTrackDO
    {
        /// <summary>
        /// The Id of the <see cref="Subscriber"/>
        /// </summary>
        public int SubscriberID { get; set; }
        /// <summary>
        /// The Id of the <see cref="County"/>
        /// </summary>
        public int CountyID { get; set; }

        /// <summary>
        /// How often the subscriber has chosen to be notified.
        /// </summary>
        public string Frequency { get; set; }

        /// <summary>
        /// The last time the subscriber was notified about this county
        /// </summary>
        public DateTime? LastNotification { get; set; }

        [Computed]
        /// <summary>
        /// The County FIPS ID.  Has to be selected via join, not part of CountySubscriber table.
        /// </summary>
        public string FIPS { get; set; }

        [Computed]
        /// <summary>
        /// The subscriber.
        /// </summary>
        public Subscriber Subscriber { get; set; }

        [Computed]
        /// <summary>
        /// The County.
        /// </summary>
        public County County { get; set; }
    }
}
