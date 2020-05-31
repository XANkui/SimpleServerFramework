using ServerBase;
using SimpleNetServer.Business;
using SimpleNetServer.Proto;


namespace SimpleNetServer.Net
{
    public partial class MessageHandler
    {
        /// <summary>
        /// 所有的协议处理函数对后市这个标准，函数名= 协议名枚举名 = 类名
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="messageBase"></param>
        public static void MessageSecret(ClientSocket clientSocket, MessageBase messageBase) {
            MessageSecret messageSecret = (MessageSecret)messageBase;
            messageSecret.Secret = ServerSocket.SecretKey;
            ServerSocket.Send(clientSocket, messageSecret);
        }

        /// <summary>
        /// 心跳包
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="messageBase"></param>
        public static void MessagePing(ClientSocket clientSocket, MessageBase messageBase)
        {
            Debug.Log("收到客户端的心跳包");

            // 刷新心跳时间
            clientSocket.LastPingTime = ServerSocket.GetTimeStamp();
            MessagePing messagePing = (MessagePing)messageBase;            
            ServerSocket.Send(clientSocket, messagePing);
        }

        /// <summary>
        /// 测试粘包分包
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="messageBase"></param>
        public static void MessageTest(ClientSocket clientSocket, MessageBase messageBase)
        {
            MessageTest messageTest = (MessageTest)messageBase;
            Debug.Log("收到客户端的分包测试数据："+ messageTest.RequestContent);

            messageTest.ResponseContent = "服务器收到客户端的粘包测试数据：";

            ServerSocket.Send(clientSocket, messageTest);
            ServerSocket.Send(clientSocket, messageTest);
            ServerSocket.Send(clientSocket, messageTest);
            ServerSocket.Send(clientSocket, messageTest);
            ServerSocket.Send(clientSocket, messageTest);
            ServerSocket.Send(clientSocket, messageTest);
        }

        /// <summary>
        /// 处理注册信息
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="messageBase"></param>
        public static void MessageRegister(ClientSocket clientSocket, MessageBase messageBase) {
            MessageRegister messageRegister = (MessageRegister)messageBase;
            var rst = UserManager.Instance.Register(messageRegister.RegisterType,messageRegister.Username,messageRegister.Password,out string token);

            messageRegister.Result = rst;

            ServerSocket.Send(clientSocket, messageRegister);
        }

        /// <summary>
        /// 处理登录信息
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="messageBase"></param>
        public static void MessageLogin(ClientSocket clientSocket, MessageBase messageBase) {

            MessageLogin messageLogin = (MessageLogin)messageBase;
            var rst = UserManager.Instance.Login(messageLogin.LoginType,messageLogin.Username,messageLogin.Password,out int userid,out string token);

            messageLogin.Result = rst;
            messageLogin.Token = token;
            clientSocket.UserID = userid;
            ServerSocket.Send(clientSocket,messageLogin);
        }
    }
}
