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

    }    
}