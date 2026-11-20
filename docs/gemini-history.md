# Gemini 치과 이미지 생성 기록 정리

2026-11-20 기준으로 Chrome의 Gemini(gemini.google.com) 대화 기록을 훑어 정리했다. 전체 대화는 533개이고, 그중 치과와 관련된 것이 약 290개다.
대화 링크는 `https://gemini.google.com/app/<id>` 형식이다.

| 묶음 | 대화 수 (대략) | 만든 방법 | 결과물 |
|---|---|---|---|
| A. 게임 에셋 (치과 키우기) | ~220 | `dental-clinic-clicker/tools/gemini_batch.js` 로 자동 투입 | 웹툰풍 PNG 205장 (`dental-clinic-clicker/art/raw`) |
| B. 스토리 컷 (게임 대사 배경) | ~30 | 같은 배치의 `story()` 와 손입력 | `story_*.png` |
| C. 실사 사진 | ~25 | 손입력, 참고 이미지 첨부 후 수정 반복 | 진료 장면 사진 |
| D. 웹툰 "보존과 전문의 안수연" | ~15 | 손입력, 콘티를 컷 단위로 요청 | 웹툰 컷 |
| E. 영상 (Veo) | ~15 | 이미지 첨부 + 장면 설명 | 8~10초 영상, GIF |

---

## A. 게임 에셋 — 이미 자동화되어 있음

프롬프트 원본은 `dental-clinic-clicker/tools/gemini_batch.js` 와 `docs/image-prompts.md` 에 있다. 구조는 다음과 같다.

**프롬프트 = 접두어 + 공통 스타일(STYLE) + 대상 설명 + 접미어**

| 블록 | 쓰임 |
|---|---|
| `STYLE` | 한국 웹툰 화풍, 한국 치과, 글자·말풍선·로고 금지 (모든 그림 공통) |
| `CUT` | 단독 피사체를 마젠타 `#FF00FF` 단색 배경 위에 그림 (투명 처리용) |
| `ROOM` | 3:2 진료실 탑다운 3/4 뷰, 바닥은 비움, 포인트 벽만 코발트 블루 `#0047FF`, 사람 없음 |
| `LOUNGE` | 16:9 대기실 정면, 소파·리셉션, 사람 없음 |
| `ICON` / `TINY` | 게임 아이콘 / 48px UI 아이콘 |
| `SAME` | 같은 채팅을 이어 가며 "같은 사람으로" 변형 (주인공 학생→페이닥터→레지던트) |
| 편집형 | 원본 PNG를 첨부하고 "Edit the attached image. Keep everything exactly the same… Only replace X" |

생성한 종류와 Gemini 대화 수:

| 종류 | 파일 접두어 | 대화 수 |
|---|---|---|
| 체어 환자 교체 (아이·20대 여성·할머니·할아버지 × 6장면) | `chair_*_{kid,woman,gran,grandpa}` | 24 |
| 주인공 초상에 마스크 씌우기 | `hero_*_mask` | 18 |
| 주인공 초상 남 9 · 여 9 | `hero_m*`, `hero_f*` | ~20 |
| 위생사 초상 20명 | `hyg_*` | 20 |
| 빈 진료실 · 대기실 배경 | `bg_space*`, `wait_space*` | ~20 |
| 체어 기본·진료 장면 | `chair_idle`, `chair_busy_*`, `chair_hyg_only` | ~15 |
| 장비 (X-ray, 오토클레이브, 컴프레서, 3D 프린터) | `eq_*` | 5 |
| 진료·스킬·아이템·메뉴·재화 아이콘 | `tx_*`, `sk_*`, `it_*`, `mn_*`, `ic_*`, `up_*` | ~60 |
| 인물 초상 (원장·학생·교수·엄마 등) | `face_*` | ~15 |
| 이벤트 · 앱 아이콘 · 타이틀 | `ev_*`, `app_icon`, `title_bg` | ~10 |

