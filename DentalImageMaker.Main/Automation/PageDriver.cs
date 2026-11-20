using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;

namespace DentalImageMaker.Main.Automation;

/// <summary>
/// WebView2 페이지를 Playwright 비슷하게 다루는 도우미.
/// 스크립트 실행은 ExecuteScriptAsync 대신 DevTools 프로토콜(Runtime.evaluate)을 쓴다:
/// Promise 를 기다릴 수 있고(awaitPromise), 파일 첨부(DOM.setFileInputFiles)·글자 입력(Input.insertText)도 같은 경로로 한다.
/// 모든 호출은 UI 스레드에서 해야 한다(WebView2 제약).
/// </summary>
public sealed class PageDriver(CoreWebView2 core)
{
    public CoreWebView2 Core { get; } = core;

    // ---------- 스크립트 ----------

    /// <summary>식을 평가해 JSON 값으로 돌려준다. Promise 면 끝날 때까지 기다린다.</summary>
    public async Task<JsonNode?> EvalAsync(string expression)
    {
        var args = new JsonObject
        {
            ["expression"] = expression,
            ["awaitPromise"] = true,
            ["returnByValue"] = true,
        };
        var raw = await Core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", args.ToJsonString());
        var reply = JsonNode.Parse(raw);
        if (reply?["exceptionDetails"] is { } error)
            throw new InvalidOperationException("페이지 스크립트 오류: " + (error["exception"]?["description"] ?? error["text"]));
        return reply?["result"]?["value"];
    }

    public async Task<bool> EvalBoolAsync(string expression)
        => (await EvalAsync(expression))?.GetValue<bool>() ?? false;

    public async Task<int> EvalIntAsync(string expression)
        => (await EvalAsync(expression))?.GetValue<int>() ?? 0;

    public async Task<string> EvalStringAsync(string expression)
        => (await EvalAsync(expression))?.GetValue<string>() ?? "";

    /// <summary>선택자 목록을 JS 배열 문자열로. 앞에서부터 처음 맞는 것을 쓴다.</summary>
    public static string Js(IEnumerable<string> selectors) => JsonSerializer.Serialize(selectors);

    public static string Js(string text) => JsonSerializer.Serialize(text);

    /// <summary>선택자 중 하나라도 맞는 요소가 있는지.</summary>
    public Task<bool> ExistsAsync(string[] selectors)
        => EvalBoolAsync($"{Js(selectors)}.some(s => !!document.querySelector(s))");

    public Task<int> CountAsync(string[] selectors)
        => EvalIntAsync($"Math.max(0, ...{Js(selectors)}.map(s => document.querySelectorAll(s).length))");

    public async Task<bool> ClickAsync(string[] selectors)
        => await EvalBoolAsync($$"""
            (() => {
              for (const s of {{Js(selectors)}}) {
                const el = document.querySelector(s);
                if (el) { el.click(); return true; }
              }
              return false;
            })()
            """);

    // ---------- 대기 ----------

