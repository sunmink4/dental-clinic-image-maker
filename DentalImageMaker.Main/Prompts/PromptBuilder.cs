using System.Text;
using DentalImageMaker.Main.Automation;

namespace DentalImageMaker.Main.Prompts;

public static class PromptBuilder
{
    /// <summary>
    /// 이미지: "Generate an image. [비율] [화풍] [장면 앞] [입력] [장면 뒤] [카메라] [규칙]"
    /// 영상·이어서 보내기(수정 요청)는 머리말 없이 입력 위주로 만든다.
    /// "Generate an image." 를 붙이면 Gemini·ChatGPT 둘 다 글 대신 그림으로 답한다.
    /// </summary>
    public static string Build(
        GenerationKind kind,
        bool continuing,
        PromptPreset? style,
        PromptPreset? scene,
        AspectRatio? aspect,
        IEnumerable<PromptRule> rules,
        string freeText,
        IEnumerable<CameraOption?>? camera = null)
    {
        var parts = new List<string>();

        if (!continuing)
        {
            if (kind == GenerationKind.Image)
                parts.Add("Generate an image.");
            else
                parts.Add("동영상을 만들어줘.");
            Add(parts, aspect?.Text);
            Add(parts, style?.Before);
        }

        var body = new StringBuilder();
        body.Append(scene?.Before ?? "");
        body.Append(freeText.Trim());
        if (!string.IsNullOrEmpty(scene?.After))
        {
            if (body.Length > 0 && !char.IsWhiteSpace(body[^1]) && !scene.After.StartsWith('"'))
                body.Append(' ');
            body.Append(scene.After);
        }
        Add(parts, body.ToString());

        // 카메라 구도는 이어서 보내기(수정 요청)에도 붙인다. "탑뷰로 바꿔줘" 같은 요청이 여기서 나온다.
        foreach (var option in camera ?? [])
            Add(parts, option?.Text);

        foreach (var rule in rules)
            Add(parts, rule.Text);

        return string.Join(" ", parts).Trim();
    }

    private static void Add(List<string> parts, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add(text.Trim());
    }
}
