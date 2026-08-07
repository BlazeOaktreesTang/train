using System.Net;
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
using static System.Net.Mime.MediaTypeNames;

namespace WpfSocketSever1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        TcpListener listener;
        private CancellationTokenSource _cts; //新增取消令牌源
        private readonly List<TcpClient> _clientList = new List<TcpClient>();
        /// 向TextBlock追加日志，最多保留maxLine行，超过自动丢弃最旧行
        /// </summary>
        /// <param name="tb">目标TextBlock</param>
        /// <param name="newLineContent">新增一行日志</param>
        /// <param name="maxLine">最大保留行数</param>
        private void AppendLog(TextBlock tb, string newLineContent, int maxLine = 50)
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
         async System.Threading.Tasks.Task AddClient()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            Console.WriteLine("异步服务启动 8888");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 把AcceptTcpClientAsync包装成可以被取消的任务
                    var acceptTask = listener.AcceptTcpClientAsync();
                    var cancelTask = Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    }, token);

                    var completed = await Task.WhenAny(acceptTask, cancelTask);
                    if (completed == cancelTask)
                    {
                        //触发取消
                        break;
                    }

                    TcpClient client = await acceptTask;
                    _ = HandleClientAsync(client);
                }
            }
            catch (OperationCanceledException)
            {
                //正常取消，忽略
            }
            finally
            {
                listener?.Stop();
                Console.WriteLine("服务端已经停止");
            }
        }
        private async System.Threading.Tasks.Task HandleClientAsync(TcpClient client)
        {
            IPEndPoint remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
            string clientIp = remoteEp?.Address.ToString();
            int clientPort = remoteEp?.Port ?? 0;
            DisplayIP(clientIp+"   port:"+clientPort);
            lock (_clientList)
            {
                _clientList.Add(client);
            }
            using (client)
            using (var stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                try
                {
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        //string text = Encoding.UTF8.GetString(buffer, 0, read);
                        //Console.WriteLine($"接收:{text}");
                        string text = "Received:" + string.Join(',',buffer.Take(read).ToArray());
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog(tReceived, text, 13);
                        });
                        byte[] bSend = new byte[buffer[4]+read];
                        bSend[0] = buffer[0];
                        bSend[1] = buffer[1];
                        bSend[2] = (byte)(buffer[2] + buffer[4]);
                        bSend[3] = buffer[3];
                        bSend[4] = (byte)(buffer[4] + 1);
                        byte k = buffer[4];
                        for (int i = 5; i < 5 + buffer[4]; i++)
                        {
                            bSend[i] = k++;
                        }
                        bSend[5+ buffer[4]] = buffer[5];
                        bSend[6 + buffer[4]] = buffer[6];
                        string sSend = "Send:" + string.Join(',', bSend.ToArray());
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog(tReceived, sSend, 13);
                        });
                        await stream.WriteAsync(bSend, 0, bSend.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            lock (_clientList)
            {
                _clientList.Remove(client);
            }
            Console.WriteLine("客户端退出");
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                return;
            }
            _ = AddClient();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            lock (_clientList)
            {
                foreach (var c in _clientList.ToList())
                {
                    c.Close();
                }
                _clientList.Clear();
            }
            if (_cts != null)
            {
                _cts.Cancel(); //发出取消信号，跳出while循环
                _cts.Dispose();
                _cts = null;
            }
        }
        private void DisplayIP(string s)
        {
            Dispatcher.Invoke(() =>
            {
                lIP.Content = s;
            });
        }
    }
}