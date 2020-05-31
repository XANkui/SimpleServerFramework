using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.MySqlData
{
    [SugarTable("user")]
    public class User
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime Logindata { get; set; }
        public string LoginType { get; set; }
        public string Token { get; set; }

    }
}
