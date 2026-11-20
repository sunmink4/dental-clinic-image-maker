using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DentalImageMaker.Main.Prompts;

namespace DentalImageMaker.Forms.ViewModels;

/// <summary>
/// 예제 그림 카드로 고르는 한 칸(카메라 시점·앵글, 주인공 헤어·복장 …).
/// 버튼을 누르면 OptionPickerWindow 가 뜬다.
/// </summary>
public partial class OptionSlot : ObservableObject
{
    public OptionSlot(string id, string title, IReadOnlyList<PromptOption> options, bool videoOnly = false)
    {
        Id = id;
        Title = title;
        _options = options;
        VideoOnly = videoOnly;
        _selected = options[0];
        _isVisible = !videoOnly;
    }

    /// <summary>설정 파일에 고른 값을 저장할 때 쓰는 이름(예: "dentist.hair").</summary>
    public string Id { get; }

    public string Title { get; }

    /// <summary>선택 창 제목 앞에 붙는 묶음 이름(예: "치과의사", "카메라 구도").</summary>
    public string Group { get; set; } = "";

    /// <summary>문장 속 {who} 를 바꿀 주어(예: "The dentist"). 카메라 칸은 비어 있다.</summary>
    public string Subject { get; set; } = "";

    /// <summary>카메라 움직임처럼 영상일 때만 쓰는 칸.</summary>
    public bool VideoOnly { get; }

    /// <summary>헤어처럼 성별에 따라 선택지 목록이 바뀌는 칸이 있다.</summary>
    [ObservableProperty]
    private IReadOnlyList<PromptOption> _options;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Thumbnail), nameof(IsSet), nameof(PromptText))]
    private PromptOption _selected;

    [ObservableProperty]
    private bool _isVisible;

    public bool IsSet => !string.IsNullOrEmpty(Selected.Text);

    public ImageSource? Thumbnail => ExampleStore.Load(Selected.Key, 120);

    /// <summary>프롬프트에 붙일 문장. {who} 를 주어로 바꾼다.</summary>
    public string PromptText => Selected.Text.Replace("{who}", Subject);

    /// <summary>목록을 바꾸고, 지금 고른 값이 새 목록에 없으면 첫 항목(기본값)으로 돌린다.</summary>
    public void SwapOptions(IReadOnlyList<PromptOption> options)
    {
        if (ReferenceEquals(Options, options))
            return;
        Options = options;
        if (!options.Contains(Selected))
            Selected = options[0];
    }

    /// <summary>저장된 키로 고른 값을 되살린다. 없는 키면 그대로 둔다.</summary>
    public void SelectByKey(string? key)
    {
        if (key is null)
            return;
        var match = Options.FirstOrDefault(o => o.Key == key);
        if (match is not null)
            Selected = match;
    }

    /// <summary>예제를 바꾼 뒤 썸네일을 다시 읽게 한다.</summary>
    public void RefreshThumbnail() => OnPropertyChanged(nameof(Thumbnail));
}

/// <summary>선택 창의 카드 하나.</summary>
public sealed class OptionCard(PromptOption option, bool isCurrent, string subject)
{
    public PromptOption Option { get; } = option;
    public bool IsCurrent { get; } = isCurrent;
    public string Name => Option.Name;
    public string Description => string.IsNullOrEmpty(Option.Text)
        ? "프롬프트에 넣지 않습니다."
        : Option.Text.Replace("{who}", string.IsNullOrEmpty(subject) ? "The person" : subject);
    public bool CanHaveExample => !string.IsNullOrEmpty(Option.Key);
    public ImageSource? Image { get; } = ExampleStore.Load(option.Key);
    public bool HasImage => Image is not null;
}
