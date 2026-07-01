using System.Buffers.Binary;
using System.Net.Sockets;
using local_tagcollector.Models;
using shared_dotnet.LocalConfiguration;

namespace local_tagcollector.Modbus;

internal sealed class ModbusTcpDeviceClient : IDisposable
{
    private readonly TagCollectorDeviceDto _device;
    private readonly ModbusTcpConnectionSettingsDto _connectionSettings;
    private readonly TcpClient _client = new();
    private NetworkStream? _stream;
    private ushort _nextTransactionId = 1;

    public ModbusTcpDeviceClient(TagCollectorDeviceDto device)
    {
        _device = device;
        _connectionSettings = device.ModbusTcpConnectionSettings
            ?? throw new InvalidOperationException($"Device '{device.Name}' is missing Modbus TCP/IP connection settings.");
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _client.NoDelay = true;
        await _client.ConnectAsync(_connectionSettings.Host, _connectionSettings.Port, cancellationToken);
        _stream = _client.GetStream();
    }

    public async Task<TagReading> ReadTagAsync(TagCollectorTagDto tag, CancellationToken cancellationToken)
    {
        object value = IsBitArea(tag.Area)
            ? await ReadBitAsync(tag, cancellationToken)
            : await ReadRegistersAsync(tag, cancellationToken);

        return new TagReading(
            _device.Id,
            _device.Name,
            _device.ConnectionMethod,
            (byte)_connectionSettings.UnitId,
            tag.Id,
            tag.Name,
            tag.Area,
            tag.Address,
            tag.DataType,
            tag.EngineeringUnit,
            value,
            DateTimeOffset.UtcNow);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client.Dispose();
    }

    public static bool IsBitArea(ModbusArea area)
    {
        return area is ModbusArea.Coil or ModbusArea.DiscreteInput;
    }

    public static int GetRegisterCount(TagDataType dataType)
    {
        return dataType switch
        {
            TagDataType.Bool or TagDataType.Int16 or TagDataType.UInt16 => 1,
            TagDataType.Int32 or TagDataType.UInt32 or TagDataType.Float32 => 2,
            TagDataType.Int64 or TagDataType.UInt64 or TagDataType.Float64 => 4,
            _ => throw new InvalidOperationException($"Unsupported tag data type '{dataType}'.")
        };
    }

    private async Task<bool> ReadBitAsync(TagCollectorTagDto tag, CancellationToken cancellationToken)
    {
        byte functionCode = tag.Area switch
        {
            ModbusArea.Coil => 0x01,
            ModbusArea.DiscreteInput => 0x02,
            _ => throw new InvalidOperationException($"Area '{tag.Area}' is not a bit area.")
        };

        byte[] responsePdu = await SendReadRequestAsync(functionCode, checked((ushort)tag.Address), 1, cancellationToken);

        if (responsePdu.Length < 3 || responsePdu[1] < 1)
        {
            throw new InvalidOperationException($"Modbus response for tag '{tag.Name}' did not include bit data.");
        }

        return (responsePdu[2] & 0x01) != 0;
    }

    private async Task<object> ReadRegistersAsync(TagCollectorTagDto tag, CancellationToken cancellationToken)
    {
        byte functionCode = tag.Area switch
        {
            ModbusArea.HoldingRegister => 0x03,
            ModbusArea.InputRegister => 0x04,
            _ => throw new InvalidOperationException($"Area '{tag.Area}' is not a register area.")
        };

        ushort quantity = checked((ushort)GetRegisterCount(tag.DataType));
        byte[] responsePdu = await SendReadRequestAsync(functionCode, checked((ushort)tag.Address), quantity, cancellationToken);
        int byteCount = quantity * 2;

        if (responsePdu.Length < 2 + byteCount || responsePdu[1] != byteCount)
        {
            throw new InvalidOperationException($"Modbus response for tag '{tag.Name}' did not include {quantity} registers.");
        }

        ushort[] registers = new ushort[quantity];
        for (int index = 0; index < quantity; index++)
        {
            registers[index] = BinaryPrimitives.ReadUInt16BigEndian(responsePdu.AsSpan(2 + (index * 2), 2));
        }

        return DecodeRegisterValue(tag.DataType, registers, tag.WordOrder);
    }

