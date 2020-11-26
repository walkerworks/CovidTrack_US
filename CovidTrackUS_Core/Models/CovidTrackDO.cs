using Dapper.Contrib.Extensions;

namespace CovidTrackUS_Core.Models
{
    public abstract class CovidTrackDO
    {
        [Key]
        public int ID { get; set; }
    }
}
