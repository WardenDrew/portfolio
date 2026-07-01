using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public class DnsPacketQuestion
    {
        public string Name { get; set; } = string.Empty;
        public DnsResourceRecordTypes Type { get; set; } = DnsResourceRecordTypes.UNSET;
        public DnsClassCodes Class { get; set; } = DnsClassCodes.INTERNET;
    }
}
