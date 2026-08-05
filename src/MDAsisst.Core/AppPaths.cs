namespace MDAsisst.Core;

/// <summary>ユーザーデータの配置場所。アプリフォルダには何も書かない（更新で消えるため）。</summary>
public static class AppPaths
{
    public const string AppFolderName = "MDAsisst";

    public static string UserDataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string LogDirectory
    {
        get
        {
            var dir = Path.Combine(UserDataDirectory, "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string UserSnippetsPath => Path.Combine(UserDataDirectory, "snippets.json");
}