**이 과정에서 겪은 문제** (대화 제목에 그대로 남아 있음)
- "Image Generation Limit Reached", "Dental Chair Image Edit Limit" 같은 일일 한도에 걸렸다. 그래서 배치는 3번 연속 실패하면 멈춘다.
- "이미지 생성 요청 불가" / "이미지 생성 요청 거절" 처럼 가끔 거절된다.
- 새 채팅을 연 뒤 이전 채팅의 그림을 새 그림으로 잘못 잡는 일을 막으려고 3초를 기다린다.
- 첨부가 끝나기 전에 보내면 그림 없이 전송되므로 6초를 기다린다.

---

## B. 스토리 컷

`story()` 템플릿은 `Generate an image. Square 1:1. ${STYLE} A single full-bleed story scene. <장면>` 이다. 주인공은 남녀 중에서 고르기 때문에 **뒷모습 · 손 · 먼 거리로만** 보이게 했다.

| 대화 제목 | 파일 |
|---|---|
| Warm Pediatric Dental Memory | `story_tower_child` |
| Webtoon-Style Korean Dental Tower at Night | `story_tower_night` |
| Proud Mother at Clinic Opening / Opening Ceremony | `story_opening_mother`, `story_hospital_ceremony` |
| Crowded Korean Dental Clinic Scene | `story_waiting_full` |
| Imagining a Future Dental Clinic | `story_dream_clinic` |
| Cozy Late-Night Bank Transfer | `story_paycheck` |
| Dental Professor Scolding Resident / Dawn at the Dental Hospital | `story_scold`, `story_resident` |
| The Doctor's Dilemma / Joyful Call | `story_crossroads`, `story_result_call` |
| First Day At Clinic, Late Night Study, Simulation Lab 등 | `story_student`, `story_study`, `story_phantom` … |

---

## C. 실사 사진 프롬프트 (그대로 다시 쓸 수 있음)

### C1. 진료 장면 와이드 · 다큐 사진 (16:9) — 가장 많이 다듬은 프롬프트
대화: `2fe6c3e1e211471b`, `cbf3b5be8eb335ff`

```
Ultra-realistic documentary-style photograph of an adult Korean woman receiving dental treatment in a modern Korean dental clinic.

Patient: An adult Korean woman lying horizontally on her back in a fully reclined dental chair. She wears a dark navy long-sleeved top and dark trousers, with a large green disposable dental bib covering her chest and abdomen. Her hands are gently clasped together over her lower abdomen. Her head rests on the dental chair headrest, and her mouth is open for treatment.

Dentist: A Korean female dentist with black hair tied back in a ponytail, wearing a white dental coat over dark navy scrubs, a white surgical mask, and white medical gloves. She is seated on a dental stool beside the patient's head, leaning forward with a focused expression while performing a dental procedure.

Treatment action: The dentist holds a realistic high-speed dental handpiece in one gloved hand and a dental suction instrument or mirror in the other. The instruments are positioned directly inside the patient's open mouth, with the working tips contacting the teeth. A bright dental examination light illuminates the patient's mouth. Both of the dentist's hands are clearly engaged in the procedure. The scene depicts an authentic, ongoing dental treatment rather than a posed consultation.

Camera and composition: A medium-wide horizontal photograph taken from a slightly elevated diagonal side angle, approximately 25–35 degrees above the patient's body. The camera is positioned beside the dental chair, showing the patient's body extending from the lower-left foreground toward the dentist on the right. The dentist's upper body, bent-forward working posture, both hands, the patient's green dental bib, and the treatment area are visible together. The patient's face is partially visible near the dentist's hands, without making the shot an extreme close-up. The camera captures the spatial relationship between the patient, dentist, chair, and surrounding equipment.

Clinic environment: A realistic dental unit with an instrument delivery console, metallic dental instruments arranged on a white tray, suction hoses, a dental operating light overhead, cream-colored walls, and white storage cabinets. Authentic everyday Korean dental clinic interior, tidy but naturally used.

Photography: Natural handheld camera perspective, soft indoor clinical lighting, realistic shadows, slightly muted colors, authentic skin and fabric textures, subtle photographic imperfections, natural depth of field. Looks like an actual photograph captured during a dental appointment, not a staged advertisement. Highly photorealistic, anatomically accurate hands, realistic dental instruments, high detail, 16:9 landscape composition.

No illustration, no CGI, no exaggerated expressions, no text, no watermark.
```
> 영상 모드에서 보내면 "I can only generate videos"라는 답이 온다. 모드를 확인해야 한다.

