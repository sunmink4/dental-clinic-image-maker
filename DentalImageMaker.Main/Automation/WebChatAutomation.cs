using System.Text.Json.Nodes;

namespace DentalImageMaker.Main.Automation;

/// <summary>
/// 채팅형 웹 화면(Gemini, ChatGPT)에 프롬프트를 넣고 생성된 그림·영상을 받아 오는 공통 흐름.
/// 사이트별 차이는 <see cref="SiteProfile"/>의 선택자로만 둔다.
/// dental-clinic-clicker/tools/gemini_batch.js 에서 겪은 문제들을 그대로 반영했다
/// (이전 채팅 그림을 새 그림으로 착각, 첨부가 끝나기 전에 전송, 한도 초과 시 연속 실패).
/// </summary>
public sealed class WebChatAutomation(AiProvider provider, SiteProfile site)
{
    public AiProvider Provider { get; } = provider;
    public SiteProfile Site { get; } = site;

    public bool SupportsVideo => Provider == AiProvider.Gemini;

    public async Task<GenerationResult> GenerateAsync(PageDriver page, GenerationRequest request, IProgress<string> progress, CancellationToken ct)
    {
        if (request.Kind == GenerationKind.Video && !SupportsVideo)
            return GenerationResult.Fail($"{Provider} 는 영상 생성을 지원하지 않습니다. Gemini 를 고르세요.");

        // 1. 채팅 준비
        if (request.NewChat)
        {
            progress.Report("새 채팅 여는 중");
            await page.NavigateAsync(Site.HomeUrl, ct);
        }

        if (!await page.WaitUntilAsync(() => page.ExistsAsync(Site.PromptBox), TimeSpan.FromSeconds(30), ct))
            return GenerationResult.Fail("프롬프트 입력칸을 찾지 못했습니다. 오른쪽 브라우저에서 로그인했는지 확인하세요.");

        if (request.NewChat)
        {
            // 새 채팅 화면이 뜬 직후엔 이전 채팅 그림이 잠깐 남아 있을 수 있다.
            await Task.Delay(2500, ct);
            await page.WaitUntilAsync(async () => await page.CountAsync(Site.GeneratedImage) == 0, TimeSpan.FromSeconds(10), ct);
        }

        var mediaSelectors = request.Kind == GenerationKind.Video ? Site.GeneratedVideo : Site.GeneratedImage;
        var mediaBefore = await page.CountAsync(mediaSelectors);
        var responsesBefore = await page.CountAsync(Site.ResponseContainer);

        // 2. 참고 이미지 첨부
        if (request.ReferenceImages.Count > 0)
        {
            progress.Report($"참고 이미지 {request.ReferenceImages.Count}장 첨부 중");
            var menuOpened = Site.UploadMenuButton.Length > 0 && await page.ClickAsync(Site.UploadMenuButton);
            if (menuOpened)
                await Task.Delay(800, ct); // 메뉴가 열려야 숨은 입력칸이 생긴다.

            if (!await page.SetFilesAsync(Site.FileInput, request.ReferenceImages))
                return GenerationResult.Fail("파일 입력칸을 찾지 못했습니다(sites.json 의 FileInput 확인).");

            if (menuOpened)
                await page.PressKeyAsync("Escape", 27);

            // 첨부가 끝나기 전에 보내면 그림 없이 전송된다.
            await page.WaitUntilAsync(() => page.ExistsAsync(Site.AttachmentPreview), TimeSpan.FromSeconds(20), ct);
            await Task.Delay(2500, ct);
        }

        // 3. 프롬프트 입력·전송
        progress.Report("프롬프트 보내는 중");
        if (!await page.TypeIntoAsync(Site.PromptBox, request.Prompt))
            return GenerationResult.Fail("프롬프트 입력칸에 글자를 넣지 못했습니다.");
        await Task.Delay(500, ct);

        var canSend = await page.WaitUntilAsync(() => page.ExistsAsync(Site.SendButton), TimeSpan.FromSeconds(30), ct);
        if (canSend)
            await page.ClickAsync(Site.SendButton);

        // 정말 보내졌는지 확인한다(입력칸이 비거나 생성이 시작됨). 사이트 화면이 바뀌어 버튼을 못 눌렀으면
        // 폼 제출 → Enter 순으로 다시 시도한다.
        if (!await WaitSentAsync(page, responsesBefore, ct))
        {
            progress.Report("전송 버튼이 안 눌려 다른 방법으로 보내는 중");
            await page.EvalBoolAsync($$"""
                (() => {
                  for (const s of {{PageDriver.Js(Site.PromptBox)}}) {
                    const form = document.querySelector(s)?.closest('form');
                    if (form) { form.requestSubmit(); return true; }
                  }
                  return false;
                })()
                """);
            if (!await WaitSentAsync(page, responsesBefore, ct))
            {
                await page.TypeIntoAsync(Site.PromptBox, ""); // 포커스만 다시 준다
                await page.PressKeyAsync("Enter", 13);
                if (!await WaitSentAsync(page, responsesBefore, ct))
                    return GenerationResult.Fail("프롬프트는 넣었지만 전송하지 못했습니다(sites.json 의 SendButton 확인).");
            }
        }

        // 4. 결과 기다리기
        progress.Report(request.Kind == GenerationKind.Video ? "영상 만드는 중 (몇 분 걸림)" : "그림 만드는 중");
        var timeout = request.Kind == GenerationKind.Video ? TimeSpan.FromMinutes(7) : TimeSpan.FromMinutes(4);
        var deadline = DateTime.UtcNow + timeout;
        var started = DateTime.UtcNow;
        var idleWithoutMedia = 0;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1500, ct);

