using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using DentalImageMaker.Main.Automation;
using DentalImageMaker.Main.Prompts;

namespace DentalImageMaker.Forms.ViewModels;

/// <summary>체크박스로 켜고 끄는 도메인 규칙 한 줄.</summary>
public partial class RuleOption(PromptRule rule) : ObservableObject
{
    public PromptRule Rule { get; } = rule;

    [ObservableProperty]
    private bool _isOn = rule.DefaultOn;
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
