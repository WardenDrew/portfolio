using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public static class DnsStringHelper
    {
        public static string ToString(Span<byte> buffer, out int numBytesParsed)
        {
            string result = string.Empty;
            if (buffer.Length < 3)
                throw new ArgumentOutOfRangeException(nameof(buffer));

            int currentLength = 0;
            int index = 0;

            while (true)
            {
                currentLength = Convert.ToInt32(buffer[index]);
                index++;

                if (currentLength == 0 ||
                    index + currentLength >= buffer.Length)
                {
                    break;
                }

                result += Encoding.UTF8.GetString(buffer.Slice(index, currentLength));
                result += '.';

                index += currentLength;
            }

            // zero length string safety
            if (index < 2) { index = 2; }

            numBytesParsed = index;

            return result.TrimEnd('.');
        }

        public static byte[] ToDnsBytes(string value)
        {
            string[] labels = value.Split('.');

            // length of each label + size byte for each label + zero byte at end
            int totallength = labels.Sum(label => label.Length) + labels.Length + 1;

            byte[] bytes = new byte[totallength];
            int index = 0;

            foreach (string label in labels)
            {
                byte[] labelBytes = Encoding.UTF8.GetBytes(label);

                if (labelBytes.Length > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value));

                bytes[index++] = Convert.ToByte(labelBytes.Length);

                labelBytes.CopyTo(bytes, index);
                index += labelBytes.Length;
            }

            return bytes;
        }
    }
}
