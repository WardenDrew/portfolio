using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public enum DnsPacketFlagsReplyCodes : byte
    {
        NO_ERROR = 0,
        FORMAT_ERROR = 1,
        SERVER_FAILURE = 2,
        NAME_ERROR = 3,
        NOT_IMPLEMENTED = 4,
        REFUSED = 5,
        YX_DOMAIN = 6,
        YX_RR_SET = 7,
        NX_RR_SET = 8,
        NOT_AUTH = 9,
        NOT_ZONE = 10,
        DSOTYPENI = 11,
        BADSIG = 16,
        BADKEY = 17,
        BADTIME = 18,
        BADMODE = 19,
        BADNAME = 20,
        BADALG = 21,
        BADTRUNC = 22,
        BADCOOKIE = 23
    }
}
