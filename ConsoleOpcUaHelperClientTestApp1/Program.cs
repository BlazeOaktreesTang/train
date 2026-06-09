using System;
using System.Threading.Tasks;
using OpcUaHelper;

namespace ConsoleOpcUaHelperClientTestApp1
{
    class Program
    {
        private const string ServerUrl = "opc.tcp://127.0.0.1:4840";

        private const string NodeId_DI1 = "ns=2;s=DI1";
        private const string NodeId_Temp = "ns=2;s=AI1";
        private const string NodeId_DO1 = "ns=2;s=DO1";

        static async Task Main(string[] args)
        {
            var client = new OpcUaClient();

            try
            {
                Console.WriteLine("正在连接...");
                await client.ConnectServer(ServerUrl);
                Console.WriteLine("连接成功！\n");

                Console.WriteLine("----- 读取变量 -----");
                await ReadNodeAsync<bool>(client, NodeId_DI1);
                await ReadNodeAsync<double>(client, NodeId_Temp);
                await ReadNodeAsync<bool>(client, NodeId_DO1);

                Console.WriteLine("\n----- 写入变量 -----");
                await WriteNodeAsync(client, NodeId_DO1, true);
                Console.WriteLine("写入完成，重新读取验证：");
                await ReadNodeAsync<bool>(client, NodeId_DO1);

                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：{ex.Message}");
            }
            finally
            {
                if (client.Connected) // 修正1：用 Connected 代替 IsConnected
                    client.Disconnect(); // 修正2：Disconnect 是同步方法，不用 await
            }
        }

        // 修正3：ReadNodeAsync 是泛型方法，必须指定类型
        static async Task ReadNodeAsync<T>(OpcUaClient client, string nodeId)
        {
            T value = await client.ReadNodeAsync<T>(nodeId);
            Console.WriteLine($"{nodeId} = {value}");
        }

        static async Task WriteNodeAsync(OpcUaClient client, string nodeId, object value)
        {
            await client.WriteNodeAsync(nodeId, value);
        }
    }
}