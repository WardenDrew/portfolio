using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    public abstract class BaseRadiusAttribute : IRadiusAttribute
    {
        public required RawRadiusAttribute Raw { get; set; }
    }
}
