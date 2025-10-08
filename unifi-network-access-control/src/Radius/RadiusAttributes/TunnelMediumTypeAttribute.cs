using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.TUNNEL_MEDIUM_TYPE)]
    public class TunnelMediumTypeAttribute : BaseRadiusAttribute
    {
        public enum Values : int
        {
            IPV4 = 1,
            IPV6 = 2,
            NSAP_8BIT = 3,
            HDLC = 4,
            BBN_1822 = 5,
            IEEE_802 = 6,
            E163_POTS = 7,
            E164_SMDS = 8,
            F69_TELEX = 9,
            X121_X25_FRAME_RELAY = 10,
            IPX = 11,
            APPLETALK = 12,
            DECNET_IV = 13,
            BANYAN_VINES = 14,
            E_164_NSAP = 15,
        }

        public int Tag
        {
            get
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                return Convert.ToInt32(Raw.Value[0]);
            }

            set
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                if (value < 0 || value > 0x1f)
                    throw new ArgumentOutOfRangeException(nameof(value));

                Raw.Value[0] = Convert.ToByte(value);
            }
        }

        public Values Value
        {
            get
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                byte[] buffer = [0, 0, 0, 0];
                Array.Copy(Raw.Value, 1, buffer, 1, 3);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                int value = BitConverter.ToInt32(buffer, 0);

                if (!Enum.IsDefined(typeof(Values), value))
                    throw new IndexOutOfRangeException(nameof(Value));

                return (Values)value;
            }

            set
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                int intValue = (int)value;

                if (intValue < 0 || intValue > 16777215)
                    throw new ArgumentOutOfRangeException(nameof(value));

                byte[] buffer = BitConverter.GetBytes(intValue);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                Array.Copy(buffer, 1, Raw.Value, 1, 3);
            }
        }

        [SetsRequiredMembers]
        public TunnelMediumTypeAttribute(int tag, Values type)
        {
            Raw = new()
            {
                Type = RadiusAttributeType.TUNNEL_MEDIUM_TYPE,
                Value = [0, 0, 0, 0]
            };

            this.Tag = tag;
            this.Value = type;
        }

        private TunnelMediumTypeAttribute() { }
    }
}
