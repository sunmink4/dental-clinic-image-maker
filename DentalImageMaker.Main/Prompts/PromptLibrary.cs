namespace DentalImageMaker.Main.Prompts;

/// <summary>
/// 프롬프트 조각. 장면 프리셋은 Before + (사용자 입력) + After 로 끼워 넣는다.
/// 출처는 docs/gemini-history.md (Gemini 대화 기록에서 다듬어진 프롬프트).
/// </summary>
/// <param name="StaffFacesHidden">의료진 얼굴이 화면 밖인 장면. 고르면 의사·위생사의 얼굴·머리 묘사 문장을 뺀다(가운·스크럽은 남김).</param>
public sealed record PromptPreset(string Name, string Before, string After = "", string Hint = "", bool StaffFacesHidden = false)
{
    public override string ToString() => Name;
}

/// <summary>TreatmentOnly: 진료 중 장면에만 맞는 규칙(핸드피스·입 벌림 등). 상담·양치 같은 상황에서는 자동으로 빠진다.</summary>
public sealed record PromptRule(string Name, string Text, bool DefaultOn, bool TreatmentOnly = false);

public sealed record AspectRatio(string Name, string Text)
{
    public override string ToString() => Name;
}

/// <summary>
/// 예제 그림 카드로 고르는 선택지 하나(카메라 구도, 주인공 설정).
/// Key 는 예제 그림 파일 이름, Text 는 프롬프트에 그대로 붙는 영어 문장(비어 있으면 지정 안 함).
/// </summary>
public sealed record PromptOption(string Key, string Name, string Text)
{
    public override string ToString() => Name;

    public bool IsMale => Key.StartsWith("gender.m", StringComparison.Ordinal);
}

public static class PromptLibrary
{
    // ---------- 치과의사 (주인공) ----------
    // 기본값은 첫 번째 항목: 20대 후반 한국 여성, 낮은 포니테일, 흰 가운 + 네이비 스크럽, 마스크 착용.
    // 예제 그림은 dental-clinic-clicker 의 주인공 초상(hero_f1~9, hero_m1~9).

    public static IReadOnlyList<PromptOption> DentistGenders { get; } =
    [
        new("gender.f20", "20대 후반 여성", "The main character is the dentist: a young Korean woman in her late 20s with a natural Korean face."),
        new("gender.f30", "30대 여성", "The main character is the dentist: a Korean woman in her 30s with a natural Korean face."),
        new("gender.f40", "40대 여성", "The main character is the dentist: a Korean woman in her 40s with a warm, mature look."),
        new("gender.m20", "20대 후반 남성", "The main character is the dentist: a young Korean man in his late 20s with a natural Korean face."),
        new("gender.m30", "30대 남성", "The main character is the dentist: a Korean man in his 30s with a natural Korean face."),
        new("gender.m40", "40대 남성", "The main character is the dentist: a Korean man in his 40s with a calm, experienced look."),
    ];

    public static IReadOnlyList<PromptOption> FemaleHairs { get; } =
    [
        new("hair.f_low_ponytail", "낮은 포니테일 + 앞머리", "{who}'s hair: dark hair in a low ponytail with soft see-through bangs."),
        new("hair.f_suyeon_bob", "갈색 단발 가르마 (안수연)", "{who}'s hair: a brown bob cut with a side part."),
        new("hair.f_bob", "단발 보브", "{who}'s hair: a short chin-length bob cut."),
        new("hair.f_long_halfup", "긴 생머리 반묶음", "{who}'s hair: long straight black hair tied half up."),
        new("hair.f_wavy", "웨이브 어깨 길이 (갈색)", "{who}'s hair: wavy shoulder-length brown hair."),
        new("hair.f_pixie", "숏컷", "{who}'s hair: a short pixie cut."),
        new("hair.f_low_bun", "낮은 올림머리 (번)", "{who}'s hair: hair neatly tied in a low bun."),
        new("hair.f_layered", "짧은 레이어드", "{who}'s hair: short layered hair."),
        new("hair.f_high_ponytail", "높은 포니테일", "{who}'s hair: a high ponytail."),
        new("hair.f_long_bangs", "긴 앞머리 + 묶음", "{who}'s hair: long bangs with the hair tied back."),
    ];

    public static IReadOnlyList<PromptOption> MaleHairs { get; } =
    [
        new("hair.m_short", "짧고 단정한 머리", "{who}'s hair: short neat black hair."),
        new("hair.m_side_part", "옆가르마 앞머리", "{who}'s hair: a soft side-parted fringe."),
        new("hair.m_undercut", "슬릭백 언더컷", "{who}'s hair: a slicked-back black undercut."),
        new("hair.m_wavy", "웨이브 중간 길이", "{who}'s hair: wavy medium-length hair."),
        new("hair.m_buzz", "버즈컷", "{who}'s hair: a buzz cut."),
        new("hair.m_center_part", "가운데 가르마", "{who}'s hair: a center part."),
        new("hair.m_gray_temples", "짧은 머리 + 옆머리 흰머리", "{who}'s hair: short hair with slight gray at the temples."),
        new("hair.m_curly", "짧은 곱슬머리", "{who}'s hair: short curly hair."),
        new("hair.m_long_bangs", "긴 앞머리", "{who}'s hair: long bangs falling over the forehead."),
    ];

    // 복장은 가운 종류 · 가운 색상 · 스크럽 색상을 따로 고른다(첫 항목이 기본: 긴 흰 가운 + 네이비 스크럽).
    public const string NoCoatKey = "coat.none";

    public static IReadOnlyList<PromptOption> DentistCoats { get; } =
    [
        new("coat.long", "긴 닥터 가운 (무릎)", "{who} wears a long knee-length doctor's coat with lapels, buttoned, over scrubs."),
        new("coat.short", "짧은 가운 (허리)", "{who} wears a short hip-length dental gown over scrubs."),
        new("coat.short_sleeve", "반팔 가운", "{who} wears a short-sleeved doctor's coat over scrubs."),
        new("coat.mandarin", "차이나 칼라 가운", "{who} wears a modern dental tunic gown with a mandarin stand-up collar and side buttons."),
        new("coat.open", "가운 단추 풀어 입음", "{who} wears a doctor's coat open and unbuttoned over scrubs."),
        new("coat.surgical", "일회용 수술 가운", "{who} wears a disposable surgical gown tied at the back over scrubs."),
        new(NoCoatKey, "가운 없음 (스크럽만)", "{who} wears scrubs only, without a coat."),
    ];

    public static IReadOnlyList<PromptOption> DentistCoatColors { get; } =
    [
        new("coatcolor.white", "흰색", "{who}'s coat is crisp white."),
        new("coatcolor.ivory", "아이보리", "{who}'s coat is soft ivory."),
        new("coatcolor.skyblue", "하늘색", "{who}'s coat is light sky blue."),
        new("coatcolor.navy", "네이비", "{who}'s coat is dark navy blue."),
        new("coatcolor.mint", "민트", "{who}'s coat is pale mint green."),
        new("coatcolor.pink", "핑크", "{who}'s coat is pastel pink."),
        new("coatcolor.lavender", "라벤더", "{who}'s coat is lavender."),
        new("coatcolor.gray", "그레이", "{who}'s coat is light gray."),
        new("coatcolor.black", "블랙", "{who}'s coat is black."),
    ];

    public static IReadOnlyList<PromptOption> DentistScrubs { get; } =
    [
        new("scrubs.navy", "네이비", "{who}'s scrubs are navy blue."),
        new("scrubs.skyblue", "하늘색", "{who}'s scrubs are light blue."),
        new("scrubs.mint", "민트", "{who}'s scrubs are mint green."),
        new("scrubs.teal", "청록 (틸)", "{who}'s scrubs are teal."),
        new("scrubs.green", "초록 수술복", "{who}'s scrubs are green surgical scrubs."),
        new("scrubs.lavender", "라벤더", "{who}'s scrubs are lavender."),
        new("scrubs.pink", "핑크", "{who}'s scrubs are soft pink."),
        new("scrubs.wine", "와인", "{who}'s scrubs are wine red."),
        new("scrubs.gray", "그레이", "{who}'s scrubs are charcoal gray."),
        new("scrubs.black", "블랙", "{who}'s scrubs are black."),
        new("scrubs.white", "화이트", "{who}'s scrubs are white."),
    ];

    public static IReadOnlyList<PromptOption> DentistMasks { get; } =
    [
        new("mask.on", "마스크 착용", "{who} wears a plain white surgical mask over the nose and mouth."),
        new("mask.chin", "마스크 턱 아래로", "{who}'s surgical mask is pulled down under the chin, so the face is visible."),
        new("mask.off", "마스크 없음", "{who} wears no mask; the face is fully visible."),
    ];

