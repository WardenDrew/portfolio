using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public enum DnsClassCodes : ushort
    {
        INTERNET = 1,
        CHAOS = 3,
        HESIOD = 4,
        NONE = 254,
        ANY = 255
    }
}
