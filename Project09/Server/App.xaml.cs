using System.Configuration;
using System.Data;
using System.Windows;

namespace Server;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    static string accountDB = "Account.db";

    // 내문서 경로 가져오는 방법
    //string forderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    // 현재 프로젝트 실행 경로 가져오는 방법 (프로젝트\bin\Debug\net8.0-windows)
    //string forderPath = Environment.CurrentDirectory;

    // 현재 프로젝트 경로 가져오기
    static string projectPath = "../../../";
    // 생성될 DB 경로(현재 프로젝트 경로 + 파일이름) 지정
    public static string databasePath = System.IO.Path.Combine(projectPath, accountDB);
}

