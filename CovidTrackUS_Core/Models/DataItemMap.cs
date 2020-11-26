using CovidTrackUS_Core.Enums;
using System;

namespace CovidTrackUS_Core.Models
{
    public class DataItemMap
    {
        public Type Type { get; private set; }
        public DataRetrieveType DataRetrieveType { get; private set; }
        public string PropertyName { get; private set; }

        public DataItemMap(Type type, DataRetrieveType dataRetrieveType, string propertyName)
        {
            Type = type;
            DataRetrieveType = dataRetrieveType;
            PropertyName = propertyName;
        }
    }
}
