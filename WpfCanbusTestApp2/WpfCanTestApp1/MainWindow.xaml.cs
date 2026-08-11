using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WpfCanTestApp1.Can;

namespace WpfCanTestApp1
{
    public partial class MainWindow : Window
    {
        private ICanAdapter _canAdapter;
        private bool _isInitialized = false;

        // 核心 1：线程安全队列，用于缓冲硬件驱动线程收到的高频 CAN 报文
        private readonly ConcurrentQueue<CanMessage> _recvQueue = new ConcurrentQueue<CanMessage>();

        // 核心 2：UI 绑定的数据源 (配合 DataGrid / ListView 使用)
        public ObservableCollection<string> DisplayLogs { get; set; } = new ObservableCollection<string>();

        // 核心 3：定时器，批量消费队列，隔离硬件线程与 UI 绘制
        private DispatcherTimer _uiBatchTimer;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            SetupUiTimer();
        }

        private void SetupUiTimer()
        {
            // 设定 50ms (20fps) 的刷新频率，既流畅又不卡顿
            _uiBatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _uiBatchTimer.Tick += UiBatchTimer_Tick;
            _uiBatchTimer.Start();
        }

        private void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;

            // 实例化虚拟适配器 (后续切真实 PCAN 卡时只需 new PcanAdapter())
            _canAdapter = new VirtualCanAdapter();
            _canAdapter.MessageReceived += OnCanMessageReceived;

            if (_canAdapter.Initialize("Virtual01", 500000))
            {
                _isInitialized = true;
                AppendLog("CAN 总线初始化成功 [线程安全批处理与 DBC 解析模式]");
            }
        }

        /// <summary>
        /// 硬件/模拟驱动线程回调 (高频，千万不能在这里直接 Dispatcher.Invoke 刷新 UI)
        /// </summary>
        private void OnCanMessageReceived(CanMessage msg)
        {
            // 只做入队操作，消耗微秒级时间，绝不卡顿
            _recvQueue.Enqueue(msg);
        }

        /// <summary>
        /// UI 线程定时消费队列中的报文
        /// </summary>
        private void UiBatchTimer_Tick(object sender, EventArgs e)
        {
            if (_recvQueue.IsEmpty) return;

            // 批量一次性拉取最多 50 条报文更新 UI
            int processCount = 0;
            while (_recvQueue.TryDequeue(out CanMessage msg) && processCount < 50)
            {
                processCount++;

                string extTag = msg.IsExtended ? "[EXT]" : "[STD]";
                string hexData = BitConverter.ToString(msg.Data);

                // 提取并打印通过 DBC 解析出的物理量
                string decodedInfo = msg.Id == 0x123
                    ? $" | 提取物理车速: {msg.SpeedValue} km/h"
                    : "";

                string logText = $"[{msg.Timestamp:HH:mm:ss.fff}] {extTag} ID: 0x{msg.Id:X} Data: {hexData}{decodedInfo}";

                DisplayLogs.Add(logText);

                // 保持显示日志不超过 200 条，防止内存泄露
                if (DisplayLogs.Count > 200)
                {
                    DisplayLogs.RemoveAt(0);
                }
            }
        }

        private void BtSend_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            try
            {
                uint id = Convert.ToUInt32(tbID.Text.Trim(), 16);
                byte[] data = tbSend.Text.Trim()
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => Convert.ToByte(b, 16))
                    .ToArray();

                var msg = new CanMessage
                {
                    Id = id,
                    Data = data,
                    IsExtended = id > 0x7FF // ID 大于 0x7FF 自动识别为 29 位扩展帧
                };

                if (_canAdapter.Send(msg))
                {
                    AppendLog($"[发送] ID: 0x{id:X}, Data: {BitConverter.ToString(data)}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"发送错误: {ex.Message}");
            }
        }

        private void AppendLog(string text)
        {
            DisplayLogs.Add($"[{DateTime.Now:HH:mm:ss.fff}] {text}");
        }

        protected override void OnClosed(EventArgs e)
        {
            _uiBatchTimer?.Stop();
            _canAdapter?.Close();
            base.OnClosed(e);
        }
    }
}