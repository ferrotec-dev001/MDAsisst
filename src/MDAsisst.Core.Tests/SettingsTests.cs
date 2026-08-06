using System.Text.Json;
using MDAsisst.Core.Settings;
using Xunit;

namespace MDAsisst.Core.Tests;

public class SettingsValidatorTests
{
    [Theory]
    [InlineData(0.0, 0.2)]      // 下限クランプ
    [InlineData(0.1, 0.2)]
    [InlineData(0.2, 0.2)]      // 境界値
    [InlineData(0.85, 0.85)]
    [InlineData(1.0, 1.0)]      // 境界値
    [InlineData(2.5, 1.0)]      // 上限クランプ
    [InlineData(double.NaN, 0.2)]
    public void 透過度は許容範囲に丸められる(double input, double expected)
    {
        var s = new AppSettings();
        s.Appearance.Opacity = input;
        SettingsValidator.Normalize(s);
        Assert.Equal(expected, s.Appearance.Opacity, 3);
    }

    [Theory]
    [InlineData("#FFF", "#FFF")]
    [InlineData("#1e1e1e", "#1E1E1E")]
    [InlineData("#FF1E1E1E", "#FF1E1E1E")]
    [InlineData("1E1E1E", "#000000")]     // # 無し → 既定値
    [InlineData("#GGGGGG", "#000000")]    // 16進以外 → 既定値
    [InlineData("", "#000000")]
    [InlineData(null, "#000000")]
    public void 色文字列は検証され不正値は既定値になる(string? input, string expected)
        => Assert.Equal(expected, SettingsValidator.NormalizeColor(input, "#000000"));

    [Fact]
    public void 自動最小化秒数の負値は0に丸められ自動最小化無効となる()
    {
        var s = new AppSettings();
        s.Behavior.AutoMinimizeDelaySeconds = -30;
        SettingsValidator.Normalize(s);
        Assert.Equal(0, s.Behavior.AutoMinimizeDelaySeconds);
    }

    [Fact]
    public void ウィンドウサイズが極小の場合は既定値へ戻る()
    {
        var s = new AppSettings();
        s.Window.Width = 10;
        s.Window.Height = double.NaN;
        SettingsValidator.Normalize(s);
        Assert.Equal(900, s.Window.Width);
        Assert.Equal(600, s.Window.Height);
    }

    [Theory]
    [InlineData(-32000, 120)]   // ISS-014: OS 最小化中に読める疑似座標
    [InlineData(-9999, -9999)]  // 境界の内側はそのまま
    [InlineData(-10000, 120)]   // 境界値
    [InlineData(10000, 120)]    // 境界値
    [InlineData(double.NaN, 120)]
    public void ウィンドウ位置が異常値の場合は既定値へ戻る(double input, double expected)
    {
        var s = new AppSettings();
        s.Window.Left = input;
        s.Window.Top = input;
        SettingsValidator.Normalize(s);
        Assert.Equal(expected, s.Window.Left);
        Assert.Equal(expected, s.Window.Top);
    }
}

public class JsonSettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mdasisst-test-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_dir, "settings.json");

    public JsonSettingsServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* テスト後始末の失敗は無視 */ }
    }

    [Fact]
    public void 設定ファイルが無い場合は既定値で生成される()
    {
        var svc = new JsonSettingsService(FilePath);
        var s = svc.Load();

        Assert.Equal(UpdateMode.Manual, s.Update.Mode);
        Assert.Equal(30, s.Behavior.AutoMinimizeDelaySeconds);
        Assert.True(File.Exists(FilePath));
    }

    [Fact]
    public void 保存した設定が再読込で復元される()
    {
        var svc = new JsonSettingsService(FilePath);
        var s = svc.Load();
        s.Appearance.Opacity = 0.5;
        s.Behavior.MinimizedCorner = ScreenCorner.TopRight;
        s.Update.Mode = UpdateMode.Auto;
        Assert.True(svc.Save(s));

        var reloaded = new JsonSettingsService(FilePath).Load();
        Assert.Equal(0.5, reloaded.Appearance.Opacity, 3);
        Assert.Equal(ScreenCorner.TopRight, reloaded.Behavior.MinimizedCorner);
        Assert.Equal(UpdateMode.Auto, reloaded.Update.Mode);
    }

    [Fact]
    public void 破損した設定ファイルは退避され既定値で起動する()
    {
        File.WriteAllText(FilePath, "{ this is not json ");

        var svc = new JsonSettingsService(FilePath);
        var s = svc.Load();

        Assert.Equal(0.85, s.Appearance.Opacity, 3);
        Assert.NotEmpty(Directory.GetFiles(_dir, "settings.corrupt.*.json"));
    }

    [Fact]
    public void 範囲外の値を含むJSONは読み込み時に丸められる()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            appearance = new { opacity = 5.0, windowColor = "zzz" }
        }));

        var s = new JsonSettingsService(FilePath).Load();
        Assert.Equal(1.0, s.Appearance.Opacity, 3);
        Assert.Equal("#1E1E1E", s.Appearance.WindowColor);
    }

    [Fact]
    public void 最近使ったファイルは重複除去され上限10件に制限される()
    {
        var s = new AppSettings();
        for (int i = 0; i < 15; i++) s.AddRecentFile($@"C:\docs\file{i}.md");
        s.AddRecentFile(@"C:\docs\file14.md");

        Assert.Equal(10, s.RecentFiles.Count);
        Assert.Equal(@"C:\docs\file14.md", s.RecentFiles[0]);
        Assert.Single(s.RecentFiles, p => p.EndsWith("file14.md", StringComparison.OrdinalIgnoreCase));
    }
}
