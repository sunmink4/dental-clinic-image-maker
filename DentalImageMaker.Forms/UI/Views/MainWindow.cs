using System.Windows;
using System.Windows.Controls;
using DentalImageMaker.Forms.UI.Units;
using DentalImageMaker.Forms.ViewModels;
using DentalImageMaker.Main.Automation;
using DentalImageMaker.Main.Services;
using DentalImageMaker.Support.UI.Units;

namespace DentalImageMaker.Forms.UI.Views;

public class MainWindow : DentalWindow
{
    static MainWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MainWindow),
            new FrameworkPropertyMetadata(typeof(MainWindow)));
    }

    private readonly MainWindowViewModel _viewModel;
    private readonly IBrowserSessionService _browser;
    private BrowserHost? _host;

    public MainWindow(MainWindowViewModel viewModel, IBrowserSessionService browser)
    {
        DataContext = viewModel;
        _viewModel = viewModel;
        _browser = browser;

        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is not null)
            Title = $"치과 이미지 메이커 v{version.ToString(3)}";

        // 브라우저 컨트롤은 뷰의 몫이라 여기서 붙인다. VM 은 어떤 서비스를 보일지만 알린다.
        viewModel.ProviderShown += provider => _host?.Show(provider);

        Loaded += (_, _) => viewModel.ShowProvider(viewModel.Provider);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_MinimizeButton") is Button minimizeButton)
            minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;

        if (GetTemplateChild("PART_MaximizeButton") is Button maximizeButton)
            maximizeButton.Click += (_, _) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        if (GetTemplateChild("PART_CloseButton") is Button closeButton)
            closeButton.Click += (_, _) => Close();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        AttachBrowsers();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AttachBrowsers();
    }

    /// <summary>본문(스타일의 Content)에서 BrowserHost 를 찾아 서비스별 브라우저를 넣는다.</summary>
    private void AttachBrowsers()
    {
        if (_host is not null || Content is not FrameworkElement root)
            return;

        _host = FindHost(root);
        if (_host is null)
            return;

        foreach (var provider in Enum.GetValues<AiProvider>())
            _host.Add(provider, _browser.GetView(provider));
        _host.Show(_viewModel.Provider);
    }

    private static BrowserHost? FindHost(DependencyObject node)
    {
        if (node is BrowserHost host)
            return host;
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
            if (FindHost(child) is { } found)
                return found;
        return null;
    }
}