### C2. 환자 얼굴 클로즈업 · 하이 오블리크 (9:16)
대화: `cbf3b5be8eb335ff`

```
Ultra-realistic close-up clinical photograph of a young adult Korean woman receiving dental treatment in a modern Korean dental clinic.

Patient and facial details: The patient is lying flat on her back in a reclining dental chair, with her head resting on a dark charcoal headrest. She has straight, dark brown hair framing her face, naturally textured fair skin, gently closed eyes, and relaxed eyebrows. Her mouth is opened very wide for dental examination, with both upper and lower teeth clearly visible, including several distinctive gold dental crowns on the upper molars. Her tongue is visible inside her open mouth. Her lips are naturally stretched by the wide opening.

Dental procedure: A Korean female dentist wearing pale medical gloves is actively examining the patient's lower teeth. A slender stainless-steel dental explorer is inserted inside the patient's mouth, with its tip touching a lower tooth. The dentist's gloved hand and metal instrument are prominently visible in the foreground. The instrument must visibly enter the oral cavity rather than merely hover near the lips. Authentic dental examination in progress, realistic tooth anatomy, natural gums, visible molars and gold restorations.

Camera angle and composition: Tight close-up photograph taken from a high oblique angle, approximately 35–45 degrees above the patient's face, between a direct side-level close-up and a bird's-eye view. The camera is positioned near the patient's head and slightly to one side, looking diagonally down toward the open mouth. The patient's face fills most of the frame, with the open mouth near the center and the dentist's gloved hand entering from the right side. The nose, closed eyes, cheeks, lips, teeth, and dental instrument remain visible. Only a small portion of the dental chair, the clinician's clothing, and the clinic environment appears around the edges.

Lighting and photographic style: Soft, warm-white clinical lighting illuminating the oral cavity and facial features. Realistic reflections on the stainless-steel instrument, subtle shadows around the lips, natural skin pores, fine facial details, anatomically accurate teeth, shallow depth of field, softly blurred cream-colored clinic background. Authentic documentary-style healthcare photography, candid moment during an actual dental examination, natural proportions, high photographic realism, detailed textures, 4K quality.

Vertical portrait composition, 9:16 aspect ratio. Prioritize the patient's widely opened mouth, visible gold molar crowns, and the dental instrument physically inside the mouth. No text, no watermark, no illustration, no CGI.
```

### C3. 구강 내부 매크로 · 골드 인레이
대화: `f82df762ce5250b0`, `b5fee914314d73ab`