    public static IReadOnlyList<PromptOption> DentistAccessories { get; } =
    [
        new("", "없음", ""),
        new("acc.thin_glasses", "얇은 안경", "{who} wears thin metal-frame glasses."),
        new("acc.round_glasses", "둥근 안경", "{who} wears round glasses."),
        new("acc.loupe", "루페 착용", "{who} wears dental surgical loupes: dark frames with two small cylindrical magnifying telescopes."),
        new("acc.earrings", "작은 금색 링 귀걸이", "{who} wears small gold hoop earrings."),
        new("acc.stethoscope", "청진기 + 이름표", "{who} has a stethoscope around the neck and a blank name badge."),
    ];


    // ---------- 치과위생사 ----------
    // 머리·복장·마스크 문장의 {who} 는 "The dental hygienist" 로 바뀐다. 예제는 hyg_1~20 초상.

    public static IReadOnlyList<PromptOption> HygienistGenders { get; } =
    [
        new("hyg.f20", "20대 여성", "A Korean dental hygienist assists: a young Korean woman in her 20s."),
        new("hyg.f30", "30대 여성", "A Korean dental hygienist assists: a Korean woman in her 30s."),
        new("hyg.f40", "40대 여성", "A Korean dental hygienist assists: a Korean woman in her 40s with a warm smile."),
        new("hyg.f50", "50대 여성 (실장)", "A Korean dental hygienist assists: an experienced Korean head hygienist in her 50s."),
        new("hyg.m20", "20대 남성", "A Korean dental hygienist assists: a young Korean man in his 20s."),
        new("hyg.m30", "30대 남성", "A Korean dental hygienist assists: a Korean man in his 30s."),
    ];

    public static IReadOnlyList<PromptOption> HygienistFemaleHairs { get; } =
    [
        new("hyghair.low_ponytail", "낮은 포니테일", "{who}'s hair: dark hair in a neat low ponytail."),
        new("hyghair.bob_bangs", "단발 + 앞머리", "{who}'s hair: a short bob with soft bangs."),
        new("hyghair.high_ponytail", "높은 포니테일", "{who}'s hair: a high ponytail."),
        new("hyghair.low_bun", "낮은 올림머리 (번)", "{who}'s hair: a tidy low bun."),
        new("hyghair.half_up", "반묶음", "{who}'s hair: a half-up hairstyle."),
        new("hyghair.side_braid", "옆으로 땋은 머리", "{who}'s hair: a side braid."),
        new("hyghair.short_perm", "짧은 파마", "{who}'s hair: short permed hair."),
        new("hyghair.pixie", "숏컷", "{who}'s hair: a pixie cut."),
        new("hyghair.long_ponytail", "긴 포니테일", "{who}'s hair: a long ponytail."),
        new("hyghair.twin_buns", "양쪽 낮은 번", "{who}'s hair: twin low buns."),
        new("hyghair.nape_wavy", "웨이브 목뒤 묶음", "{who}'s hair: wavy hair tied at the nape."),
        new("hyghair.long_bangs", "긴 앞머리 생머리", "{who}'s hair: long straight hair with long bangs."),
    ];

    public static IReadOnlyList<PromptOption> HygienistOutfits { get; } =
    [
        new("hygoutfit.mint", "민트 스크럽", "{who} wears mint scrubs."),
        new("hygoutfit.green", "초록 스크럽", "{who} wears green scrubs."),
        new("hygoutfit.navy", "네이비 스크럽", "{who} wears navy scrubs."),
        new("hygoutfit.lavender", "라벤더 스크럽", "{who} wears lavender scrubs."),
        new("hygoutfit.pink", "핑크 스크럽", "{who} wears soft pink scrubs."),
        new("hygoutfit.skyblue", "하늘색 스크럽", "{who} wears sky blue scrubs."),
        new("hygoutfit.black", "블랙 스크럽", "{who} wears black scrubs."),
        new("hygoutfit.white", "흰 유니폼", "{who} wears a white dental uniform."),
    ];

    public static IReadOnlyList<PromptOption> HygienistAccessories { get; } =
    [
        new("", "없음", ""),
        new("hygacc.glasses", "안경", "{who} wears thin glasses."),
        new("hygacc.round_glasses", "둥근 안경", "{who} wears round glasses."),
        new("hygacc.cap", "수술 모자", "{who} wears a light blue surgical cap."),
        new("hygacc.faceshield", "페이스 쉴드 (올림)", "{who} has a clear face shield pushed up on the head."),
        new("hygacc.lanyard", "목걸이 이름표", "{who} wears a lanyard with a blank name badge."),
    ];

    // ---------- 환자 ----------
    // {who} 는 "The patient" 로 바뀐다. 모든 칸의 첫 항목은 '지정 안 함'(문장 없음).

    /// <summary>첨부 이미지의 환자를 그대로 쓰는 선택지. 고르면 헤어·옷 칸은 무시한다.</summary>
    public const string PatientFromReferenceKey = "patient.reference";

    public static IReadOnlyList<PromptOption> PatientGenders { get; } =
    [
        new("", "지정 안 함", ""),
        new(PatientFromReferenceKey, "첨부 이미지의 환자 (옷 포함)",
            "The patient must be exactly the same person as in the attached reference image: keep the identical face, age, hairstyle, body type and the exact same clothing and its colors. Do not change the patient's outfit."),
        new("patient.child_f", "어린이 여자 (7–10세)", "The patient is a Korean girl about 7–10 years old."),
        new("patient.child_m", "어린이 남자 (7–10세)", "The patient is a Korean boy about 7–10 years old."),
        new("patient.teen_f", "10대 여학생", "The patient is a Korean teenage girl."),
        new("patient.teen_m", "10대 남학생", "The patient is a Korean teenage boy."),
        new("patient.f20", "20대 여성", "The patient is a young Korean woman in her 20s."),
        new("patient.m20", "20대 남성", "The patient is a young Korean man in his 20s."),
        new("patient.f30", "30–40대 여성", "The patient is a Korean woman in her 30s or 40s."),
        new("patient.m30", "30–40대 남성", "The patient is a Korean man in his 30s or 40s."),
        new("patient.f50", "50–60대 여성", "The patient is a middle-aged Korean woman in her 50s or 60s."),
        new("patient.m50", "50–60대 남성", "The patient is a middle-aged Korean man in his 50s or 60s."),
        new("patient.grandma", "70대 할머니", "The patient is an elderly Korean grandmother in her 70s with gray hair."),
        new("patient.grandpa", "70대 할아버지", "The patient is an elderly Korean grandfather in his 70s with gray hair."),
    ];

    public static IReadOnlyList<PromptOption> PatientHairs { get; } =
    [
        new("", "지정 안 함", ""),
        new("phair.long", "긴 생머리", "{who} has long straight dark hair spread on the headrest."),
        new("phair.bob", "단발", "{who} has a short bob haircut."),
        new("phair.tied", "묶은 머리", "{who}'s hair is tied back."),
        new("phair.wavy", "웨이브 긴 머리", "{who} has long wavy brown hair."),
        new("phair.short", "짧은 머리 (남)", "{who} has short neat black hair."),
        new("phair.perm", "뽀글 파마", "{who} has short permed hair."),
        new("phair.gray", "흰머리", "{who} has short gray hair."),
    ];

    public static IReadOnlyList<PromptOption> PatientOutfits { get; } =
    [
        new("", "지정 안 함", ""),
        new("poutfit.navy", "네이비 긴팔 + 어두운 바지", "{who} wears a dark navy long-sleeved top and dark trousers."),
        new("poutfit.knit", "아이보리 니트", "{who} wears a cozy ivory knit sweater and jeans."),
        new("poutfit.hoodie", "후드티", "{who} wears a gray hoodie."),
        new("poutfit.shirt", "셔츠 (직장인)", "{who} wears a white office shirt and slacks."),
        new("poutfit.uniform", "교복", "{who} wears a Korean school uniform."),
        new("poutfit.cardigan", "카디건", "{who} wears a soft beige cardigan."),
    ];

    public static IReadOnlyList<PromptOption> PatientCovers { get; } =
    [
        new("", "지정 안 함", ""),
        new("pcover.bib_green", "초록 bib", "A green disposable dental bib covers {who}'s chest."),
        new("pcover.bib_blue", "하늘색 bib", "A light blue disposable dental bib covers {who}'s chest."),
        new("pcover.drape", "초록 소공포 + bib", "{who}'s face is covered by a green dental drape with an opening only around the mouth, and a dental bib covers the chest."),
        new("pcover.glasses", "환자 보안경", "{who} wears clear protective safety glasses."),
    ];

