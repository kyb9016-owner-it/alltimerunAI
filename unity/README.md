# Unity 실행 시점
지금 바로 실행하면 됩니다. 아래 순서대로 하면 3개 화면(Home/In-Run/Result)을 즉시 볼 수 있습니다.

## 1) Unity 프로젝트 열기
- Unity Hub -> `Open` -> 이 저장소 경로 선택
- 권장 버전: Unity 2022 LTS 이상 (2D 템플릿)

## 2) 스크립트 배치 확인
- 파일 위치:
  - `unity/Assets/Scripts/AiPetGamePrototype.cs`
- Unity Project 창에서 `Assets/Scripts/`에 스크립트가 보이도록 동일 경로로 복사/이동

## 3) 빈 씬 구성
- 새 Scene 생성 (`PrototypeScene`)
- Hierarchy에서 빈 오브젝트 생성: `GamePrototype`
- `AiPetGamePrototype` 컴포넌트를 `GamePrototype`에 추가

## 4) 실행
- Play 버튼 클릭
- 흐름:
  - Home -> `Start Run`
  - In-Run -> `+10 Score`, `Fail`
  - Result -> `Retry`, `Home`

## 5) 다음 단계
- 현재 화면은 uGUI 런타임 프로토타입입니다.
- 이후에는 이 구조를 기준으로 실제 아트/애니메이션/Ads/IAP를 연결하면 됩니다.
