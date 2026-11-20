namespace DentalImageMaker.Main.Automation;

/// <summary>프롬프트를 보낼 웹 서비스. API가 아니라 로그인된 웹 화면을 앱 안 브라우저로 조작한다(추가 요금 없음).</summary>
public enum AiProvider
{
    Gemini,
    ChatGpt,
}

public enum GenerationKind
{
    Image,
    Video,
}

public sealed class GenerationRequest
{
    public required string Prompt { get; init; }

    /// <summary>함께 첨부할 참고 이미지(원본 체어, 인물 등). 서비스마다 장수 제한이 있다(Gemini 영상은 3장).</summary>
    public IReadOnlyList<string> ReferenceImages { get; init; } = [];

    public GenerationKind Kind { get; init; } = GenerationKind.Image;

    /// <summary>false면 지금 열린 채팅에 이어 보낸다("줌아웃 30%" 같은 수정 요청).</summary>
    public bool NewChat { get; init; } = true;

    /// <summary>저장 파일 이름(확장자 제외). 확장자는 받은 파일에 맞춰 붙는다.</summary>
    public required string OutputPathWithoutExtension { get; init; }
}

public sealed class GenerationResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Files { get; init; } = [];

    /// <summary>실패했을 때 서비스가 남긴 답변(한도 초과, 거절 등).</summary>
    public string Message { get; init; } = "";

    /// <summary>일일 한도에 걸린 것으로 보이면 true. 대기열은 이때 멈춘다.</summary>
    public bool LimitReached { get; init; }

    public static GenerationResult Ok(params string[] files) => new() { Success = true, Files = files };
    public static GenerationResult Fail(string message, bool limit = false) => new() { Message = message, LimitReached = limit };
}
