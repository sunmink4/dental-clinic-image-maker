using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DentalImageMaker.Forms.UI.Views;
using DentalImageMaker.Main.Automation;
using DentalImageMaker.Main.Prompts;
using DentalImageMaker.Main.Services;
using Microsoft.Win32;

namespace DentalImageMaker.Forms.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IGenerationService _generation;
    private readonly IBrowserSessionService _browser;
    private readonly AppSettings _settings;
    private CancellationTokenSource? _cts;

    public MainWindowViewModel(IGenerationService generation, IBrowserSessionService browser, AppSettings settings)
    {
        _generation = generation;
        _browser = browser;
        _settings = settings;

        Rules = new(PromptLibrary.Rules.Select(r => new RuleOption(r)));
        foreach (var rule in Rules)
            rule.PropertyChanged += (_, _) => OnPropertyChanged(nameof(FinalPrompt));
        // 저장해 둔 주인공·위생사 설정을 되살린다(헤어는 성별에 맞는 목록을 먼저 고른다).
        _includeDentist = settings.IncludeDentist;
        _includeHygienist = settings.IncludeHygienist;
        _dentistGender.SelectByKey(settings.SlotSelections.GetValueOrDefault(_dentistGender.Id));
        _hygienistGender.SelectByKey(settings.SlotSelections.GetValueOrDefault(_hygienistGender.Id));
        SyncHairOptions();
        foreach (var slot in DentistSlots.Concat(HygienistSlots))
            slot.SelectByKey(settings.SlotSelections.GetValueOrDefault(slot.Id));

        foreach (var slot in CameraSlots.Concat(DentistSlots).Concat(HygienistSlots))
            slot.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(OptionSlot.Selected))
                    return;
                if (slot == _dentistGender || slot == _hygienistGender)
                    SyncHairOptions();
                if (DentistSlots.Contains(slot) || HygienistSlots.Contains(slot))
                    SaveCharacter();
                OnPropertyChanged(nameof(FinalPrompt));
            };

        _selectedStyle = Styles[0];
        _selectedScene = Scenes[0];
        _selectedAspect = AspectRatios[0];
        _provider = settings.LastProvider;
        _outputFolder = settings.OutputFolder;
    }

    // ---------- 선택지 ----------

    public IReadOnlyList<PromptPreset> Styles => PromptLibrary.Styles;
    public IReadOnlyList<PromptPreset> Scenes => PromptLibrary.Scenes;
    public IReadOnlyList<AspectRatio> AspectRatios => PromptLibrary.AspectRatios;
    public IReadOnlyList<string> EditChips => PromptLibrary.EditChips;
    public ObservableCollection<RuleOption> Rules { get; }

    public ObservableCollection<string> References { get; } = [];
    public ObservableCollection<QueueItem> Queue { get; } = [];
    public ObservableCollection<ResultItem> Results { get; } = [];

    // ---------- 입력 상태 ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGemini), nameof(IsChatGpt), nameof(CanUseVideo))]
    private AiProvider _provider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt), nameof(IsImageKind))]
    private bool _isVideoKind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private PromptPreset _selectedStyle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt), nameof(SceneHint))]
    private PromptPreset _selectedScene;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private AspectRatio _selectedAspect;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private string _freeText = "";

    // ---------- 등장인물 (치과의사·치과위생사) ----------

    private const string Dentist = "The dentist";
    private const string Hygienist = "The dental hygienist";

    private readonly OptionSlot _dentistGender = new("dentist.gender", "성별·나이", PromptLibrary.DentistGenders) { Group = "치과의사", Subject = Dentist };
    private readonly OptionSlot _dentistHair = new("dentist.hair", "헤어", PromptLibrary.FemaleHairs) { Group = "치과의사", Subject = Dentist };
    private readonly OptionSlot _hygienistGender = new("hygienist.gender", "성별·나이", PromptLibrary.HygienistGenders) { Group = "치과위생사", Subject = Hygienist };
    private readonly OptionSlot _hygienistHair = new("hygienist.hair", "헤어", PromptLibrary.HygienistFemaleHairs) { Group = "치과위생사", Subject = Hygienist };

    private IReadOnlyList<OptionSlot>? _dentistSlots;
    private IReadOnlyList<OptionSlot>? _hygienistSlots;

    /// <summary>주인공 치과의사. 기본값은 20대 후반 여성·낮은 포니테일·흰 가운+네이비 스크럽·마스크 착용.</summary>
    public IReadOnlyList<OptionSlot> DentistSlots => _dentistSlots ??=
    [
        _dentistGender,
        _dentistHair,
        new("dentist.outfit", "복장", PromptLibrary.DentistOutfits) { Group = "치과의사", Subject = Dentist },
        new("dentist.mask", "마스크", PromptLibrary.DentistMasks) { Group = "치과의사", Subject = Dentist },
        new("dentist.accessory", "안경·장신구", PromptLibrary.DentistAccessories) { Group = "치과의사", Subject = Dentist },
    ];

    public IReadOnlyList<OptionSlot> HygienistSlots => _hygienistSlots ??=
    [
        _hygienistGender,
        _hygienistHair,
        new("hygienist.outfit", "복장", PromptLibrary.HygienistOutfits) { Group = "치과위생사", Subject = Hygienist },
        new("hygienist.mask", "마스크", PromptLibrary.DentistMasks) { Group = "치과위생사", Subject = Hygienist },
        new("hygienist.accessory", "안경·장신구", PromptLibrary.HygienistAccessories) { Group = "치과위생사", Subject = Hygienist },
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private bool _includeDentist;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private bool _includeHygienist;

    partial void OnIncludeDentistChanged(bool value) => SaveCharacter();

    partial void OnIncludeHygienistChanged(bool value) => SaveCharacter();

    /// <summary>남성을 고르면 헤어 목록도 남성용으로 바꾼다(여성 헤어가 남은 채 모순된 프롬프트가 나가지 않게).</summary>
    private void SyncHairOptions()
    {
        _dentistHair.SwapOptions(_dentistGender.Selected.IsMale ? PromptLibrary.MaleHairs : PromptLibrary.FemaleHairs);
        _hygienistHair.SwapOptions(_hygienistGender.Selected.Key.StartsWith("hyg.m", StringComparison.Ordinal)
            ? PromptLibrary.MaleHairs
            : PromptLibrary.HygienistFemaleHairs);
    }

    private void SaveCharacter()
    {
        _settings.IncludeDentist = IncludeDentist;
        _settings.IncludeHygienist = IncludeHygienist;
        foreach (var slot in DentistSlots.Concat(HygienistSlots))
            _settings.SlotSelections[slot.Id] = slot.Selected.Key;
        _settings.Save();
    }

    [RelayCommand]
    private void ResetDentist()
    {
        foreach (var slot in DentistSlots)
            slot.Selected = slot.Options[0];
        SyncHairOptions();
        _dentistHair.Selected = _dentistHair.Options[0];
    }

    [RelayCommand]
    private void ResetHygienist()
    {
        foreach (var slot in HygienistSlots)
            slot.Selected = slot.Options[0];
        SyncHairOptions();
        _hygienistHair.Selected = _hygienistHair.Options[0];
    }

    // ---------- 카메라 구도 ----------

    /// <summary>카메라 구도 칸들. 칸을 누르면 예제 그림 카드가 있는 선택 창이 뜬다.</summary>
    public IReadOnlyList<OptionSlot> CameraSlots { get; } =
    [
        new("camera.view", "시점", PromptLibrary.Viewpoints) { Group = "카메라 구도" },
        new("camera.angle", "앵글", PromptLibrary.Angles) { Group = "카메라 구도" },
        new("camera.shot", "샷 크기", PromptLibrary.ShotSizes) { Group = "카메라 구도" },
        new("camera.head", "환자 머리 방향 (화면 기준)", PromptLibrary.HeadDirections) { Group = "카메라 구도" },
        new("camera.lens", "렌즈·심도", PromptLibrary.Lenses) { Group = "카메라 구도" },
        new("camera.move", "카메라 움직임 (영상)", PromptLibrary.CameraMoves, videoOnly: true) { Group = "카메라 구도" },
    ];

    /// <summary>칸을 누르면 예제 그림 카드 선택 창을 띄운다(카메라·인물 칸 공통).</summary>
    [RelayCommand]
    private void PickOption(OptionSlot slot)
    {
        var window = new OptionPickerWindow(slot, OutputFolder) { Owner = System.Windows.Application.Current.MainWindow };
        var picked = window.ShowDialog() == true ? window.Picked : null;
        if (window.ExamplesChanged)
            foreach (var s in CameraSlots.Concat(DentistSlots).Concat(HygienistSlots))
                s.RefreshThumbnail();
        if (picked is not null)
            slot.Selected = picked;
    }

    [RelayCommand]
    private void ResetCamera()
    {
        foreach (var slot in CameraSlots)
            slot.Selected = slot.Options[0];
    }

    /// <summary>프롬프트에 붙일 인물·카메라 문장. 인물이 먼저, 카메라가 뒤.</summary>
    private IEnumerable<string> OptionSentences()
    {
        if (IncludeDentist)
            foreach (var slot in DentistSlots)
                yield return slot.PromptText;
        if (IncludeHygienist)
            foreach (var slot in HygienistSlots)
                yield return slot.PromptText;
        foreach (var slot in CameraSlots.Where(s => !s.VideoOnly || IsVideoKind))
            yield return slot.PromptText;
    }

    partial void OnIsVideoKindChanged(bool value)
    {
        foreach (var slot in CameraSlots.Where(s => s.VideoOnly))
            slot.IsVisible = value;
    }

    /// <summary>켜면 지금 열린 채팅에 이어서 보낸다(머리말·화풍 없이 수정 요청만).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private bool _continueChat;

    [ObservableProperty]
    private string _outputName = "";

    [ObservableProperty]
    private string _outputFolder;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand), nameof(RunQueueCommand), nameof(StopCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "오른쪽 브라우저에서 Gemini·ChatGPT 에 한 번 로그인하면 다음부터는 유지됩니다.";

    public bool IsGemini
    {
        get => Provider == AiProvider.Gemini;
        set { if (value) Provider = AiProvider.Gemini; }
    }

    public bool IsChatGpt
    {
        get => Provider == AiProvider.ChatGpt;
        set { if (value) Provider = AiProvider.ChatGpt; }
    }

    public bool IsImageKind
    {
        get => !IsVideoKind;
        set => IsVideoKind = !value;
    }

    public bool CanUseVideo => Provider == AiProvider.Gemini;

    public string SceneHint => SelectedScene.Hint;

    private GenerationKind Kind => IsVideoKind ? GenerationKind.Video : GenerationKind.Image;

    public string FinalPrompt => PromptBuilder.Build(
        Kind, ContinueChat, SelectedStyle, SelectedScene, SelectedAspect,
        Rules.Where(r => r.IsOn).Select(r => r.Rule), FreeText, OptionSentences());

    /// <summary>MainWindow 가 화면 전환(브라우저 보이기)을 처리하도록 알린다.</summary>
    public event Action<AiProvider>? ProviderShown;

    partial void OnProviderChanged(AiProvider value)
    {
        if (value != AiProvider.Gemini)
            IsVideoKind = false;
        _settings.LastProvider = value;
        _settings.Save();
        ShowProvider(value);
    }

    /// <summary>해당 서비스의 브라우저를 보이게 하고 페이지를 준비한다(로그인 화면이 바로 뜨도록).</summary>
    public void ShowProvider(AiProvider provider)
    {
        ProviderShown?.Invoke(provider);
        _ = PrepareAsync(provider);
    }

    private async Task PrepareAsync(AiProvider provider)
    {
        try
        {
            await _browser.GetDriverAsync(provider);
        }
        catch (Exception ex)
        {
            Status = "브라우저를 띄우지 못했습니다(WebView2 런타임 확인): " + ex.Message;
        }
    }

    // ---------- 명령 ----------

    [RelayCommand]
    private void AppendChip(string chip)
    {
        FreeText = string.IsNullOrWhiteSpace(FreeText) ? chip : FreeText.TrimEnd() + ", " + chip;
        ContinueChat = true;
    }

    [RelayCommand]
    private void AddReferences()
    {
        var dialog = new OpenFileDialog
        {
            Title = "참고 이미지",
            Filter = "이미지|*.png;*.jpg;*.jpeg;*.webp|모든 파일|*.*",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;
        foreach (var file in dialog.FileNames)
            if (!References.Contains(file))
                References.Add(file);
    }

    /// <summary>
    /// 클립보드의 그림을 참고 이미지로 넣는다. 캡처·브라우저에서 "이미지 복사"한 그림(비트맵),
    /// 탐색기에서 복사한 파일, 파일 경로 글자 모두 받는다. 비트맵은 PNG 로 저장해 첨부한다.
    /// </summary>
    [RelayCommand]
    private void PasteReference()
    {
        try
        {
            var added = 0;
            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                foreach (string? file in System.Windows.Clipboard.GetFileDropList())
                    if (file is not null && IsImageFile(file))
                        added += AddReference(file);
            }
            else if (System.Windows.Clipboard.ContainsImage() && System.Windows.Clipboard.GetImage() is { } bitmap)
            {
                Directory.CreateDirectory(ClipboardFolder);
                var path = Path.Combine(ClipboardFolder, $"paste_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var stream = File.Create(path))
                    encoder.Save(stream);
                added += AddReference(path);
            }
            else if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText().Trim().Trim('"');
                if (File.Exists(text) && IsImageFile(text))
                    added += AddReference(text);
            }

            Status = added > 0 ? $"클립보드에서 참고 이미지 {added}장을 넣었습니다." : "클립보드에 그림이 없습니다.";
        }
        catch (Exception ex)
        {
            // 다른 프로그램이 클립보드를 잡고 있으면 실패할 수 있다.
            Status = "클립보드를 읽지 못했습니다: " + ex.Message;
        }
    }

    /// <summary>붙여 넣은 그림을 저장하는 곳(결과 폴더와 섞이지 않게 따로 둔다).</summary>
    private static readonly string ClipboardFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DentalImageMaker", "clipboard");

    private static bool IsImageFile(string path)
        => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp";

    private int AddReference(string path)
    {
        if (References.Contains(path))
            return 0;
        References.Add(path);
        return 1;
    }

    [RelayCommand]
    private void RemoveReference(string path) => References.Remove(path);

    [RelayCommand]
    private void ClearReferences() => References.Clear();

    /// <summary>받은 결과를 다음 요청의 참고 이미지로 쓴다(체어 원본 → 환자 교체 같은 흐름).</summary>
    [RelayCommand]
    private void UseResultAsReference(ResultItem item)
    {
        if (item.IsImage && !References.Contains(item.Path))
            References.Add(item.Path);
    }

    [RelayCommand]
    private void OpenResult(ResultItem item) => OpenPath(item.Path);

    [RelayCommand]
    private void OpenOutputFolder()
    {
        Directory.CreateDirectory(OutputFolder);
        OpenPath(OutputFolder);
    }

    [RelayCommand]
    private void ChangeOutputFolder()
    {
        var dialog = new OpenFolderDialog { Title = "저장 폴더", InitialDirectory = OutputFolder };
        if (dialog.ShowDialog() != true)
            return;
        OutputFolder = dialog.FolderName;
        _settings.OutputFolder = OutputFolder;
        _settings.Save();
    }

    [RelayCommand]
    private void OpenSiteSettings() => OpenPath(SiteProfileStore.Location);

    [RelayCommand]
    private void CopyPrompt() => System.Windows.Clipboard.SetText(FinalPrompt);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task GenerateAsync()
    {
        var item = CreateQueueItem();
        await RunItemsAsync([item]);
    }

    [RelayCommand]
    private void AddToQueue()
    {
        Queue.Add(CreateQueueItem());
        Status = $"대기열 {Queue.Count(q => q.Status == JobStatus.Waiting)}건";
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task RunQueueAsync()
        => RunItemsAsync(Queue.Where(q => q.Status is JobStatus.Waiting or JobStatus.Failed).ToList());

    [RelayCommand(CanExecute = nameof(IsBusy))]
    private void Stop() => _cts?.Cancel();

    [RelayCommand]
    private void ClearQueue()
    {
        foreach (var item in Queue.Where(q => q.Status != JobStatus.Running).ToList())
            Queue.Remove(item);
    }

    [RelayCommand]
    private void LoadQueue()
    {
        var dialog = new OpenFileDialog { Title = "대기열 JSON 불러오기", Filter = "JSON|*.json" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            };
            var entries = JsonSerializer.Deserialize<List<QueueFileEntry>>(File.ReadAllText(dialog.FileName), options) ?? [];
            var baseDir = Path.GetDirectoryName(dialog.FileName)!;
            foreach (var e in entries.Where(e => !string.IsNullOrWhiteSpace(e.Prompt)))
            {
                Queue.Add(new QueueItem
                {
                    Name = string.IsNullOrWhiteSpace(e.Name) ? $"job{Queue.Count + 1}" : e.Name,
                    Prompt = e.Prompt,
                    Provider = e.Provider,
                    Kind = e.Kind,
                    NewChat = e.NewChat,
                    // 상대 경로는 JSON 파일 위치 기준.
                    References = e.References.Select(r => Path.IsPathRooted(r) ? r : Path.Combine(baseDir, r)).ToList(),
                });
            }
            Status = $"{entries.Count}건을 대기열에 넣었습니다.";
        }
        catch (Exception ex)
        {
            Status = "대기열 파일을 읽지 못했습니다: " + ex.Message;
        }
    }

    private bool CanStart() => !IsBusy;

    private QueueItem CreateQueueItem()
    {
        var name = string.IsNullOrWhiteSpace(OutputName)
            ? $"{(IsVideoKind ? "video" : "image")}_{DateTime.Now:HHmmss}"
            : OutputName.Trim();
        return new QueueItem
        {
            Name = name,
            Prompt = FinalPrompt,
            Provider = Provider,
            Kind = Kind,
            NewChat = !ContinueChat,
            References = References.ToList(),
        };
    }

    private async Task RunItemsAsync(IReadOnlyList<QueueItem> items)
    {
        if (items.Count == 0)
        {
            Status = "보낼 항목이 없습니다.";
            return;
        }

        IsBusy = true;
        _cts = new CancellationTokenSource();
        var failures = 0;
        var progress = new Progress<string>(s => Status = s);

        try
        {
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Prompt))
                    continue;

                if (!Queue.Contains(item))
                    Queue.Add(item);

                // 진행 상황을 볼 수 있게 해당 서비스 화면으로 바꾼다.
                Provider = item.Provider;
                ProviderShown?.Invoke(item.Provider);

                item.Status = JobStatus.Running;
                var folder = Path.Combine(OutputFolder, DateTime.Now.ToString("yyyyMMdd"));
                var request = new GenerationRequest
                {
                    Prompt = item.Prompt,
                    ReferenceImages = item.References,
                    Kind = item.Kind,
                    NewChat = item.NewChat,
                    OutputPathWithoutExtension = UniquePath(Path.Combine(folder, Sanitize(item.Name))),
                };

                var result = await _generation.GenerateAsync(item.Provider, request, progress, _cts.Token);
                if (result.Success)
                {
                    item.Status = JobStatus.Done;
                    item.Message = string.Join(", ", result.Files.Select(f => Path.GetFileName(f)));
                    foreach (var file in result.Files)
                        Results.Insert(0, new ResultItem(file, item.Provider, item.Prompt));
                    failures = 0;
                    // 다음 항목이 이어서 보내기라면 방금 받은 채팅에 이어진다.
                }
                else
                {
                    item.Status = JobStatus.Failed;
                    item.Message = result.Message;
                    failures++;
                    if (result.LimitReached || failures >= _settings.MaxConsecutiveFailures)
                    {
                        Status = "한도에 걸린 것 같아 멈췄습니다: " + result.Message;
                        return;
                    }
                }
            }

            Status = $"끝났습니다. 저장 폴더: {OutputFolder}";
        }
        catch (OperationCanceledException)
        {
            foreach (var item in items.Where(i => i.Status == JobStatus.Running))
                item.Status = JobStatus.Waiting;
            Status = "중지했습니다.";
        }
        finally
        {
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    /// <summary>같은 이름이 있으면 _2, _3 을 붙인다(덮어쓰지 않는다).</summary>
    private static string UniquePath(string pathWithoutExtension)
    {
        var dir = Path.GetDirectoryName(pathWithoutExtension)!;
        var name = Path.GetFileName(pathWithoutExtension);
        bool Taken(string n) => Directory.Exists(dir) && Directory.EnumerateFiles(dir, n + ".*").Any();

        if (!Taken(name))
            return pathWithoutExtension;
        for (var i = 2; ; i++)
            if (!Taken($"{name}_{i}"))
                return Path.Combine(dir, $"{name}_{i}");
    }

    private static void OpenPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // 연결 프로그램이 없으면 조용히 넘어간다.
        }
    }
}
