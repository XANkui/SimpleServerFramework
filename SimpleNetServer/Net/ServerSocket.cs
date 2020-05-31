using ServerBase;
using SimpleNetServer.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer.Net
{
    public class ServerSocket:Singleton<ServerSocket>
    {
        // 公钥
        public static string PublicKey = "TestPublicKey";

        // 密钥
        public static string SecretKey = "Test&&$%ScretKey";


#if DEBUG

        private string m_IpStr = "127.0.0.1";
#else
        // 阿里云或者腾讯云的 本地 ip 地址（不是公共ip地址）
        private string mIpStr = "172.48.12.1";
#endif
        //监听端口号
        private const int m_Port = 8021;

        //心跳间隔时间
        private static long m_PingInterval = 30;


        //服务器监听Socket
        private static Socket m_ListenSocket;

        // 客户端socket集合
        private static List<Socket> m_CheckReadList = new List<Socket>();

        // 客户端对应的操作字典
        private static Dictionary<Socket, ClientSocket> m_ClientDic = new Dictionary<Socket, ClientSocket>();

        private static List<ClientSocket> m_TempList = new List<ClientSocket>();


        public void Init()
        {
            IPAddress ip = IPAddress.Parse(m_IpStr);
            IPEndPoint ipEndPoint = new IPEndPoint(ip, m_Port);

            m_ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_ListenSocket.Bind(ipEndPoint);

            m_ListenSocket.Listen(50000);

            Debug.LogInfo("服务器启动监听{0}成功", m_ListenSocket.LocalEndPoint.ToString());

            while (true)
            {

                // 检查是否有读取的Socket

                // 处理找出所有Socket
                ResetCheckRead();

                try
                {
                    // 最后处理的单位是1000微秒
                    Socket.Select(m_CheckReadList, null, null, 1000);
                }
                catch (Exception e)
                {

                    Debug.LogError(e);
                }

                for (int i = m_CheckReadList.Count - 1; i >= 0; i--)
                {
                    Socket s = m_CheckReadList[i];

                    if (s == m_ListenSocket)
                    {
                        // 说明有客户端链接到服务器了，所以服务器Socket可读
                        ReadListen(s);
                    }
                    else
                    {
                        // 说明链接的客户端可读，证明有信息上传过来

                        ReadClient(s);
                    }

                }


                //检查是否心跳包超时的计算 (心跳超时 2 分钟，关闭客户端)
                long timeNow = GetTimeStamp();
                m_TempList.Clear();
                foreach (ClientSocket clientSocket in m_ClientDic.Values)
                {
                    if (timeNow - clientSocket.LastPingTime > m_PingInterval * 4)
                    {
                        Debug.Log("心跳超时，Ping Close " + clientSocket.Socket.RemoteEndPoint.ToString());
                        m_TempList.Add(clientSocket);
                    }
                }

                foreach (ClientSocket clientSocket in m_TempList)
                {
                    CloseClient(clientSocket);
                }
                m_TempList.Clear();
            }

        }



        private void ResetCheckRead()
        {
            m_CheckReadList.Clear();

            m_CheckReadList.Add(m_ListenSocket);
            foreach (Socket socket in m_ClientDic.Keys)
            {
                m_CheckReadList.Add(socket);
            }
        }

        /// <summary>
        /// 监听链接的客户端
        /// </summary>
        /// <param name="m_ListenSocket"></param>
        private void ReadListen(Socket listenSocket)
        {
            try
            {
                Socket client = listenSocket.Accept();
                ClientSocket clientSocket = new ClientSocket()
                {
                    Socket = client,
                    LastPingTime = GetTimeStamp()
                };

                m_ClientDic.Add(client, clientSocket);

                Debug.Log("一个客户端连接:{0},当前{1}个客户端在线", client.LocalEndPoint.ToString(), m_ClientDic.Count);
            }
            catch (SocketException e)
            {

                Debug.LogError("Accept Fail: " + e.ToString());
            }
        }


        private void ReadClient(Socket client)
        {


            ClientSocket clientSocket = m_ClientDic[client];
            ByteArray readBuff = clientSocket.ReadBuff;
            // 接收信息，根据信息解析协议，根据协议内容处理消息  再下发到客户端
            int count = 0;

            //如果上一次接收数据刚好沾满默认 1024 大小的数组，则需要扩充或者移动数组
            if (readBuff.Remain <= 0)
            {
                OnReceiveData(clientSocket);
                readBuff.CheckAndMoveBytes();
                //还是不够大小，则扩充数据缓冲数组
                while (readBuff.Remain <= 0)
                {
                    int expandSize = readBuff.Length < ByteArray.DEFAULT_SIZE ? ByteArray.DEFAULT_SIZE : readBuff.Length;
                    readBuff.Resize(expandSize * 2);
                }
            }

            try
            {
                count = client.Receive(readBuff.Bytes, readBuff.WriteIndex, readBuff.Remain, 0);
            }
            catch (Exception ex)
            {

                Debug.LogError("Receive Fail: " + ex);
                CloseClient(clientSocket);
                return;
            }

            //代表客户端断开连接了
            if (count <= 0)
            {
                CloseClient(clientSocket);
                return;
            }

            readBuff.WriteIndex += count;
            OnReceiveData(clientSocket);


            // 解析数据信息
            readBuff.CheckAndMoveBytes();
        }

        /// <summary>
        /// 接收信息，并解析
        /// </summary>
        /// <param name="cleint"></param>
        void OnReceiveData(ClientSocket clintSocket)
        {

            ByteArray readBuff = clintSocket.ReadBuff;

            // 基本消息长度判断（一个协议头为 4）
            if (readBuff.Length <= 4 || readBuff.ReadIndex < 0)
            {
                Debug.Log("数据小于 4");
                return;
            }

            int readIndex = readBuff.ReadIndex;
            byte[] bytes = readBuff.Bytes;

            //解析协议头，获得中的长度
            int bodyLength = BitConverter.ToInt32(bytes, readIndex);

            // （分包处理）判断接收消息长度是否小于包体长度+包头长度，如果小于，代表接收到的消息不全(返回，不做处理，等下一次全了在处理)，大于则代表消息全了（也有可能有粘包存在）
            if (readBuff.Length < bodyLength + 4)
            {
                Debug.Log("数据不完整");
                return;
            }

            // 移动到消息体位置
            readBuff.ReadIndex += 4;

            // 解析协议名称
            int nameCount = 0;
            ProtocolEnum protocol = ProtocolEnum.None;

            try
            {

                protocol = MessageBase.DecodeName(readBuff.Bytes, readBuff.ReadIndex, out nameCount);

            }
            catch (Exception ex)
            {

                Debug.LogError("解析协议名称 Error :" + ex);
                CloseClient(clintSocket);
                return;
            }

            // 协议名称为空
            if (protocol == ProtocolEnum.None)
            {
                Debug.LogError("OnReceiveData MessageBase.DecodeName() Fail :");
                CloseClient(clintSocket);
                return;
            }

            // 解析协议内容
            readBuff.ReadIndex += nameCount;

            int bodyCount = bodyLength - nameCount;

            MessageBase messageBase = null;
            try
            {
                messageBase = MessageBase.Decode(protocol, readBuff.Bytes, readBuff.ReadIndex, bodyCount);
                if (messageBase == null)
                {
                    Debug.LogError("{0} 解析协议内容 Error ", protocol.ToString());
                    CloseClient(clintSocket);
                    return;

                }
            }
            catch (Exception ex)
            {

                Debug.LogError("解析协议内容 Error :" + ex);
                CloseClient(clintSocket);
                return;
            }

            // 移动读取位置，并且继续判断移动数据
            readBuff.ReadIndex += bodyCount;
            readBuff.CheckAndMoveBytes();

            //通过反射，分发消息（解析完数据，对应的处理）
            MethodInfo methodInfo = typeof(MessageHandler).GetMethod(protocol.ToString());
            object[] o = { clintSocket, messageBase };
            if (methodInfo != null)
            {

                methodInfo.Invoke(null, o);
            }
            else
            {
                Debug.LogError("OnReceiveData Invok Fail:" + protocol.ToString());
            }


            // 继续读取消息（粘包现象处理）
            if (readBuff.Length > 4)
            {
                OnReceiveData(clintSocket);
            }

        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="messageBase"></param>
        public static void Send(ClientSocket clientSocket, MessageBase messageBase)
        {
            if (clientSocket == null || clientSocket.Socket.Connected == false)
            {

                return;
            }

            try
            {
                // 分为三部分，协议头(总协议长度);协议名称;协议类容
                byte[] nameBytes = MessageBase.EncodeName(messageBase);
                byte[] bodyBytes = MessageBase.Encode(messageBase);
                int len = nameBytes.Length + bodyBytes.Length;

                byte[] byteHead = BitConverter.GetBytes(len);
                byte[] sendBytes = new byte[byteHead.Length + len];

                Array.Copy(byteHead, 0, sendBytes, 0, byteHead.Length);
                Array.Copy(nameBytes, 0, sendBytes, byteHead.Length, nameBytes.Length);
                Array.Copy(bodyBytes, 0, sendBytes, byteHead.Length + nameBytes.Length, bodyBytes.Length);

                try
                {
                    clientSocket.Socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
                }
                catch (Exception ex)
                {

                    Debug.LogError("Socket Begin Send Error :" + ex);
                }

            }
            catch (Exception ex)
            {

                Debug.LogError("Socket Send Mesaage Fail :" + ex);
            }
        }

        /// <summary>
        /// 关闭客户端
        /// </summary>
        /// <param name="client"></param>
        public void CloseClient(ClientSocket client)
        {
            client.Socket.Close();
            m_ClientDic.Remove(client.Socket);

            Debug.Log("一个客户端断开连接，当前总连接数为：{0} ", m_ClientDic.Count);
        }

        /// <summary>
        /// 获得当前的时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {

            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return Convert.ToInt64(ts.TotalSeconds);
        }





        ////服务器监听socket
        //private static Socket m_ListenSocket;

        ////临时保存所有socket的集合
        //private static List<Socket> m_CheckReadList = new List<Socket>();

        ////所有客户端的一个字典
        //public static Dictionary<Socket, ClientSocket> m_ClientDic = new Dictionary<Socket, ClientSocket>();

        //public static List<ClientSocket> m_TempList = new List<ClientSocket>();

        //public void Init()
        //{
        //    IPAddress ip = IPAddress.Parse(m_IpStr);
        //    IPEndPoint ipEndPoint = new IPEndPoint(ip, m_Port);
        //    m_ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    m_ListenSocket.Bind(ipEndPoint);
        //    m_ListenSocket.Listen(50000);

        //    Debug.LogInfo("服务器启动监听{0}成功", m_ListenSocket.LocalEndPoint.ToString());

        //    while (true)
        //    {
        //        //检查是否有读取的socket

        //        //处理找出所有socket
        //        ResetCheckRead();

        //        try
        //        {
        //            //最后等待时间单位是微妙
        //            Socket.Select(m_CheckReadList, null, null, 1000);
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.LogError(e);
        //        }

        //        for (int i = m_CheckReadList.Count - 1; i >= 0; i--)
        //        {
        //            Socket s = m_CheckReadList[i];
        //            if (s == m_ListenSocket)
        //            {
        //                //说明有客户端链接到服务器了，所以服务器socket可读
        //                ReadListen(s);
        //            }
        //            else
        //            {
        //                //说明链接的客户端可读，证明有信息传上来了
        //                ReadClient(s);
        //            }
        //        }

        //        //检测是否心跳包超时的计算


        //        long timeNow = GetTimeStamp();
        //        m_TempList.Clear();
        //        foreach (ClientSocket clientSocket in m_ClientDic.Values)
        //        {
        //            if (timeNow - clientSocket.LastPingTime > m_PingInterval * 4)
        //            {
        //                Debug.Log("Ping Close" + clientSocket.Socket.RemoteEndPoint.ToString());
        //                m_TempList.Add(clientSocket);
        //            }
        //        }

        //        foreach (ClientSocket clientSocket in m_TempList)
        //        {
        //            CloseClient(clientSocket);
        //        }
        //        m_TempList.Clear();
        //    }
        //}

        //public void ResetCheckRead()
        //{
        //    m_CheckReadList.Clear();
        //    m_CheckReadList.Add(m_ListenSocket);
        //    foreach (Socket s in m_ClientDic.Keys)
        //    {
        //        m_CheckReadList.Add(s);
        //    }
        //}

        //void ReadListen(Socket listen)
        //{
        //    try
        //    {
        //        Socket client = listen.Accept();
        //        ClientSocket clientSocket = new ClientSocket();
        //        clientSocket.Socket = client;
        //        clientSocket.LastPingTime = GetTimeStamp();
        //        m_ClientDic.Add(client, clientSocket);
        //        Debug.Log("一个客户端链接：{0},当前{1}个客户端在线！", client.LocalEndPoint.ToString(), m_ClientDic.Count);
        //    }
        //    catch (SocketException ex)
        //    {
        //        Debug.LogError("Accept fali:" + ex.ToString());
        //    }
        //}

        //void ReadClient(Socket client)
        //{
        //    ClientSocket clientSocket = m_ClientDic[client];
        //    ByteArray readBuff = clientSocket.ReadBuff;
        //    //接受信息，根据信息解析协议，根据协议内容处理消息再下发到客户端
        //    int count = 0;
        //    //如果上一次接收数据刚好占满了1024的数组，
        //    if (readBuff.Remain <= 0)
        //    {
        //        //数据移动到index =0 位置。
        //        OnReceiveData(clientSocket);
        //        readBuff.CheckAndMoveBytes();
        //        //保证到如果数据长度大于默认长度，扩充数据长度，保证信息的正常接收
        //        while (readBuff.Remain <= 0)
        //        {
        //            int expandSize = readBuff.Length < ByteArray.DEFAULT_SIZE ? ByteArray.DEFAULT_SIZE : readBuff.Length;
        //            readBuff.Resize(expandSize * 2);
        //        }
        //    }
        //    try
        //    {
        //        count = client.Receive(readBuff.Bytes, readBuff.WriteIndex, readBuff.Remain, 0);
        //    }
        //    catch (SocketException ex)
        //    {
        //        Debug.LogError("Receive fali:" + ex);
        //        CloseClient(clientSocket);
        //        return;
        //    }

        //    //代表客户端断开链接了
        //    if (count <= 0)
        //    {
        //        CloseClient(clientSocket);
        //        return;
        //    }

        //    readBuff.WriteIndex += count;
        //    //解析我们的信息
        //    OnReceiveData(clientSocket);
        //    readBuff.CheckAndMoveBytes();
        //}

        ///// <summary>
        ///// 接收数据处理
        ///// </summary>
        ///// <param name="clientSocket"></param>
        //void OnReceiveData(ClientSocket clientSocket)
        //{
        //    ByteArray readbuff = clientSocket.ReadBuff;
        //    //基本消息长度判断
        //    if (readbuff.Length <= 4 || readbuff.ReadIndex < 0)
        //    {
        //        return;
        //    }
        //    int readIdx = readbuff.ReadIndex;
        //    byte[] bytes = readbuff.Bytes;
        //    int bodyLength = BitConverter.ToInt32(bytes, readIdx);
        //    //判断接收到的信息长度是否小于包体长度+包体头长度，如果小于，代表我们的信息不全，大于代表信息全了（有可能有粘包存在）
        //    if (readbuff.Length < bodyLength + 4)
        //    {
        //        return;
        //    }
        //    readbuff.ReadIndex += 4;
        //    //解析协议名
        //    int nameCount = 0;
        //    ProtocolEnum proto = ProtocolEnum.None;
        //    try
        //    {
        //        proto = MessageBase.DecodeName(readbuff.Bytes, readbuff.ReadIndex, out nameCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogError("解析协议名出错：" + ex);
        //        CloseClient(clientSocket);
        //        return;
        //    }

        //    if (proto == ProtocolEnum.None)
        //    {
        //        Debug.LogError("OnReceiveData MsgBase.DecodeName  fail");
        //        CloseClient(clientSocket);
        //        return;
        //    }

        //    readbuff.ReadIndex += nameCount;

        //    //解析协议体
        //    int bodyCount = bodyLength - nameCount;
        //    MessageBase msgBase = null;
        //    try
        //    {
        //        msgBase = MessageBase.Decode(proto, readbuff.Bytes, readbuff.ReadIndex, bodyCount);
        //        if (msgBase == null)
        //        {
        //            Debug.LogError("{0}协议内容解析错误：" + proto.ToString());
        //            CloseClient(clientSocket);
        //            return;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogError("接收数据协议内容解析错误：" + ex);
        //        CloseClient(clientSocket);
        //        return;
        //    }

        //    readbuff.ReadIndex += bodyCount;
        //    readbuff.CheckAndMoveBytes();
        //    //通过反射分发消息
        //    MethodInfo mi = typeof(MessageHandler).GetMethod(proto.ToString());
        //    object[] o = { clientSocket, msgBase };

        //    if (mi != null)
        //    {
        //        mi.Invoke(null, o);
        //    }
        //    else
        //    {
        //        Debug.LogError("OnReceiveData Invoke fail:" + proto.ToString());
        //    }

        //    //继续读取消息
        //    if (readbuff.Length > 4)
        //    {
        //        OnReceiveData(clientSocket);
        //    }
        //}

        ///// <summary>
        ///// 发送数据
        ///// </summary>
        ///// <param name="cs"></param>
        ///// <param name="msgBase"></param>
        //public static void Send(ClientSocket cs, MessageBase msgBase)
        //{
        //    if (cs == null || !cs.Socket.Connected)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        //分为三部分，头：总协议长度；名字；协议内容。
        //        byte[] nameBytes = MessageBase.EncodeName(msgBase);
        //        byte[] bodyBytes = MessageBase.Encode(msgBase);
        //        int len = nameBytes.Length + bodyBytes.Length;
        //        byte[] byteHead = BitConverter.GetBytes(len);
        //        byte[] sendBytes = new byte[byteHead.Length + len];
        //        Array.Copy(byteHead, 0, sendBytes, 0, byteHead.Length);
        //        Array.Copy(nameBytes, 0, sendBytes, byteHead.Length, nameBytes.Length);
        //        Array.Copy(bodyBytes, 0, sendBytes, byteHead.Length + nameBytes.Length, bodyBytes.Length);
        //        try
        //        {
        //            cs.Socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
        //        }
        //        catch (SocketException ex)
        //        {
        //            Debug.LogError("Socket BeginSend Error：" + ex);
        //        }
        //    }
        //    catch (SocketException ex)
        //    {
        //        Debug.LogError("Socket发送数据失败：" + ex);
        //    }
        //}

        //public void CloseClient(ClientSocket client)
        //{
        //    client.Socket.Close();
        //    m_ClientDic.Remove(client.Socket);
        //    Debug.Log("一个客户端断开链接，当前总连接数：{0}", m_ClientDic.Count);
        //}


        //public static long GetTimeStamp()
        //{
        //    TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //    return Convert.ToInt64(ts.TotalSeconds);
        //}


    }
}

