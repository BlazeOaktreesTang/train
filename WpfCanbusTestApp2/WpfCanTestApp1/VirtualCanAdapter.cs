using System;
using System.Threading;
using System.Threading.Tasks;

namespace WpfCanTestApp1.Can
{
    public class VirtualCanAdapter1 : ICanAdapter
    {
        public event Action<CanMessage> MessageReceived;
        private bool _isRunning;
        private CancellationTokenSource _cts;

        public bool Initialize(string channelName, int baudRate)
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();

            // 模拟真实的 CAN 节点：每 100ms 自动广播一条车辆车速报文 (ID: 0x123)
            Task.Run(() => SimulatePeriodicCanNode(_cts.Token));

            return true;
        }

        public bool Send(CanMessage message)
        {
            if (!_isRunning) return false;

            // 模拟硬件 10ms 回调 (Echo / 节点响应)
            Task.Delay(10).ContinueWith(_ =>
            {
                MessageReceived?.Invoke(message);
            });

            return true;
        }

        private async Task SimulatePeriodicCanNode(CancellationToken token)
        {
            byte speed = 0;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(100, token); // 100ms 周期

                // 模拟数据变化
                //speed = (byte)((speed + 1) % 220);
                speed = (byte)((speed + 1));
                var simMsg = new CanMessage
                {
                    Id = 0x123,
                    Data = new byte[] { 0x0a, speed, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 },
                    IsExtended = false
                };

                MessageReceived?.Invoke(simMsg);
            }
        }

        public void Close()
        {
            _isRunning = false;
            _cts?.Cancel();
        }
    }
}