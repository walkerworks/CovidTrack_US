using CovidTrackUS_Core.Enums;
using Dapper.Contrib.Extensions;
using System;

namespace CovidTrackUS_Core.Models.Data
{
    /// <summary>
    /// Represents a subscriber and a method of notification.
    /// Essentially, a 10 digit phone number or an Email address.
    /// </summary>
    [Table("Subscriber")]
    public class Subscriber : CovidTrackDO
    {
        public string Handle { get; set; }
        public bool Verified { get; set; }
        public HandleType Type { get; set; }
        public int Notifications { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UnsubscribedOn { get; set; }

    }
}
