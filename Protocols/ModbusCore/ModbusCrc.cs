namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>CRC-16/Modbus (polynomial 0xA001, init 0xFFFF) used by RTU framing.
/// On the wire the low byte is transmitted first.</summary>
public static class ModbusCrc
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                var lsb = (crc & 0x0001) != 0;
                crc >>= 1;
                if (lsb)
                {
                    crc ^= 0xA001;
                }
            }
        }

        return crc;
    }
}
