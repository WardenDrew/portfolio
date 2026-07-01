using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public class DnsPacketFlags
    {
        public bool IsResponse { get; set; }

        // 4 bits
        public DnsPacketFlagsOpCodes OpCode { get; set; } = DnsPacketFlagsOpCodes.QUERY;

        // Response Only
        public bool IsAuthoritative { get; set; }

        public bool IsTruncated { get; set; }

        public bool IsRecursionDesired { get; set; }

        // Response Only
        public bool IsRecursionAvailable { get; set; }

        // zero bit here

        // Response Only
        public bool IsAuthenticData { get; set; }

        public bool IsCheckingDisabled { get; set; }

        // 4 bits
        public DnsPacketFlagsReplyCodes ReplyCode { get; set; } = DnsPacketFlagsReplyCodes.NO_ERROR;

        public static DnsPacketFlags FromSpan(Span<byte> buffer)
        {
            if (buffer.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(buffer));

            DnsPacketFlags flags = new();

            flags.IsResponse = ((buffer[0] >> 7) & 1) == 1;

            byte opCode = (byte)((buffer[0] >> 3) & 15);
            if (!Enum.IsDefined(typeof(DnsPacketFlagsOpCodes), opCode))
                throw new ArgumentOutOfRangeException(nameof(buffer));
            flags.OpCode = (DnsPacketFlagsOpCodes)opCode;

            flags.IsAuthoritative = ((buffer[0] >> 2) & 1) == 1;
            flags.IsTruncated = ((buffer[0] >> 1) & 1) == 1;
            flags.IsRecursionDesired = (buffer[0] & 1) == 1;
            
            flags.IsRecursionAvailable = ((buffer[1] >> 7) & 1) == 1;
            flags.IsAuthenticData = ((buffer[1] >> 5) & 1) == 1;
            flags.IsCheckingDisabled = ((buffer[1] >> 4) & 1) == 1;

            byte replyCode = (byte)(buffer[1] & 15);
            if (!Enum.IsDefined(typeof(DnsPacketFlagsReplyCodes), replyCode))
                throw new ArgumentOutOfRangeException(nameof(buffer));
            flags.ReplyCode = (DnsPacketFlagsReplyCodes)replyCode;

            return flags;
        }

        public void WriteSpan(Span<byte> buffer)
        {
            if (buffer.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(buffer));

            _ = this.IsResponse
                ? buffer[0] |= 128
                : buffer[0] &= unchecked((byte)~128);

            buffer[0] |= (byte)((byte)this.OpCode << 3);

            _ = this.IsAuthoritative
                ? buffer[0] |= 4
                : buffer[0] &= unchecked((byte)~4);

            _ = this.IsTruncated
                ? buffer[0] |= 2
                : buffer[0] &= unchecked((byte)~2);

            _ = this.IsRecursionDesired
                ? buffer[0] |= 1
                : buffer[0] &= unchecked((byte)~1);

            _ = this.IsRecursionAvailable
                ? buffer[1] |= 128
                : buffer[1] &= unchecked((byte)~128);

            _ = this.IsAuthenticData
                ? buffer[1] |= 64
                : buffer[1] &= unchecked((byte)~64);

            _ = this.IsCheckingDisabled
                ? buffer[1] |= 32
                : buffer[1] &= unchecked((byte)~32);

            buffer[1] |= (byte)this.ReplyCode;
        }
    }
}
