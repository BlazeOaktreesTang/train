using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfSocketClient1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _clientCts;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void AppendLog(TextBlock tb, string newLineContent, int maxLine = 13)
        {
            // 分割现有全部行，按换行符拆分
            var lines = tb.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

            // 添加新日志行
            lines.Add(newLineContent + DateTime.Now.ToString("        HH:mm:ss.fff"));

            // 如果总行数超过maxLine，移除前面旧的行
            while (lines.Count > maxLine)
            {
                lines.RemoveAt(0); // 删除第一行（最旧）
            }

            // 重新拼接回文本
            tb.Text = string.Join("\r\n", lines);
        }
        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpClient != null && _tcpClient.Connected)
                return;

            _tcpClient = new TcpClient();
            try
            {
                await _tcpClient.ConnectAsync("127.0.0.1", 8888);
                _stream = _tcpClient.GetStream();
                _clientCts = new CancellationTokenSource();
                Console.WriteLine("客户端已连上服务端");
                //启动后台接收循环
                _ = ClientReceiveLoop(_clientCts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败 {ex.Message}");
            }
        }
        private async Task ClientReceiveLoop(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested && _tcpClient.Connected)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (read == 0) break;

                    // 这里你现在服务端是发送字节逗号格式，如果要文本就换成UTF8.GetString
                    string recvText = string.Join(',', buffer.Skip(3).Take(read-5).ToArray());
                    Dispatcher.Invoke(() =>
                    {
                        AppendLog(tReceived, recvText,13);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                //正常取消
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收异常:{ex.Message}");
            }
            //断开清理
            ClientCleanup();
        }
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            _clientCts?.Cancel();
            ClientCleanup();
        }
        private void ClientCleanup()
        {
            _stream?.Close();
            _tcpClient?.Close();
            _clientCts?.Dispose();
            _stream = null;
            _tcpClient = null;
            _clientCts = null;
            Console.WriteLine("客户端已断开");
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_stream == null || !_tcpClient.Connected) return;
            string s = (string)((Button)sender).Content;
            
            byte[] data = new byte[7];
            data[0] = 0x3c;
            data[1] = 0x01;
            data[2] = 0x05;
            data[3] = 0x02;
            data[4] = (byte)(2*Convert.ToByte(s.Substring(2))-1);
            data[5] = 0x2F;
            data[6] = 0x3E;
            await _stream.WriteAsync(data, 0, data.Length);
        }
    }
}