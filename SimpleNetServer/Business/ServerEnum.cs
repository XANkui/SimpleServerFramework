using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer.Business
{

    public enum RegisterType {
        Phone,
        Mail
    }

    public enum LoginType
    {
        Phone,
        Mail,
        WX,
        QQ,
        Token,
    }

    public enum RegisterResult {

        Success,
        Fail,
        AlreadyExit,
        WrongCode,
        Forbidden,
    }

    public enum LoginResult
    {

        Success,
        Fail,
        WrongPwd,
        UserNotExit,
        TimeoutToken,
    }
}
