# Step 1: Unity 프로토타입 실행 (순서대로)

아래 순서대로 진행하면 **Home → In-Run → Result** 3개 화면을 바로 확인할 수 있습니다.

---

## 1) Unity 프로젝트 열기

- **Unity Hub** 실행 → **Open** → 이 저장소의 **`unity`** 폴더 선택  
  (예: `/Users/kong/projects/alltimerunAI/unity`)
- 권장: **Unity 2022 LTS** 이상, **2D** 템플릿

---

## 2) 스크립트 배치 확인

- 경로: `unity/Assets/Scripts/AiPetGamePrototype.cs`
- Unity **Project** 창에서 `Assets/Scripts/` 아래에 위 스크립트가 보이는지 확인  
  (이미 같은 경로에 있으면 추가 작업 없음)

---

## 3) 씬 구성 (없을 때만)

- **File → New Scene** → **PrototypeScene** 등으로 저장
- **Hierarchy**에서 빈 오브젝트 생성: 이름 **`GamePrototype`**
- **GamePrototype** 선택 → **Add Component** → **Ai Pet Game Prototype** 추가

---

## 4) 실행

- 상단 **Play** 버튼 클릭
- 예상 흐름:
  - **Home** → **Start Run** 클릭
  - **In-Run** → **+10 Score**, **Fail** 등 동작 확인
  - **Result** → **Retry** 또는 **Home**으로 복귀

---

## 5) 체크리스트

- [ ] Unity Hub에서 `unity` 폴더 열기
- [ ] `Assets/Scripts/AiPetGamePrototype.cs` 존재 확인
- [ ] `GamePrototype` 오브젝트에 `AiPetGamePrototype` 컴포넌트 붙임
- [ ] Play → Home → Start Run → In-Run → Fail → Result → Retry/Home 흐름 확인

완료되면 **Step 2 (로컬 테스트)** 로 진행하면 됩니다.
