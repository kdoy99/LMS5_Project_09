using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Project09.Data;
using SQLite;

namespace Project09
{
    /// <summary>
    /// newAccount.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class newAccount : Window
    {
        public newAccount()
        {
            InitializeComponent();
        }

        public bool DupCheck_ID = false;
        public bool DupCheck_Contact = false;

        private void new_joinButton_Click(object sender, RoutedEventArgs e)
        {
            if (new_passwordBox.Password.Length == 0 || new_nameBox.Text.Length == 0)
            {
                MessageBox.Show("입력되지 않은 칸이 있습니다!");
                return;
            }            

            if (DupCheck_ID == true && DupCheck_Contact == true)
            {
                // AddNewContactWindow 윈도우의 입력 TextBox 컨트롤에서 값을 가져와
                // Contact 데이터 모델 객체의 필드에 각각 값을 할당한다.
                Account account = new Account()
                {
                    // 유저번호 = [PrimaryKey, AutoIncrement] 이기 때문에
                    // 값을 지정하지 않아도, PK는 자동으로 AutoIncrement 된다.
                    // 그외 Name, Email, Phone 등 프로퍼티에 값을 할당한다.       
                    ID = new_idBox.Text,
                    Password = new_passwordBox.Password,
                    Name = new_nameBox.Text,                    
                    Contact = new_contactBox.Text
                };

                // 지정된 경로에 생성할 DB 연결 객체 생성
                SQLiteConnection connection = new SQLiteConnection(App.databasePath);
                // Contact 클래스 정의를 기반으로 SQLite DB Table 생성 (테이블이 없을 경우, 있으면 X)
                connection.CreateTable<Account>();

                // UI 컨트롤에 입력된 데이터를 Account 객체 형태로, 생성한 SQLite DB Table에 삽입
                connection.Insert(account);

                // DB 연결을 닫음 (임시 구현)
                MessageBox.Show($"회원가입 완료! ID : {new_idBox.Text}");
                connection.Close();

                Close();
            }
            else
            {
                MessageBox.Show("중복 검사를 통과하고 오세요.");
            }
        }

        private void new_cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void new_idDupCheck_Click(object sender, RoutedEventArgs e)
        {
            List<Account> idCheck;

            // DB 연결
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Account>();
                idCheck = connection.Table<Account>().ToList();
            }
            // 반복문으로 중복 검사
            for (int i = 0; i < idCheck.Count; i++)
            {
                if (idCheck[i].ID == new_idBox.Text)
                {
                    MessageBox.Show("이미 존재하는 아이디입니다!");
                    DupCheck_ID = false;
                    return;
                }
            }
            // 중복 검사 통과 기록 남김
            if (new_idBox.Text.Length != 0)
            {
                MessageBox.Show("사용할 수 있는 아이디입니다!");
                DupCheck_ID = true;
            }
            else
            {
                MessageBox.Show("아무것도 입력되지 않았습니다.");
                DupCheck_ID = false;
            }
        }

        private void new_contactDupCheck_Click(object sender, RoutedEventArgs e)
        {
            List<Account> contactCheck;

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Account>();
                contactCheck = connection.Table<Account>().ToList();
            }
            // 반복문으로 중복 검사
            for (int i = 0; i < contactCheck.Count; i++)
            {
                if (contactCheck[i].Contact == new_contactBox.Text)
                {
                    MessageBox.Show($"이미 사용중인 번호입니다!");
                    DupCheck_Contact = false;
                    return;
                }
            }
            // 중복 검사 통과 기록 남김
            if (new_contactBox.Text.Length == 11)
            {
                MessageBox.Show("사용할 수 있는 번호입니다!");
                DupCheck_Contact = true;
            }
            else
            {
                MessageBox.Show("제대로 된 번호를 입력해주세요!");
                DupCheck_Contact = false;
            }
        }

        private void new_contactBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // PreviewTextInput 사용, Regex를 이용해 숫자만 입력되게
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
            // 최대 입력 길이 11
            new_contactBox.MaxLength = 11;
        }

        private void new_idBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DupCheck_ID = false;
        }

        private void new_contactBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DupCheck_Contact = false;
        }
    }
}