    public static IReadOnlyList<PromptOption> PatientStates { get; } =
    [
        new("", "지정 안 함", ""),
        new("pstate.wide", "눈 감고 입 크게", "{who} lies fully reclined with eyes closed and mouth opened as wide as possible."),
        new("pstate.grimace", "눈 질끈 감고 찡그림", "{who} lies fully reclined, eyes squeezed tightly shut, brows drawn together in a slight frown of discomfort, mouth opened wide."),
        new("pstate.tense", "긴장 (손 꽉 쥠)", "{who} looks tense, eyebrows raised, hands tightly clasped on the stomach, mouth wide open."),
        new("pstate.relaxed", "편안함", "{who} lies relaxed and calm with mouth open."),
        new("pstate.scared", "겁먹은 표정", "{who} looks scared with teary eyes, gripping the armrest."),
        new("pstate.looking", "눈 뜨고 의사 봄", "{who} has eyes open, looking up at the dentist."),
        new("pstate.sitting", "앉아서 상담", "{who} sits upright in the dental chair, mouth closed, listening to the dentist."),
    ];

    public static IReadOnlyList<PromptOption> PatientTeeth { get; } =
    [
        new("", "지정 안 함", ""),
        new("teeth.caries", "어금니 충치 (갈색·검은 점)", "Oral condition: the molars show visible tooth decay — brown and black carious spots in the grooves of the chewing surfaces."),
        new("teeth.deep_caries", "깊은 충치 (구멍)", "Oral condition: one molar has a deep cavity, a dark brown-black hole of soft decayed dentin on the chewing surface, surrounded by chalky white enamel."),
        new("teeth.gold", "금 인레이", "Oral condition: several molars have shiny gold inlays embedded in the middle of their chewing surfaces — small gold patches inside otherwise white teeth, clearly visible. Not gold crowns covering whole teeth, not braces."),
        new("teeth.resin", "흰 레진 충전", "Oral condition: some molars have tooth-colored white resin fillings."),
        new("teeth.amalgam", "은색 아말감", "Oral condition: some molars have old dark silver amalgam fillings."),
        new("teeth.braces", "교정 장치", "Oral condition: {who} wears metal orthodontic braces with brackets and wires."),
        new("teeth.tartar", "치석·잇몸 염증", "Oral condition: yellowish tartar along the gumline and slightly red, swollen gums."),
        new("teeth.healthy", "건강한 치아", "Oral condition: clean, healthy white teeth and pink gums."),
    ];

    // ---------- 치과 상황 ----------
    // '진료 중'이 아닌 상황(situ.* 중 TreatmentSituationKey 외)을 고르면 진료용 규칙이 빠진다.
    public const string TreatmentSituationKey = "situ.treatment";

    public static IReadOnlyList<PromptOption> Situations { get; } =
    [
        new("", "지정 안 함", ""),
        new(TreatmentSituationKey, "진료 중", "Situation: dental treatment in progress in the reclined dental chair."),
        new("situ.consult", "상담 (모니터로 엑스레이 설명)", "Situation: consultation — the patient sits upright in the dental chair while the dentist, seated beside them, points at a panoramic X-ray on a monitor and explains the treatment plan; no instruments in the mouth."),
        new("situ.rinse", "타구대에서 양치", "Situation: the patient sits up in the dental chair, leans toward the small round spittoon (cuspidor) bowl attached to the dental unit, and rinses the mouth with water from a small paper cup; the dentist waits beside."),
        new("situ.reception", "접수·대기실", "Situation: at the clinic reception desk, a staff member greets the patient who is checking in; waiting room sofa in the background."),
        new("situ.guide", "체어로 안내", "Situation: a dental hygienist guides the patient to sit down in the dental chair and puts a bib on them."),
        new("situ.xray", "파노라마 엑스레이 촬영", "Situation: the patient stands inside a panoramic dental X-ray machine, biting on the bite block, wearing a lead apron; the hygienist adjusts the position."),
        new("situ.anesthesia", "마취 주사", "Situation: the dentist gently gives a local anesthetic injection into the gum with a dental syringe while the patient grips the armrest."),
        new("situ.intraoral_cam", "구강 카메라 촬영", "Situation: the hygienist takes pictures inside the patient's mouth with a pen-shaped intraoral camera; the teeth appear on a monitor."),
        new("situ.impression", "본뜨기", "Situation: an impression tray filled with pink alginate is pressed onto the patient's teeth."),
        new("situ.mirror_check", "진료 후 손거울 확인", "Situation: after treatment, the patient sits up and checks their teeth in a hand mirror, smiling, while the dentist explains."),
        new("situ.brushing_edu", "칫솔질 교육", "Situation: the hygienist demonstrates correct tooth brushing on a large tooth model to the patient."),
        new("situ.child_calm", "아이 달래기", "Situation: the dentist kneels to eye level and comforts a nervous child patient before treatment, smiling."),
        new("situ.payment", "수납·예약", "Situation: the patient pays and books the next appointment at the reception counter."),
        new("situ.finish", "진료 끝 (bib 벗기)", "Situation: treatment is finished — the chair is raised, the dentist pulls down the mask and the hygienist removes the patient's bib."),
    ];

    /// <summary>치식 차트를 숨길 때 쓰는 이름(칸 Id 가 아니라 차트 전체).</summary>
    public const string ToothChartId = "patient.chart";

    // 상황마다 그 장면에 없는(또는 상황 문장과 부딪치는) 칸. 화면에서 숨기고 프롬프트에서도 뺀다.
    // 고른 값은 지우지 않으므로 '진료 중'으로 돌아오면 그대로 되살아난다.
    private static readonly string[] Mouth = ["mouth.frame", "mouth.view", "mouth.open", "mouth.hands"];
    private static readonly string[] Oral = ["treatment.kind", "treatment.tooth", "treatment.detail", "patient.teeth", ToothChartId, .. Mouth];
    private static readonly string[] OutOfChair = [.. Oral, "room.chair", "room.recline", "patient.cover", "patient.state"];

    public static IReadOnlyDictionary<string, string[]> SituationHiddenSlots { get; } = new Dictionary<string, string[]>
    {
        ["situ.consult"] = [.. Oral, "room.recline"],
        ["situ.rinse"] = [.. Oral, "room.recline", "patient.state"],
        ["situ.reception"] = OutOfChair,
        ["situ.payment"] = OutOfChair,
        ["situ.xray"] = OutOfChair,
        ["situ.guide"] = [.. Oral, "room.recline", "patient.state"],
        ["situ.anesthesia"] = ["treatment.kind"],
        ["situ.intraoral_cam"] = ["treatment.kind", "treatment.detail"],
        ["situ.impression"] = ["treatment.kind", "treatment.tooth", "treatment.detail"],
        ["situ.mirror_check"] = ["treatment.kind", "treatment.tooth", "treatment.detail", "room.recline", "patient.state", .. Mouth],
        ["situ.brushing_edu"] = [.. Oral, "room.recline", "patient.state"],
        ["situ.child_calm"] = [.. Oral, "patient.gender", "patient.state"],
        ["situ.finish"] = [.. Oral, "room.recline", "patient.state"],
    };

    // ---------- 진료 내용 ----------
    // 입안 장면이 잘 나오려면 '무엇을 어디에 하는지'를 구체적으로 적어야 한다.

    public static IReadOnlyList<PromptOption> Treatments { get; } =
    [
        new("", "지정 안 함", ""),
        new("treat.caries_removal", "충치 제거 (드릴)", "Treatment: cavity removal in progress — the tip of a thin bur on the high-speed handpiece is cutting into the dark brown decay inside the tooth, with a fine water spray mist around the bur and a suction tip removing the water."),
        new("treat.exam", "검진 (미러·탐침)", "Treatment: dental check-up — a dental mirror and a sharp explorer probe are inside the mouth, the explorer tip touching the molar grooves."),
        new("treat.resin_fill", "레진 충전 + 광중합", "Treatment: resin filling — white composite resin is being placed in the prepared cavity and a blue curing light is held over the tooth."),
        new("treat.scaling", "스케일링", "Treatment: scaling — an ultrasonic scaler tip removes tartar along the gumline with a fine water spray."),
        new("treat.root_canal", "신경치료", "Treatment: root canal — a small endodontic file is inserted into an opening in the top of the molar, with a rubber dam isolating the tooth."),
        new("treat.extraction", "발치", "Treatment: tooth extraction — dental forceps grip the tooth, with gauze nearby; no excessive blood."),
        new("treat.crown_prep", "크라운 삭제·본뜨기", "Treatment: crown preparation — the tooth is being shaped into a smaller stump by a diamond bur, impression material and a tray are ready nearby."),
        new("treat.implant", "임플란트", "Treatment: dental implant placement — a surgical implant motor drill works in the gum where a tooth is missing, under a green surgical drape."),
        new("treat.fluoride", "불소 도포 (어린이)", "Treatment: fluoride varnish is brushed onto the teeth with a small applicator."),
    ];

