using System;

namespace WpfCanTestApp1.Can
{
    public static class DbcSignalDecoder
    {
        /// <summary>
        /// 提取并解析 CAN 报文中的信号 (Intel / Little-Endian 格式)
        /// </summary>
        public static double ExtractSignal(byte[] data, int startBit, int length, double factor, double offset)
        {
            if (data == null || data.Length * 8 < startBit + length) return 0;

            // 1. 将 8 字节数组拼成 64 位无符号整数 (UInt64)
            ulong rawBuffer = 0;
            for (int i = 0; i < data.Length; i++)
            {
                rawBuffer |= ((ulong)data[i]) << (i * 8);
            }

            // 2. 按 StartBit 和 BitLength 进行掩码提取
            ulong mask = (1UL << length) - 1;
            ulong rawValue = (rawBuffer >> startBit) & mask;

            // 3. 应用 DBC 转换公式：Physical = Raw * Factor + Offset
            return (rawValue * factor) + offset;
        }
    }
}
