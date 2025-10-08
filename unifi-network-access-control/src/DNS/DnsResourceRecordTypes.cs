using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public enum DnsResourceRecordTypes : ushort
    {
        UNSET = 0,
        A = 1,
        NS = 2,
        [Obsolete]
        MD = 3,
        [Obsolete]
        MF = 4,
        CNAME = 5,
        SOA = 6,
        [Obsolete]
        MB = 7,
        [Obsolete]
        MG = 8,
        [Obsolete]
        MR = 9,
        [Obsolete]
        NULL = 10,
        [Obsolete]
        WKS = 11,
        PTR = 12,
        HINFO = 13,
        [Obsolete]
        MINFO = 14,
        MX = 15,
        TXT = 16,
        RP = 17,
        AFSDB = 18,
        X25 = 19,
        ISDN = 20,
        RT = 21,
        NSAP = 22,
        NSAP_PTR = 23,
        SIG = 24,
        KEY = 25,
        PX = 26,
        GPOS = 27,
        AAAA = 28,
        LOC = 29,
        [Obsolete]
        NXT = 30,
        EID = 31,
        NIMLOC = 32,
        SRV = 33,
        ATMA = 34,
        NAPTR = 35,
        KS = 36,
        CERT = 37,
        [Obsolete]
        A6 = 38,
        DNAME = 39,
        SINK = 40,
        OPT = 41,
        APL = 42,
        DS = 43,
        SSHFP = 44,
        IPSECKEY = 45,
        RRSIG = 46,
        NSEC = 47,
        DNSKEY = 48,
        DHCID = 49,
        NSEC3 = 50,
        NSEC3PARAM = 51,
        TLSA = 52,
        SMIMEA = 53,
        HIP = 55,
        [Obsolete]
        NINFO = 56,
        [Obsolete]
        RKEY = 57,
        TALINK = 58,
        CDS = 59,
        CDNSKEY = 60,
        OPENPGPKEY = 61,
        CSYNC = 62,
        ZONEMD = 63,
        SVCB = 64,
        HTTPS = 65,
        [Obsolete]
        SPF = 99,
        [Obsolete]
        UINFO = 100,
        [Obsolete]
        UID = 101,
        [Obsolete]
        GID = 102,
        [Obsolete]
        UNSPEC = 103,
        NID = 104,
        L32 = 105,
        L64 = 106,
        LP = 107,
        EUI48 = 108,
        TSIG = 250,
        URI = 256,
        CAA = 257,
        TKEY = 249,
        IXFR = 251,
        AXFR = 252,
        [Obsolete]
        MAILB = 253,
        [Obsolete]
        MAILA = 254,
        ALL = 255,
        DOA = 259,
        TA = 32768,
        DLV = 32769,
    }
}