    public static IReadOnlyList<PromptOption> ToothPositions { get; } =
    [
        new("", "지정 안 함", ""),
        new("tooth.lower_right_molar", "아래 오른쪽 어금니", "The treated tooth is a lower right molar (the patient's right side)."),
        new("tooth.lower_left_molar", "아래 왼쪽 어금니", "The treated tooth is a lower left molar (the patient's left side)."),
        new("tooth.upper_right_molar", "위 오른쪽 어금니", "The treated tooth is an upper right molar, seen in the dental mirror."),
        new("tooth.upper_left_molar", "위 왼쪽 어금니", "The treated tooth is an upper left molar, seen in the dental mirror."),
        new("tooth.upper_front", "위 앞니", "The treated tooth is an upper front incisor."),
        new("tooth.lower_front", "아래 앞니", "The treated tooth is a lower front incisor."),
    ];

    public static IReadOnlyList<PromptOption> IntraoralDetails { get; } =
    [
        new("", "지정 안 함", ""),
        new("oral.mirror_suction", "미러 + 석션 + 물 분사", "Inside the mouth: a dental mirror holds the cheek aside, a suction tip removes water, droplets of water spray glisten on the teeth."),
        new("oral.rubber_dam", "러버댐", "A green rubber dam with a metal clamp isolates the treated tooth; only that tooth pokes through the dam."),
        new("oral.retractor", "개구기 (입술 벌림)", "A clear plastic cheek retractor holds the lips wide open."),
        new("oral.mirror_reflection", "미러에 비친 치아", "The treated tooth is clearly visible as a reflection in the round dental mirror."),
        new("oral.light", "강한 진료 조명", "Bright focused dental operating light illuminates the inside of the mouth; moist tissues and enamel glisten realistically."),
    ];

    // ---------- 환자 입안 ----------
    // 입안을 크게 보여 주는 장면의 구도. 화면에 어디까지 들어오는지(치아만 ~ 얼굴 전체)를 '위·아래 경계'로 적어야 지켜진다.

    /// <summary>얼굴 위쪽이 화면 밖인 범위. 이걸 고르면 입안 장면으로 보고 의료진·환자 외모 문장을 뺀다.</summary>
    public static IReadOnlySet<string> MouthCloseFrames { get; } =
        new HashSet<string> { "mframe.teeth", "mframe.lips", "mframe.nose_mouth", "mframe.nose_chin", "mframe.below_eyes" };

    public static IReadOnlyList<PromptOption> MouthFrames { get; } =
    [
        new("", "지정 안 함", ""),
        new("mframe.teeth", "치아만 꽉 차게", "Framing: macro close-up inside the open mouth — the teeth, gums and tongue fill the whole frame; only the inner edges of the lips show at the borders; no nose, no chin, no face."),
        new("mframe.lips", "입술까지 (입만)", "Framing: extreme close-up cropped tightly on the mouth only — the frame shows just the wide-open lips and teeth with a thin margin of skin around the lips; the nose, the eyes and the chin are completely outside the frame and not visible."),
        new("mframe.nose_mouth", "코~입 (턱 잘림)", "Framing: tight close-up crop — the top edge of the frame cuts through the tip of the nose and the nostrils, the bottom edge cuts just below the lower lip so the chin is cut off; the eyes and the forehead are not visible at all."),
        new("mframe.nose_chin", "코끝~턱", "Framing: close-up crop of the lower half of the face only — the top edge of the frame is at the nose, the bottom edge at the chin; the wide-open mouth is in the center; the eyes and the forehead are completely outside the frame and not visible."),
        new("mframe.below_eyes", "눈 아래~턱", "Framing: close-up crop of the lower face — the top edge of the frame cuts just under the eyes so the eyes are not visible; cheeks, nose, the wide-open mouth, the whole jaw and a bit of the neck are visible."),
        new("mframe.face", "얼굴 전체", "Framing: the patient's whole face from the forehead to the chin fills the frame, the wide-open mouth in the lower center; hair at the sides."),
        new("mframe.face_shoulder", "얼굴~어깨", "Framing: the patient's head and shoulders, the face with the wide-open mouth clearly visible; the top of the chest at the bottom edge."),
    ];

    public static IReadOnlyList<PromptOption> MouthViews { get; } =
    [
        new("", "지정 안 함", ""),
        new("mview.front", "정면 (입과 마주 봄)", "Mouth view: straight-on frontal view looking directly into the open mouth, the face symmetrical in the frame."),
        new("mview.oblique", "사선 (비스듬히 옆에서)", "Mouth view: oblique three-quarter view from the patient's right side, about 45 degrees, so the mouth opening is seen at an angle and the molars on one side are visible."),
        new("mview.dentist_above", "위에서 (의사 자리)", "Mouth view: from above, from the dentist's seat at the patient's right side, looking down into the open mouth of the reclined patient."),
        new("mview.head_12", "머리 쪽 12시 (얼굴 거꾸로)", "Mouth view: from behind the patient's head at the 12 o'clock position, looking down over the forehead into the mouth, so the face appears upside down with the chin at the top of the frame and the upper teeth closest to the camera."),
        new("mview.side", "측면 90°", "Mouth view: pure side profile at 90 degrees, showing the open mouth, lips and jaw line from the side."),
        new("mview.below", "아래에서 (턱 밑)", "Mouth view: low angle from below the chin, looking up toward the upper teeth and palate."),
    ];

    public static IReadOnlyList<PromptOption> MouthOpenings { get; } =
    [
        new("", "지정 안 함", ""),
        new("mopen.slight", "살짝 벌림", "Mouth opening: the lips are only slightly parted, a gap of about one centimeter with the front teeth just visible — the mouth is NOT opened wide."),
        new("mopen.medium", "보통", "Mouth opening: the mouth is open normally, upper and lower teeth visible with a gap of about two fingers."),
        new("mopen.wide", "크게", "Mouth opening: the mouth is opened wide, the tongue and the lower molars clearly visible."),
        new("mopen.max", "최대한 (목젖까지)", "Mouth opening: the mouth is opened as wide as physically possible — the jaw fully dropped, the tongue, the back molars and the uvula at the back of the throat are visible."),
    ];

    public static IReadOnlyList<PromptOption> MouthHands { get; } =
    [
        new("", "지정 안 함", ""),
        new("mhand.none", "손·기구 없음", "No hands and no instruments in the frame — only the patient's open mouth."),
        new("mhand.mirror", "미러 한 손", "One gloved hand enters from the side of the frame holding a round dental mirror inside the mouth, the teeth reflected in it."),
        new("mhand.mirror_explorer", "미러 + 탐침 (양손)", "Two gloved hands enter from both sides of the frame: one holds a round dental mirror inside the mouth, the other a thin explorer probe touching a molar."),
        new("mhand.handpiece", "핸드피스 + 미러", "Two gloved hands: one holds a high-speed handpiece with a bur fitted, its tip on a molar, the other holds a dental mirror."),
        new("mhand.big_foreground", "크게 보이는 의사 손 (위에서)", "The dentist's large white-gloved hands dominate the foreground, seen from above over the patient, holding a mirror and an explorer in the mouth; the dentist's dark scrubs show at the frame edge."),
    ];

    // ---------- 진료실 ----------

    public static IReadOnlyList<PromptOption> ChairColors { get; } =
    [
        new("", "지정 안 함", ""),
        new("chair.skyblue", "하늘색", "The dental unit chair upholstery is light sky blue."),
        new("chair.mint", "민트", "The dental unit chair upholstery is soft mint green."),
        new("chair.teal", "청록 (틸)", "The dental unit chair upholstery is teal blue-green."),
        new("chair.navy", "네이비", "The dental unit chair upholstery is dark navy blue."),
        new("chair.blue", "코발트 블루", "The dental unit chair upholstery is vivid cobalt blue."),
        new("chair.white", "화이트", "The dental unit chair upholstery is clean white."),
        new("chair.gray", "그레이", "The dental unit chair upholstery is light gray."),
        new("chair.charcoal", "차콜·블랙", "The dental unit chair upholstery is charcoal black."),
        new("chair.beige", "베이지·크림", "The dental unit chair upholstery is warm beige cream."),
        new("chair.brown", "브라운", "The dental unit chair upholstery is warm brown."),
        new("chair.pink", "핑크", "The dental unit chair upholstery is pastel pink."),
        new("chair.yellow", "옐로", "The dental unit chair upholstery is pastel yellow."),
        new("chair.orange", "오렌지", "The dental unit chair upholstery is bright orange."),
        new("chair.red", "레드·와인", "The dental unit chair upholstery is deep wine red."),
        new("chair.purple", "퍼플", "The dental unit chair upholstery is lavender purple."),
    ];

