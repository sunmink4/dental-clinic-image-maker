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
        foreach (var custom in settings.CustomRules.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
            Rules.Add(new RuleOption(new PromptRule(custom.Text, custom.Text, true), isCustom: true, isOn: custom.IsOn));
        foreach (var rule in Rules)
            HookRule(rule);
        // 저장해 둔 주인공·위생사 설정을 되살린다(헤어는 성별에 맞는 목록을 먼저 고른다).
        _includeDentist = settings.IncludeDentist;
        _includeHygienist = settings.IncludeHygienist;
        _includePatient = settings.IncludePatient;
        _isMouthMode = settings.MouthMode;
        _dentistGender.SelectByKey(settings.SlotSelections.GetValueOrDefault(_dentistGender.Id));
        _hygienistGender.SelectByKey(settings.SlotSelections.GetValueOrDefault(_hygienistGender.Id));
        SyncHairOptions();
        foreach (var slot in CharacterSlots)
            slot.SelectByKey(settings.SlotSelections.GetValueOrDefault(slot.Id));

        foreach (var tooth in AllTeeth)
        {
            var key = settings.ToothChart.GetValueOrDefault(tooth.Number);
            tooth.Condition = DentalChart.Conditions.FirstOrDefault(c => c.Key == key && !string.IsNullOrEmpty(key)) ?? DentalChart.Normal;
        }

        ApplySituation();
        foreach (var slot in AllSlots)
            slot.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(OptionSlot.Selected))
                    return;
                if (slot == _situation)
                    ApplySituation();
                if (slot == _dentistGender || slot == _hygienistGender)
                    SyncHairOptions();
                if (CharacterSlots.Contains(slot))
                    SaveCharacter();
                if (slot == _patientGender)
                    OnPropertyChanged(nameof(ReferenceLabel));
                OnPropertyChanged(nameof(FinalPrompt));
            };

        _selectedStyle = Styles.FirstOrDefault(s => s.Name == settings.StyleName) ?? Styles[0];
        _selectedAspect = AspectRatios[0];
        EditRequests = new(settings.EditRequests.Where(r => !string.IsNullOrWhiteSpace(r)));
        // 첨부를 넣고 빼면 '첨부는 참고만, 화풍대로' 문장이 붙고 빠진다.
        References.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FinalPrompt));
        _provider = settings.LastProvider;
        _outputFolder = settings.OutputFolder;
    }

    // ---------- 선택지 ----------

    public IReadOnlyList<PromptPreset> Styles => PromptLibrary.Styles;
    public IReadOnlyList<AspectRatio> AspectRatios => PromptLibrary.AspectRatios;
    /// <summary>손으로 적어 보낸 수정 요청(새것이 앞). 누르면 내용에 붙는다.</summary>
    public ObservableCollection<string> EditRequests { get; }
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

    private readonly OptionSlot _dentistCoat = new("dentist.coat", "가운", PromptLibrary.DentistCoats) { Group = "치과의사", Subject = Dentist };
    private readonly OptionSlot _dentistCoatColor = new("dentist.coat_color", "가운 색상", PromptLibrary.DentistCoatColors) { Group = "치과의사", Subject = Dentist };
    private IReadOnlyList<OptionSlot>? _dentistSlots;
    private IReadOnlyList<OptionSlot>? _hygienistSlots;

    /// <summary>주인공 치과의사. 기본값은 20대 후반 여성·낮은 포니테일·흰 가운+네이비 스크럽·마스크 착용.</summary>
    public IReadOnlyList<OptionSlot> DentistSlots => _dentistSlots ??=
    [
        _dentistGender,
        _dentistHair,
        _dentistCoat,
        _dentistCoatColor,
        new("dentist.scrubs", "스크럽 색상", PromptLibrary.DentistScrubs) { Group = "치과의사", Subject = Dentist },
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

    private const string Patient = "The patient";

    private readonly OptionSlot _patientGender = new("patient.gender", "성별·나이", PromptLibrary.PatientGenders) { Group = "환자", Subject = Patient };
    private readonly OptionSlot _patientHair = new("patient.hair", "헤어", PromptLibrary.PatientHairs) { Group = "환자", Subject = Patient };
    private readonly OptionSlot _patientOutfit = new("patient.outfit", "옷", PromptLibrary.PatientOutfits) { Group = "환자", Subject = Patient };
    private IReadOnlyList<OptionSlot>? _patientSlots;

    /// <summary>환자. 모든 칸의 기본값은 '지정 안 함'이라 고른 것만 프롬프트에 붙는다.</summary>
    public IReadOnlyList<OptionSlot> PatientSlots => _patientSlots ??=
    [
        _patientGender,
        _patientHair,
        _patientOutfit,
        new("patient.cover", "bib·덮개", PromptLibrary.PatientCovers) { Group = "환자", Subject = Patient },
        new("patient.state", "자세·표정", PromptLibrary.PatientStates) { Group = "환자", Subject = Patient },
    ];

    private readonly OptionSlot _situation = new("treatment.situation", "상황 고르기", PromptLibrary.Situations) { Group = "치과 상황" };
    private IReadOnlyList<OptionSlot>? _situationSlots;
    private IReadOnlyList<OptionSlot>? _treatmentSlots;

    /// <summary>접수·상담·진료 중·결제 같은 장면의 큰 틀. 화면에서는 '종류' 바로 아래에 둔다.</summary>
    public IReadOnlyList<OptionSlot> SituationSlots => _situationSlots ??= [_situation];

    /// <summary>상담·양치·접수처럼 진료 중이 아닌 상황을 골랐는지. 이때 진료용 규칙은 빠진다.</summary>
    public bool IsNonTreatmentSituation =>
        _situation.IsVisible && _situation.IsSet && _situation.Selected.Key != PromptLibrary.TreatmentSituationKey;

    /// <summary>치식 차트를 보일지(접수·수납처럼 입안이 안 보이는 상황에서는 숨긴다).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private bool _showToothChart = true;

    /// <summary>진료 내용 · 진료실 칸이 하나라도 보이는지(머리글도 같이 숨긴다).</summary>
    public bool ShowTreatmentSection => TreatmentSlots.Any(s => s.IsVisible);

    /// <summary>만들 장면. false = 치과 상황(진료실 장면), true = 환자 입안(입안 클로즈업).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSceneMode), nameof(FinalPrompt), nameof(ReferenceLabel))]
    private bool _isMouthMode;

    public bool IsSceneMode
    {
        get => !IsMouthMode;
        set => IsMouthMode = !value;
    }

    partial void OnIsMouthModeChanged(bool value)
    {
        ApplySituation();
        SaveCharacter();
    }

    /// <summary>환자 입안 모드에서 숨기는 카메라 칸: 입안의 '보는 방향 · 범위'와 겹친다. 환자 머리 방향은 입안에서도 고른다(요청).</summary>
    private static readonly string[] MouthModeHiddenCamera = ["camera.view", "camera.angle", "camera.shot"];

    /// <summary>
    /// 지금 모드와 치과 상황에 맞지 않는 칸을 숨긴다(숨긴 칸은 프롬프트에서도 빠진다). 고른 값은 남겨 둔다.
    /// 치과 상황 모드: 상황표대로 숨기고 입안 칸은 모두 숨긴다.
    /// 환자 입안 모드: 상황 · 의료진 · 겹치는 카메라 칸을 숨기고 입안 칸을 보인다.
    /// </summary>
    private void ApplySituation()
    {
        var hidden = IsMouthMode
            ? []
            : PromptLibrary.SituationHiddenSlots.GetValueOrDefault(_situation.Selected.Key) ?? [];
        _situation.IsVisible = !IsMouthMode;
        foreach (var slot in DentistSlots.Concat(HygienistSlots))
            slot.IsVisible = !IsMouthMode && !hidden.Contains(slot.Id);
        foreach (var slot in PatientSlots.Concat(TreatmentSlots))
            slot.IsVisible = !hidden.Contains(slot.Id);
        foreach (var slot in MouthSlots)
            slot.IsVisible = IsMouthMode;
        foreach (var slot in CameraSlots.Where(s => !s.VideoOnly))
            slot.IsVisible = !(IsMouthMode && MouthModeHiddenCamera.Contains(slot.Id));
        ShowToothChart = !hidden.Contains(PromptLibrary.ToothChartId);
        OnPropertyChanged(nameof(ShowTreatmentSection));
        OnPropertyChanged(nameof(FinalPrompt));
    }

    /// <summary>무슨 치료를 어느 치아에 하는지. 입안 장면은 이게 구체적이어야 잘 나온다.</summary>
    public IReadOnlyList<OptionSlot> TreatmentSlots => _treatmentSlots ??=
    [
        new("treatment.kind", "치료 종류", PromptLibrary.Treatments) { Group = "진료 내용" },
        new("treatment.tooth", "치아 위치", PromptLibrary.ToothPositions) { Group = "진료 내용" },
        new("treatment.detail", "입안 디테일", PromptLibrary.IntraoralDetails) { Group = "진료 내용" },
        new("room.chair", "유니트 체어 색상", PromptLibrary.ChairColors) { Group = "진료실" },
        new("room.recline", "체어 등받이 각도", PromptLibrary.ChairRecline) { Group = "진료실" },
    ];

    // ---------- 환자 입안 ----------

    private readonly OptionSlot _mouthFrame = new("mouth.frame", "입안 범위 (화면에 어디까지)", PromptLibrary.MouthFrames) { Group = "환자 입안" };
    private IReadOnlyList<OptionSlot>? _mouthSlots;

    /// <summary>환자 입안 모드의 칸: 범위·방향·벌림·손. 이 모드에서는 카메라의 시점·앵글·샷·머리 방향을 숨긴다(겹치므로).</summary>
    public IReadOnlyList<OptionSlot> MouthSlots => _mouthSlots ??=
    [
        _mouthFrame,
        new("mouth.view", "보는 방향", PromptLibrary.MouthViews) { Group = "환자 입안" },
        new("mouth.open", "입 벌림", PromptLibrary.MouthOpenings) { Group = "환자 입안" },
        new("mouth.hands", "손·기구", PromptLibrary.MouthHands) { Group = "환자 입안" },
    ];

    private bool MouthFrameActive => _mouthFrame.IsVisible && _mouthFrame.IsSet;

    [RelayCommand]
    private void ResetMouth()
    {
        foreach (var slot in MouthSlots)
            slot.Selected = slot.Options[0];
    }

    /// <summary>settings.json 에 저장하는 칸(인물·진료). 카메라 구도는 매번 고른다.</summary>
    private IEnumerable<OptionSlot> CharacterSlots =>
        SituationSlots.Concat(DentistSlots).Concat(HygienistSlots).Concat(PatientSlots).Concat(TreatmentSlots).Concat(MouthSlots);

    private IEnumerable<OptionSlot> AllSlots => CameraSlots.Concat(CharacterSlots);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrompt))]
    private bool _includePatient;

    // ---------- 치식 차트 ----------
    // 화면 배치는 진료 차트와 같다: 환자 오른쪽이 화면 왼쪽. 위 18…11 | 21…28, 아래 48…41 | 31…38.

    public IReadOnlyList<ToothItem> UpperRightTeeth { get; } = DentalChart.UpperRight.Select(n => new ToothItem(n)).ToList();
    public IReadOnlyList<ToothItem> UpperLeftTeeth { get; } = DentalChart.UpperLeft.Select(n => new ToothItem(n)).ToList();
    public IReadOnlyList<ToothItem> LowerRightTeeth { get; } = DentalChart.LowerRight.Select(n => new ToothItem(n)).ToList();
    public IReadOnlyList<ToothItem> LowerLeftTeeth { get; } = DentalChart.LowerLeft.Select(n => new ToothItem(n)).ToList();

    private IEnumerable<ToothItem> AllTeeth => UpperRightTeeth.Concat(UpperLeftTeeth).Concat(LowerLeftTeeth).Concat(LowerRightTeeth);

    public IReadOnlyList<ToothCondition> ToothConditions => DentalChart.Conditions;

    /// <summary>정상이 아닌 치아 목록(한국어). 차트 아래에 보여 준다.</summary>
    public string ToothChartSummary
    {
        get
        {
            var lines = AllTeeth.Where(t => t.Condition != DentalChart.Normal)
                .OrderBy(t => t.Number)
                .Select(t => $"{DentalChart.KoreanName(t.Number)}: {t.Condition.Name}")
                .ToList();
            return lines.Count == 0 ? "치아를 눌러 고른 뒤(여러 개 가능) 아래 상태를 누르세요." : string.Join(Environment.NewLine, lines);
        }
    }

    [RelayCommand]
    private void ToggleTooth(ToothItem tooth) => tooth.IsSelected = !tooth.IsSelected;

    /// <summary>고른 치아들에 상태를 넣고 선택을 푼다.</summary>
    [RelayCommand]
    private void ApplyToothCondition(ToothCondition condition)
    {
        var selected = AllTeeth.Where(t => t.IsSelected).ToList();
        if (selected.Count == 0)
        {
            Status = "먼저 치식 차트에서 치아를 눌러 고르세요.";
            return;
        }
        foreach (var tooth in selected)
        {
            tooth.Condition = condition;
            tooth.IsSelected = false;
        }
        OnToothChartChanged();
    }

    [RelayCommand]
    private void ClearToothChart()
    {
        foreach (var tooth in AllTeeth)
        {
            tooth.Condition = DentalChart.Normal;
            tooth.IsSelected = false;
        }
        OnToothChartChanged();
    }

    private void OnToothChartChanged()
    {
        _settings.ToothChart = AllTeeth.Where(t => t.Condition != DentalChart.Normal)
            .ToDictionary(t => t.Number, t => t.Condition.Key);
        _settings.Save();
        OnPropertyChanged(nameof(ToothChartSummary));
        OnPropertyChanged(nameof(FinalPrompt));
    }

    private string ToothChartSentence() =>
        DentalChart.BuildSentence(AllTeeth.OrderBy(t => t.Number).Select(t => (t.Number, t.Condition)));

    partial void OnIncludePatientChanged(bool value) => SaveCharacter();

    [RelayCommand]
    private void ResetPatient()
    {
        foreach (var slot in PatientSlots)
            slot.Selected = slot.Options[0];
    }

    [RelayCommand]
    private void ResetTreatment()
    {
        foreach (var slot in TreatmentSlots)
            slot.Selected = slot.Options[0];
    }

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
        _settings.IncludePatient = IncludePatient;
        _settings.MouthMode = IsMouthMode;
        foreach (var slot in CharacterSlots)
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
        new("camera.headroll", "환자 고개 돌림 (롤, 좌우)", PromptLibrary.HeadRolls) { Group = "카메라 구도" },
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
            foreach (var s in AllSlots)
                s.RefreshThumbnail();
        if (picked is not null)
            slot.Selected = picked;
    }

    /// <summary>예제 그림이 없는 카메라 구도 선택지에 방금 받은 첫 그림을 예제로 넣는다.</summary>
    private void AddMissingExamples(QueueItem item, IEnumerable<string> files)
    {
        if (item.Kind != GenerationKind.Image || item.ExampleKeys.Count == 0)
            return;
        var image = files.FirstOrDefault(f =>
            Path.GetExtension(f).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp");
        if (image is null)
            return;

        var added = item.ExampleKeys.Where(k => ExampleStore.AddIfMissing(k, image)).ToList();
        if (added.Count == 0)
            return;
        foreach (var s in AllSlots)
            s.RefreshThumbnail();
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
        // 상황·진료 내용·치식을 먼저 둬야 모델이 '무엇을 그리는지'를 놓치지 않는다.
        // 숨긴 칸(지금 상황에 맞지 않는 칸)은 고른 값이 남아 있어도 넣지 않는다.
        // 입안 범위는 상황 바로 뒤에 둔다: 화면에 어디까지 들어오는지가 가장 먼저 정해져야 한다.
        foreach (var slot in SituationSlots.Concat(MouthSlots).Concat(TreatmentSlots).Where(s => s.IsVisible))
            yield return slot.PromptText;
        if (IncludePatient)
        {
            if (ShowToothChart)
                yield return ToothChartSentence();
            // 입안 클로즈업에서도 성별·나이는 넣는다. 빼면 모델이 아무렇게나(흔히 남성으로) 그린다.
            // 헤어·옷처럼 넓은 구도를 부르는 묘사만 아래(!IsIntraoral)에서 뺀다.
            if (IsIntraoral && _patientGender.IsVisible)
                yield return _patientGender.PromptText;
        }

        // 입안 클로즈업에서는 얼굴·머리·복장 묘사가 넓은 구도를 불러오므로 뺀다(손·기구·치아만 보이는 장면).
        if (!IsIntraoral)
        {
            // 의료진 얼굴이 화면 밖인 구도: 나이·헤어·마스크·안경 문장이 있으면 모델이 얼굴을 그리려 든다.
            // 의사는 몸통이 보이니 가운·스크럽만 남기고, 위생사는 손만 나오니 전부 뺀다.
            var facesHidden = StaffFacesHidden;
            if (IncludeDentist && !IsMouthMode)
                foreach (var slot in DentistSlots.Where(s => s.IsVisible))
                {
                    // 가운을 안 입으면 가운 색상 문장은 모순이라 뺀다.
                    if (slot == _dentistCoatColor && _dentistCoat.Selected.Key == PromptLibrary.NoCoatKey)
                        continue;
                    if (facesHidden && slot.Id is not ("dentist.coat" or "dentist.coat_color" or "dentist.scrubs"))
                        continue;
                    yield return slot.PromptText;
                }
            if (IncludeHygienist && !facesHidden && !IsMouthMode)
                foreach (var slot in HygienistSlots.Where(s => s.IsVisible))
                    yield return slot.PromptText;
            if (IncludePatient)
            {
                // 첨부 이미지의 환자를 쓰면 헤어·옷은 사진 그대로 — 다른 묘사가 섞이면 옷이 바뀐다.
                var fromReference = _patientGender.Selected.Key == PromptLibrary.PatientFromReferenceKey;
                foreach (var slot in PatientSlots.Where(s => s.IsVisible))
                {
                    if (fromReference && (slot == _patientHair || slot == _patientOutfit))
                        continue;
                    yield return slot.PromptText;
                }
            }
        }

        // 영상 전용 칸은 영상일 때만, 샷 크기는 입안 범위를 안 골랐을 때만 보인다.
        foreach (var slot in CameraSlots.Where(s => s.IsVisible))
            yield return slot.PromptText;
    }

    /// <summary>의료진 얼굴이 화면 밖인 구도인지: 시점이 '의사 뒤 위에서'.</summary>
    private bool StaffFacesHidden =>
        CameraSlots.Any(s => s.Selected.Key == PromptLibrary.FacelessViewKey);

    /// <summary>입안 클로즈업 장면인지: 입안 범위가 클로즈업이거나 샷 크기가 ECU.</summary>
    private bool IsIntraoral =>
        (MouthFrameActive && PromptLibrary.MouthCloseFrames.Contains(_mouthFrame.Selected.Key))
        || CameraSlots.Any(s => s.IsVisible && s.Selected.Key == "shot.ecu");

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

    /// <summary>첨부 칸 머리글 — 환자 입안이거나 환자를 '첨부 이미지의 환자'로 골랐으면 첨부가 곧 환자 사진이다.</summary>
    public string ReferenceLabel =>
        IsMouthMode || _patientGender.Selected.Key == PromptLibrary.PatientFromReferenceKey ? "환자 이미지 (첨부)" : "참고 이미지 (첨부)";

    private GenerationKind Kind => IsVideoKind ? GenerationKind.Video : GenerationKind.Image;

    /// <summary>
    /// 사진을 첨부하고 그림 화풍(웹툰 등)을 고르면 모델이 사진의 실사 느낌을 그대로 따라 그린다 — 첨부는 인물 · 구도 참고일 뿐이라고 못 박는다.
    /// </summary>
    private string? ReferenceStyleSentence =>
        References.Count > 0 && !string.IsNullOrWhiteSpace(SelectedStyle?.Before) && SelectedStyle.Before != PromptLibrary.PhotoStyle
            ? "Use the attached image only as a reference for the person's identity and the composition; draw the final image entirely in the art style described above, not as a photograph."
            : null;

    public string FinalPrompt => PromptBuilder.Build(
        Kind, ContinueChat, SelectedStyle, null, SelectedAspect,
        Rules.Where(r => r.IsOn && !(r.Rule.TreatmentOnly && IsNonTreatmentSituation)).Select(r => r.Rule),
        FreeText, OptionSentences().Append(ReferenceStyleSentence));

    partial void OnSelectedStyleChanged(PromptPreset value)
    {
        _settings.StyleName = value?.Name ?? "";
        _settings.Save();
    }

    /// <summary>손으로 적어 '이어서 보내기'로 보낸 수정 요청을 칩으로 등록한다(같은 글은 앞으로 옮긴다, 30개까지).</summary>
    private void RegisterEditRequest(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return;
        EditRequests.Remove(text);
        EditRequests.Insert(0, text);
        while (EditRequests.Count > 30)
            EditRequests.RemoveAt(EditRequests.Count - 1);
        _settings.EditRequests = EditRequests.ToList();
        _settings.Save();
    }

    [RelayCommand]
    private void RemoveEditRequest(string text)
    {
        if (!EditRequests.Remove(text))
            return;
        _settings.EditRequests = EditRequests.ToList();
        _settings.Save();
    }

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

    private void HookRule(RuleOption rule)
    {
        rule.PropertyChanged += (_, _) =>
        {
            if (rule.IsCustom)
                SaveCustomRules();
            OnPropertyChanged(nameof(FinalPrompt));
        };
    }

    private void SaveCustomRules()
    {
        _settings.CustomRules = Rules.Where(r => r.IsCustom)
            .Select(r => new CustomRule { Text = r.Rule.Text, IsOn = r.IsOn })
            .ToList();
        _settings.Save();
    }

    /// <summary>'내용' 칸의 줄마다 사용자 규칙으로 추가하고 칸을 비운다(같은 문장이 두 번 붙지 않게).</summary>
    [RelayCommand]
    private void AddFreeTextToRules()
    {
        var lines = (FreeText ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .Distinct()
            .ToList();
        if (lines.Count == 0)
        {
            Status = "규칙으로 추가할 내용이 없습니다.";
            return;
        }

        var added = 0;
        foreach (var line in lines)
        {
            var existing = Rules.FirstOrDefault(r => r.Rule.Text == line);
            if (existing is not null)
            {
                existing.IsOn = true;
                continue;
            }

            var rule = new RuleOption(new PromptRule(line, line, true), isCustom: true, isOn: true);
            HookRule(rule);
            Rules.Add(rule);
            added++;
        }

        SaveCustomRules();
        FreeText = "";
        Status = $"규칙 {added}개를 추가했습니다.";
        OnPropertyChanged(nameof(FinalPrompt));
    }

    [RelayCommand]
    private void RemoveRule(RuleOption rule)
    {
        if (!rule.IsCustom)
            return;
        Rules.Remove(rule);
        SaveCustomRules();
        OnPropertyChanged(nameof(FinalPrompt));
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

    /// <summary>'첨부 이미지의 환자'를 골랐는데 첨부가 없으면 보내지 않는다(이어서 보내기는 이전 그림을 쓰므로 허용).</summary>
    private bool CheckPatientReference()
    {
        if (IncludePatient && !ContinueChat && References.Count == 0
            && _patientGender.Selected.Key == PromptLibrary.PatientFromReferenceKey)
        {
            Status = "환자를 '첨부 이미지의 환자'로 골랐습니다. 참고 이미지에 환자 사진을 첨부해 주세요.";
            return false;
        }
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task GenerateAsync()
    {
        if (!CheckPatientReference())
            return;
        var item = CreateQueueItem();
        await RunItemsAsync([item]);
    }

    [RelayCommand]
    private void AddToQueue()
    {
        if (!CheckPatientReference())
            return;
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
        if (ContinueChat)
            RegisterEditRequest(FreeText);
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
            ExampleKeys = CameraSlots.Concat(SituationSlots).Concat(MouthSlots).Concat(TreatmentSlots.Where(s => !s.Id.StartsWith("room.", StringComparison.Ordinal)))
                .Where(s => s.IsVisible && s.IsSet)
                .Select(s => s.Selected.Key)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList(),
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
                    AddMissingExamples(item, result.Files);
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
