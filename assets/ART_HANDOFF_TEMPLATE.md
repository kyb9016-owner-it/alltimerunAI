# Art Handoff Template (Unity MVP)

## 1) 폴더 구조
```text
Assets/Art/
  Characters/
  Backgrounds/
  UI/
  FX/
```

## 2) 필수 에셋 목록
- `Characters/core_idle.png`
- `Characters/protohuman_idle.png`
- `Backgrounds/lab_bg_day.png`
- `UI/event_overheat_icon.png`
- `UI/event_breach_icon.png`
- `UI/event_boost_icon.png`
- `FX/evolution_glow.png`

## 3) 권장 캔버스/해상도
- 기준 해상도: `1080 x 1920` (Portrait)
- 캐릭터 원본: 긴 변 `1024px` 이상
- 아이콘 원본: `256 x 256`
- 배경 원본: `2048 x 2048` 이상

## 4) 파일 규칙
- 포맷: `PNG` (투명 배경 필요 시 alpha 포함)
- 색공간: `sRGB`
- 파일명: 소문자 + 스네이크 케이스
- 버전 예시: `core_idle_v02.png`

## 5) 캐릭터 스펙
- Core:
  - 형태: 원형/심볼형 실루엣
  - 기준 배치: 화면 중앙 기준 `y -40`
  - 안전영역: 외곽 8% 비우기
- ProtoHuman:
  - 형태: 다이아/홀로그램 느낌의 상반신 실루엣
  - Core 대비 시각 크기: `110~130%`
  - 진화 전환 시 알파 페이드 대응 가능한 명도 대비 유지

## 6) UI 아이콘 스펙
- 이벤트 아이콘 3종(Overheat/Breach/Boost)
- 1색 + 포인트 1색 중심
- 24dp에서도 식별 가능하도록 내부 디테일 최소화

## 7) Unity Import 설정(권장)
- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Filter Mode: `Bilinear`
- Compression: `Normal Quality`
- Max Size:
  - 캐릭터/배경: `2048`
  - 아이콘/FX: `512`

## 8) 전달 체크리스트
- [ ] 파일명/폴더 규칙 준수
- [ ] Core/ProtoHuman 명확히 구분
- [ ] 투명 영역/가장자리 깨짐 없음
- [ ] 모바일(1080x1920) 기준 가독성 확인
- [ ] Unity Import 설정 스크린샷 첨부(선택)

## 9) 전달 메시지 예시
```text
아트 1차 전달합니다.
- Characters/core_idle_v01.png
- Characters/protohuman_idle_v01.png
- Backgrounds/lab_bg_day_v01.png
- UI/event_overheat_icon_v01.png
- UI/event_breach_icon_v01.png
- UI/event_boost_icon_v01.png
- FX/evolution_glow_v01.png

요청: Core->Proto 전환 시 5.5초 페이드 기준으로 보정 부탁.
```
