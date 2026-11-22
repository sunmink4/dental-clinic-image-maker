using System.Windows;
using Velopack;
using Velopack.Sources;

namespace DentalImageMaker;

/// <summary>
/// 설치판(Setup.exe 로 설치한 앱)이면 켤 때 GitHub 릴리스에서 새 버전을 찾아 업데이트할지 묻는다.
/// 설치 없이 쓰는 단일 exe 는 IsInstalled 가 false 라 아무것도 하지 않는다.
/// </summary>
internal static class Updater
{
    private const string RepoUrl = "https://github.com/sunmink4/dental-clinic-image-maker";

    public static async Task CheckAsync()
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(RepoUrl, null, false));
            if (!manager.IsInstalled)
                return;

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
                return;

            var target = update.TargetFullRelease.Version;
            var answer = MessageBox.Show(
                $"새 버전 v{target} 이 있습니다(지금 v{manager.CurrentVersion}).\n지금 업데이트하고 다시 시작할까요?",
                "치과 이미지 메이커 업데이트",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (answer != MessageBoxResult.Yes)
                return;

            await manager.DownloadUpdatesAsync(update);
            manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
        }
        catch
        {
            // 인터넷이 없거나 GitHub 이 안 될 때는 조용히 넘어간다(앱 사용에는 지장 없음).
        }
    }
}
