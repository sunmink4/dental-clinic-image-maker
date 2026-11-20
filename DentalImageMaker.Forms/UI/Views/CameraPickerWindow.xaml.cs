using System.Windows;
using System.Windows.Controls;
using DentalImageMaker.Forms.ViewModels;
using DentalImageMaker.Main.Prompts;
using Microsoft.Win32;

namespace DentalImageMaker.Forms.UI.Views;

/// <summary>카메라 구도 한 칸의 선택지를 예제 그림 카드로 보여 주고 하나를 고르게 한다.</summary>
public partial class CameraPickerWindow
{
    private readonly CameraSlot _slot;
    private readonly string _exampleFolder;

    public CameraOption? Picked { get; private set; }

    /// <summary>예제를 바꿨으면 true. 호출한 쪽이 썸네일을 새로 읽는다.</summary>
    public bool ExamplesChanged { get; private set; }

    public CameraPickerWindow(CameraSlot slot, string exampleFolder)
    {
        InitializeComponent();
        _slot = slot;
        _exampleFolder = exampleFolder;
        Title = $"카메라 구도 — {slot.Title}";
        Reload();
    }

    private void Reload()
        => Cards.ItemsSource = _slot.Options.Select(o => new CameraCard(o, o == _slot.Selected)).ToList();

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_CloseButton") is Button close)
            close.Click += (_, _) => Close();
        if (GetTemplateChild("PART_MinimizeButton") is Button min)
            min.Visibility = Visibility.Collapsed;
        if (GetTemplateChild("PART_MaximizeButton") is Button max)
            max.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCardClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CameraCard card)
            return;
        Picked = card.Option;
        DialogResult = true;
    }

    private void OnChangeExampleClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // 카드 선택으로 번지지 않게
        if ((sender as FrameworkElement)?.Tag is not CameraCard card)
            return;

        var dialog = new OpenFileDialog
        {
            Title = $"'{card.Name}' 예제로 쓸 그림",
            Filter = "이미지|*.png;*.jpg;*.jpeg;*.webp",
            InitialDirectory = _exampleFolder,
        };
        if (dialog.ShowDialog(this) != true)
            return;

        CameraExampleStore.SetUserExample(card.Option.Key, dialog.FileName);
        ExamplesChanged = true;
        Reload();
    }
}
