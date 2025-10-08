using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.NAS_IP_ADDRESS)]
    public class NasIpAddressAttribute : BaseRadiusAttribute
    {
        public IPAddress Address
        {
            get
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                return new IPAddress(Raw.Value);
            }

            set
            {
                byte[] buffer = value.GetAddressBytes();
                if (buffer.Length != 4)
                    throw new ArgumentOutOfRangeException(nameof(value));

                Raw.Value = buffer;
            }
        }

        [SetsRequiredMembers]
        public NasIpAddressAttribute(IPAddress address)
        {
            Raw = new()
            {
                Type = RadiusAttributeType.NAS_IP_ADDRESS,
                Value = []
            };

            this.Address = address;
        }

        private NasIpAddressAttribute() { }
    }
}