    public async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, CancellationToken ct, int pollMs = 500)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (await condition())
                    return true;
            }
            catch (InvalidOperationException)
            {
                // 페이지가 넘어가는 중이면 스크립트가 실패할 수 있다. 다음 회차에 다시 본다.
            }
            await Task.Delay(pollMs, ct);
        }
        return false;
    }

    public async Task NavigateAsync(string url, CancellationToken ct)
    {
        var done = new TaskCompletionSource();
        void Handler(object? s, CoreWebView2NavigationCompletedEventArgs e) => done.TrySetResult();
        Core.NavigationCompleted += Handler;
        try
        {
            Core.Navigate(url);
            await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(30), ct));
        }
        finally
        {
            Core.NavigationCompleted -= Handler;
        }
    }

    // ---------- 입력 ----------

    /// <summary>입력칸에 포커스를 주고 끝으로 커서를 옮긴 뒤 글자를 넣는다. 키 이벤트가 아니라 Enter 로 전송되지 않는다.</summary>
    public async Task<bool> TypeIntoAsync(string[] selectors, string text)
    {
        var focused = await EvalBoolAsync($$"""
            (() => {
              for (const s of {{Js(selectors)}}) {
                const el = document.querySelector(s);
                if (!el) continue;
                el.focus();
                const range = document.createRange();
                range.selectNodeContents(el);
                range.collapse(false);
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(range);
                return true;
              }
              return false;
            })()
            """);
        if (!focused)
            return false;

        await Core.CallDevToolsProtocolMethodAsync("Input.insertText", new JsonObject { ["text"] = text }.ToJsonString());
        return true;
    }

    public async Task PressKeyAsync(string key, int keyCode)
    {
        foreach (var type in new[] { "keyDown", "keyUp" })
        {
            var args = new JsonObject
            {
                ["type"] = type,
                ["key"] = key,
                ["code"] = key,
                ["windowsVirtualKeyCode"] = keyCode,
                ["nativeVirtualKeyCode"] = keyCode,
            };
            if (type == "keyDown" && key == "Enter")
                args["text"] = "\r";
            await Core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", args.ToJsonString());
        }
    }

    /// <summary>숨은 파일 입력칸에 로컬 파일을 넣는다(Playwright 의 setInputFiles 와 같은 일).</summary>
    public async Task<bool> SetFilesAsync(string[] selectors, IReadOnlyList<string> files)
    {
        foreach (var selector in selectors)
        {
            var args = new JsonObject
            {
                ["expression"] = $"document.querySelector({Js(selector)})",
                ["returnByValue"] = false,
            };
            var reply = JsonNode.Parse(await Core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", args.ToJsonString()));
            var objectId = reply?["result"]?["objectId"]?.GetValue<string>();
            if (objectId is null)
                continue;

            var setArgs = new JsonObject
            {
                ["objectId"] = objectId,
                ["files"] = new JsonArray(files.Select(f => (JsonNode)Path.GetFullPath(f)).ToArray()),
            };
            await Core.CallDevToolsProtocolMethodAsync("DOM.setFileInputFiles", setArgs.ToJsonString());
            return true;
        }
        return false;
    }

    // ---------- 받기 ----------

    /// <summary>
    /// 다음에 시작되는 다운로드를 가로채 지정한 경로로 저장한다. trigger 안에서 다운로드 버튼을 누른다.
    /// 확장자는 사이트가 준 파일 이름을 따른다.
    /// </summary>
    public async Task<string?> CaptureDownloadAsync(string pathWithoutExtension, Func<Task<bool>> trigger, TimeSpan timeout, CancellationToken ct)
    {
        var finished = new TaskCompletionSource<string?>();

        void OnDownload(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            var ext = Path.GetExtension(e.ResultFilePath);
            if (string.IsNullOrEmpty(ext))
                ext = e.DownloadOperation.MimeType.Contains("video") ? ".mp4" : ".png";
            var target = pathWithoutExtension + ext;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            e.ResultFilePath = target;
            e.Handled = true; // 브라우저 다운로드 창을 띄우지 않는다.
            e.DownloadOperation.StateChanged += (_, _) =>
            {
                switch (e.DownloadOperation.State)
                {
                    case CoreWebView2DownloadState.Completed:
                        finished.TrySetResult(target);
                        break;
                    case CoreWebView2DownloadState.Interrupted:
                        finished.TrySetResult(null);
                        break;
                }
            };
        }

        Core.DownloadStarting += OnDownload;
        try
        {
            if (!await trigger())
                return null;
            var winner = await Task.WhenAny(finished.Task, Task.Delay(timeout, ct));
            return winner == finished.Task ? finished.Task.Result : null;
        }
        finally
        {
            Core.DownloadStarting -= OnDownload;
        }
    }

    /// <summary>페이지 안에서 로그인 쿠키를 실어 주소를 받아 와 파일로 쓴다(blob: 주소도 된다).</summary>
    public async Task<string?> FetchToFileAsync(string url, string pathWithoutExtension)
    {
        var node = await EvalAsync($$"""
            (async () => {
              const r = await fetch({{Js(url)}}, { credentials: 'include' });
              if (!r.ok) return null;
              const blob = await r.blob();
              const data = await new Promise(res => {
                const fr = new FileReader();
                fr.onload = () => res(fr.result);
                fr.readAsDataURL(blob);
              });
              return { type: blob.type, data: data.substring(data.indexOf(',') + 1) };
            })()
            """);
        if (node is null)
            return null;

        var type = node["type"]?.GetValue<string>() ?? "";
        var ext = type switch
        {
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "video/mp4" => ".mp4",
            "image/gif" => ".gif",
            _ when type.StartsWith("video") => ".mp4",
            _ => ".png",
        };
        var target = pathWithoutExtension + ext;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllBytesAsync(target, Convert.FromBase64String(node["data"]!.GetValue<string>()));
        return target;
    }
}
