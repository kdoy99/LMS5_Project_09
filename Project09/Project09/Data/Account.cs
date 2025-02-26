using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Project09.Data
{
    public class Account
    {
        [PrimaryKey, AutoIncrement]
        public int UserNumber { get; set; }
        public string ID { get; set; } // 아이디
        public string Password { get; set; } // 비밀번호
        public string Name { get; set; } // 이름        
        public string Contact { get; set; } // 연락처        
    }
}
