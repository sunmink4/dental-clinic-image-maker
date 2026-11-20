using System.Windows;

namespace DentalImageMaker.Support.UI.Units;

public class DentalWindow : Window
{
    static DentalWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DentalWindow),
            new FrameworkPropertyMetadata(typeof(DentalWindow)));
    }

    public DentalWindow()
    {
        // youtube-mp3는 둥근 모서리를 위해 AllowsTransparency=true를 쓰지만,
        // 투명(layered) 창에서는 WebView2(HwndHost)가 그려지지 않아 여기서는 끈다. 모서리는 각지게 나온다.
        WindowStyle = WindowStyle.None;
        AllowsTransparency = false;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x1E));
    }

    /// <summary>타이틀바 가운데에 표시할 앱별 콘텐츠(예: 화면 전환 버튼).</summary>
    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarContent), typeof(object), typeof(DentalWindow),
            new PropertyMetadata(null));

    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    /// <summary>타이틀바에 창 제목을 표시할지 여부. 제목을 다른 곳(메뉴 등)에 두는 창은 끈다.</summary>
    public static readonly DependencyProperty ShowTitleTextProperty =
        DependencyProperty.Register(
            nameof(ShowTitleText), typeof(bool), typeof(DentalWindow),
            new PropertyMetadata(true));

    public bool ShowTitleText
    {
        get => (bool)GetValue(ShowTitleTextProperty);
        set => SetValue(ShowTitleTextProperty, value);
    }

    /// <summary>타이틀바 맨 왼쪽(제목 앞)에 표시할 앱별 콘텐츠(예: 설정 버튼).</summary>
    public static readonly DependencyProperty TitleBarLeftContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarLeftContent), typeof(object), typeof(DentalWindow),
            new PropertyMetadata(null));

    public object? TitleBarLeftContent
    {
        get => GetValue(TitleBarLeftContentProperty);
        set => SetValue(TitleBarLeftContentProperty, value);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Maximized)
            MaxHeight = SystemParameters.WorkArea.Height;
        else
            MaxHeight = double.PositiveInfinity;
    }
}
