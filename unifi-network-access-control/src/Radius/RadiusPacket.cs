using Microsoft.VisualBasic.FileIO;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RadiusPacket
    {
        public static readonly byte[] EMPTY_AUTHENTICATOR = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,];


        public RadiusCode Code { get; set; }

        public byte Identifier { get; set; }

        public ushort Length { get; private set; } = 20;

        public byte[] Authenticator { get; private set; } = Array.Empty<byte>();

        private List<IRadiusAttribute> Attributes = [];

        private RadiusPacket() { }

        public static RadiusPacket Create(
            RadiusCode code,
            byte? identifier = null,
            byte[]? authenticator = null,
            params IRadiusAttribute[] attributes)
        {
            RadiusPacket packet = new()
            {
                Code = code,
                Identifier = identifier ?? 0,
                Authenticator = authenticator ?? EMPTY_AUTHENTICATOR,
                Attributes = [.. attributes]
            };

            return packet.UpdateLength();
        }

        public RadiusPacket UpdateLength()
        {
            // Calculate Length
            int packetLength = 20;

            foreach (IRadiusAttribute attribute in this.Attributes)
            {
                packetLength += attribute.Raw.Length;
            }

            if (packetLength > ushort.MaxValue)
            {
                throw new RadiusException($"Malformed RADIUS Packet. Length larger than {ushort.MaxValue} bytes");
            }

            this.Length = (ushort)packetLength;

            return this;
        }

        public RadiusPacket ReplaceAuthenticator(byte[]? authenticator)
        {
            if (authenticator is null)
                authenticator = EMPTY_AUTHENTICATOR;

            if (authenticator.Length != 16)
                throw new RadiusException($"Invalid Authenticator Length");

            this.Authenticator = authenticator;
            return this;
        }

        public byte[] CalculateAuthenticator(byte[] secret, byte[]? preinsertAuthenticator = null)
        {
            byte[] buffer = this.ToBytes();

            if (preinsertAuthenticator is not null)
            {
                if (preinsertAuthenticator.Length != 16)
                    throw new RadiusException("Invalid Pre-Insert Authenticator Length!");

                Array.Copy(preinsertAuthenticator, 0, buffer, 4, 16);
            }

            byte[] md5Buffer = new byte[buffer.Length + secret.Length];

            Array.Copy(
                sourceArray: buffer,
                sourceIndex: 0,
                destinationArray: md5Buffer,
                destinationIndex: 0,
                length: buffer.Length);
            Array.Copy(
                sourceArray: secret,
                sourceIndex: 0,
                destinationArray: md5Buffer,
                destinationIndex: buffer.Length,
                length: secret.Length);

            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            return md5.ComputeHash(md5Buffer);
        }

        public RadiusPacket AddResponseAuthenticator(byte[] secret, byte[] requestAuthenticator)
        {
            return this.ReplaceAuthenticator(this.CalculateAuthenticator(secret, requestAuthenticator));
        }

        public RadiusPacket AddMessageAuthenticator(byte[] secret)
        {
            byte[] buffer = this.ToBytes();

            using System.Security.Cryptography.HMACMD5 hmac = new(secret);

            byte[] hash = hmac.ComputeHash(buffer);

            RawRadiusAttribute messageAuthenticator
                    = RawRadiusAttribute.Build(RadiusAttributeType.MESSAGE_AUTHENTICATOR, hash);

            return this.AddAttribute(messageAuthenticator);
        }

        public RadiusPacket AddAttribute(IRadiusAttribute? attribute)
        {
            if (attribute is null) return this;
            
            if (this.Attributes.Any(x => x.Raw.Type == attribute.Raw.Type))
            {
                throw new RadiusException($"Refusing to add duplicate attribute of type {attribute.Raw.Type}!");
            }

            this.Attributes.Add(attribute);
            this.Length += attribute.Raw.Length;

            return this;
        }

        public RadiusPacket AddAttributes(params IRadiusAttribute[] attributes)
        {
            foreach (IRadiusAttribute attribute in attributes)
            {
                this.AddAttribute(attribute);
            }

            return this;
        }

        public RadiusPacket RemoveAttribute(RadiusAttributeType type)
        {
            IRadiusAttribute? attribute = this.Attributes
                .Where(x => x.Raw.Type == type)
                .FirstOrDefault();

            if (attribute is null) return this;

            this.Attributes.Remove(attribute);
            this.Length -= attribute.Raw.Length;

            return this;
        }

        public static RadiusPacket FromBytes(byte[] bytes, RadiusAttributeParser? parser)
        {
            Span<byte> buffer = bytes.AsSpan();

            if (buffer.Length < 20)
            {
                throw new RadiusException("Malformed RADIUS Packet. Length must be a minimum of 20 bytes!");
            }

            if (!Enum.IsDefined(typeof(RadiusCode), buffer[0]))
            {
                throw new RadiusException("Malformed RADIUS Packet. Unknown Code!");
            }

            RadiusPacket packet = new();
            packet.Code = (RadiusCode)buffer[0];
            packet.Identifier = buffer[1];
            if (BitConverter.IsLittleEndian)
            {
                packet.Length = BinaryPrimitives.ReverseEndianness(
                    BitConverter.ToUInt16(buffer[2..4]));
            }
            else
            {
                packet.Length = BitConverter.ToUInt16(buffer[2..4]);
            }
            packet.Authenticator = buffer[4..20].ToArray();

            packet.Attributes =  RawRadiusAttribute.ExtractAll(buffer[20..].ToArray(), parser);

            return packet;
        }

        public byte[] ToBytes()
        {
            byte[] buffer = new byte[this.Length];

            buffer[0] = (byte)this.Code;
            buffer[1] = this.Identifier;

            byte[] lengthBytes;
            if (BitConverter.IsLittleEndian)
            {
                lengthBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(this.Length));
            }
            else
            {
                lengthBytes = BitConverter.GetBytes(this.Length);
            }

            Array.Copy(
                sourceArray: lengthBytes, 
                sourceIndex: 0, 
                destinationArray: buffer, 
                destinationIndex: 2, 
                length: 2);

            if (this.Authenticator.Length != 16)
            {
                throw new RadiusException("Malformed RADIUS Packet. Authenticator length is not 16 bytes!");
            }

            Array.Copy(
                sourceArray: this.Authenticator,
                sourceIndex: 0,
                destinationArray: buffer,
                destinationIndex: 4,
                length: 16);

            int index = 20;
            foreach (IRadiusAttribute attribute in this.Attributes)
            {
                buffer[index] = (byte)attribute.Raw.Type;
                buffer[index + 1] = attribute.Raw.Length;

                Array.Copy(
                    sourceArray: attribute.Raw.Value,
                    sourceIndex: 0,
                    destinationArray: buffer,
                    destinationIndex: index + 2,
                    length: attribute.Raw.Length - 2);

                index += attribute.Raw.Length;
            }

            return buffer;
        }

        public IRadiusAttribute? GetAttribute(RadiusAttributeType type)
        {
            return this.Attributes
                .Where(x => x.Raw.Type == type)
                .FirstOrDefault();
        }

        public T? GetAttribute<T>()
        {
            return this.Attributes
                .Where(x => x.GetType() == typeof(T))
                .Cast<T>()
                .FirstOrDefault();
        }
    }
}
