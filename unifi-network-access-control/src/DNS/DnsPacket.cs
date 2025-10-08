using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public class DnsPacket
    {
        public ushort TransactionId { get; set; }
        public DnsPacketFlags Flags { get; set; } = new();
        public ushort NumQuestions { get; set; }
        public ushort NumAnswers { get; set; }
        public ushort NumAuthorities { get; set; }
        public ushort NumAdditional { get; set; }

        public List<DnsPacketQuestion> Questions { get; set; } = new();
        public List<IDnsResourceRecord> Answers { get; set; } = new();
        public List<IDnsResourceRecord> Authorities { get; set; } = new();
        public List<IDnsResourceRecord> Additional { get; set; } = new();

        public static DnsPacket FromBytes(byte[] bytes)
        {
            Span<byte> buffer = bytes.AsSpan();
            if (buffer.Length < 12)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            DnsPacket packet = new();

            packet.TransactionId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));
            packet.Flags = DnsPacketFlags.FromSpan(buffer.Slice(2, 2));
            packet.NumQuestions = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4, 2));
            packet.NumAnswers = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(6, 2));
            packet.NumAuthorities = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(8, 2));
            packet.NumAdditional = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(10, 2));

            int index = 12;

            for (int i = 0; i < packet.NumQuestions; i++)
            {
                DnsPacketQuestion question = new();
                question.Name = DnsStringHelper.ToString(buffer.Slice(index), out int numBytesParsed);
                index += numBytesParsed;

                question.Type = (DnsResourceRecordTypes)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index, 2));
                question.Class = (DnsClassCodes)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index + 2, 2));
                index += 4;

                packet.Questions.Add(question);
            }

            index = ResourceRecordSetFromBytes(buffer, index, packet.NumAnswers, packet.Answers);
            index = ResourceRecordSetFromBytes(buffer, index, packet.NumAuthorities, packet.Authorities);
            index = ResourceRecordSetFromBytes(buffer, index, packet.NumAdditional, packet.Additional);

            return packet;
        }

        private static int ResourceRecordSetFromBytes(Span<byte> buffer, int index, int numRecords, List<IDnsResourceRecord> recordSet)
        {
            for (int i = 0; i < numRecords; i++)
            {
                RawDnsResourceRecord record = new();
                record.Name = DnsStringHelper.ToString(buffer.Slice(index), out int numBytesParsed);
                index += numBytesParsed;

                record.Type = (DnsResourceRecordTypes)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index, 2));
                record.Class = (DnsClassCodes)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index + 2, 2));
                record.TTL = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(index + 4, 4));
                record.ValueLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index + 8, 2));
                index += 10;

                record.Value = buffer.Slice(index, record.ValueLength).ToArray();
                index += record.ValueLength;

                recordSet.Add(record);
            }

            return index;
        }

        public byte[] ToBytes()
        {
            byte[] headerBytes = new byte[12];
            Span<byte> headerBuffer = new(headerBytes);

            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(0, 2), this.TransactionId);
            this.Flags.WriteSpan(headerBuffer.Slice(2, 2));
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(4, 2), Convert.ToUInt16(this.Questions.Count));
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(6, 2), Convert.ToUInt16(this.Answers.Count));
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(8, 2), Convert.ToUInt16(this.Authorities.Count));
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(10, 2), Convert.ToUInt16(this.Additional.Count));

            List<byte[]> chunks = new();

            foreach (DnsPacketQuestion question in this.Questions)
            {
                byte[] name = DnsStringHelper.ToDnsBytes(question.Name);
                byte[] chunk = new byte[name.Length + 4];
                name.CopyTo(chunk, 0);

                Span<byte> chunkBuffer = new(chunk);

                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(name.Length, 2), (ushort)question.Type);
                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(name.Length+2, 2), (ushort)question.Class);

                chunks.Add(chunk);
            }

            foreach (IDnsResourceRecord record in this.Answers)
            {
                byte[] name = DnsStringHelper.ToDnsBytes(record.Raw.Name);
                int nameLength = name.Length;

                byte[] chunk = new byte[nameLength + 10 + record.Raw.Value.Length];
                name.CopyTo(chunk, 0);
                record.Raw.Value.CopyTo(chunk, nameLength + 10);

                Span<byte> chunkBuffer = new(chunk);

                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(nameLength, 2), (ushort)record.Raw.Type);
                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(nameLength + 2, 2), (ushort)record.Raw.Class);
                BinaryPrimitives.TryWriteUInt32BigEndian(chunkBuffer.Slice(nameLength + 4, 4), record.Raw.TTL);
                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(nameLength + 8, 2), Convert.ToUInt16(record.Raw.Value.Length));

                chunks.Add(chunk);
            }

            int chunksLength = chunks.Sum(x => x.Length);
            byte[] result = new byte[headerBytes.Length + chunksLength];

            headerBytes.CopyTo(result, 0);

            int index = 12;
            foreach (byte[] chunk in chunks)
            {
                chunk.CopyTo(result, index);
                index += chunk.Length;
            }

            return result;
        }
    }
}