```
Ultra-realistic close-up intraoral dental photography, first-person patient POV, looking directly into the open mouth of a young Korean woman during a dental procedure.

The camera is positioned very close to the oral cavity, with the face mostly outside the frame. The mouth is held wide open and the upper and lower molars are clearly visible. Several posterior molars have realistic old gold inlay restorations, irregularly shaped gold-colored dental fillings embedded into the chewing surfaces, with natural variations in color, reflections, wear and slight staining around the restoration margins.

A Korean female dentist wearing sterile white medical gloves is performing the procedure, with dental instruments entering the mouth from above. A dental mirror and periodontal explorer are visible near the molars, while another gloved dental professional assists with suction.

Bright focused surgical dental light directly illuminates the oral cavity. Green sterile surgical drape surrounds the mouth. Realistic moist oral tissues, natural saliva reflections, pink gums, tongue partially visible, highly detailed tooth enamel and realistic dental anatomy.

Documentary medical photography, authentic dental clinic procedure, macro intraoral photography, realistic clinical lighting, shallow depth of field, natural skin texture, highly detailed dental instruments, photorealistic, 4K, extremely realistic, no beauty retouching.

Composition should closely resemble an actual intraoral dental procedure photograph: mouth and molars dominate the entire frame, minimal face visible, instruments partially entering from the edges of the image.

The molars must clearly show multiple existing gold inlay restorations, not gold crowns and not metallic braces. The gold inlays are embedded specifically into the occlusal surfaces of several posterior molars, with realistic dental anatomy visible around them.

No dentures, no dental implants, no braces, no silver fillings, no white composite fillings replacing the gold inlays, no gold crowns, no exaggerated teeth, no deformed teeth, no extra teeth, no missing anatomy, no cartoon, no CGI, no illustration, no artificial plastic-looking gums, no excessive blood, no gore, no smiling portrait, no wide shot of the dental clinic, no beauty photography.
```

### C4. 입술을 당긴 하안면 매크로 (보철 흔적)
대화: `3124af390b9f314e`

```
Macro close-up photograph of a person's lower face, focusing tightly from the nostrils down to the chin. A finger gently pulls down the lower lip to clearly expose the teeth and gums. The mouth is slightly open, revealing natural anterior teeth alongside prominent metallic dental restorations on the posterior molars—specifically shiny silver stainless steel crowns on the lower left molars and a gold crown on the lower right molars. Realistic wet gum tissue, natural tooth enamel texture, soft clinical lighting, shallow depth of field, raw unretouched medical photography style.
```

### C5. 캔디드 미드샷 (참고 이미지 기반)
대화: `7da58abc46178388`, `a405003529a9634b`

```
A photorealistic, candid-style photo taken from a mid-ground angle, capturing an Asian female dentist performing a procedure on a patient in a modern dental clinic, based on image_0.png. The dentist, wearing a white coat, blue scrubs, a blue medical mask, and white latex gloves, is positioned over the patient, holding a dental handpiece (drill) with her right hand in the patient's open mouth and a dental mirror with her left hand, intently focused on the patient's teeth. The Asian female patient is lying on the dental chair, eyes closed, with her mouth wide open, wearing a light blue dental bib. Her neck scarf is partially visible. From the right edge of the frame, another figure, wearing blue scrubs and gloves, holds a blue and purple suction tool. In the foreground on the left, other dental tools are neatly on a tray with a highspeed hand piece. Above them is a powerful dental light fixture, with a visible incandescent bulb. The background shows a clean clinic with a wooden-toned floor and various other dental equipment carts, all softly blurred. Natural indoor lighting mixes with the dental light. High resolution, detailed textures of gloves and skin, realistic color.
```

### C6. 환자 시점 로우 앵글 (worm's-eye) — 한글
대화: `dbb2fac2ec030b8c`

