using MySql;
using SimpleNetServer.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer
{
    class Program
    {
        static void Main(string[] args)
        {

            //连接数据库
            MySqlManager.Instance.Init();

            // 开启服务器
            ServerSocket.Instance.Init();

            Console.ReadKey();
        }
    }
}
