using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RawRadiusAttribute : IRadiusAttribute
    {
        public RawRadiusAttribute Raw => this;
        
        public RadiusAttributeType Type { get; set; }
        public byte Length { get; set; }

        private byte[]? _value;
        public byte[] Value
        {
            get { return _value ?? throw new NullReferenceException(); }
            set
            {
                if (2 + value.Length > byte.MaxValue)
                    throw new RadiusException("Attribute Value length exceeds overall attribute length limit!");

                _value = value;
                Length = Convert.ToByte(2 + value.Length);
            }
        }

        public static RawRadiusAttribute Build(RadiusAttributeType type, byte[] value)
        {
            RawRadiusAttribute attribute = new()
            {
                Type = type,
                Value = value
            };

            return attribute;
        }

        /// <summary>
        /// Extract the first AVP from the byte array. Returns true if there are more values to extract
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="attribute"></param>
        /// <param name="remainder"></param>
        /// <returns></returns>
        /// <exception cref="RadiusException"></exception>
        public static bool FromBytes(byte[] bytes, RadiusAttributeParser? parser, out IRadiusAttribute attribute, out byte[] remainder)
        {
            // Build Raw Attribute
            attribute = new RawRadiusAttribute();
            Span<byte> buffer = bytes.AsSpan();

            if (buffer.Length < 2)
            {
                throw new RadiusException("Malformed RADIUS Attribute. Length must be a minimum of 2 bytes!");
            }

            if (!Enum.IsDefined(typeof(RadiusAttributeType), buffer[0]))
            {
                throw new RadiusException("Malformed RADIUS Attribute. Unknown Type!");
            }

            attribute.Raw.Type = (RadiusAttributeType)buffer[0];
            byte length = buffer[1];
            if (length > 2)
            {
                attribute.Raw.Value = buffer[2..length].ToArray();
            }

            // Check if we can make this more specific
            if (parser is not null)
            {
                attribute = parser.Parse(attribute);
            }

            // Determine Remainder
            if (buffer.Length > attribute.Raw.Length)
            {
                remainder = buffer[attribute.Raw.Length..].ToArray();
                return true;
            }

            remainder = Array.Empty<byte>();
            return false;
        }

        public static List<IRadiusAttribute> ExtractAll(byte[] bytes, RadiusAttributeParser? parser)
        {
            List<IRadiusAttribute> attributes = new();

            while (bytes.Length > 0)
            {
                _ = FromBytes(bytes, parser, out IRadiusAttribute attribute, out bytes);

                attributes.Add(attribute);
            }

            return attributes;
        }
    }
}
