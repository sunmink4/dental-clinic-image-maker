using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using DentalImageMaker.Main.Automation;
using DentalImageMaker.Main.Prompts;

namespace DentalImageMaker.Forms.ViewModels;

/// <summary>체크박스로 켜고 끄는 도메인 규칙 한 줄.</summary>
public partial class RuleOption(PromptRule rule, bool isCustom = false, bool? isOn = null) : ObservableObject
{
    public PromptRule Rule { get; } = rule;

    /// <summary>'내용' 칸에서 사용자가 추가한 규칙이면 true(삭제 가능, 설정에 저장).</summary>
    public bool IsCustom { get; } = isCustom;

    /// <summary>진료 중 전용 규칙은 ★ 를 붙여 보여 준다.</summary>
    public string DisplayName => Rule.TreatmentOnly ? "★ " + Rule.Name : Rule.Name;

    [ObservableProperty]
    private bool _isOn = isOn ?? rule.DefaultOn;
}

/// <summary>치식 차트의 치아 한 칸(FDI 번호 + 상태). 눌러서 고른 뒤 상태 칩을 누르면 바뀐다.</summary>
public partial class ToothItem(int number) : ObservableObject
{
    public int Number { get; } = number;

    public string ToolTip => DentalChart.KoreanName(Number) + (Condition == DentalChart.Normal ? "" : " — " + Condition.Name);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToolTip))]
    private ToothCondition _condition = DentalChart.Normal;

    [ObservableProperty]
    private bool _isSelected;
}

public enum JobStatus
{
    Waiting,
    Running,
    Done,
    Failed,
}

/// <summary>대기열 한 건. 배치 JSON 으로 불러올 수도 있다(dental-clinic-clicker 의 batch 대기열 이식용).</summary>
public partial class QueueItem : ObservableObject
{
    public required string Name { get; init; }
    public required string Prompt { get; init; }
    public AiProvider Provider { get; init; }
    public GenerationKind Kind { get; init; }
    public bool NewChat { get; init; } = true;
    public IReadOnlyList<string> References { get; init; } = [];

    /// <summary>대기열에 넣을 때 고른 카메라 구도 선택지 키. 예제가 없는 선택지는 생성 결과가 예제로 등록된다.</summary>
    public IReadOnlyList<string> ExampleKeys { get; init; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private JobStatus _status = JobStatus.Waiting;

    public string StatusText => Status switch
    {
        JobStatus.Running => "진행 중",
        JobStatus.Done => "완료",
        JobStatus.Failed => "실패",
        _ => "대기",
    };

    [ObservableProperty]
    private string _message = "";
}

/// <summary>받은 결과 파일 하나.</summary>
public sealed class ResultItem(string path, AiProvider provider, string prompt)
{
    public string Path { get; } = path;
    public AiProvider Provider { get; } = provider;
    public string Prompt { get; } = prompt;
    public string FileName => System.IO.Path.GetFileName(Path);

    public bool IsImage => System.IO.Path.GetExtension(Path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif";

    public bool Exists => File.Exists(Path);
}

/// <summary>배치 JSON 한 항목의 모양.</summary>
public sealed class QueueFileEntry
{
    public string Name { get; set; } = "";
    public string Prompt { get; set; } = "";
    public AiProvider Provider { get; set; } = AiProvider.Gemini;
    public GenerationKind Kind { get; set; } = GenerationKind.Image;
    public bool NewChat { get; set; } = true;
    public List<string> References { get; set; } = [];
}