```
환자 시점의 극단적인 로우 앵글(worms-eye view) 치과 진료실 사진.
피사체: 여성 치과의사. 복장 및 장비: 흰색 일회용 접이식 마스크(주름이 있는 스타일) 착용. 의료용 라텍스 장갑을 낀 손.
핵심 구도 및 도구:
환자의 입장에서 치과의사를 정면으로 올려다보는 시점. 치과의사의 얼굴이 중앙에 있고, 눈은 가려지지 않은 채 아래를 향해 직접 응시하고 있다.
장갑을 낀 오른손이 전경 상단 중앙과 우측에 크게 위치하여, 작은 치과용 거울의 손잡이를 잡고 있다.
거울의 반사 면은 환자 쪽을 향해 있으며, 치과의사의 눈 바로 위에 위치하되 시야를 가리지 않는다. 반대 손은 하이스피드 핸드피스를 자연스럽게 쥐고 있다.
장갑을 낀 왼손의 일부가 전경 우측에 추가되어 공간감을 준다.
배경 및 조명:
치과 진료실 천장이 보이며, 여러 개의 밝은 원형 매립형 LED 조명이 있다.
우측에는 크고 강력한 치과용 유닛 조명이 밝게 켜져 있어, 따뜻한 빛과 뚜렷한 조명 플레어를 만든다.
스타일: 고해상도, 사실적인 사진. 얕은 피사계 심도 (전경의 손과 거울은 약간 흐리고, 얼굴과 거울 반사는 선명하게). 클로즈업.
```
짧은 버전: `환자 시점의 초근접 사진. 젊은 여성 치과의사가 마스크를 착용하고 치과 거울, 하이스피드 핸드피스를 자연스럽게 들고 위에서 내려다보고 있다. 뒤로 치과 조명이 보이며, 약간의 렌즈 플레어가 발생한다.`

### C7. 탑다운 오버헤드 (업로드한 인물로 다시 구성)
대화: `26b567d671e38d7f`, `54ff542a85f57f2a`

```
치과 진료실을 위에서 내려다본 탑다운 앵글의 초고화질 사진. 업로드한 사진의 여성 환자가 치과 의자에 누워 입을 벌리고 있고, 위에서 내려오는 밝은 수술 조명이 환자의 얼굴을 비추고 있다. 의사의 흰색 가운 소매와 조수의 네이비색 유니폼 소매가 보이며 의료진의 손이 환자의 구강 안팎에서 다양한 기구를 사용하여 섬세하게 작업하고 있다. 치과용 드릴, 거울, 탐침 등이 입안에 들어가 있고, 환자의 머리 주변으로 치과용 제어 패널, 트레이, 기구들이 배치된 전문적이고 세밀한 장면.
```
변형: 녹색 소공포를 덮은 환자를 가운데 두고, 왼쪽 아래에 위생사, 오른쪽 아래에 여성 치과의사를 둔다. 왼쪽 위에는 서랍장과 컨트롤 패널, 오른쪽 위에는 기구 트레이를 배치한다.

### C8. 하이앵글 짧은 한글 버전
대화: `5d4dfd9413de90df`, `f2d1065aeb1be2b1`

```
사진 스타일, 고화질 실사. 위에서 아래로 내려다보는 하이앵글(탑뷰) 구도. 밝고 깨끗한 현대적인 치과 진료실. 화면 중앙의 치과 체어에 누워 입을 벌리고 진료를 받고 있는 젊은 여성 환자. 환자는 청바지를 입고 가슴에 하늘색 치과용 덮개(drape)를 덮고 있음. 화면 오른쪽에는 흰색 가운을 입고 라텍스 장갑을 낀 여성 치과의사가 스툴에 앉아 환자의 입안을 진찰하고 있음. 의사는 양손에 치과용 기구(거울 등)를 쥐고 환자에게 집중하고 있음. 화면 우측 하단에는 치과용 장비 트레이가 일부 보임. 흰색 타일 바닥, 밝고 전문적인 병원 조명, 4k 해상도.
```
> 환자 머리 방향(예: "The patient's head is angled toward the top-right (2 o'clock position in the frame).")은 **따로 한 문장으로** 적어야 반영된다.

---

## D. 웹툰 시리즈 — "보존과 전문의 안수연"

