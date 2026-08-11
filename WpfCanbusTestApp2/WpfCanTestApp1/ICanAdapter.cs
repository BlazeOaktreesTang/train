using System;
using System.Threading;
using System.Threading.Tasks;

namespace WpfCanTestApp1.Can
{
    public class CanMessage
    {
        public uint Id { get; set; }
        public byte[] Data { get; set; }
        public byte Dlc => (byte)(Data?.Length ?? 0);
        public bool IsExtended { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // 解读物理数值（以车速信号为例：StartBit=8, Length=8, Factor=1.0, Offset=0）
        public double SpeedValue => (Id == 0x123 && Data?.Length >= 2)
            ? DbcSignalDecoder.ExtractSignal(Data, 8, 8, 1.0, 0)
            : 0;
    }

    public interface ICanAdapter
    {
        event Action<CanMessage> MessageReceived;
        bool Initialize(string channelName, int baudRate);
        bool Send(CanMessage message);
        void Close();
    }

    public class VirtualCanAdapter : ICanAdapter
    {
        public event Action<CanMessage> MessageReceived;
        private bool _isRunning;
        private CancellationTokenSource _cts;

        public bool Initialize(string channelName, int baudRate)
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();

            // 启动高频 CAN 节点数据广播模拟 (每 10ms 广播一次)
            Task.Run(() => SimulateHighFrequencyCanBus(_cts.Token));
            return true;
        }

        public bool Send(CanMessage message)
        {
            if (!_isRunning) return false;

            // 模拟发送硬件回传 (Echo / Response)
            Task.Delay(5).ContinueWith(_ => MessageReceived?.Invoke(message));
            return true;
        }

        private async Task SimulateHighFrequencyCanBus(CancellationToken token)
        {
            byte speed = 0;
            byte engineTemp = 75;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10, token); // 10ms 周期，模拟 100 帧/秒 高频输入

                speed = (byte)((speed + 1) % 220);

                // 1. 模拟标准 CAN 报文 (车速/发动机数据)
                var msg1 = new CanMessage
                {
                    Id = 0x123,
                    Data = new byte[] { 0x01, speed, engineTemp, 0x00, 0x00, 0x00, 0x00, 0x00 },
                    IsExtended = false
                };
                MessageReceived?.Invoke(msg1);

                // 2. 模拟 J1939 / CANopen 扩展帧 (ID 0x18DAF100)
                var msg2 = new CanMessage
                {
                    Id = 0x18DAF100,
                    Data = new byte[] { 0x02, 0x09, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 },
                    IsExtended = true
                };
                MessageReceived?.Invoke(msg2);
            }
        }

        public void Close()
        {
            _isRunning = false;
            _cts?.Cancel();
        }
    }
}