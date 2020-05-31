using ProtoBuf;
using ServerBase;
using SimpleNetServer.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer.Proto
{
    public class MessageBase
    {
        public virtual ProtocolEnum ProtoType { get; set; }

        /// <summary>
        /// 协议名的编码
        /// </summary>
        /// <param name="messageBase"></param>
        /// <returns></returns>
        public static byte[] EncodeName(MessageBase messageBase)
        {
            // 编码协议名称
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(messageBase.ProtoType.ToString());
            Int16 len = (Int16)nameBytes.Length;

            byte[] bytes = new byte[2 + len];
            bytes[0] = (byte)(len % 256);
            bytes[1] = (byte)(len / 256);
            Array.Copy(nameBytes, 0, bytes, 2, len);

            return bytes;

        }

        /// <summary>
        /// 协议名的解码
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static ProtocolEnum DecodeName(byte[] bytes, int offset, out int count)
        {
            count = 0;

            // 偏移量加 2 大于 事件数组长度
            if (offset + 2 > bytes.Length)
            {
                return ProtocolEnum.None;
            }



            Int16 len = (Int16)((bytes[offset + 1] << 8) | bytes[offset]);

            // 偏移量加 2 + len 大于 事件数组长度
            if (offset + 2 + len > bytes.Length)
            {
                return ProtocolEnum.None;
            }

            count = len + 2;

            try
            {

                string name = System.Text.Encoding.UTF8.GetString(bytes, offset + 2, len);
                return (ProtocolEnum)System.Enum.Parse(typeof(ProtocolEnum), name);

            }
            catch (Exception ex)
            {

                Debug.LogError("不存在该协议 ：" + ex);
                return ProtocolEnum.None;
            }
        }

        /// <summary>
        /// 协议序列化，以及进行数据加密
        /// </summary>
        /// <param name="messageBase"></param>
        /// <returns></returns>
        public static byte[] Encode(MessageBase messageBase)
        {
            using (var memory = new MemoryStream())
            {
                // 将我们的协议类进行序列化转换为字节数组
                Serializer.Serialize(memory, messageBase);
                byte[] bytes = memory.ToArray();
                string secret = ServerSocket.SecretKey;

                // 若果是请求进行加密，公钥加密
                if (messageBase is MessageSecret)
                {
                    secret = ServerSocket.PublicKey;
                }

                // 加密数据
                bytes = AES.AESEncrypt(bytes, secret);
                return bytes;
            }
        }

        /// <summary>
        /// 协议解密，以及反序列化
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static MessageBase Decode(ProtocolEnum protocol, byte[] bytes, int offset, int count)
        {

            if (count <= 0)
            {
                Debug.LogError("协议解密出错，数据长度为0");
                return null;
            }
            try
            {

                byte[] newBytes = new byte[count];
                Array.Copy(bytes, offset, newBytes, 0, count);
                string secret = ServerSocket.SecretKey;
                // 请求加密使用的是公钥加密
                if (protocol == ProtocolEnum.MessageSecret)
                {

                    secret = ServerSocket.PublicKey;
                }

                // 解密
                newBytes = AES.AESDecrypt(newBytes, secret);

                using (var memory = new MemoryStream(newBytes, 0, newBytes.Length))
                {
                    // 这里要求对应的协议类型类，要与协议枚举的名字一一对应（这里的类最好不要有命名空间包裹，若有，可能识别不到）
                    Type t = System.Type.GetType(protocol.ToString());
                    return (MessageBase)Serializer.NonGeneric.Deserialize(t, memory);
                }

            }
            catch (Exception ex)
            {

                Debug.LogError("协议解密出错 ：" + ex);
                return null;
            }



        }











        //public virtual ProtocolEnum ProtoType { get; set; }

        ///// <summary>
        ///// 编码协议名
        ///// </summary>
        ///// <param name="msgBase"></param>
        ///// <returns></returns>
        //public static byte[] EncodeName(MessageBase msgBase)
        //{
        //    byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(msgBase.ProtoType.ToString());
        //    Int16 len = (Int16)nameBytes.Length;
        //    byte[] bytes = new byte[2 + len];
        //    bytes[0] = (byte)(len % 256);
        //    bytes[1] = (byte)(len / 256);
        //    Array.Copy(nameBytes, 0, bytes, 2, len);
        //    return bytes;
        //}

        ///// <summary>
        ///// 解码协议名
        ///// </summary>
        ///// <param name="bytes"></param>
        ///// <returns></returns>
        //public static ProtocolEnum DecodeName(byte[] bytes, int offset, out int count)
        //{
        //    count = 0;
        //    if (offset + 2 > bytes.Length) return ProtocolEnum.None;
        //    Int16 len = (Int16)((bytes[offset + 1] << 8) | bytes[offset]);
        //    if (offset + 2 + len > bytes.Length) return ProtocolEnum.None;
        //    count = 2 + len;
        //    try
        //    {
        //        string name = System.Text.Encoding.UTF8.GetString(bytes, offset + 2, len);
        //        return (ProtocolEnum)System.Enum.Parse(typeof(ProtocolEnum), name);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogError("不存在的协议:" + ex.ToString());
        //        return ProtocolEnum.None;
        //    }
        //}

        ///// <summary>
        ///// 协议序列化及加密
        ///// </summary>
        ///// <param name="msgBase"></param>
        ///// <returns></returns>
        //public static byte[] Encode(MessageBase msgBase)
        //{
        //    using (var memory = new MemoryStream())
        //    {
        //        //将我们的协议类进行序列化转换成数组
        //        Serializer.Serialize(memory, msgBase);
        //        byte[] bytes = memory.ToArray();
        //        string secret = ServerSocket.SecretKey;
        //        //对数组进行加密
        //        if (msgBase is MessageSecret)
        //        {
        //            secret = ServerSocket.PublicKey;
        //        }
        //        bytes = AES.AESEncrypt(bytes, secret);
        //        return bytes;
        //    }
        //}

        ///// <summary>
        ///// 协议解密
        ///// </summary>
        ///// <param name="protocol"></param>
        ///// <param name="bytes"></param>
        ///// <param name="offset"></param>
        ///// <param name="count"></param>
        ///// <returns></returns>
        //public static MessageBase Decode(ProtocolEnum protocol, byte[] bytes, int offset, int count)
        //{
        //    if (count <= 0)
        //    {
        //        Debug.LogError("协议解密出错，数据长度为0");
        //        return null;
        //    }

        //    try
        //    {
        //        byte[] newBytes = new byte[count];
        //        Array.Copy(bytes, offset, newBytes, 0, count);
        //        string secret = ServerSocket.SecretKey;
        //        if (protocol == ProtocolEnum.MessageSecret)
        //        {
        //            secret = ServerSocket.PublicKey;
        //        }
        //        newBytes = AES.AESDecrypt(newBytes, secret);
        //        using (var memory = new MemoryStream(newBytes, 0, newBytes.Length))
        //        {
        //            Type t = System.Type.GetType(protocol.ToString());
        //            return (MessageBase)Serializer.NonGeneric.Deserialize(t, memory);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogError("协议解密出错:" + ex.ToString());
        //        return null;
        //    }
        //}

    }
}