- **주인공**: 안수연. 30대 여성, 보존과 전문의, 갈색 단발머리. 이름이 대화 `4cf69fa943e72a66`의 제목에 나온다.
- **화풍 블록** (`a405003529a9634b`): 현대적인 디지털 페인팅 스타일, 얼굴 비율은 애니메이션·게임 캐릭터처럼 이상적으로 (얼굴형, 큰 눈), 깨끗한 선화와 부드러운 명암.
- **에피소드 1: 외항사 합격 예비 승무원의 금니 3개 교체** (`86049108a1305cce`, `4b8bec2bbd2e0d8e`, `60a08cfb4e1813ed`)
  - 흐름: 접수 → 진료실 안내 (칸막이 체어 3개) → 체어 눕힘 → 금니 제거 → POV 금니 제거 후 → 신경치료가 필요할 만큼 썩은 치아 → 마스크 내리는 컷 → 카운터 결제
  - 말풍선 초상 프롬프트:
    ```
    A young-aged 한국 woman holds a dental drill and dental mirror the other side. Her face and chest are in the center of the frame, looking straight at the camera. There is a speech bubble next to her with the text "오늘 바로 기존 금니 제거 작업부터 들어갈게요. 아- 해보세요" in Korean. Small Korean characters "위잉~~" are near the dental drill.
    ```
- **에피소드 2: Ghost in the Tooth** (`8c91ec3fc5c2221e`): 15컷 콘티. 신경치료와 크라운까지 끝났는데 계속 아프다. 교합지로 0.01mm 간섭을 찾고, 원인은 턱관절(TMJ)과 이갈이였다. 개발자 비유(Error 메시지, 서버 구조도)가 들어간다. 콘티 전체를 붙여 넣고 "1 그려줘"처럼 컷 번호로 요청했다.
- 콘티를 받은 뒤 "N컷 그려줘", "다음 컷으로 …"처럼 이어서 요청했다 (`e0859aa2afdab52b`).

---

## E. 영상 (Veo) 프롬프트

참고 이미지(체어에 누운 환자)를 첨부하고 보낸다. 영상 길이는 8~10초다.

```
치과의사가 진료를 시작하기 위해 마스크를 올리고 instrument bar에 버튼을 눌러서 리클라이너 치과 체어를 끝까지 내립니다.
카메라는 내려가는 체어를 따라가면서 top view로 천천히 바뀝니다. 버튼을 누르면서 대사는 기대보실게요한다.
다 내려간 후에는 라이트로 환자 입안을 비추고 환자는 입을 최대한 크게 벌려서 진료 받을 준비를 하는 장면. 치과의사는 오른손에는 핸드피스를 자연스럽게 쥐고 왼손에는 덴탈 미러를 쥐고 있다. 치과의사 대사는 "충치 치료 시작 하겠습니다. 아 크게 해보세요 물 많이 나옵니다."가 나오고 카메라가 치과의사 시점 POV 여성 환자 얼굴 시점으로 전환된다.
```
- 소리만 지정할 때: `충치 제거 하는 비디오 하이스피드 핸드피스 소리만 들린다.` / `배경음 치과 하이스피드 핸드피스로 충치 제거 하는 소리 넣어줘. 하이스피드 핸드피스에 버는 끼워져있어야 한다.`
- 이어 붙이기: `이전 동영상의 11초 지점에서 이어집니다. …` → 영상이 20초, 30초, 40초로 늘어난다.
- 언어 지정: `대화하는 언어는 한국어`
- 이미지를 GIF로 바꿀 때: `이 이미지를 GIF 애니메이션으로 변환해 줘`
- **제한**: 일일 영상 한도가 있다 ("오늘은 더 이상 영상을 생성해 드릴 수 없습니다"). 첨부 이미지는 최대 3장이다.

---

## F. 자주 쓴 수정 명령 (이미지 편집)

