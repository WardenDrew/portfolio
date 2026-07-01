using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public enum DnsPacketFlagsOpCodes : byte
    {
        QUERY = 0,
        [Obsolete]
        IQUERY = 1,
        STATUS = 2,
        NOTIFY = 4,
        UPDATE = 5,
        DSO = 6
    }
}
