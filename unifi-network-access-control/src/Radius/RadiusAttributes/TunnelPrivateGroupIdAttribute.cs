using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.TUNNEL_PRIVATE_GROUP_ID)]
    public class TunnelPrivateGroupIdAttribute : BaseRadiusAttribute
    {
        public int? Tag
        {
            get
            {
                if (Raw.Value.Length < 1)
                    throw new InvalidOperationException();

                int tag = Convert.ToInt32(Raw.Value[0]);

                // Values larger than 0x1f should be interpreted as the first character of the string
                return tag <= 0x1f ? tag : null;
            }

            set
            {
                if (Raw.Value.Length < 1)
                    throw new InvalidOperationException();

                if (value < 0 || value > 0x1f)
                    throw new ArgumentOutOfRangeException(nameof(value));

                // Check if we need to shift the string out of the way
                int currentTag = Convert.ToInt32(Raw.Value[0]);
                if (currentTag > 0x1f)
                {
                    byte[] buffer = new byte[Raw.Value.Length + 1];
                    Array.Copy(Raw.Value, 0, buffer, 1, Raw.Value.Length);
                    Raw.Value = buffer;
                }

                Raw.Value[0] = Convert.ToByte(value);
            }
        }

        public string GroupId
        {
            get
            {
                if (Raw.Value.Length < 1)
                    throw new InvalidOperationException();

                // Check if we can fastpath with no tag
                int currentTag = Convert.ToInt32(Raw.Value[0]);
                if (currentTag > 0x1f)
                {
                    return Encoding.UTF8.GetString(Raw.Value);
                }

                // Skip first byte otherwise
                return Encoding.UTF8.GetString(Raw.Value[1..]);
            }

            set
            {
                if (Raw.Value.Length < 1)
                    throw new InvalidOperationException();

                byte[] buffer = Encoding.UTF8.GetBytes(value);

                // Check if we can fastpath with no tag
                int currentTag = Convert.ToInt32(Raw.Value[0]);
                if (currentTag > 0x1f)
                {
                    Raw.Value = buffer;
                }

                byte[] newVal = new byte[buffer.Length + 1];
                newVal[0] = Raw.Value[0];
                Array.Copy(buffer, 0, newVal, 1, buffer.Length);

                Raw.Value = newVal;
            }
        }

        [SetsRequiredMembers]
        public TunnelPrivateGroupIdAttribute(int? tag, string groupId)
        {
            Raw = new()
            {
                Type = RadiusAttributeType.TUNNEL_PRIVATE_GROUP_ID,
                Value = [0]
            };

            this.Tag = tag;
            this.GroupId = groupId;
        }

        private TunnelPrivateGroupIdAttribute() { }
    }
}
