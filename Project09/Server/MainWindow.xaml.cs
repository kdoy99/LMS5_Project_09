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

namespace Server;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Socket ServerSocket; // 서버 소켓, 하나
    List<Socket> ClientSockets = new List<Socket>(); // 클라이언트 소켓, 리스트    

    object lockObject = new object(); // 멀티스레드 동기화용

    public MainWindow()
    {
        InitializeComponent();
    }

    private void openServerButton_Click(object sender, RoutedEventArgs e)
    {
        ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Any, Convert.ToInt32(portBox.Text));
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        AcceptClient();
    }

    private void AcceptClient()
    {
        try
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += ClientAccepted;
            ServerSocket.AcceptAsync(args);
            AddLog("서비스 시작");
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
        AddLog("클라이언트 연결");

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
                var userChat = JsonConvert.DeserializeObject<Chat>(json);

                AddLog($"전체 채팅 : {userChat.Message}");     

                // 3. 객체의 내용을 UI에 반영, 클라이언트에 다시 보내기
                HandlingMessage(json);


                // 4. 채팅 데이터베이스에 저장
                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    // Chat 클래스 정의 기반으로 테이블 생성
                    connection.CreateTable<Chat>();

                    // UI 컨트롤에 입력된 데이터를 chat 객체 형태로 테이블에 삽입
                    connection.Insert(userChat);
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"DataReceived 메소드에서 오류 발생! : {ex.Message}");
            }

            // 4. 다시 수신 작업 수행
            ReceiveInfo(ClientSocket);
        }
        else
        {
            lock (lockObject)
            {
                ClientSockets.Remove(ClientSocket);
            }
            // 누가 연결 종료했는지 알 수 있도록 추가하기
            AddLog($"연결 종료됨");
            ClientSocket.Close();
        }

    }

    private void HandlingMessage(string json)
    {
        lock (lockObject)
        {
            foreach (var socket in ClientSockets)
            {
                try
                {
                    byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
                    socket.SendAsync(new ArraySegment<byte>(bytesToSend), SocketFlags.None);

                }
                catch
                {
                    ClientSockets.Remove(socket);
                    socket.Close();
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