using System.IO;
using System.Text.Json;

namespace DentalImageMaker.Main.Automation;

/// <summary>
/// 웹 화면을 조작할 때 쓰는 CSS 선택자 묶음. 사이트 화면이 바뀌면 여기만 고치면 된다.
/// %AppData%\DentalImageMaker\sites.json 에 기본값을 써 두므로 앱을 다시 빌드하지 않고 손으로 고칠 수 있다.
/// 각 항목은 앞에서부터 차례로 시도한다(한국어·영어 UI 모두 대응).
/// </summary>
public sealed class SiteProfile
{
    /// <summary>
    /// 기본 선택자 판. 앱이 고친 기본값을 내보내면 올린다. sites.json 의 판이 이보다 낮으면
    /// (손으로 고친 게 아니라 예전 기본값이므로) 새 기본값으로 덮어쓴다.
    /// </summary>
    public const int CurrentRevision = 2;

    public int Revision { get; set; }

    public string HomeUrl { get; set; } = "";

    /// <summary>프롬프트 입력칸(contenteditable).</summary>
    public string[] PromptBox { get; set; } = [];

    public string[] SendButton { get; set; } = [];

    /// <summary>답변을 만드는 중에만 보이는 요소(중지 버튼 등).</summary>
    public string[] BusyIndicator { get; set; } = [];

    /// <summary>파일 입력칸이 숨어 있을 때 먼저 눌러 메뉴를 여는 버튼. 비워 두면 바로 입력칸을 찾는다.</summary>
    public string[] UploadMenuButton { get; set; } = [];

    public string[] FileInput { get; set; } = [];

    /// <summary>첨부한 그림이 입력칸에 미리보기로 붙었는지 확인하는 요소.</summary>
    public string[] AttachmentPreview { get; set; } = [];

    /// <summary>답변(모델 응답) 한 덩어리. 마지막 것이 방금 받은 답이다.</summary>
    public string[] ResponseContainer { get; set; } = [];

    /// <summary>응답 안의 완성된 생성 이미지.</summary>
    public string[] GeneratedImage { get; set; } = [];

    public string[] GeneratedVideo { get; set; } = [];

    /// <summary>응답 안의 원본 다운로드 버튼. 있으면 이걸로 받고, 없으면 이미지 주소를 직접 받아 온다.</summary>
    public string[] DownloadButton { get; set; } = [];

    /// <summary>답변에 이 문구가 있으면 한도 초과로 보고 대기열을 멈춘다.</summary>
    public string[] LimitPhrases { get; set; } = [];

    public static SiteProfile DefaultGemini() => new()
    {
        Revision = CurrentRevision,
        HomeUrl = "https://gemini.google.com/app",
        PromptBox =
        [
            "rich-textarea div.ql-editor[contenteditable='true']",
            "div[contenteditable='true'][aria-label*='프롬프트']",
            "div[contenteditable='true'][aria-label*='prompt' i]",
        ],
        SendButton =
        [
            "button.send-button:not([disabled])",
            "button[aria-label='메시지 보내기']",
            "button[aria-label='Send message']",
        ],
        BusyIndicator =
        [
            "button[aria-label*='중지']",
            "button[aria-label*='Stop' i]",
            "model-response .loading-content-spinner-container",
        ],
        UploadMenuButton =
        [
            "button[aria-label='업로드 및 도구']",
            "button[aria-label*='업로드']",
            "button[aria-label*='upload' i]",
        ],
        FileInput = ["input[type='file'][accept^='image/']", "input[type='file']"],
        AttachmentPreview = ["uploader-file-preview", ".file-preview-container img", "img[data-test-id='image-preview']"],
        ResponseContainer = ["model-response"],
        GeneratedImage = ["img.image.loaded"],
        GeneratedVideo = ["video"],
        DownloadButton =
        [
            "button[aria-label='원본 크기 이미지 다운로드']",
            "button[aria-label*='다운로드']",
            "button[aria-label*='Download' i]",
        ],
        LimitPhrases = ["한도", "더 이상", "limit", "내일 다시", "재설정되는 대로"],
    };

    public static SiteProfile DefaultChatGpt() => new()
    {
        Revision = CurrentRevision,
        HomeUrl = "https://chatgpt.com/",
        PromptBox = ["#prompt-textarea", "div.ProseMirror[contenteditable='true']", "[contenteditable='true'][aria-label*='ChatGPT']"],
        // 2026-11: data-testid 가 빠지고 form 안의 submit 버튼(aria-label "보내기")만 남았다.
        // 입력칸이 비어 있을 때 같은 자리에 '음성 대화 시작' 버튼이 있으므로 aria-label 로 구분한다.
        SendButton =
        [
            "button[data-testid='send-button']:not([disabled])",
            "form button[type='submit'][aria-label='보내기']:not([disabled])",
            "form button[type='submit'][aria-label*='보내']:not([disabled])",
            "form button[type='submit'][aria-label*='Send' i]:not([disabled])",
            "#composer-submit-button[aria-label*='Send' i]:not([disabled])",
        ],
        BusyIndicator =
        [
            "button[data-testid='stop-button']",
            "form button[aria-label*='중지']",
            "form button[aria-label*='Stop' i]",
        ],
        UploadMenuButton = [],
        FileInput = ["input[type='file'][accept*='image']", "input[type='file']"],
        AttachmentPreview = ["form img[src^='blob:']", "form img", "[data-testid*='attachment']"],
        ResponseContainer = ["[data-message-author-role='assistant']", "article[data-testid^='conversation-turn']"],
        // 진행 중에는 흐린 미리보기가 같은 자리에 깔리므로, 중지 버튼이 사라진 뒤의 이미지만 결과로 본다.
        GeneratedImage =
        [
            "img[alt*='Generated image' i]",
            "img[alt*='생성된 이미지']",
            "img[src*='oaiusercontent']",
            "img[src*='/backend-api/estuary/content']",
        ],
        GeneratedVideo = ["video"],
        DownloadButton = [],
        LimitPhrases = ["limit", "한도", "제한", "try again later", "나중에 다시"],
    };
}

/// <summary>sites.json 을 읽고 쓴다. 파일이 없거나 깨졌으면 기본값을 쓴다.</summary>
public static class SiteProfileStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DentalImageMaker", "sites.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Location => FilePath;

    public static Dictionary<AiProvider, SiteProfile> Load()
    {
        var defaults = new Dictionary<AiProvider, SiteProfile>
        {
            [AiProvider.Gemini] = SiteProfile.DefaultGemini(),
            [AiProvider.ChatGpt] = SiteProfile.DefaultChatGpt(),
        };

        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<AiProvider, SiteProfile>>(File.ReadAllText(FilePath));
                if (loaded is not null)
                {
                    var outdated = false;
                    foreach (var (key, value) in loaded)
                    {
                        // 예전 판 기본값이면 새 기본값을 쓴다(사이트가 바뀌어 고친 선택자를 받도록).
                        if (value.Revision < SiteProfile.CurrentRevision)
                            outdated = true;
                        else
                            defaults[key] = value;
                    }
                    if (!outdated)
                        return defaults;
                }
            }

            // 처음 실행: 손으로 고칠 수 있게 기본값을 써 둔다.
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(defaults, JsonOptions));
        }
        catch
        {
            // 깨진 파일은 무시하고 기본값으로 돈다.
        }

        return defaults;
    }
}
