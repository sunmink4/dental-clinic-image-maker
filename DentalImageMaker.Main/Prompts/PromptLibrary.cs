namespace DentalImageMaker.Main.Prompts;

/// <summary>
/// 프롬프트 조각. 장면 프리셋은 Before + (사용자 입력) + After 로 끼워 넣는다.
/// 출처는 docs/gemini-history.md (Gemini 대화 기록에서 다듬어진 프롬프트).
/// </summary>
public sealed record PromptPreset(string Name, string Before, string After = "", string Hint = "")
{
    public override string ToString() => Name;
}

public sealed record PromptRule(string Name, string Text, bool DefaultOn);

public sealed record AspectRatio(string Name, string Text)
{
    public override string ToString() => Name;
}

/// <summary>카메라 구도 선택지 하나. Text 는 프롬프트에 그대로 붙는 영어 문장(비어 있으면 지정 안 함).</summary>
public sealed record CameraOption(string Key, string Name, string Text)
{
    public override string ToString() => Name;
}

public static class PromptLibrary
{
    // ---------- 카메라 구도 ----------
    // 기록에서 얻은 교훈: 각도·방향은 숫자를 넣은 별도 문장이어야 반영된다(예: 2시 방향, 35–45°).

    public static IReadOnlyList<CameraOption> Viewpoints { get; } =
    [
        new("", "지정 안 함", ""),
        new("view.observer", "관찰자 (3인칭)", "Camera viewpoint: a third-person observer standing beside the dental chair."),
        new("view.dentist_pov", "치과의사 시점 POV", "Camera viewpoint: first-person POV from the dentist's eyes, looking down into the patient's open mouth; only the dentist's gloved hands and instruments are visible."),
        new("view.patient_pov", "환자 시점 POV", "Camera viewpoint: first-person POV from the patient lying in the chair, looking up at the dentist's masked face and the dental light above."),
        new("view.hygienist_pov", "위생사 시점 POV", "Camera viewpoint: first-person POV from the dental hygienist on the patient's left side, holding the suction."),
        new("view.ots", "의사 어깨 너머 (OTS)", "Camera viewpoint: over the dentist's shoulder, looking down at the patient."),
    ];

    public static IReadOnlyList<CameraOption> Angles { get; } =
    [
        new("", "지정 안 함", ""),
        new("angle.top", "탑뷰 (바로 위 90°)", "Camera angle: straight top-down overhead view, 90 degrees above the patient."),
        new("angle.high", "하이앵글 35–45°", "Camera angle: high oblique angle, approximately 35–45 degrees above the patient's face."),
        new("angle.diag", "사선 하이앵글 25–35°", "Camera angle: slightly elevated diagonal side angle, approximately 25–35 degrees above the patient's body."),
        new("angle.eye", "눈높이", "Camera angle: eye level."),
        new("angle.low", "로우앵글", "Camera angle: low angle looking up."),
        new("angle.worm", "웜즈아이 (극단적 로우)", "Camera angle: extreme low worm's-eye view looking straight up."),
        new("angle.side", "사이드뷰 (측면 90°)", "Camera angle: side view, perpendicular to the dental chair."),
        new("angle.dutch", "더치앵글 (기울임)", "Camera angle: Dutch angle, the frame tilted about 15 degrees for tension."),
    ];

    public static IReadOnlyList<CameraOption> ShotSizes { get; } =
    [
        new("", "지정 안 함", ""),
        new("shot.ecu", "ECU 구강 내부", "Shot size: extreme close-up of the inside of the open mouth; teeth and molars fill the entire frame, face mostly outside the frame."),
        new("shot.closeup", "클로즈업 (얼굴)", "Shot size: close-up; the patient's face fills most of the frame."),
        new("shot.bust", "바스트샷 (가슴 위)", "Shot size: bust shot from the chest up."),
        new("shot.medium", "미디엄 (상반신)", "Shot size: medium shot showing the dentist's upper body, both hands and the patient's head."),
        new("shot.mediumwide", "미디엄 와이드 (체어 전체)", "Shot size: medium-wide shot showing the whole dental chair, the patient's full body and the staff around it."),
        new("shot.wide", "와이드 (진료실 전체)", "Shot size: wide establishing shot of the whole treatment room."),
    ];

    public static IReadOnlyList<CameraOption> HeadDirections { get; } =
    [
        new("", "지정 안 함", ""),
        new("head.12", "화면 위 (12시)", "The patient's head points toward the top of the frame (12 o'clock position), feet toward the bottom."),
        new("head.2", "우상단 (2시)", "The patient's head is angled toward the top-right of the frame (2 o'clock position)."),
        new("head.6", "화면 아래 (6시)", "The patient's head points toward the bottom of the frame (6 o'clock position), feet toward the top."),
        new("head.3", "오른쪽 (3시)", "The patient's head points to the right side of the frame (3 o'clock position)."),
        new("head.10", "좌상단 (10시)", "The patient's head is angled toward the top-left of the frame (10 o'clock position)."),
        new("head.9", "왼쪽 (9시)", "The patient's head points to the left side of the frame (9 o'clock position)."),
        new("head.front", "정면 (카메라 쪽)", "The patient's face is turned straight toward the camera, head tilted back."),
    ];

    public static IReadOnlyList<CameraOption> Lenses { get; } =
    [
        new("", "지정 안 함", ""),
        new("lens.shallow", "얕은 심도 (배경 흐림)", "Lens: shallow depth of field, background softly blurred."),
        new("lens.macro", "매크로", "Lens: macro lens, extremely fine detail on teeth and enamel."),
        new("lens.24", "광각 24mm", "Lens: 24mm wide-angle lens, slight perspective exaggeration."),
        new("lens.50", "표준 50mm", "Lens: 50mm standard lens, natural perspective."),
        new("lens.85", "망원 85mm", "Lens: 85mm telephoto lens, compressed background."),
        new("lens.handheld", "핸드헬드 다큐", "Lens: natural handheld documentary framing, slightly off-center."),
    ];

    public static IReadOnlyList<CameraOption> CameraMoves { get; } =
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

    public static IReadOnlyList<PromptPreset> Styles { get; } =
    [
        new("없음 (장면 프리셋에 포함)", ""),
        new("한국 웹툰 (게임 에셋)", WebtoonStyle),
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
            "The dentist sits on the patient's right side; the dental hygienist assists from the patient's left side holding the suction.", true),
        new("오른손 핸드피스, 왼손 미러",
            "The dentist holds a high-speed handpiece in the right hand and a dental mirror in the left hand.", true),
        new("핸드피스에 버 장착 (아래 향함)",
            "The high-speed handpiece has a thin bur fitted, pointing down toward the teeth.", true),
        new("골드 인레이 ≠ 크라운",
            "Gold inlays are embedded in the occlusal surfaces of the molars — not gold crowns, not braces.", false),
        new("초록 소공포 + dental bib",
            "The patient's face is covered by a green dental drape with an opening only around the nose and mouth, and a dental bib covers the chest.", false),
        new("눈 감고 입 최대한 벌림",
            "The patient lies fully reclined with eyes closed and mouth opened as wide as possible.", true),
        new("마스크·라텍스 장갑, 루페 없음",
            "Dental staff wear surgical masks and white latex gloves; no loupes, no goggles.", true),
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
