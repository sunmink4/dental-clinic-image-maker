using System.Windows;
using DentalImageMaker.Forms;
using DentalImageMaker.Forms.UI.Views;
using DentalImageMaker.Forms.ViewModels;
using DentalImageMaker.Main.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DentalImageMaker;

public class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => ConfigureServices(services))
            .Build();

        AppServices.Current = _host.Services;

        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 설정은 시작할 때 한 번 읽어 앱 전체가 같은 인스턴스를 본다.
        services.AddSingleton(AppSettings.Load());
        // 브라우저(WebView2)는 서비스마다 하나씩, 앱이 끝날 때까지 유지한다(로그인·채팅 상태).
        services.AddSingleton<IBrowserSessionService, BrowserSessionService>();
        services.AddSingleton<IGenerationService, GenerationService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
