using ProtoBuf;
using SimpleNetServer.Business;
using SimpleNetServer.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// protobuff 序列化
[ProtoContract]
public class MessageRegister : MessageBase
{

    // 每一个协议类型必然包含构造函数，来确定当前协议类型，并且都有ProtoType进行序列化标记
    public MessageRegister()
    {

        ProtoType = ProtocolEnum.MessageRegister;
    }

    [ProtoMember(1)]
    public override ProtocolEnum ProtoType { get; set; }

    // 客户端向服务器发送的数据
    [ProtoMember(2)]
    public string Username;
    [ProtoMember(3)]
    public string Password;

    [ProtoMember(4)]
    public string Code;
    [ProtoMember(5)]
    public RegisterType RegisterType;
    [ProtoMember(6)]
    public RegisterResult Result;
}

// protobuff 序列化
[ProtoContract]
public class MessageLogin : MessageBase
{

    // 每一个协议类型必然包含构造函数，来确定当前协议类型，并且都有ProtoType进行序列化标记
    public MessageLogin()
    {

        ProtoType = ProtocolEnum.MessageLogin;
    }

    [ProtoMember(1)]
    public override ProtocolEnum ProtoType { get; set; }

    // 服务器向客户端发送的数据
    [ProtoMember(2)]
    public string Username;
    [ProtoMember(3)]
    public string Password;
    [ProtoMember(4)]
    public LoginType LoginType;
    [ProtoMember(5)]
    public LoginResult Result;
    [ProtoMember(6)]
    public string Token;
}
