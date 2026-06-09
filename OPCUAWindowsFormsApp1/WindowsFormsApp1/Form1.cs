using Opc.Ua;
using Opc.Ua.Client;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        OPCUAHelper oPCUA;
        private void button1_Click(object sender, EventArgs e)
        {
            string serverUrl = "opc.tcp://192.168.100.100:4840";
            oPCUA = new OPCUAHelper();
            oPCUA.OpenConnectOfAnonymous(serverUrl);
            loop1();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            oPCUA.CloseConnect();
        }
        bool a1 = false;
        int a2 = 0;
        int a3 = 0;
        float a4 = 0;
        string a5 = "";
        string a6 = "";
        int a7 = 0;
        int a8 = 0;
        int a9 = 0;
        int a10 = 0;
        private string[] MonitorNodeTags = null;
        private void button3_Click(object sender, EventArgs e)
        {
            //DataValue a21 = oPCUA.GetCurrentNodeValue ("ns=3;s=\"数据块_1\".\"Static_1\"");
            //a1 = oPCUA.GetCurrentNodeValue<bool>("ns=3;s=\"数据块_1\".\"Static_1\"");
            //a2 = oPCUA.GetCurrentNodeValue<short>("ns=3;s=\"数据块_1\".\"Static_2\"");
            //a3 = oPCUA.GetCurrentNodeValue<int>("ns=3;s=\"数据块_1\".\"Static_3\"");
            //a4 = oPCUA.GetCurrentNodeValue<float>("ns=3;s=\"数据块_1\".\"Static_4\"");
            //a5 = oPCUA.GetCurrentNodeValue<string>("ns=3;s=\"数据块_1\".\"Static_5\"");//string
            //a6 = oPCUA.GetCurrentNodeValue<string>("ns=3;s=\"数据块_1\".\"Static_6\"");//string
            //a7 = oPCUA.GetCurrentNodeValue<int>("ns=3;s=\"OPC通讯数据块\".\"A1\"[3]");
            //a8 = oPCUA.GetCurrentNodeValue<short>("ns=3;s=\"OPC通讯数据块\".\"c2\".\"A2\"");
            //a9 = oPCUA.GetCurrentNodeValue<int>("ns=3;s=\"OPC通讯数据块\".\"c2\".\"A4\"[1]");
            //a10 = oPCUA.GetCurrentNodeValue<int>("ns=3;s=\"数据块_1\".\"Static_7\"[1000]");//string

            //// 添加所有的读取的节点，此处的示例是类型不一致的情况
            //List<NodeId> nodeIds = new List<NodeId>();
            //nodeIds.Add(new NodeId("ns=3;s=\"数据块_1\".\"Static_1\""));
            //nodeIds.Add(new NodeId("ns=3;s=\"数据块_1\".\"Static_2\""));
            //nodeIds.Add(new NodeId("ns=3;s=\"数据块_1\".\"Static_3\""));
            //nodeIds.Add(new NodeId("ns=3;s=\"数据块_1\".\"Static_4\""));
            //nodeIds.Add(new NodeId("ns=3;s=\"数据块_1\".\"Static_5\""));
            //nodeIds.Add(new NodeId("ns=3;s=\"数据块_1\".\"Static_6\""));

            //// 批量读
            //Dictionary<string, DataValue> dataValues = oPCUA.GetBatchNodeDatasOfSync(nodeIds);


            //单节点订阅  A是字典的Key，可以随便定义， 只用执行一次，值发生改变会调用 回调函数
            oPCUA.SingleNodeIdDatasSubscription("A", "ns=3;s=\"数据块_1\".\"Static_1\"", SubCallback);
            //1-取消单节点数据订阅  取消订阅 把Key值传入参数
            //oPCUA.CancelSingleNodeIdDatasSubscription("A");


            // 多个节点的订阅
            MonitorNodeTags = new string[]
            {
                "ns=3;s=\"数据块_1\".\"Static_2\"",
                "ns=3;s=\"数据块_1\".\"Static_3\"",
                "ns=3;s=\"数据块_1\".\"Static_4\"",
                "ns=3;s=\"数据块_1\".\"Static_5\"",
                "ns=3;s=\"数据块_1\".\"Static_7\"[1000]",
            };
            oPCUA.BatchNodeIdDatasSubscription("B", MonitorNodeTags, SubCallback);

        }
        //1-单节点数据订阅的回调函数
        private void SubCallback(string key, MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, MonitoredItem, MonitoredItemNotificationEventArgs>(SubCallback), key, monitoredItem, args);
                return;
            }

            if (key == "A")
            {
                // 如果有多个的订阅值都关联了当前的方法，可以通过key和monitoredItem来区分
                MonitoredItemNotification notification = args.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    textBox1.Text = notification.Value.WrappedValue.Value.ToString();
                }
            }
            else if (key == "B")
            {
                // 需要区分出来每个不同的节点信息
                MonitoredItemNotification notification = args.NotificationValue as MonitoredItemNotification;
                if (monitoredItem.StartNodeId.ToString() == MonitorNodeTags[0])
                {
                    textBox2.Text = notification.Value.WrappedValue.Value.ToString();
                }
                else if (monitoredItem.StartNodeId.ToString() == MonitorNodeTags[1])
                {
                    textBox3.Text = notification.Value.WrappedValue.Value.ToString();
                }
                else if (monitoredItem.StartNodeId.ToString() == MonitorNodeTags[2])
                {
                    textBox4.Text = notification.Value.WrappedValue.Value.ToString();
                }
                else if (monitoredItem.StartNodeId.ToString() == MonitorNodeTags[3])
                {
                    textBox5.Text = notification.Value.WrappedValue.Value.ToString();
                }
                else if (monitoredItem.StartNodeId.ToString() == MonitorNodeTags[4])
                {
                    textBox6.Text = notification.Value.WrappedValue.Value.ToString();
                }
                else if (monitoredItem.StartNodeId.ToString() == MonitorNodeTags[5])
                {
                    textBox7.Text = notification.Value.WrappedValue.Value.ToString();
                }
            }
        }




        private void loop1()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    bool isconn = oPCUA.ConnectStatus;
                    this.Invoke(new Action(() =>
                    {
                        if (isconn)
                        {
                            label1.BackColor = Color.Green;
                        }
                        else
                        {
                            label1.BackColor = Color.Red;
                        }
                        //textBox1.Text = a1.ToString();
                        //textBox2.Text = a2.ToString();
                        //textBox3.Text = a3.ToString();
                        //textBox4.Text = a4.ToString();
                        //textBox5.Text = a5.ToString();
                        //textBox6.Text = a6.ToString();
                        //textBox7.Text = a7.ToString();
                        //textBox8.Text = a8.ToString();
                        //textBox9.Text = a9.ToString();
                    }));
                    Thread.Sleep(1000);
                }
            });
        }
    }
}
