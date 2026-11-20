using System.IO;
using System.Windows;
using DentalImageMaker.Main.Automation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace DentalImageMaker.Main.Services;

public interface IBrowserSessionService
{
    /// <summary>화면에 띄울 브라우저 컨트롤. 로그인과 진행 상황 확인을 여기서 한다.</summary>
    FrameworkElement GetView(AiProvider provider);

    Task<PageDriver> GetDriverAsync(AiProvider provider);

    WebChatAutomation GetAutomation(AiProvider provider);
}

/// <summary>
/// 서비스마다 WebView2 하나씩 둔다. 사용자 데이터 폴더를 고정해서 한 번 로그인하면 다음 실행에도 유지된다.
/// 같은 환경(쿠키 저장소)을 공유하므로 구글 로그인 한 번으로 Gemini 를 쓸 수 있다.
/// </summary>
public sealed class BrowserSessionService : IBrowserSessionService
{
    public static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DentalImageMaker", "WebView2");

    private readonly Dictionary<AiProvider, WebView2> _views = [];
    private readonly Dictionary<AiProvider, Task<PageDriver>> _drivers = [];
    private readonly Dictionary<AiProvider, WebChatAutomation> _automations;
    private Task<CoreWebView2Environment>? _environment;

    public BrowserSessionService()
    {
        _automations = SiteProfileStore.Load().ToDictionary(p => p.Key, p => new WebChatAutomation(p.Key, p.Value));
    }

    public WebChatAutomation GetAutomation(AiProvider provider) => _automations[provider];

    public FrameworkElement GetView(AiProvider provider)
    {
        if (!_views.TryGetValue(provider, out var view))
        {
            view = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.FromArgb(0x1E, 0x1E, 0x1E) };
            _views[provider] = view;
        }
        return view;
    }

    public Task<PageDriver> GetDriverAsync(AiProvider provider)
    {
        if (!_drivers.TryGetValue(provider, out var task))
        {
            task = InitializeAsync(provider);
            _drivers[provider] = task;
        }
        return task;
    }

    private async Task<PageDriver> InitializeAsync(AiProvider provider)
    {
        _environment ??= CoreWebView2Environment.CreateAsync(null, UserDataFolder);
        var environment = await _environment;

        var view = (WebView2)GetView(provider);
        // 컨트롤이 화면 트리에 붙어야 초기화가 끝난다. 그래서 호출하는 쪽에서 먼저 해당 탭을 보여 준다.
        await view.EnsureCoreWebView2Async(environment);

        var core = view.CoreWebView2;
        core.Settings.AreDevToolsEnabled = true; // F12 로 선택자를 확인할 수 있게 둔다.
        core.Settings.IsStatusBarEnabled = false;
        // 새 창으로 열리는 링크(구글 로그인 팝업 등)는 같은 화면에서 연다.
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            core.Navigate(e.Uri);
        };
        core.Navigate(_automations[provider].Site.HomeUrl);

        return new PageDriver(core);
    }
}