    // 각도는 숫자를 넣어야 반영된다(등받이와 바닥 사이 각도가 아니라 등받이·좌석 사이 각도로 적는다).
    public static IReadOnlyList<PromptOption> ChairRecline { get; } =
    [
        new("", "지정 안 함", ""),
        new("recline.upright", "똑바로 세움 (약 100°)", "The dental chair backrest is upright, about 100 degrees from the seat; the patient sits up straight."),
        new("recline.slight", "살짝 기울임 (약 120°)", "The dental chair backrest is slightly reclined, about 120 degrees from the seat; the patient leans back a little."),
        new("recline.half", "반쯤 눕힘 (약 140°)", "The dental chair is half reclined, the backrest about 140 degrees from the seat, the patient's upper body at roughly 45 degrees."),
        new("recline.mostly", "거의 눕힘 (약 160°)", "The dental chair is mostly reclined, the backrest about 160 degrees from the seat, the patient's upper body nearly horizontal."),
        new("recline.flat", "완전 수평 (180°)", "The dental chair is fully reclined flat, the backrest at 180 degrees; the patient lies completely horizontal on the back, head level with the knees."),
        new("recline.supine", "머리가 살짝 낮게", "The dental chair is fully reclined with the head slightly lower than the feet (supine position for upper-jaw treatment)."),
    ];

    // ---------- 카메라 구도 ----------
    // 기록에서 얻은 교훈: 각도·방향은 숫자를 넣은 별도 문장이어야 반영된다(예: 2시 방향, 35–45°).

    public static IReadOnlyList<PromptOption> Viewpoints { get; } =
    [
        new("", "지정 안 함", ""),
        new("view.observer", "관찰자 (3인칭)", "Camera viewpoint: a third-person observer standing beside the dental chair."),
        new("view.dentist_pov", "치과의사 시점 POV", "Camera viewpoint: first-person POV from the dentist's eyes, looking down into the patient's open mouth; only the dentist's gloved hands and instruments are visible."),
        new("view.patient_pov", "환자 시점 POV", "Camera viewpoint: first-person POV from the patient lying in the chair, looking up at the dentist's masked face and the dental light above."),
        new("view.hygienist_pov", "위생사 시점 POV", "Camera viewpoint: first-person POV through the eyes of the dental hygienist sitting on the patient's left side; the hygienist's own gloved hands holding a suction tip appear in the lower-left foreground, and the dentist works across the chair on the far side."),
        new("view.ots", "의사 어깨 너머 (OTS)", "Camera viewpoint: over the dentist's shoulder, looking down at the patient."),
        new(FacelessViewKey, "의사 뒤 위에서 (의사 얼굴 화면 밖)",
            "Camera viewpoint: from just behind and above the standing dentist, looking down past the dentist's shoulder at the reclined patient. " +
            "Only the dentist's torso, arms and gloved hands are visible; the dentist's head and face are cut off above the top edge of the frame."),
    ];

    /// <summary>의료진 얼굴이 화면 밖인 시점. 고르면 의사·위생사의 얼굴·머리 묘사 문장을 뺀다.</summary>
    public const string FacelessViewKey = "view.ots_faceless";

    public static IReadOnlyList<PromptOption> Angles { get; } =
    [
        new("", "지정 안 함", ""),
        new("angle.top", "탑뷰 (바로 위 90°)", "Camera angle: straight top-down overhead view, 90 degrees above the patient."),
        new("angle.high", "하이앵글 35–45°", "Camera angle: high oblique angle, approximately 35–45 degrees above the patient's face."),
        new("angle.diag", "사선 하이앵글 25–35°", "Camera angle: slightly elevated diagonal side angle, approximately 25–35 degrees above the patient's body."),
        new("angle.eye", "눈높이", "Camera angle: eye level."),
        new("angle.low", "로우앵글", "Camera angle: low angle looking up."),
        new("angle.worm", "웜즈아이 (극단적 로우)", "Camera angle: extreme low worm's-eye view looking straight up."),
        new("angle.side", "사이드뷰 (측면 90°)", "Camera angle: side view, perpendicular to the dental chair."),
        new("angle.dutch", "더치앵글 (기울임)", "Camera angle: strong Dutch angle — the camera is rolled about 25 degrees, so walls, ceiling lines and the monitor are clearly slanted diagonally across the frame, creating tension."),
    ];

    public static IReadOnlyList<PromptOption> ShotSizes { get; } =
    [
        new("", "지정 안 함", ""),
        new("shot.ecu", "ECU 구강 내부", "Shot size: extreme close-up of the inside of the open mouth; teeth and molars fill the entire frame, face mostly outside the frame."),
        new("shot.closeup", "클로즈업 (얼굴)", "Shot size: close-up; the patient's face fills most of the frame."),
        new("shot.bust", "바스트샷 (가슴 위)", "Shot size: bust shot from the chest up."),
        new("shot.medium", "미디엄 (상반신)", "Shot size: medium shot showing the dentist's upper body, both hands and the patient's head."),
        new("shot.mediumwide", "미디엄 와이드 (체어 전체)", "Shot size: medium-wide shot showing the whole dental chair, the patient's full body and the staff around it."),
        new("shot.wide", "와이드 (진료실 전체)", "Shot size: wide establishing shot of the whole treatment room."),
    ];

    public static IReadOnlyList<PromptOption> HeadDirections { get; } =
    [
        new("", "지정 안 함", ""),
        new("head.12", "화면 위 (12시)", "The patient's head points toward the top of the frame (12 o'clock position), feet toward the bottom."),
        new("head.2", "우상단 (2시)", "The patient's head is angled toward the top-right of the frame (2 o'clock position)."),
        new("head.6", "화면 아래 (6시)", "The patient's head points toward the bottom of the frame (6 o'clock position), feet toward the top."),
        new("head.4", "우하단 (4~5시)", "The patient's head points toward the bottom-right of the frame (about 4–5 o'clock position), body extending toward the top-left; seen from above, the face appears upside down with the chin toward the top-left."),
        new("head.3", "오른쪽 (3시)", "The patient's head points to the right side of the frame (3 o'clock position)."),
        new("head.10", "좌상단 (10시)", "The patient's head is angled toward the top-left of the frame (10 o'clock position)."),
        new("head.9", "왼쪽 (9시)", "The patient's head points to the left side of the frame (9 o'clock position)."),
        new("head.front", "정면 (카메라 쪽)", "The patient's face is turned straight toward the camera, head tilted back."),
    ];

    public static IReadOnlyList<PromptOption> Lenses { get; } =
    [
        new("", "지정 안 함", ""),
        new("lens.shallow", "얕은 심도 (배경 흐림)", "Lens: shallow depth of field, background softly blurred."),
        new("lens.macro", "매크로", "Lens: macro lens, extremely fine detail on teeth and enamel."),
        new("lens.24", "광각 24mm", "Lens: 24mm wide-angle lens, slight perspective exaggeration."),
        new("lens.50", "표준 50mm", "Lens: 50mm standard lens, natural perspective."),
        new("lens.85", "망원 85mm", "Lens: 85mm telephoto lens, compressed background."),
        new("lens.handheld", "핸드헬드 다큐", "Lens: natural handheld documentary framing, slightly off-center."),
    ];

    public static IReadOnlyList<PromptOption> CameraMoves { get; } =
    [
        new("", "지정 안 함", ""),
        new("move.static", "고정", "Camera movement: static locked-off shot."),
        new("move.follow_top", "체어 따라 내려가며 탑뷰로", "Camera movement: the camera follows the chair as it reclines and slowly transitions into a top-down view."),
        new("move.push", "천천히 줌인", "Camera movement: slow push-in toward the patient's mouth."),
        new("move.pull", "천천히 줌아웃", "Camera movement: slow pull-back revealing the treatment room."),
        new("move.pan", "좌우 패닝 + 클로즈업", "Camera movement: pans left and right across the mouth, then moves into close-up."),
        new("move.pov", "POV 전환", "Camera movement: cuts to the dentist's first-person POV of the patient's face."),
    ];

    // ---------- 화풍 ----------

    public const string WebtoonStyle =
        "Modern Korean webtoon illustration style: clean confident ink line art, soft flat cel coloring with gentle airbrushed gradients, " +
        "pale warm skin tones with light blush on the cheeks, realistic adult proportions and natural Korean faces, delicate hair strands, " +
        "muted warm neutral palette of white, light gray and warm wood, the only saturated blue is where specifically requested, bright soft lighting, " +
        "set in a present-day South Korean dental clinic (not an American or European dental office) unless stated otherwise, with Korean staff and patients. " +
        "No text, no letters, no speech bubbles, no sound effects, no panels, no logos, no watermark. " +
        "Full-bleed image with no border or frame; no posters, calendars, charts or signs with any writing or symbols.";

