using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DentalImageMaker.Main.Prompts;

namespace DentalImageMaker.Forms.ViewModels;

/// <summary>카메라 구도 한 칸(시점, 앵글 …). 버튼을 누르면 예제 그림이 있는 선택 창이 뜬다.</summary>
public partial class CameraSlot : ObservableObject
{
    public CameraSlot(string title, IReadOnlyList<CameraOption> options, bool videoOnly = false)
    {
        Title = title;
        Options = options;
        VideoOnly = videoOnly;
        _selected = options[0];
        _isVisible = !videoOnly;
    }

    public string Title { get; }
    public IReadOnlyList<CameraOption> Options { get; }

    /// <summary>카메라 움직임처럼 영상일 때만 쓰는 칸.</summary>
    public bool VideoOnly { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Thumbnail), nameof(IsSet))]
    private CameraOption _selected;

    [ObservableProperty]
    private bool _isVisible;

    public bool IsSet => !string.IsNullOrEmpty(Selected.Text);

    public ImageSource? Thumbnail => CameraExampleStore.Load(Selected.Key, 120);

    /// <summary>예제를 바꾼 뒤 썸네일을 다시 읽게 한다.</summary>
    public void RefreshThumbnail() => OnPropertyChanged(nameof(Thumbnail));
}

/// <summary>선택 창의 카드 하나.</summary>
public sealed class CameraCard(CameraOption option, bool isCurrent)
{
    public CameraOption Option { get; } = option;
    public bool IsCurrent { get; } = isCurrent;
    public string Name => Option.Name;
    public string Description => string.IsNullOrEmpty(Option.Text) ? "프롬프트에 구도를 넣지 않습니다." : Option.Text;
    public bool CanHaveExample => !string.IsNullOrEmpty(Option.Key);
    public ImageSource? Image { get; } = CameraExampleStore.Load(option.Key);
    public bool HasImage => Image is not null;
}