    private async Task<byte[]> SendReadRequestAsync(
        byte functionCode,
        ushort startAddress,
        ushort quantity,
        CancellationToken cancellationToken)
    {
        NetworkStream stream = _stream
            ?? throw new InvalidOperationException($"Modbus device '{_device.Name}' is not connected.");

        ushort transactionId = _nextTransactionId++;
        byte[] request = new byte[12];

        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(0, 2), transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(2, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(4, 2), 6);
        request[6] = (byte)_connectionSettings.UnitId;
        request[7] = functionCode;
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(8, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(10, 2), quantity);

        await stream.WriteAsync(request, cancellationToken);

        byte[] header = new byte[7];
        await ReadExactlyAsync(stream, header, cancellationToken);

        ushort responseTransactionId = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0, 2));
        ushort protocolId = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2, 2));
        ushort responseLength = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4, 2));
        byte unitId = header[6];

        if (responseTransactionId != transactionId)
        {
            throw new InvalidOperationException(
                $"Modbus transaction mismatch for device '{_device.Name}'. Expected {transactionId}, received {responseTransactionId}.");
        }

        if (protocolId != 0)
        {
            throw new InvalidOperationException($"Modbus response from device '{_device.Name}' used protocol id {protocolId}.");
        }

        if (unitId != _connectionSettings.UnitId)
        {
            throw new InvalidOperationException(
                $"Modbus response from device '{_device.Name}' used unit id {unitId}, expected {_connectionSettings.UnitId}.");
        }

        if (responseLength < 2)
        {
            throw new InvalidOperationException($"Modbus response from device '{_device.Name}' was too short.");
        }

        byte[] responsePdu = new byte[responseLength - 1];
        await ReadExactlyAsync(stream, responsePdu, cancellationToken);

        if (responsePdu.Length >= 2 && responsePdu[0] == (byte)(functionCode | 0x80))
        {
            throw new InvalidOperationException(
                $"Modbus device '{_device.Name}' returned exception code {responsePdu[1]} for function 0x{functionCode:X2}.");
        }

        if (responsePdu.Length == 0 || responsePdu[0] != functionCode)
        {
            throw new InvalidOperationException(
                $"Modbus device '{_device.Name}' returned unexpected function code 0x{(responsePdu.Length == 0 ? 0 : responsePdu[0]):X2}.");
        }

        return responsePdu;
    }

    private static async Task ReadExactlyAsync(
        NetworkStream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException("The Modbus connection closed before the response was complete.");
            }

            offset += bytesRead;
        }
    }

    private static object DecodeRegisterValue(
        TagDataType dataType,
        IReadOnlyList<ushort> registers,
        RegisterWordOrder wordOrder)
    {
        ushort[] orderedRegisters = [.. registers];
        if (wordOrder == RegisterWordOrder.LittleEndian && orderedRegisters.Length > 1)
        {
            Array.Reverse(orderedRegisters);
        }

        Span<byte> bytes = stackalloc byte[8];
        for (int index = 0; index < orderedRegisters.Length; index++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(index * 2, 2), orderedRegisters[index]);
        }

        return dataType switch
        {
            TagDataType.Bool => orderedRegisters[0] != 0,
            TagDataType.Int16 => BinaryPrimitives.ReadInt16BigEndian(bytes[..2]),
            TagDataType.UInt16 => BinaryPrimitives.ReadUInt16BigEndian(bytes[..2]),
            TagDataType.Int32 => BinaryPrimitives.ReadInt32BigEndian(bytes[..4]),
            TagDataType.UInt32 => BinaryPrimitives.ReadUInt32BigEndian(bytes[..4]),
            TagDataType.Int64 => BinaryPrimitives.ReadInt64BigEndian(bytes[..8]),
            TagDataType.UInt64 => BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]),
            TagDataType.Float32 => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(bytes[..4])),
            TagDataType.Float64 => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(bytes[..8])),
            _ => throw new InvalidOperationException($"Unsupported tag data type '{dataType}'.")
        };
    }
}
