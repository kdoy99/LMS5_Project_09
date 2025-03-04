using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Server.ServerData;
using Newtonsoft.Json;
using SQLite;
using System.Collections;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Server;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Socket ServerSocket; // 서버 소켓, 하나
    List<Socket> ClientSockets = new List<Socket>(); // 클라이언트 소켓, 리스트
    Dictionary<Socket, Account> userInfo = new Dictionary<Socket, Account>(); // 각 소켓별 접속한 유저 정보 딕셔너리

    private ObservableCollection<string> onlineUserList = new ObservableCollection<string>(); // 온라인 유저 정보 리스트

    object lockObject = new object(); // 멀티스레드 동기화용

    // 채팅방 목록


    public MainWindow()
    {
        InitializeComponent();
        OnlineList.ItemsSource = onlineUserList; // 접속중인 유저 리스트
    }

    private void openServerButton_Click(object sender, RoutedEventArgs e)
    {
        ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Any, Convert.ToInt32(portBox.Text));
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        AddLog("서비스 시작");
        AcceptClient();
    }

    private void closeServerButton_Click(object sender, RoutedEventArgs e)
    {
        AddLog("서버 종료");
        ServerSocket.Close();
    }


    private void AcceptClient()
    {
        try
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += ClientAccepted;
            ServerSocket.AcceptAsync(args);            
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AcceptClient 메소드 오류 : {ex.Message}");
        }

    }

    private void ClientAccepted(object sender, SocketAsyncEventArgs e)
    {
        // 전역변수로 선언하지 않음 -> 비동기로 각자 굴릴 것이기 때문에 변수 따로 필요
        Socket ClientSocket = e.AcceptSocket;

        // 소켓 리스트 데이터 보호 (비동기에서는 필수적)
        lock (lockObject)
        {
            ClientSockets.Add(ClientSocket); // 리스트에 추가            
        }

        // 클라이언트와 연결 완료
        AddLog($"클라이언트 연결 완료!");
       

        // 클라이언트를 상대하기 위해서 동적으로 생성된 소켓을 멤버변수에 저장
        ReceiveInfo(ClientSocket);
        AcceptClient(); // 새 클라이언트 수집          
    }        

    private void ReceiveInfo(Socket ClientSocket) // 매개변수에 접속한 클라이언트 받아옴
    {
        var args = new SocketAsyncEventArgs();
        args.SetBuffer(new byte[1024], 0, 1024);
        args.UserToken = ClientSocket; // 어떤 클라이언트인지 알아내기
        args.Completed += DataReceived;
        ClientSocket.ReceiveAsync(args);
    }

    private void DataReceived(object sender, SocketAsyncEventArgs e)
    {
        // 변수 다시 선언, 넘어온 소켓 데이터 받아서 집어넣음
        Socket ClientSocket = (Socket)e.UserToken;

        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            // 1. 도착한 바이트 배열을 Json 문자열로 변환
            string json = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);

            // 2. Json 문자열을 객체로 역직렬화
            try
            {
                // 예외) 클라이언트가 연결될 때 계정 정보 받아오는 부분
                if (userInfo.ContainsKey(ClientSocket) == false)
                {
                    // 클라이언트에서 가져온 계정 정보를 지역 변수에 담기
                    var account = JsonConvert.DeserializeObject<Account>(json);

                    // lock 이용해서 딕셔너리에 가져온 계정 정보 추가
                    lock (lockObject)
                    {
                        userInfo.Add(ClientSocket, account);
                    }

                    // 추가했다는 것은, 입장과 똑같기 때문에 log로 남김
                    AddLog($"{account.Name} 님 입장");

                    // 온라인 유저 리스트에 담기
                    Dispatcher.Invoke(() =>
                    {
                        onlineUserList.Add($"{account.Name}");
                    });
                    SendOnlineUserList(ClientSocket);
                }
                else if (userInfo.ContainsKey(ClientSocket) && e.BytesTransferred > 0)
                {
                    var userChat = JsonConvert.DeserializeObject<Chat>(json);

                    AddLog($"{userChat.Target},{userChat.Sender},{userChat.Number},{userChat.Message}");

                    // 3. 객체의 내용을 UI에 반영, 클라이언트에 다시 보내기
                    HandlingMessage(json, userChat);


                    //// 4. 채팅 데이터베이스에 저장
                    //using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                    //{
                    //    // Chat 클래스 정의 기반으로 테이블 생성
                    //    connection.CreateTable<Chat>();

                    //    // UI 컨트롤에 입력된 데이터를 chat 객체 형태로 테이블에 삽입
                    //    connection.Insert(userChat);
                    //}
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DataReceived 메소드에서 오류 발생! : {ex.Message}");
            }

            // 4. 다시 수신 작업 수행
            ReceiveInfo(ClientSocket);
        }
        else // 유저 접속 종료
        {
            if (userInfo.ContainsKey(ClientSocket))
            {
                // 누가 연결 종료했는지
                AddLog($"{userInfo[ClientSocket].Name} 연결 종료됨");

                foreach (var socket in ClientSockets)
                {                    
                    // 다시 json 변환
                    string jsonUserList = JsonConvert.SerializeObject(new Chat
                    {
                        Type = "Message",
                        Target = "전체",
                        Number = 1,
                        Sender = userInfo[ClientSocket].Name,
                        Message = $"{userInfo[ClientSocket].Name}님 퇴장"
                    });
                    // byte 변환
                    byte[] bytesToSend = Encoding.UTF8.GetBytes(jsonUserList);
                    socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);
                }

                // 딕셔너리에서 밸류 제거
                Dispatcher.Invoke(() =>
                {
                    onlineUserList.Remove(userInfo[ClientSocket].Name);
                });
                
                
                lock (lockObject)
                {
                    ClientSockets.Remove(ClientSocket); // 소켓 리스트에서 소켓 제거
                    userInfo.Remove(ClientSocket); // 딕셔너리에서 키 제거
                }
                ClientSocket.Close();
            }
        }
    }

    private void SendOnlineUserList(Socket clientSocket)
    {
        List<string> userList = userInfo.Values.Select(user => user.Name).ToList();

        // 다시 json 변환
        string jsonUserList = JsonConvert.SerializeObject(new Chat
        {
            Type = "UserList",            
            Users = userList
        });
        // byte 변환
        byte[] bytesToSend = Encoding.UTF8.GetBytes(jsonUserList);
        // 모든 클라이언트에 전송
        foreach (var socket in ClientSockets)
        {
            socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);
        }
    }

    private void HandlingMessage(string json, Chat chat) // 메시지 다루는 메소드
    {
        lock (lockObject)
        {
            foreach (var socket in ClientSockets) // foreach 반복문으로 소켓들 준비
            {
                try
                {
                    if (chat.Target == "전체") // 전체 채팅
                    {
                        byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
                        socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);
                    }
                    else if (chat.Target == "단체")
                    {
                        byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
                        socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);
                    }
                    else if (chat.Target == "1:1")
                    {
                        byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
                        socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);
                    }
                    else if (chat.Target == "쪽지")
                    {
                        byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
                        socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);
                    }
                    else { return; }
                     
                    // 소켓 모두에 메시지 보냄
                    
                }
                catch // 메시지 안 보내지면 문제 발생한 걸로 간주하고 소켓 닫음
                {
                    lock (lockObject) // 여기도 lock 사용해서 안전하게 제거
                    {
                        ClientSockets.Remove(socket); // 소켓 리스트에서 제거
                        userInfo.Remove(socket); // 딕셔너리에서 해당 소켓과 관련된 유저 정보 제거
                    }
                    socket.Close(); // 마지막으로, 소켓 닫기
                }
            }
        }
    }

    private void AddLog(string log)
    {
        // 메인 스레드에서 UI 속성을 접근하는 로직이 수행되도록 위임
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText(log + "\r\n");
            LogBox.ScrollToEnd();
        });
    }


}