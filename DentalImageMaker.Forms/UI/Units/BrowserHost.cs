using System.Windows;
using System.Windows.Controls;
using DentalImageMaker.Main.Automation;

namespace DentalImageMaker.Forms.UI.Units;

/// <summary>
/// 서비스별 브라우저(WebView2)를 모두 자식으로 붙여 두고 하나만 보이게 한다.
/// ContentControl 로 갈아 끼우면 WebView2 창이 떼었다 붙었다 해서 깜빡이고 상태가 흔들리므로 보이기만 바꾼다.
/// </summary>
public class BrowserHost : Grid
{
    private readonly Dictionary<AiProvider, FrameworkElement> _views = [];

    public void Add(AiProvider provider, FrameworkElement view)
    {
        _views[provider] = view;
        view.Visibility = Visibility.Collapsed;
        Children.Add(view);
    }

    public void Show(AiProvider provider)
    {
        foreach (var (key, view) in _views)
            view.Visibility = key == provider ? Visibility.Visible : Visibility.Collapsed;
    }
}
