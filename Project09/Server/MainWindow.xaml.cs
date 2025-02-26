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

namespace Server;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Socket ServerSocket;
    Socket ClientSocket;

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
        var args = new SocketAsyncEventArgs();
        args.Completed += ClientAccepted;

        ServerSocket.AcceptAsync(args);
        AddLog("서비스 시작");
    }

    private void ClientAccepted(object sender, SocketAsyncEventArgs e)
    {
        // 클라이언트와 연결 완료
        AddLog("클라이언트 연결");

        // 클라이언트를 상대하기 위해서 동적으로 생성된 소켓을 멤버변수에 저장
        ClientSocket = e.AcceptSocket;
        ReceiveInfo();
    }

    private void ReceiveInfo()
    {
        var args = new SocketAsyncEventArgs();
        args.SetBuffer(new byte[1024], 0, 1024);
        args.Completed += DataReceived;
        ClientSocket.ReceiveAsync(args);
    }

    private void DataReceived(object sender, SocketAsyncEventArgs e)
    {
        // 1. 도착한 바이트 배열을 Json 문자열로 변환
        string json = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);

        // 2. Json 문자열을 객체로 역직렬화
        try
        {
            var deviceinfo = JsonConvert.DeserializeObject<Chat>(json);

             AddLog(json);

            // 3. 객체의 내용을 UI에 반영
            RefreshDeviceInfo(deviceinfo);            
        }
        catch { }

        // 4. 다시 수신 작업 수행
        ReceiveInfo();
    }

    private void RefreshDeviceInfo(Chat chat)
    {
        Action action = () =>
        {
            // 1. 전송할 데이터 엔터티 객체에 준비
            var info = chat;

            // 2. 객체를 json 문자열로 직렬화
            string json = JsonConvert.SerializeObject(info);

            // 3. 문자열 byte 배열로 변환
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);

            // 4. SocketAsyncEventArgs 객체에 전송할 데이터 설정
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(bytesToSend, 0, bytesToSend.Length);

            // 5. 비동기적으로 전송
            ClientSocket.SendAsync(args);
            

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                // Chat 클래스 정의 기반으로 테이블 생성
                connection.CreateTable<Chat>();

                // UI 컨트롤에 입력된 데이터를 chat 객체 형태로 테이블에 삽입
                connection.Insert(chat);
            }
        };

        Dispatcher.Invoke(action);
    }

    private void AddLog(string log)
    {
        // 메인 스레드에서 UI 속성을 접근하는 로직이 수행되도록 위임
        Action action = () => { LogBox.AppendText(log + "\r\n"); };
        Dispatcher.Invoke(action);
    }
}