    public const string PhotoStyle =
        "Ultra-realistic documentary-style photograph in a modern Korean dental clinic. Natural handheld camera perspective, soft indoor clinical lighting, " +
        "realistic shadows, slightly muted colors, authentic skin and fabric textures, subtle photographic imperfections, natural depth of field. " +
        "Looks like an actual photograph captured during a dental appointment, not a staged advertisement. Highly photorealistic, anatomically accurate hands, " +
        "realistic dental instruments. No illustration, no CGI, no exaggerated expressions, no text, no watermark.";

    public const string PaintingStyle =
        "현대적인 디지털 페인팅 스타일. 캐릭터의 얼굴 비율은 애니메이션이나 게임 캐릭터처럼 이상적이고 예쁘게(얼굴형, 큰 눈). " +
        "깨끗한 선화와 부드러운 명암 표현, 고해상도.";

    // 웹툰 장르별 화풍. 그림체만 다르고 배경(한국 치과)·글자 금지 규칙은 아래 꼬리로 똑같이 붙인다.
    private const string WebtoonTail =
        " The only saturated blue is where specifically requested. " +
        "Set in a present-day South Korean dental clinic (not an American or European dental office) unless stated otherwise, with Korean staff and patients. " +
        "No text, no letters, no speech bubbles, no sound effects, no panels, no logos, no watermark. " +
        "Full-bleed image with no border or frame; no posters, calendars, charts or signs with any writing or symbols.";

    public const string RomanceWebtoonStyle =
        "Korean romance webtoon illustration style: beautiful idealized faces with large glossy eyes and long lashes, slim elegant proportions, " +
        "fine thin line art, smooth glossy cel shading with soft bloom and gentle sparkles of light, flowing detailed hair, " +
        "dreamy pastel pink and warm cream palette, soft backlight and rim light." + WebtoonTail;

    public const string GagWebtoonStyle =
        "Korean slice-of-life comedy webtoon style: simple bold even line art, flat bright colors with minimal shading, " +
        "slightly cartoonish proportions with rounder heads, big exaggerated expressive faces and reactions (sweat drops, wide eyes), " +
        "clean simplified backgrounds, cheerful bright lighting." + WebtoonTail;

    public const string HealingWebtoonStyle =
        "Korean healing webtoon style: soft watercolor-like coloring with visible gentle brush texture, thin light sketchy line art in warm brown instead of black, " +
        "pastel mint, peach and cream palette, cozy warm sunlight through windows, calm gentle expressions, airy and comforting atmosphere." + WebtoonTail;

    public const string RealisticWebtoonStyle =
        "Semi-realistic Korean drama webtoon style: realistic anatomy and proportions, detailed painterly rendering of faces, skin and fabric, " +
        "precise line art that fades into painted shading, accurate dental equipment, natural clinic lighting with soft shadows, " +
        "grounded muted palette, looks like a high-budget digital webtoon rather than a photograph." + WebtoonTail;

    public const string ThrillerWebtoonStyle =
        "Korean thriller webtoon style: sharp angular line art, heavy black shadows and strong chiaroscuro, desaturated cold palette of gray and teal " +
        "with one warm accent, dramatic low or high camera angles, tense serious expressions, cinematic hard lighting from a single source." + WebtoonTail;

    public const string ChibiWebtoonStyle =
        "Cute Korean webtoon SD (super-deformed) chibi style: characters 2 to 3 heads tall with big heads and tiny bodies, round simple faces with large shiny eyes, " +
        "thick soft rounded outlines, flat bright pastel colors with simple one-step shading, playful and adorable mood." + WebtoonTail;

    public const string MonochromeWebtoonStyle =
        "Black-and-white Korean comic style: crisp black ink line art with varied pen pressure, gray screentone shading and cross-hatching, " +
        "no color at all, high contrast, clean white highlights, detailed linework on hair and clothing." + WebtoonTail;

    public static IReadOnlyList<PromptPreset> Styles { get; } =
    [
        new("없음 (장면 프리셋에 포함)", ""),
        new("한국 웹툰 (게임 에셋)", WebtoonStyle),
        new("웹툰: 로맨스 (화사한 미형)", RomanceWebtoonStyle),
        new("웹툰: 일상 · 개그", GagWebtoonStyle),
        new("웹툰: 힐링 (수채 파스텔)", HealingWebtoonStyle),
        new("웹툰: 반실사 드라마", RealisticWebtoonStyle),
        new("웹툰: 스릴러 (진한 명암)", ThrillerWebtoonStyle),
        new("웹툰: SD 꼬마 캐릭터", ChibiWebtoonStyle),
        new("웹툰: 흑백 펜선 + 스크린톤", MonochromeWebtoonStyle),
        new("실사 다큐 사진", PhotoStyle),
        new("디지털 페인팅 (웹툰 캐릭터)", PaintingStyle),
    ];

    // ---------- 장면 ----------

    private const string Cut =
        "Single isolated subject centered, full subject visible with margin around it, on a flat solid magenta #FF00FF background with no shadow on the background.";

    private const string Room =
        "Wide horizontal 3:2 interior of a single dental treatment room seen from above at a top-down 3/4 view. The whole floor is a large empty open space " +
        "with no furniture, no chairs, no dividing walls and no partitions, so game objects can be placed on it. Details only along the top wall: cabinets, a sink and windows. " +
        "Typical Korean clinic interior: bright white rectangular LED ceiling panels, white walls, light wood-look vinyl tile floor, all signs blank. " +
        "The accent wall panels and trim are painted in flat pure cobalt blue #0047FF, and nothing else in the room is blue; any sky through windows is pale and hazy. " +
        "After hours: completely deserted, absolutely no people, no human figures anywhere.";

    private const string Lounge =
        "Very wide horizontal banner interior of a dental clinic waiting room seen straight on from the front, low camera. A long fabric waiting sofa fills the center and right, " +
        "a reception counter stands at the far left, nothing in the foreground. Typical Korean clinic interior: bright white rectangular LED ceiling panels, white walls, " +
        "light wood-look vinyl tile floor, all signs blank. The accent wall panels and trim are painted in flat pure cobalt blue #0047FF, and nothing else in the room is blue. " +
        "After hours: completely deserted, absolutely no people, no human figures anywhere.";

