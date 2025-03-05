using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
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
    /// CreateRoom.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateRoom : Window
    {
        private ObservableCollection<string> onlineUserList_create = new ObservableCollection<string>(); // 온라인 유저 정보 리스트
        private ObservableCollection<string> selecetedUserList = new ObservableCollection<string>(); // 온라인 유저 정보 리스트
        private List<string> userList_create = new List<string>();

        // 방장 정보
        private Account host;

        // 멀티스레드 동기화용
        object lockObject = new object();

        Socket socket;
        public CreateRoom(ObservableCollection<string> onlineUserList, Socket Client, Account user)
        {
            InitializeComponent();

            socket = Client;
            host = user;

            onlineUserList_create = onlineUserList;

            OnlineUserList_create.ItemsSource = onlineUserList_create;

            lock (lockObject)
            {
                selecetedUserList.Add(host.Name);
            }
            SelectedUserList_create.ItemsSource = selecetedUserList;

            
        }

        private void onlineUserList_create_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 마우스 이벤트가 발생한 곳에 OriginalSource를 사용
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is ListViewItem))
            {
                // 더블 클릭한 대상중 ListViewItem이 있을 때까지
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            if (OnlineUserList_create.SelectedItem.ToString() == host.Name)
            {
                MessageBox.Show("자기 자신은 선택할 수 없습니다!!");
                return;
            }

            Dispatcher.Invoke(() =>
            {                
                selecetedUserList.Add(OnlineUserList_create.SelectedItem.ToString());
                userList_create.Add(OnlineUserList_create.SelectedItem.ToString());
            });
        }

        private void CreateRoom_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string jsonList = JsonConvert.SerializeObject(new Chat_Client
                {
                    Type = "Create",
                    Sender = host.Name,
                    Users = userList_create,
                    RoomTitle = chatTitle.Text
                });
                byte[] bytesToSend = Encoding.UTF8.GetBytes(jsonList);
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(bytesToSend, 0, bytesToSend.Length);

                // 비동기적으로 전송
                socket.SendAsync(args);

                MessageBox.Show("채팅방 개설 완료!");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"채팅방 개설 중 오류 발생: {ex.Message}");
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void chatTitle_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            chatTitle.MaxLength = 10;
        }
    }
}
