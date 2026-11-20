using System.Text;

namespace DentalImageMaker.Main.Prompts;

/// <summary>
/// 치식 차트의 상태 하나. Short 는 차트 칸에 찍는 한두 글자, Color 는 칸 배경색(#RRGGBB),
/// Text 는 프롬프트에 들어가는 영어 명사구("치아 이름: 묘사" 꼴로 붙는다).
/// </summary>
public sealed record ToothCondition(string Key, string Name, string Short, string Color, string Text)
{
    public override string ToString() => Name;
}

/// <summary>
/// 치과 차트(FDI 두 자리 번호) — 영구치 32개.
/// 첫 자리: 1 위 오른쪽, 2 위 왼쪽, 3 아래 왼쪽, 4 아래 오른쪽(환자 기준). 둘째 자리: 1 중절치 … 8 사랑니.
/// 이미지 모델은 번호를 잘 모르므로 프롬프트에는 "lower left first molar (#36)" 처럼 이름을 같이 적는다.
/// </summary>
public static class DentalChart
{
    public static readonly ToothCondition Normal = new("", "정상", "", "#3A3A3A", "");

    public static IReadOnlyList<ToothCondition> Conditions { get; } =
    [
        Normal,
        new("caries", "충치", "C", "#8B5A2B", "visible tooth decay, brown and black carious spots in the grooves"),
        new("deep_caries", "깊은 충치", "C2", "#5A3215", "a deep cavity, a dark brown-black hole of soft decayed dentin surrounded by chalky white enamel"),
        new("drilling", "충치 제거 중", "삭", "#B03A2E", "being drilled right now, the thin bur of a high-speed handpiece cutting out the brown decay with a fine water spray"),
        new("prepared", "삭제된 와동", "P", "#C0705A", "a freshly prepared clean cavity with the decay removed, ready for filling"),
        new("resin", "레진", "R", "#D9D2C0", "a tooth-colored white resin filling"),
        new("gold_inlay", "금 인레이", "GI", "#C9A227", "a gold inlay embedded in the chewing surface (not a crown)"),
        new("amalgam", "아말감", "Am", "#7D8590", "an old dark silver amalgam filling"),
        new("gold_crown", "금 크라운", "GC", "#E0B000", "a shiny full gold crown"),
        new("zirconia", "지르코니아 크라운", "Zr", "#F2F2F2", "a natural white zirconia crown"),
        new("pfm", "PFM (금속 테두리)", "PFM", "#B8B8B8", "a porcelain crown with a thin dark metal line at the gumline"),
        new("root_canal", "신경치료 중", "RCT", "#7A4FA0", "an opening in the top for root canal treatment with a small endodontic file inserted"),
        new("temp", "임시 충전", "T", "#E8E0F0", "a white temporary filling"),
        new("implant", "임플란트", "Im", "#4F8FBF", "a dental implant with a crown"),
        new("implant_fixture", "임플란트 지대주", "Ab", "#2E6A99", "an implant site with a metal abutment post sticking out of the gum, no crown yet"),
        new("missing", "빠짐 (결손)", "×", "#151515", "missing tooth, an empty gap with healed gum"),
        new("extract", "발치 예정", "Ex", "#D04040", "a broken, badly decayed tooth to be extracted"),
        new("fracture", "깨짐", "Fx", "#E07B39", "a chipped tooth with a broken edge"),
        new("tartar", "치석", "Ta", "#C8B560", "yellowish tartar along the gumline"),
        new("bracket", "교정 브라켓", "Br", "#9AA9B8", "a metal orthodontic bracket with an archwire"),
        new("sealant", "실란트", "S", "#E8F4F8", "a clear sealant on the grooves"),
    ];

    public static readonly int[] UpperRight = [18, 17, 16, 15, 14, 13, 12, 11];
    public static readonly int[] UpperLeft = [21, 22, 23, 24, 25, 26, 27, 28];
    public static readonly int[] LowerRight = [48, 47, 46, 45, 44, 43, 42, 41];
    public static readonly int[] LowerLeft = [31, 32, 33, 34, 35, 36, 37, 38];

    private static readonly string[] TypeNames =
    [
        "", "central incisor", "lateral incisor", "canine", "first premolar", "second premolar",
        "first molar", "second molar", "third molar (wisdom tooth)",
    ];

    private static readonly string[] TypeNamesKo = ["", "중절치", "측절치", "송곳니", "제1소구치", "제2소구치", "제1대구치", "제2대구치", "사랑니"];

    /// <summary>예: 36 → "lower left first molar (#36)".</summary>
    public static string EnglishName(int fdi)
    {
        var quadrant = (fdi / 10) switch
        {
            1 => "upper right",
            2 => "upper left",
            3 => "lower left",
            _ => "lower right",
        };
        return $"{quadrant} {TypeNames[fdi % 10]} (#{fdi})";
    }

    /// <summary>예: 36 → "36 아래 왼쪽 제1대구치".</summary>
    public static string KoreanName(int fdi)
    {
        var quadrant = (fdi / 10) switch
        {
            1 => "위 오른쪽",
            2 => "위 왼쪽",
            3 => "아래 왼쪽",
            _ => "아래 오른쪽",
        };
        return $"{fdi} {quadrant} {TypeNamesKo[fdi % 10]}";
    }

    /// <summary>정상이 아닌 치아만 모아 한 문장으로 만든다. 같은 상태는 묶는다. 없으면 빈 문자열.</summary>
    public static string BuildSentence(IEnumerable<(int Tooth, ToothCondition Condition)> teeth)
    {
        var groups = teeth
            .Where(t => !string.IsNullOrEmpty(t.Condition.Text))
            .GroupBy(t => t.Condition)
            .ToList();
        if (groups.Count == 0)
            return "";

        var sb = new StringBuilder(
            "Dental chart of the patient's teeth (FDI numbering; left/right are the patient's own sides). Draw each listed tooth exactly as described; all other teeth are healthy and natural. ");
        foreach (var g in groups)
        {
            var names = g.Select(t => EnglishName(t.Tooth)).ToList();
            var subject = names.Count == 1 ? names[0] : string.Join(", ", names[..^1]) + " and " + names[^1];
            sb.Append(char.ToUpperInvariant(subject[0])).Append(subject[1..]).Append(": ").Append(g.Key.Text).Append(". ");
        }
        return sb.ToString().TrimEnd();
    }
}
