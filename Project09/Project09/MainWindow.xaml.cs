using System.Security.Principal;
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
using SQLite;
using Project09.Data;
using System.Windows.Threading;

namespace Project09;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    
    public MainWindow()
    {
        InitializeComponent();             
    }   

    private void loginButton_Click(object sender, RoutedEventArgs e) // TODO : 서버 연결 필요, 멀티 쓰레드 이용
    {
        // 계정 정보 리스트 준비
        List<Account> accounts;

        string inputID = idBox.Text;
        string inputPW = passwordBox.Password;

        // DB에서 불러온 계정 정보들 리스트에 담음
        using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
        {
            connection.CreateTable<Account>(); // 테이블이 없으면 만들고 있으면 연결
            accounts = connection.Table<Account>().ToList();
        }

        for (int i = 0; i < accounts.Count; i++)
        {
            if (inputID == accounts[i].ID)
            {
                if (inputPW == accounts[i].Password)
                {
                    MessageBox.Show($"로그인 성공! {accounts[i].Name}님 환영합니다.");
                    //입력되어있는 ID와 비밀번호 저장
                    Account account = new Account()
                    {
                        ID = accounts[i].ID,
                        Password = accounts[i].Password,
                        Name = accounts[i].Name,
                        Contact = accounts[i].Contact
                    };
                    
                    // 메인 페이지 창 불러오기
                    ChatRoom chatRoom = new ChatRoom(account);
                    Close(); // 로그인 창은 닫고
                    chatRoom.ShowDialog();
                    return; // return으로 종료해도 되나? ShowDialog 보는 동안엔 이 함수가 계속 남아있는 상태인데
                }
            }
        }
        MessageBox.Show($"없는 아이디거나 비밀번호가 틀렸습니다!!");
    }

    private void joinButton_Click(object sender, RoutedEventArgs e)
    {
        newAccount NewAccount = new newAccount();
        NewAccount.ShowDialog();
    }
}