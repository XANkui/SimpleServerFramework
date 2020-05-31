using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using SimpleNetServer.Proto;


// protobuff 序列化
[ProtoContract]
public class MessageSecret : MessageBase
{

    // 每一个协议类型必然包含构造函数，来确定当前协议类型，并且都有ProtoType进行序列化标记
    public MessageSecret()
    {

        ProtoType = ProtocolEnum.MessageSecret;
    }

    [ProtoMember(1)]
    public override ProtocolEnum ProtoType { get; set; }

    // 数据加密码密钥
    [ProtoMember(2)]
    public string Secret;
}

// protobuff 序列化
[ProtoContract]
public class MessagePing : MessageBase
{

    // 每一个协议类型必然包含构造函数，来确定当前协议类型，并且都有ProtoType进行序列化标记
    public MessagePing()
    {

        ProtoType = ProtocolEnum.MessageSecret;
    }

    [ProtoMember(1)]
    public override ProtocolEnum ProtoType { get; set; }


}

// protobuff 序列化
[ProtoContract]
public class MessageTest : MessageBase
{

    // 每一个协议类型必然包含构造函数，来确定当前协议类型，并且都有ProtoType进行序列化标记
    public MessageTest()
    {

        ProtoType = ProtocolEnum.MessageTest;
    }

    [ProtoMember(1)]
    public override ProtocolEnum ProtoType { get; set; }

    [ProtoMember(2)]
    public string RequestContent { get; set; }

    [ProtoMember(3)]
    public string ResponseContent { get; set; }


}