            bool busy, answered;
            int media;
            try
            {
                busy = await page.ExistsAsync(Site.BusyIndicator);
                media = await page.CountAsync(mediaSelectors);
                answered = await page.CountAsync(Site.ResponseContainer) > responsesBefore;
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (media > mediaBefore && !busy)
            {
                await Task.Delay(1500, ct); // 원본으로 바뀌는 시간
                if (await page.ExistsAsync(Site.BusyIndicator))
                    continue;
                return await DownloadAsync(page, request, mediaSelectors, media - mediaBefore, progress, ct);
            }

            // 답은 끝났는데 그림이 없다: 거절이나 한도 초과. 잠깐은 더 기다려 본다.
            if (answered && !busy && DateTime.UtcNow - started > TimeSpan.FromSeconds(10))
            {
                if (++idleWithoutMedia >= 6)
                    return await FailWithLastAnswerAsync(page);
            }
            else
            {
                idleWithoutMedia = 0;
            }
        }

        return await FailWithLastAnswerAsync(page, "시간 초과: ");
    }

    private async Task<GenerationResult> DownloadAsync(PageDriver page, GenerationRequest request, string[] mediaSelectors, int newCount, IProgress<string> progress, CancellationToken ct)
    {
        progress.Report("받는 중");
        var files = new List<string>();

        // 다운로드 버튼이 있으면(Gemini) 원본 크기로 받는다. 버튼은 마우스를 올려야 보이지만 JS click 은 상관없다.
        if (Site.DownloadButton.Length > 0)
        {
            var path = await page.CaptureDownloadAsync(
                request.OutputPathWithoutExtension,
                () => page.EvalBoolAsync($$"""
                    (() => {
                      const responses = {{PageDriver.Js(Site.ResponseContainer)}}.flatMap(s => [...document.querySelectorAll(s)]);
                      const last = responses[responses.length - 1];
                      if (!last) return false;
                      const media = {{PageDriver.Js(mediaSelectors)}}.flatMap(s => [...last.querySelectorAll(s)]);
                      media.forEach(m => m.dispatchEvent(new MouseEvent('mouseover', { bubbles: true })));
                      const buttons = {{PageDriver.Js(Site.DownloadButton)}}.flatMap(s => [...last.querySelectorAll(s)]);
                      const button = buttons[buttons.length - 1];
                      if (!button) return false;
                      button.click();
                      return true;
                    })()
                    """),
                TimeSpan.FromSeconds(60), ct);
            if (path is not null)
                return GenerationResult.Ok(path);
        }

        // 버튼이 없거나 실패하면 그림 주소를 로그인된 페이지 안에서 직접 받는다(ChatGPT).
        var urls = await page.EvalAsync($$"""
            (() => {
              const all = {{PageDriver.Js(mediaSelectors)}}.flatMap(s => [...document.querySelectorAll(s)]);
              const srcs = [...new Set(all.map(m => m.currentSrc || m.src).filter(Boolean))];
              return srcs.slice(-{{Math.Max(1, newCount)}});
            })()
            """) as JsonArray;

        var index = 0;
        foreach (var url in urls ?? new JsonArray())
        {
            var suffix = index == 0 ? "" : $"_{index + 1}";
            var path = await page.FetchToFileAsync(url!.GetValue<string>(), request.OutputPathWithoutExtension + suffix);
            if (path is not null)
                files.Add(path);
            index++;
        }

        return files.Count > 0
            ? GenerationResult.Ok([.. files])
            : GenerationResult.Fail("결과는 나왔지만 파일을 받지 못했습니다. 브라우저에서 직접 저장하세요.");
    }

    /// <summary>전송됐으면 true: 입력칸이 비었거나, 생성 중 표시가 떴거나, 답변 칸이 늘었다.</summary>
    private Task<bool> WaitSentAsync(PageDriver page, int responsesBefore, CancellationToken ct)
        => page.WaitUntilAsync(async () =>
        {
            if (await page.ExistsAsync(Site.BusyIndicator))
                return true;
            if (await page.CountAsync(Site.ResponseContainer) > responsesBefore)
                return true;
            return await page.EvalBoolAsync($$"""
                (() => {
                  for (const s of {{PageDriver.Js(Site.PromptBox)}}) {
                    const el = document.querySelector(s);
                    if (el) return el.innerText.trim().length === 0;
                  }
                  return false;
                })()
                """);
        }, TimeSpan.FromSeconds(5), ct, pollMs: 300);

    private async Task<GenerationResult> FailWithLastAnswerAsync(PageDriver page, string prefix = "")
    {
        var text = "";
        try
        {
            text = await page.EvalStringAsync($$"""
                (() => {
                  const responses = {{PageDriver.Js(Site.ResponseContainer)}}.flatMap(s => [...document.querySelectorAll(s)]);
                  const last = responses[responses.length - 1];
                  return last ? last.innerText.trim().slice(0, 300) : '';
                })()
                """);
        }
        catch (InvalidOperationException)
        {
        }

        var limit = Site.LimitPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
        return GenerationResult.Fail(prefix + (string.IsNullOrWhiteSpace(text) ? "그림이 나오지 않았습니다." : text), limit);
    }
}