    public static IReadOnlyList<PromptPreset> Scenes { get; } =
    [
        new("자유 입력", "", "", "입력한 내용만 보낸다."),

        // --- 게임 에셋 (gemini_batch.js) ---
        new("에셋: 단독 피사체 (마젠타 배경)", "", Cut, "인물 초상·장비·체어. 배경은 나중에 지운다."),
        new("에셋: 빈 진료실 배경", Room, "", "뒤에 공간 설명(예: 상가 2층 작은 치과)을 쓴다."),
        new("에셋: 빈 대기실 배너", Lounge),
        new("에셋: 게임 아이콘", "", "Game icon, bold simple silhouette readable at small size. " + Cut),
        new("에셋: 작은 UI 아이콘", "", "Tiny game UI icon, extremely simple, readable at 48 pixels. " + Cut),
        new("에셋: 스토리 컷", "A single full-bleed story scene. ", "", "주인공은 뒷모습·손·먼 거리로만."),
        new("편집: 첨부 이미지에서 X만 바꾸기",
            "Edit the attached image. Keep everything exactly the same: the composition, every person, the art style and the background. Only ", "",
            "예: replace the patient lying in the chair with an elderly Korean grandmother."),

        // --- 실사 (C1~C8) ---
        new("실사 C1: 진료 장면 와이드 16:9",
            """
            Ultra-realistic documentary-style photograph of an adult Korean woman receiving dental treatment in a modern Korean dental clinic.

            Patient: An adult Korean woman lying horizontally on her back in a fully reclined dental chair. She wears a dark navy long-sleeved top and dark trousers, with a large green disposable dental bib covering her chest and abdomen. Her hands are gently clasped together over her lower abdomen. Her head rests on the dental chair headrest, and her mouth is open for treatment.

            Dentist: A Korean female dentist with black hair tied back in a ponytail, wearing a white dental coat over dark navy scrubs, a white surgical mask, and white medical gloves. She is seated on a dental stool beside the patient's head, leaning forward with a focused expression while performing a dental procedure.

            Treatment action: The dentist holds a realistic high-speed dental handpiece in one gloved hand and a dental suction instrument or mirror in the other. The instruments are positioned directly inside the patient's open mouth, with the working tips contacting the teeth. A bright dental examination light illuminates the patient's mouth. Both of the dentist's hands are clearly engaged in the procedure. The scene depicts an authentic, ongoing dental treatment rather than a posed consultation.

            Camera and composition: A medium-wide horizontal photograph taken from a slightly elevated diagonal side angle, approximately 25–35 degrees above the patient's body. The camera is positioned beside the dental chair, showing the patient's body extending from the lower-left foreground toward the dentist on the right. The dentist's upper body, bent-forward working posture, both hands, the patient's green dental bib, and the treatment area are visible together. The patient's face is partially visible near the dentist's hands, without making the shot an extreme close-up.

            Clinic environment: A realistic dental unit with an instrument delivery console, metallic dental instruments arranged on a white tray, suction hoses, a dental operating light overhead, cream-colored walls, and white storage cabinets. Authentic everyday Korean dental clinic interior, tidy but naturally used.

            Photography: Natural handheld camera perspective, soft indoor clinical lighting, realistic shadows, slightly muted colors, authentic skin and fabric textures, subtle photographic imperfections, natural depth of field. Looks like an actual photograph captured during a dental appointment, not a staged advertisement. Highly photorealistic, anatomically accurate hands, realistic dental instruments, high detail, 16:9 landscape composition.

            No illustration, no CGI, no exaggerated expressions, no text, no watermark.
            """),
        new("실사 C2: 얼굴 클로즈업 하이앵글 9:16",
            """
            Ultra-realistic close-up clinical photograph of a young adult Korean woman receiving dental treatment in a modern Korean dental clinic.

            Patient and facial details: The patient is lying flat on her back in a reclining dental chair, with her head resting on a dark charcoal headrest. She has straight, dark brown hair framing her face, naturally textured fair skin, gently closed eyes, and relaxed eyebrows. Her mouth is opened very wide for dental examination, with both upper and lower teeth clearly visible, including several distinctive gold dental restorations on the upper molars. Her tongue is visible inside her open mouth.

            Dental procedure: A Korean female dentist wearing pale medical gloves is actively examining the patient's lower teeth. A slender stainless-steel dental explorer is inserted inside the patient's mouth, with its tip touching a lower tooth. The instrument must visibly enter the oral cavity rather than merely hover near the lips.

            Camera angle and composition: Tight close-up photograph taken from a high oblique angle, approximately 35–45 degrees above the patient's face. The patient's face fills most of the frame, with the open mouth near the center and the dentist's gloved hand entering from the right side.

            Lighting and photographic style: Soft, warm-white clinical lighting illuminating the oral cavity and facial features. Realistic reflections on the stainless-steel instrument, natural skin pores, anatomically accurate teeth, shallow depth of field, softly blurred cream-colored clinic background. Authentic documentary-style healthcare photography, 4K quality.

            Vertical portrait composition, 9:16 aspect ratio. No text, no watermark, no illustration, no CGI.
            """),
        new("실사 C3: 구강 내부 매크로 (골드 인레이)",
            """
            Ultra-realistic close-up intraoral dental photography, first-person patient POV, looking directly into the open mouth of a young Korean woman during a dental procedure.

            The camera is positioned very close to the oral cavity, with the face mostly outside the frame. The mouth is held wide open and the upper and lower molars are clearly visible. Several posterior molars have realistic old gold inlay restorations, irregularly shaped gold-colored dental fillings embedded into the chewing surfaces, with natural variations in color, reflections, wear and slight staining around the restoration margins.

            A Korean female dentist wearing sterile white medical gloves is performing the procedure, with dental instruments entering the mouth from above. A dental mirror and periodontal explorer are visible near the molars, while another gloved dental professional assists with suction.

            Bright focused surgical dental light directly illuminates the oral cavity. Green sterile surgical drape surrounds the mouth. Realistic moist oral tissues, natural saliva reflections, pink gums, tongue partially visible, highly detailed tooth enamel and realistic dental anatomy.

            Documentary medical photography, macro intraoral photography, realistic clinical lighting, shallow depth of field, photorealistic, 4K, no beauty retouching. Mouth and molars dominate the entire frame, minimal face visible, instruments partially entering from the edges of the image.

            The molars must clearly show multiple existing gold inlay restorations, not gold crowns and not metallic braces.

            No dentures, no dental implants, no braces, no silver fillings, no gold crowns, no deformed teeth, no extra teeth, no cartoon, no CGI, no illustration, no excessive blood, no gore, no wide shot of the dental clinic.
            """),
        new("실사 C4: 입술 당긴 하안면 매크로",
            "Macro close-up photograph of a person's lower face, focusing tightly from the nostrils down to the chin. A finger gently pulls down the lower lip to clearly expose the teeth and gums. " +
            "The mouth is slightly open, revealing natural anterior teeth alongside prominent metallic dental restorations on the posterior molars. " +
            "Realistic wet gum tissue, natural tooth enamel texture, soft clinical lighting, shallow depth of field, raw unretouched medical photography style. ",
            "", "뒤에 보철 종류를 적는다(예: gold crown on the lower right molars)."),
        new("실사 C5: 캔디드 미드샷 (참고 이미지 기반)",
            "A photorealistic, candid-style photo taken from a mid-ground angle, capturing an Asian female dentist performing a procedure on a patient in a modern dental clinic, based on the attached image. " +
            "The dentist, wearing a white coat, blue scrubs, a blue medical mask, and white latex gloves, is positioned over the patient, holding a dental handpiece with her right hand in the patient's open mouth and a dental mirror with her left hand, intently focused on the patient's teeth. " +
            "The Asian female patient is lying on the dental chair, eyes closed, with her mouth wide open, wearing a light blue dental bib. From the right edge of the frame, another figure, wearing blue scrubs and gloves, holds a suction tool. " +
            "In the foreground on the left, other dental tools are neatly on a tray with a high-speed handpiece. Above them is a powerful dental light fixture. The background shows a clean clinic with a wooden-toned floor and dental equipment carts, all softly blurred. " +
            "High resolution, detailed textures of gloves and skin, realistic color."),
        new("실사 C6: 환자 시점 로우앵글",
            """
            환자 시점의 극단적인 로우 앵글(worms-eye view) 치과 진료실 사진.
            피사체: 여성 치과의사. 흰색 일회용 접이식 마스크(주름이 있는 스타일) 착용. 의료용 라텍스 장갑을 낀 손.
            환자의 입장에서 치과의사를 정면으로 올려다보는 시점. 치과의사의 얼굴이 중앙에 있고, 눈은 가려지지 않은 채 아래를 향해 직접 응시하고 있다.
            장갑을 낀 오른손이 전경 상단 중앙과 우측에 크게 위치하여 작은 치과용 거울의 손잡이를 잡고 있다. 반대 손은 하이스피드 핸드피스를 자연스럽게 쥐고 있다.
            치과 진료실 천장이 보이며 여러 개의 밝은 원형 매립형 LED 조명이 있다. 우측에는 크고 강력한 치과용 유닛 조명이 밝게 켜져 있어 따뜻한 빛과 뚜렷한 조명 플레어를 만든다.
            고해상도, 사실적인 사진. 얕은 피사계 심도(전경의 손과 거울은 약간 흐리고 얼굴은 선명하게). 클로즈업.
            """),
        new("실사 C7: 탑다운 오버헤드",
            "치과 진료실을 위에서 내려다본 탑다운 앵글의 초고화질 사진. 첨부한 사진의 환자가 치과 의자에 누워 입을 벌리고 있고, 위에서 내려오는 밝은 수술 조명이 환자의 얼굴을 비추고 있다. " +
            "의사의 흰색 가운 소매와 조수의 네이비색 유니폼 소매가 보이며 의료진의 손이 환자의 구강 안팎에서 다양한 기구를 사용하여 섬세하게 작업하고 있다. " +
            "치과용 드릴, 거울, 탐침 등이 입안에 들어가 있고, 환자의 머리 주변으로 치과용 제어 패널, 트레이, 기구들이 배치된 전문적이고 세밀한 장면."),
        new("실사 C9: 충치 제거 구강 내부 (치과의사 POV)",
            """
            Ultra-realistic intraoral dental photograph from the dentist's first-person point of view, looking straight down into a patient's wide-open mouth during cavity removal.

            Framing: extreme close-up — the open mouth, teeth and molars fill the entire frame; the face, eyes and nose are outside the frame, only the stretched lips and a little skin around them are visible. This is NOT a wide shot of the clinic and NOT a portrait of the dentist or the patient.

            The treated tooth: a lower molar in the center of the frame with a clearly visible dark brown-black carious lesion in its chewing surface, surrounded by chalky white enamel. Part of the decay has already been drilled out, exposing a small prepared cavity.

            Action: a white-gloved hand enters from the top-right holding a high-speed dental handpiece; its thin bur tip is inside the cavity, cutting away the brown decay, with a fine mist of cooling water spraying around the bur. Another gloved hand from the top-left holds a round dental mirror that pulls the cheek aside. A suction tip in the corner of the mouth removes the water.

            Details: moist pink gums, tongue partly visible, realistic saliva and water droplets, anatomically correct number and shape of teeth, bright focused dental light, macro lens, shallow depth of field.

            Documentary clinical photography, photorealistic, no beauty retouching. No text, no watermark, no excessive blood, no gore, no deformed or extra teeth, no braces.
            """, "", "입안 장면: 인물 외모·복장 문장은 자동으로 빠진다(넓은 구도를 부르기 때문)."),
        new("실사 C10: 의사 뒤 하이앵글 충치 치료 (의사 얼굴 없음)",
            """
            Ultra-realistic candid photograph of cavity treatment in a modern Korean dental clinic, taken from just behind and above the standing dentist, looking steeply down (about 50–60 degrees) at the fully reclined patient.

            Framing: the dentist's torso, arms and gloved hands fill the right side of the frame; the dentist's head and face are cut off above the top edge of the frame. The patient's head lies on the headrest in the lower center, pointing toward the bottom-right of the frame, so the face is seen upside down from above with the chin toward the top-left; the patient's body and a light blue disposable bib extend toward the top-left.

            The patient: a young Korean woman with long dark hair spread on the headrest, eyes squeezed shut and brows drawn together in a slight frown of discomfort, mouth opened wide.

            Action: the dentist's upper gloved hand holds a slim silver high-speed handpiece with a thin bur angled down into the patient's mouth; the other gloved hand holds a round dental mirror inside the mouth and rests gently against the patient's cheek. One white-gloved hand of an assistant enters from the bottom-left corner holding a suction tip or air-water syringe. A fine mist of water spray is visible in the air near the dental unit.

            The dentist wears a long-sleeved ivory isolation gown with elastic cuffs over dark scrubs and white latex gloves. Dental unit arms, hoses and the instrument console are softly blurred in the background, light gray chair upholstery.

            Soft natural clinic lighting, shallow depth of field, authentic skin and glove textures, documentary style, not a staged advertisement. No text, no watermark, no excessive blood, no deformed hands or extra fingers.
            """, "",
            "의사·위생사 얼굴·머리 설정은 자동으로 빠진다(얼굴이 화면 밖이라). 뒤에 진료 부위나 환자 차이를 적는다.",
            StaffFacesHidden: true),
        new("웹툰: 충치 제거 입안 클로즈업",
            "Korean webtoon panel, extreme close-up inside a patient's wide-open mouth seen from the dentist's eyes. A lower molar in the center has a dark brown cavity; " +
            "the thin bur of a high-speed handpiece held by a white-gloved hand is drilling out the decay with a little spray of water, a dental mirror holds the cheek aside. " +
            "The mouth fills the whole frame, no faces. ", "", "뒤에 대사·효과(예: 위잉~)를 적는다."),
        new("실사 C8: 하이앵글 (짧은 한글)",
            "사진 스타일, 고화질 실사. 위에서 아래로 내려다보는 하이앵글(탑뷰) 구도. 밝고 깨끗한 현대적인 치과 진료실. 화면 중앙의 치과 체어에 누워 입을 벌리고 진료를 받고 있는 젊은 여성 환자. " +
            "화면 오른쪽에는 흰색 가운을 입고 라텍스 장갑을 낀 여성 치과의사가 스툴에 앉아 환자의 입안을 진찰하고 있음. 화면 우측 하단에는 치과용 장비 트레이가 일부 보임. 밝고 전문적인 병원 조명, 4k 해상도. "),

        // --- 웹툰 ---
        new("웹툰: 말풍선 초상 (안수연)",
            "A young Korean female dentist with brown bob hair holds a dental drill and a dental mirror in the other hand. Her face and chest are in the center of the frame, looking straight at the camera. " +
            "There is a speech bubble next to her with the text \"", "\" in Korean. Small Korean characters \"위잉~~\" are near the dental drill.",
            "말풍선 대사를 입력한다."),
        new("웹툰: 다음 컷", "첨부한 웹툰 컷의 다음 장면으로, 같은 인물과 화풍을 유지해서: ", "", "예: 치과의사가 진료 끝나고 마스크 내리는 장면"),

        // --- 영상 ---
        new("영상: 체어 하강 → 진료 시작",
            """
            치과의사가 진료를 시작하기 위해 마스크를 올리고 instrument bar에 버튼을 눌러서 리클라이너 치과 체어를 끝까지 내립니다.
            카메라는 내려가는 체어를 따라가면서 top view로 천천히 바뀝니다. 버튼을 누르면서 대사는 "기대보실게요"라고 한다.
            다 내려간 후에는 라이트로 환자 입안을 비추고 환자는 입을 최대한 크게 벌려서 진료 받을 준비를 하는 장면. 치과의사는 오른손에는 핸드피스를 자연스럽게 쥐고 왼손에는 덴탈 미러를 쥐고 있다.
            치과의사 대사는 "충치 치료 시작 하겠습니다. 아 크게 해보세요 물 많이 나옵니다."가 나오고 카메라가 치과의사 시점 POV 환자 얼굴 시점으로 전환된다. 대화하는 언어는 한국어.
            """),
        new("영상: 충치 제거 (핸드피스 소리만)",
            "첨부한 이미지가 움직이는 충치 치료 동영상. 핸드피스로 계속 충치를 제거하는 장면만. 하이스피드 핸드피스에는 버가 끼워져 있다. 소리는 하이스피드 핸드피스로 충치를 제거하는 소리만 들린다."),
        new("영상: 이전 영상에 이어서", "이전 동영상의 마지막 장면에서 이어집니다. ", "", "예: 환자가 누워 있는 체어가 천천히 끝까지 내려갑니다."),
    ];

