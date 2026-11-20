# 치과 이미지 메이커

치과 장면용 프롬프트를 조립한 뒤, **로그인된 Gemini·ChatGPT 웹 화면**을 앱 안의 브라우저(WebView2)로 조작해 그림과 영상을 받아 오는 WPF 앱이다.
API를 쓰지 않으므로 지금 쓰는 구독 외에 추가 요금이 들지 않는다.
프롬프트 프리셋은 실제 Gemini 사용 기록에서 뽑았다. 출처는 [docs/gemini-history.md](docs/gemini-history.md)에 정리했다.

## 실행

```
dotnet build DentalImageMaker.slnx
dotnet run --project DentalImageMaker/DentalImageMaker.csproj
```

처음 한 번은 오른쪽 브라우저에서 Gemini(구글)와 ChatGPT에 로그인한다. 로그인은 `%LocalAppData%\DentalImageMaker\WebView2`에 남아 다음 실행에도 유지된다.

## 쓰는 법

1. 타이틀바에서 Gemini 또는 ChatGPT를 고른다. 영상은 Gemini만 된다.
2. 장면 프리셋, 화풍, 비율을 고르고 내용을 적는다. 치과 규칙 체크박스는 프롬프트 끝에 붙는다.
3. 필요하면 참고 이미지를 첨부하고 **생성**을 누른다.
4. 결과는 `저장 폴더\yyyyMMdd\이름.png`로 저장된다. 결과의 **참고로** 버튼을 누르면 다음 요청에 첨부된다.
5. 수정 요청 칩(줌아웃 30%, 탑뷰 등)을 누르면 "이어서 보내기"가 켜진다. 이 상태로 보내면 같은 채팅에서 수정을 요청한다.
6. 여러 건은 **대기열에 추가**한 뒤 **대기열 실행**으로 보낸다. 연속으로 3번 실패하거나 한도 문구가 보이면 멈춘다.
   JSON으로도 불러올 수 있다: `[{"name":"chair_idle","prompt":"...","provider":"Gemini","kind":"Image","newChat":true,"references":["raw/chair.png"]}]`

## 사이트 화면이 바뀌어 안 될 때

자동 조작은 CSS 선택자로 한다. 선택자는 `%AppData%\DentalImageMaker\sites.json`에 있다. 왼쪽 아래 **선택자 설정 열기** 버튼으로 열어서 고친 뒤 앱을 다시 켜면 된다.
브라우저 화면에서 F12를 누르면 개발자 도구가 열려 요소를 확인할 수 있다.

## 구조 (youtube-mp3와 같은 방식)

| 프로젝트 | 역할 |
|---|---|
| `DentalImageMaker` | 진입점(`Starter`), Generic Host DI 설정(`App`) |
| `DentalImageMaker.Forms` | 화면(`MainWindow` + `Themes/UI/Views/MainWindow.xaml`), `MainWindowViewModel`, `AppSettings` |
| `DentalImageMaker.Main` | `Automation/` 웹 조작(`PageDriver`: CDP 기반 입력·첨부·다운로드, `WebChatAutomation`: 생성 흐름, `SiteProfile`: 선택자), `Services/` 브라우저 세션·생성 서비스, `Prompts/` 프리셋·조립 |
| `DentalImageMaker.Support` | 커스텀 창(`DentalWindow`). WebView2가 투명 창에서 그려지지 않아 `AllowsTransparency=false`로 둔다. |
