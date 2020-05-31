using MySql;
using MySql.MySqlData;
using ServerBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer.Business
{
    public class UserManager:Singleton<UserManager>
    {
        /// <summary>
        /// 正常情况下，还要包括检测验证码是否正确
        /// </summary>
        /// <param name="registerType"></param>
        /// <param name="username"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public RegisterResult Register(RegisterType registerType, string username, string pwd, out string token) {
            token = "";

            try
            {
                // 查询数据库中是否已经有的对应的用户名称
                int count = MySqlManager.Instance.sqlSugarClient.Queryable<User>().Where(it => it.Username == username).Count();

                if (count > 0)
                {

                    return RegisterResult.AlreadyExit;
                }

                User user = new User();

                // 判断是手机登录还是邮箱登陆，对应验证码校验（正常路程），才能注册
                switch (registerType)
                {
                    case RegisterType.Phone:

                        user.LoginType = RegisterType.Phone.ToString();

                        break;
                    case RegisterType.Mail:

                        user.LoginType = RegisterType.Mail.ToString();

                        break;
                    default:
                        break;
                }

                user.Username = username;
                user.Password = pwd;
                user.Logindata = DateTime.Now;
                user.Token = Guid.NewGuid().ToString();
                token = user.Token;

                // 插入数据到数据库
                MySqlManager.Instance.sqlSugarClient.Insertable(user).ExecuteCommand();

                return RegisterResult.Success;


            }
            catch (Exception ex)
            {

                Debug.LogError("注册失败:"+ex.ToString());

                return RegisterResult.Fail;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginType"></param>
        /// <param name="username"></param>
        /// <param name="pwd"></param>
        /// <param name="userid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public LoginResult Login(LoginType loginType, string username, string pwd, out int userid, out string token) {
            userid = 0;
            token = "";
            try
            {
                User user = null;
                switch (loginType)
                {
                    case LoginType.Phone:
                       
                    case LoginType.Mail:

                        user = MySqlManager.Instance.sqlSugarClient.Queryable<User>().Where(
                            it => it.Username == username).Single();

                        break;
                    // 如果是微信和QQ，在User里面要多存一个UnionID，在这里判断的时候就是 it.UnionID == username
                    case LoginType.WX:
                        
                    case LoginType.QQ:
                        break;
                    case LoginType.Token:

                        user = MySqlManager.Instance.sqlSugarClient.Queryable<User>().Where(
                           it => it.Username == username).Single();
                        break;
                    default:
                        break;
                }

                if (user == null)
                {
                    // 如果是微信QQ首次登录的话相当于注册
                    if (loginType == LoginType.WX || loginType == LoginType.QQ)
                    {
                        // 在数据库注册QQWX
                        user = new User();
                        user.Username = username;
                        user.Password = pwd;
                        user.LoginType = loginType.ToString();
                        user.Token = Guid.NewGuid().ToString();
                        user.Logindata = DateTime.Now;
                        token = user.Token;
                        userid = MySqlManager.Instance.sqlSugarClient.Insertable(user).ExecuteReturnIdentity();
                        
                        return LoginResult.Success;


                    }
                    else {
                        return LoginResult.UserNotExit;
                    }
                }
                else {
                    if (loginType != LoginType.Token)
                    {
                        if (loginType == LoginType.Phone)
                        {
                            if (user.Password != pwd)
                            {
                                return LoginResult.WrongPwd;
                            }
                        }
                        else if (loginType == LoginType.Mail)
                        {
                            if (user.Password != pwd)
                            {
                                return LoginResult.WrongPwd;
                            }
                        }
                    }
                    else {
                        if (user.Token != pwd)
                        {
                            return LoginResult.TimeoutToken;
                        }
                    }


                    user.Token = Guid.NewGuid().ToString();
                    user.Logindata = DateTime.Now;
                    token = user.Token;
                    MySqlManager.Instance.sqlSugarClient.Updateable(user).ExecuteCommand();
                    userid = user.Id;
                    

                    return LoginResult.Success;
                }

            }
            catch (Exception ex)
            {

                Debug.LogError("登录失败:" + ex.ToString());

                return LoginResult.Fail;
            }
        }

    }
}
