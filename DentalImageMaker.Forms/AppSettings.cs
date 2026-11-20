using System.IO;
using System.Text.Json;
using DentalImageMaker.Main.Automation;

namespace DentalImageMaker.Forms;

/// <summary>
/// 사용자 설정. %AppData%\DentalImageMaker\settings.json 에 저장해 다음 실행에도 유지한다.
/// (사이트 선택자는 따로 sites.json, 로그인 정보는 WebView2 사용자 폴더에 있다.)
/// </summary>
public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DentalImageMaker", "settings.json");

    public static string DefaultOutputFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DentalImageMaker");

    /// <summary>받은 그림·영상을 날짜별 하위 폴더로 저장하는 곳.</summary>
    public string OutputFolder { get; set; } = DefaultOutputFolder;

    public AiProvider LastProvider { get; set; } = AiProvider.Gemini;

    /// <summary>대기열이 연속으로 이만큼 실패하면 한도에 걸린 것으로 보고 멈춘다.</summary>
    public int MaxConsecutiveFailures { get; set; } = 3;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (loaded is not null)
                {
                    if (string.IsNullOrWhiteSpace(loaded.OutputFolder))
                        loaded.OutputFolder = DefaultOutputFolder;
                    if (loaded.MaxConsecutiveFailures < 1)
                        loaded.MaxConsecutiveFailures = 3;
                    return loaded;
                }
            }
        }
        catch
        {
            // 설정 파일이 없거나 깨졌으면 기본값으로 시작한다.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(
                SettingsPath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // 저장 실패는 무시(이번 실행에만 적용된다).
        }
    }
}
