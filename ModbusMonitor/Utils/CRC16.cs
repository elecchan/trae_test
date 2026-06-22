namespace ModbusMonitor.Utils
{
    public static class CRC16
    {
        private static readonly ushort[] CrcTable = new ushort[256];

        static CRC16()
        {
            ushort polynomial = 0xA001;
            for (int i = 0; i < 256; i++)
            {
                ushort crc = (ushort)i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ polynomial) : (ushort)(crc >> 1);
                }
                CrcTable[i] = crc;
            }
        }

        public static ushort Calculate(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc = (ushort)(CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8));
            }
            return crc;
        }

        public static ushort Calculate(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc = (ushort)(CrcTable[(crc ^ data[offset + i]) & 0xFF] ^ (crc >> 8));
            }
            return crc;
        }

        public static bool Verify(byte[] data)
        {
            if (data.Length < 2) return false;
            ushort calculatedCrc = Calculate(data, 0, data.Length - 2);
            ushort receivedCrc = (ushort)(data[data.Length - 1] << 8 | data[data.Length - 2]);
            return calculatedCrc == receivedCrc;
        }
    }
}