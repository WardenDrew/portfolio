using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public class DnsResourceAttribute(DnsResourceRecordTypes type) : Attribute
    {
        public DnsResourceRecordTypes Type { get; set; } = type;
    }
}