| 분류 | 실제로 쓴 표현 |
|---|---|
| 지우기 | 자막/UI/로고 빼줘 (왼쪽 위 포함), 워터마크·하얀 테두리 빼줘, 개구기 빼줘 (치아 상태는 건들지 말고), 루페·안경·고글 빼줘, 치과의사 손·기구 지워줘, 빨간 영역 지워줘 |
| 보철 바꾸기 | 빨간 점 찍은 #16번 치아만 금니로, 빨간색 칠한 치아 골드인레이로, 아래 어금니 아말감 치료 흔적 (양쪽에 하나씩), 크라운 프렙 과정의 치아 모양으로, 색이 너무 튀지 않고 자연스럽게 |
| 구도 | 줌아웃 30% / 50%, 탑뷰로, POV 치과의사 시점 환자 입안, 환자 시점 로우앵글, ECU, 사이드뷰, 9가지 각도로, 가로로 찍은 사진으로 |
| 인물 | image1 치과의사가 image2 환자를 치료하는 장면, 치과의사를 한국 여성으로, 위생사 복장 네이비 |
| 장비 | instrument holder 체어사이드 타입, 버가 꽂힌 하이스피드 핸드피스 2개, 기구 5개 / 닥터 테이블에 연결 / 천장형 / 플로팅형 |
| 자세 | 입을 최대한 크게 (30%만 더), 등받이는 내려가고 체어는 올라온, 고개 15도 오른쪽, 머리를 더 뒤로 젖히게 |
| 화풍 | 웹툰 컷으로 바꿔줘, 실사로 안 되나 이전 이미지처럼, 화풍은 유지하고 치과의사로 바꿔줘 (가운 입혀줘) |
| 크기 | 320×416에 넣을 거라 가슴까지만, 머리 위 여백 거의 없게 |

---

## G. 도메인 규칙 (프롬프트에 기본으로 넣을 것)

수정 요청을 반복하며 굳어진 규칙이다. 앱의 기본 문구로 넣으면 다시 고치는 횟수가 줄어든다.

1. **치과의사는 환자 기준 오른쪽**에 앉는다. 위생사는 왼쪽에서 석션을 잡는다.
2. **오른손에 하이스피드 핸드피스, 왼손에 미러**를 든다. 석션은 위생사가 잡는다.
3. 핸드피스에는 **버(bur)가 끼워져** 있어야 한다. 버는 아래(치아 쪽)를 향한다.
4. **골드 인레이 ≠ 골드 크라운**. "not gold crowns, not braces, embedded in occlusal surfaces"를 명시해야 구분된다.
5. 소공포는 **초록색**이고 코와 입 부분이 뚫려 있다. 인중은 가리지 않는다. 가슴에는 dental bib를 덮는다.
6. 환자는 눈을 감고 입을 최대한 벌린다. 체어는 끝까지 눕힌 상태다.
7. 치과의사는 마스크와 흰 라텍스 장갑을 착용한다. 루페는 원하지 않으면 생기므로 "no loupes"를 넣는다.
8. 머리 방향이나 카메라 각도는 별도 문장으로 숫자(2시 방향, 25–35°)를 넣어야 반영된다.
9. 그림 속 한글은 깨진다. 게임 에셋에는 글자를 넣지 않고, 웹툰 말풍선만 예외로 둔다.
10. 영상 모드와 이미지 모드를 헷갈리면 "I can only generate videos"라는 답이 온다.

---

## H. 앱에 옮길 때

- `youtube-mp3` 구조(WPF · .NET 8 · MVVM · Generic Host DI · CommunityToolkit.Mvvm)를 그대로 따른다. `Main` 프로젝트에 `IGeminiService`를 둔다.
- **프롬프트 조립 = 스타일 프리셋(웹툰 / 실사 / 디지털 페인팅) + 장면 프리셋(C1~C8, E) + 도메인 규칙(G) + 자유 입력 + 수정 칩(F)**
- 참고 이미지 첨부, 같은 채팅에서 이어 가며 수정하기, 배치 대기열(A)이 핵심 기능이다.
- 지금까지는 Gemini **웹 UI 자동화**(Playwright)로 돌렸다. 앱은 **Gemini API**(이미지: `gemini-*-image` 계열, 영상: Veo)로 바꿀지 웹 자동화를 유지할지 정해야 한다. API로 가면 웹 한도 대신 과금이 붙고, 다운로드 대기 같은 우회 코드가 필요 없다.