    // ---------- 도메인 규칙 (수정 요청을 반복하며 굳어진 것) ----------

    public static IReadOnlyList<PromptRule> Rules { get; } =
    [
        new("의사는 환자 오른쪽, 위생사는 왼쪽",
            "The dentist sits on the patient's right side; the dental hygienist assists from the patient's left side holding the suction.", true, TreatmentOnly: true),
        new("오른손 핸드피스, 왼손 미러",
            "The dentist holds a high-speed handpiece in the right hand and a dental mirror in the left hand.", true, TreatmentOnly: true),
        new("핸드피스에 버 장착 (아래 향함)",
            "The high-speed handpiece has a thin bur fitted, pointing down toward the teeth.", true, TreatmentOnly: true),
        new("위생사는 손만 (석션, 화면 왼쪽 아래)",
            "The dental hygienist is not shown except for one white-gloved hand entering from the bottom-left corner of the frame, holding a suction tip or air-water syringe near the patient's mouth.", false, TreatmentOnly: true),
        new("골드 인레이 ≠ 크라운",
            "Gold inlays are embedded in the occlusal surfaces of the molars — not gold crowns, not braces.", false),
        new("초록 소공포 + dental bib",
            "The patient's face is covered by a green dental drape with an opening only around the nose and mouth, and a dental bib covers the chest.", false, TreatmentOnly: true),
        new("눈 감고 입 최대한 벌림",
            "The patient lies fully reclined with eyes closed and mouth opened as wide as possible.", true, TreatmentOnly: true),
        new("흰 라텍스 장갑 (마스크·루페는 인물 설정에서)",
            "Dental staff wear white latex gloves.", true),
        new("한국 치과, 한국인",
            "Set in a present-day South Korean dental clinic with Korean staff and patients.", false),
        new("글자·자막·워터마크 금지",
            "No text, no letters, no subtitles, no UI, no logos, no watermark.", true),
    ];

    // ---------- 수정 요청 칩 (이어서 보내기용) ----------

    public static IReadOnlyList<string> EditChips { get; } =
    [
        "자막·UI·로고·워터마크 빼줘",
        "줌아웃 30%",
        "탑뷰로 바꿔줘",
        "치과의사 시점 POV 환자 입안",
        "환자 시점 로우앵글",
        "ECU 클로즈업",
        "입안만 꽉 차게, 충치 깎는 버 끝이 보이게",
        "충치를 더 진한 갈색으로, 물 분사 보이게",
        "환자가 입을 최대한 크게 벌리게",
        "빨간 점 찍은 치아만 금니로",
        "빨간색 칠한 부분 골드인레이로, 색은 자연스럽게",
        "루페·고글 빼줘",
        "치과의사 손과 기구 지워줘",
        "웹툰 컷으로 바꿔줘",
        "실사로 바꿔줘",
        "가로 사진으로",
        "이 이미지를 GIF 애니메이션으로 변환해 줘",
    ];

    public static IReadOnlyList<AspectRatio> AspectRatios { get; } =
    [
        new("지정 안 함", ""),
        new("정사각 1:1", "Square 1:1."),
        new("가로 16:9", "Wide 16:9."),
        new("세로 9:16", "Vertical 9:16."),
        new("가로 3:2", "Horizontal 3:2."),
    ];
}
