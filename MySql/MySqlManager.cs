using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql
{
    public class MySqlManager : Singleton<MySqlManager>
    {
#if DEBUG

        private const string connectingStr = "server=localhost;uid=root;pwd=root123;database=simpleserverfwdb";

#else
        // 对应服务器的配置
        private const string connectingStr = "server=localhost;uid=root;pwd=root123;database=simpleserverfw";

#endif

        public SqlSugarClient sqlSugarClient = null;


        public void Init() {
            sqlSugarClient = new SqlSugarClient(
                new ConnectionConfig() {
                    ConnectionString = connectingStr,
                    DbType = DbType.MySql,//设置数据库类型
                    IsAutoCloseConnection = true,//自动释放数据务，如果存在事务，在事务结束后释放
                    InitKeyType = InitKeyType.Attribute //从实体特性中读取主键自增列信息
                });

#if DEBUG  

            //用来打印Sql方便你调式    
            sqlSugarClient.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine(sql + "\r\n" +
                sqlSugarClient.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
                Console.WriteLine();
            };
#endif
        }
    }
}
