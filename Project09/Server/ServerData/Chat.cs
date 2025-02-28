using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Server.ServerData
{
    class Chat
    {
        public string Type { get; set; } // 무슨 종류 데이터를 보내는 건지 (온라인 유저, 메시지 등)
        public string Message { get; set; }

        public List<string> Users { get; set; }
    }
}
