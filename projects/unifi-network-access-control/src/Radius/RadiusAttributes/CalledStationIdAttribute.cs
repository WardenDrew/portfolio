using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.CALLED_STATION_ID)]
    public class CalledStationIdAttribute : BaseStringAttribute
    {
        [SetsRequiredMembers]
        public CalledStationIdAttribute(string value)
            : base(RadiusAttributeType.CALLED_STATION_ID, value)
        {}

        private CalledStationIdAttribute() { }
    }
}
