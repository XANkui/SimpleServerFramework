using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer.Proto
{
    public enum ProtocolEnum
    {
        None=0,
        MessageSecret =1,
        MessagePing =2,
        MessageRegister =3,
        MessageLogin =4,

        MessageTest = 9999, // 用户来测试粘包分包

    }
}
