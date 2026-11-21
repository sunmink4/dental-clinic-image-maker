using System.Windows;
using System.Windows.Media;

namespace DentalImageMaker.Forms;

/// <summary>
/// 색만 다른 선택지(가운 색상 · 스크럽 색상 · 위생사 복장 · 체어 시트 색)는 예제 그림 대신 색 견본을 보여 준다.
/// 예제 그림이 있으면 그쪽이 먼저다(<see cref="ExampleStore.Load"/>).
/// </summary>
public static class ColorSwatch
{
    // 프롬프트 문장(PromptLibrary)의 색 이름에 맞춘 값.
    private static readonly Dictionary<string, string> Colors = new()
    {
        ["coatcolor.white"] = "#FFFFFF",
        ["coatcolor.ivory"] = "#F3EBD8",
        ["coatcolor.skyblue"] = "#A9D4F0",
        ["coatcolor.navy"] = "#1F2E4D",
        ["coatcolor.mint"] = "#BFE8D8",
        ["coatcolor.pink"] = "#F6C6D2",
        ["coatcolor.lavender"] = "#C9B8E6",
        ["coatcolor.gray"] = "#C8CCD2",
        ["coatcolor.black"] = "#1B1B1F",

        ["scrubs.navy"] = "#1F2E4D",
        ["scrubs.skyblue"] = "#8EC3E8",
        ["scrubs.mint"] = "#9ED9C4",
        ["scrubs.teal"] = "#1E8A8A",
        ["scrubs.green"] = "#3F7F5C",
        ["scrubs.lavender"] = "#B9A5DE",
        ["scrubs.pink"] = "#F2B8C8",
        ["scrubs.wine"] = "#6E1F33",
        ["scrubs.gray"] = "#4A4F57",
        ["scrubs.black"] = "#1B1B1F",
        ["scrubs.white"] = "#FFFFFF",

        ["hygoutfit.mint"] = "#9ED9C4",
        ["hygoutfit.green"] = "#3F7F5C",
        ["hygoutfit.navy"] = "#1F2E4D",
        ["hygoutfit.lavender"] = "#B9A5DE",
        ["hygoutfit.pink"] = "#F2B8C8",
        ["hygoutfit.skyblue"] = "#8EC3E8",
        ["hygoutfit.black"] = "#1B1B1F",
        ["hygoutfit.white"] = "#FFFFFF",

        // 체어 시트 색
        ["chair.skyblue"] = "#A9D4F0",
        ["chair.mint"] = "#AEE0CC",
        ["chair.teal"] = "#1E8A8A",
        ["chair.navy"] = "#1F2E4D",
        ["chair.blue"] = "#0047AB",
        ["chair.white"] = "#FFFFFF",
        ["chair.gray"] = "#C8CCD2",
        ["chair.charcoal"] = "#2A2C30",
        ["chair.beige"] = "#EADBC0",
        ["chair.brown"] = "#8A5A3C",
        ["chair.pink"] = "#F6C6D2",
        ["chair.yellow"] = "#F7E39A",
        ["chair.orange"] = "#F28C28",
        ["chair.red"] = "#7A1F2E",
        ["chair.purple"] = "#B9A5DE",
    };

    private static readonly Dictionary<string, ImageSource> Cache = new();

    /// <summary>색 견본 그림. 색이 정해진 선택지가 아니면 null.</summary>
    public static ImageSource? Get(string key)
    {
        if (!Colors.TryGetValue(key, out var hex))
            return null;
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var color = (Color)ColorConverter.ConvertFromString(hex);
        var group = new DrawingGroup();
        // 칸이 UniformToFill 로 그리므로 정사각형 하나를 꽉 채운다. 아래쪽을 살짝 어둡게 해 천 같은 느낌만 준다.
        var fill = new LinearGradientBrush(color, Shade(color, 0.86), new Point(0, 0), new Point(0, 1));
        group.Children.Add(new GeometryDrawing(fill, null, new RectangleGeometry(new Rect(0, 0, 100, 100))));
        // 흰색 · 아이보리처럼 밝은 색은 테두리가 없으면 바탕과 구분이 안 된다.
        var edge = new Pen(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), 2);
        group.Children.Add(new GeometryDrawing(null, edge, new RectangleGeometry(new Rect(1, 1, 98, 98))));
        group.Freeze();

        var image = new DrawingImage(group);
        image.Freeze();
        Cache[key] = image;
        return image;
    }

    private static Color Shade(Color c, double f) =>
        Color.FromRgb((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f));
}
