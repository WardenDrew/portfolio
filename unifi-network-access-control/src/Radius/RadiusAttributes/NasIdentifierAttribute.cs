using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.NAS_IDENTIFIER)]
    public class NasIdentifierAttribute : BaseStringAttribute
    {
        [SetsRequiredMembers]
        public NasIdentifierAttribute(string value)
            : base(RadiusAttributeType.NAS_IDENTIFIER, value)
        {}

        private NasIdentifierAttribute() { }
    }
}
