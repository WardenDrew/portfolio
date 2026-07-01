using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.CALLING_STATION_ID)]
    public class CallingStationIdAttribute : BaseStringAttribute
    {
        [SetsRequiredMembers]
        public CallingStationIdAttribute(string value)
            : base(RadiusAttributeType.CALLING_STATION_ID, value)
        {}

        private CallingStationIdAttribute() { }
    }
}
