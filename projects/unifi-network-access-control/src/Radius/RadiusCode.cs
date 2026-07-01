using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public enum RadiusCode : byte
    {
        // RFC2865
        ACCESS_REQUEST = 1,
        ACCESS_ACCEPT = 2,
        ACCESS_REJECT = 3,
        ACCOUNTING_REQUEST = 4,
        ACCOUNTING_RESPONSE = 5,

        // RFC3575
        ACCOUNTING_STATUS = 6, // Also known as INTERIM_ACCOUNTING
        PASSWORD_REQUEST = 7,
        PASSWORD_ACK = 8,
        PASSWORD_REJECT = 9,
        ACCOUNTING_MESSAGE = 10,

        // RFC2865
        ACCESS_CHALLENGE = 11,
        STATUS_SERVER = 12,
        STATUS_CLIENT = 13,

        // RFC3575
        RESOURCE_FREE_REQUEST = 21,
        RESOURCE_FREE_RESPONSE = 22,
        RESOURCE_QUERY_REQUEST = 23,
        RESOURCE_QUERY_RESPONSE = 24,
        ALTERNATE_RESOURCE_RECLAIM_REQUEST = 25,
        NAS_REBOOT_REQUEST = 26,
        NAS_REBOOT_RESPONSE = 27,
        NEXT_PASSCODE = 29,
        NEW_PIN = 30,
        TERMINATE_SESSION = 31,
        PASSWORD_EXPIRED = 32,
        EVENT_REQUEST = 33,
        EVENT_RESPONSE = 34,

        // RFC5176 & RFC3575
        DISCONNECT_REQUEST = 40,
        DISCONNECT_ACK = 41,
        DISCONNECT_NAK = 42,
        COA_REQUEST = 43,
        COA_ACK = 44,
        COA_NAK = 45,

        // RFC3575
        IP_ADDRESS_ALLOCATE = 50,
        IP_ADDRESS_RELEASE = 51
    };
}
