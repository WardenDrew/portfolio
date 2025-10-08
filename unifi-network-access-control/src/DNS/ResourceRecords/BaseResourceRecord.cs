using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DNS;

namespace DNS.ResourceRecords
{
    public abstract class BaseResourceRecord : IDnsResourceRecord
    {
        public required RawDnsResourceRecord Raw { get; set; }

        public string Name { get => Raw.Name; set => Raw.Name = value; }
        public DnsResourceRecordTypes Type { get => Raw.Type; set => Raw.Type = value; }
        public DnsClassCodes Class { get => Raw.Class; set => Raw.Class = value; }
        public uint TTL { get => Raw.TTL; set => Raw.TTL = value; }
        public ushort ValueLength { get => Raw.ValueLength; set => Raw.ValueLength = value; }
        public byte[] Value { get => Raw.Value; set => Raw.Value = value; }
    }
}
