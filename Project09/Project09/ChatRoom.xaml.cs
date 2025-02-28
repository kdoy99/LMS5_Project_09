using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Newtonsoft.Json;
using Project09.Data;



namespace Project09
{
    /// <summary>
    /// ChatRoom.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatRoom : Window
    {
        // 클라이언트 소켓
        Socket ClientSocket;

        // 로그인 한 유저 정보
        public Account user;

        // 입력한 채팅 담을 리스트
        private ObservableCollection<string> messageList = new ObservableCollection<string>();

        // 온라인 유저 리스트
        private ObservableCollection<string> onlineUserList = new ObservableCollection<string>();

        // IP, port 값
        private string IP = "127.0.0.1";
        private int port = 10000;


        // 1. 방 종류, 2. 방 번호, 3. 보낸 사람, 4. 메시지
        private int Room;
        private int RoomNumber;

        public ChatRoom(Account account)
        {
            InitializeComponent();
            user = account;
            messageListView.ItemsSource = messageList;
            onlineList.ItemsSource = onlineUserList;

            // 메시지 형태 초기화
            Room = 0;
            RoomNumber = 0;
        }

        private void chatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Parse(IP), port);
            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = endPoint;
            args.Completed += ServerConnected;
            ClientSocket.ConnectAsync(args);
        }

        private void ServerConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 접속한 유저 정보 보내기                
                string json = JsonConvert.SerializeObject(user);
                byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(bytesToSend, 0, bytesToSend.Length);
                ClientSocket.SendAsync(args);

                // 서버 연결 성공시 제어 요청 받기
                ReceiveControl();
            }
        }

        private void ReceiveControl()
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(new byte[1024], 0, 1024);
            ClientSocket.ReceiveAsync(args); // 서버로부터 데이터 다시 받아오기
            args.Completed += ControlReceived;
        }

        private void ControlReceived(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    // 서버에서 받은 JSON 데이터를 문자열로 변환
                    string json = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                    var receivedData = JsonConvert.DeserializeObject<dynamic>(json);                    
                    
                    
                    if (receivedData.Type == "Message") // 문자열 용
                    {                        
                        // 문자열 파싱
                        // result[0] = 방 종류, result[1] = 방 번호, result[2] = 보낸 사람, result[3] = 메시지
                        string[] result = receivedData.Message.Split(",");

                        if (result[0] == "0")
                        {
                            if (result[1] == "0") // 채팅일 경우
                            {
                                // UI 스레드에서 채팅 리스트에 추가
                                Dispatcher.Invoke(() =>
                                {
                                    messageList.Add($"{result[2]} : {result[3]}");
                                });
                            }
                            else if (result[1] == "1") // 0,1 == 온라인 유저 삭제
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    onlineUserList.Remove(result[2]);
                                    messageList.Add(result[3]);
                                });
                            }
                        }
                    }                    
                    else if (receivedData.Type == "UserList") // 리스트 용
                    {
                        Dispatcher.Invoke(() =>
                        {
                            onlineUserList.Clear();
                            foreach (var user in receivedData.Users)
                            {
                                onlineUserList.Add(user.ToString());
                            }
                        });
                    }

                    // 다시 서버로부터 메시지를 받을 수 있도록 설정
                    ReceiveControl();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"메시지 수신 중 오류 발생: {ex.Message}");
            }

        }

        private void SendChatInfo()
        {
            if (ClientSocket == null || !ClientSocket.Connected)
                return;

            try
            {
                // 1. 전송할 데이터 엔터티 객체에 준비
                var info = new Chat_Client
                {
                    Type = "Message",
                    Message = $"{Room},{RoomNumber},{user.Name},{chatBox.Text}"
                };

                // 2. 객체를 json 문자열로 직렬화
                string json = JsonConvert.SerializeObject(info);

                // 3. 문자열 byte 배열로 변환
                byte[] bytesToSend = Encoding.UTF8.GetBytes(json);

                // 4. SocketAsyncEventArgs 객체에 전송할 데이터 설정
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(bytesToSend, 0, bytesToSend.Length);

                // 5. 비동기적으로 전송
                ClientSocket.SendAsync(args);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"메시지 전송 중 오류 발생: {ex.Message}");
            }
        }



        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = chatBox.Text;
            sendMessage(message);
        }

        private void sendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // 메시지 서버로 전송
            SendChatInfo();

            // 입력창 비우기
            Dispatcher.Invoke(() =>
            {
                chatBox.Clear();
            });

            if (VisualTreeHelper.GetChildrenCount(messageListView) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(messageListView, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        private void chatBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string message = chatBox.Text;
                sendMessage(message);
            }
        }
    }
}