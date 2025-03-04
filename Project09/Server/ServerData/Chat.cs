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
        public string Target {  get; set; } // 전체 채팅인지, 1:1 채팅인지 등
        public int Number { get; set; } // 방 번호
        public string Sender { get; set; } // 보낸 유저 이름
        public string Message { get; set; } // 보낸 메시지 내용

        public List<string> Users { get; set; } // 접속한 유저 명단
    }
}
