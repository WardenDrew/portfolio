using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RadiusAttributeAttribute(RadiusAttributeType type) : Attribute
    {
        public RadiusAttributeType Type { get; set; } = type;
    }
}
