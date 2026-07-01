using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public class RawDnsResourceRecord : IDnsResourceRecord
    {
        public RawDnsResourceRecord Raw => this;

        public string Name { get; set; } = string.Empty;
        public DnsResourceRecordTypes Type { get; set; } = DnsResourceRecordTypes.UNSET;
        public DnsClassCodes Class { get; set; } = DnsClassCodes.INTERNET;
        public uint TTL { get; set; } = 300;
        public ushort ValueLength { get; set; } = 0;

        private byte[] _value = [];
        public byte[] Value
        {
            get { return _value; }
            set
            {
                if (value.Length > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _value = value;
                ValueLength = Convert.ToUInt16(value.Length);
            }
        }
    }
}
