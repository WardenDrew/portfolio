using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.TUNNEL_TYPE)]
    public class TunnelTypeAttribute : BaseRadiusAttribute
    {
        public enum TunnelTypes : int
        {
            PPTP = 1,
            L2F = 2,
            L2TP = 3,
            ATMP = 4,
            VTP = 5,
            AH = 6,
            IP_IP_ENCAPSULATION = 7,
            MINIMAL_IP_IP = 8,
            ESP = 9,
            GRE = 10,
            DVS = 11,
            IP_IP_TUNNEL = 12,
            VLAN = 13,
        }

        public int Tag
        {
            get
            {
                CheckLength();

                return Convert.ToInt32(Raw.Value[0]);
            }

            set
            {
                CheckLength();

                if (value < 0 || value > 0x1f)
                    throw new RadiusException("Out of Range Tag value passed to Tunnel-Type Attribute!");

                Raw.Value[0] = Convert.ToByte(value);
            }
        }

        public TunnelTypes TunnelType
        {
            get
            {
                CheckLength();

                byte[] buffer = [0, 0, 0, 0];
                Array.Copy(Raw.Value, 1, buffer, 1, 3);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                int value = BitConverter.ToInt32(buffer, 0);

                if (!Enum.IsDefined(typeof(TunnelTypes), value))
                    throw new RadiusException($"Malformed Tunnel-Type Attribute: Unknown TunnelType Value {value}");

                return (TunnelTypes)value;
            }

            set
            {
                CheckLength();

                int intValue = (int)value;

                if (intValue < 0 || intValue > 16777215)
                    throw new RadiusException("Out of Range Tunnel-Type value passed to Tunnel-Type Attribute!");

                byte[] buffer = BitConverter.GetBytes(intValue);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                Array.Copy(buffer, 1, Raw.Value, 1, 3);
            }
        }

        [SetsRequiredMembers]
        public TunnelTypeAttribute(int tag, TunnelTypes tunnelType)
        {
            Raw = new()
            {
                Type = RadiusAttributeType.TUNNEL_TYPE,
                Value = [0, 0, 0, 0]
            };

            this.Tag = tag;
            this.TunnelType = tunnelType;
        }

        private TunnelTypeAttribute() { }

        private void CheckLength()
        {
            if (Raw.Value.Length != 4)
                throw new RadiusException($"Malformed Tunnel-Type Attribute: Raw value is incorrect length. Got {Raw.Value.Length} and expected 4");
        }
    }
}
