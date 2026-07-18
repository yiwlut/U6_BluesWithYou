# BLUES WITH YOU

<p align="center">
  <img src="Assets/%40Textures/Title_A.png" alt="BLUES WITH YOU title artwork" width="100%">
</p>

<p align="center">
  <a href="https://crawnsandoooo.itch.io/blueswithyou"><strong>▶ Play BLUES WITH YOU</strong></a>
</p>

> A rain-soaked alley. A borrowed light. A rose left behind.

## About

`BLUES WITH YOU`는 비가 내리는 밤의 골목을 배경으로 제작한 3인칭 빛 퍼즐 프로토타입입니다.

플레이어는 비 오는 날 골목에서 우연히 한 여자를 만나지만, 여자의 모습은 직접 등장하지 않습니다. 대신 골목에 남겨진 장미와 빛의 흔적을 따라가며 그날의 기억을 되짚게 됩니다. 게임 안에서는 별도의 설명 문구를 사용하지 않고, 조명과 반사, 오브젝트의 반응으로 퍼즐의 규칙을 전달하는 것을 목표로 개발했습니다.

현재 버전은 하나의 골목 안에서 진행되며, 자판기와 포탈을 연결한 첫 번째 환경 퍼즐을 중심으로 구성되어 있습니다.

## Gameplay

- 타이틀 화면에서 네 번의 선택을 진행하면 골목으로 진입합니다.
- 골목을 탐색하면서 빛에 반응하는 오브젝트와 상호작용합니다.
- 자판기는 포탈을 해제하기 위한 단서를 제공합니다.
- 화면 중앙의 장미는 이야기의 핵심 상징이자 플레이 방향을 잡아주는 시각적 기준입니다.
- 퍼즐 설명은 텍스트가 아니라 조명 변화, 머티리얼 반응, 사운드와 VFX로 전달합니다.

## Controls

| 입력 | 기능 |
|---|---|
| `WASD` | 캐릭터 이동 |
| 마우스 이동 | 카메라 회전 |
| `Left Shift` | 달리기 |
| `E` | 오브젝트 상호작용 및 빛 퍼즐 포커스 |
| 마우스 오른쪽 버튼 | 타겟팅 |
| 마우스 왼쪽 버튼 | 선택한 타겟 활성화 |
| `Esc` | 마우스 커서 잠금 전환 |

타이틀과 튜토리얼의 선택지는 마우스 클릭과 키보드 입력을 모두 지원합니다.

## Visual Direction

비에 젖은 밤거리를 표현하기 위해 젖은 바닥, 가로등 반사, 빗줄기, 월드 공간 안개와 파티클을 조합했습니다. 바닥은 URP Lit 계산을 기반으로 하되, 젖은 영역의 반사와 흐르는 노말, 빗방울 리플을 커스텀 HLSL 셰이더에서 처리했습니다.

웹 환경에서 실행하는 프로젝트이므로 URP Forward 렌더러를 사용했습니다. Forward+ 전용 기능에 의존하지 않고, 실시간 조명 수와 투명 파티클 오버드로우를 제한하는 방향으로 작업했습니다.

## Technical Details

| 항목 | 설정 |
|---|---|
| Unity | `6000.0.79f1` |
| Render Pipeline | Universal Render Pipeline `17.0.4` |
| Renderer | Forward |
| Target Platform | Web |
| Entry Scene | `Assets/Scenes/Scene-1 Title.unity` |
| Gameplay Scene | `Assets/Scenes/Scene-TestLevel.unity` |
| Input | Unity Input System |
| UI | TextMesh Pro |

Build Settings에는 타이틀 씬과 게임 씬이 위 순서대로 등록되어 있습니다. 저장소를 받은 뒤 동일한 Unity 버전으로 열면 됩니다.

## Project Structure

```text
Assets/
├── Scenes/          # 타이틀과 인게임 씬
├── Scripts/         # 플레이어, 카메라, 퍼즐, 상호작용 코드
├── @Prefabs/        # 레벨과 퍼즐 프리팹
├── @FX/             # HLSL 셰이더, VFX 머티리얼과 텍스처
├── @Materials/      # 환경, 플레이어, 포탈 머티리얼
├── @models/         # 공개 가능한 모델 원본
├── @Textures/       # 타이틀 이미지와 프로젝트 텍스처
└── Font/            # TextMesh Pro 한국어 폰트 에셋
```

레벨은 런타임 절차 생성이 아니라 씬과 프리팹에 직접 배치하는 방식으로 변경했습니다. 조명, 가로등, 바닥, 포그와 퍼즐 오브젝트도 씬에서 확인하고 수정할 수 있도록 구성했습니다.

## Assets & Licenses

프로젝트에서 사용한 외부 에셋과 공개 저장소 포함 여부를 아래에 정리했습니다. 자체 제작하거나 프로젝트에 맞게 작성한 코드와 셰이더는 별도로 표시했습니다.

| 리소스 | 종류 | 출처 | 라이선스 및 상태 | 공개 저장소 |
|---|---|---|---|---|
| 게임 스크립트와 퍼즐 로직 | C# | 프로젝트 제작 | 프로젝트 전용 코드 | 포함 |
| 커스텀 셰이더와 VFX 구성 | ShaderLab/HLSL, Material, Prefab | 프로젝트 제작 및 수정 | 프로젝트 전용 작업물 | 포함 |
| 타이틀 이미지, 키 이미지, 헤드셋 안내 이미지 | 이미지 | 프로젝트 제작 과정에서 생성 | 프로젝트 전용 이미지 | 포함 |
| Wet Road / Rain 텍스처 | 텍스처 | [YNL Effect](https://github.com/YNL-Collection/YNL-Effect), Yunasawa Studio | MIT | 포함 |
| Business Man 캐릭터 | 3D 모델, 텍스처 | [manoeldarochadeoliveira / Sketchfab](https://sketchfab.com/3d-models/business-man-low-polygon-game-character-b6f6740f883b4749abac47af0045a9dd) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 포함 |
| Noto Sans KR | TMP SDF 폰트 | [Noto Fonts](https://github.com/notofonts/noto-cjk) | SIL Open Font License 1.1 | 포함 |
| Unity / TextMesh Pro 기본 리소스 | UI 및 템플릿 | Unity Technologies | Unity 패키지 및 Companion 조건 | 포함 |
| Retro Vending Machine | 3D 모델 FBX | [Valentin Laffitte / itch.io](https://valentin-laffitte.itch.io/retro-vending-machine-3d-asset) | CC0 (Public Domain) | 포함 |
| Animation | 애니메이션 FBX | Adobe Mixamo | 게임 사용 가능, 원본 FBX 공개 재배포는 별도 판단 | 제외 |
| BGM / Rain Ambience | WAV | Splice | Splice Sounds License, 원본 파일 재배포 제외 | 제외 |

### YNL Effect

- 저작권: Copyright (c) 2024 Yunasawa Studio
- 사용 위치: `Assets/@FX/Textures/YNL/`
- 사용 방식: Wet Street 셰이더의 도로 컬러, 노말, 물 디테일, 웅덩이 마스크와 빗방울 리플 입력으로 사용했습니다.
- 수정 사항: 원본 Shader Graph를 그대로 포함하지 않고, 프로젝트의 URP Forward 환경에 맞춘 HLSL 셰이더에서 텍스처를 사용합니다.
- 라이선스 원문: `Assets/@FX/Textures/YNL/LICENSE-YNL-Effect.txt`

### Business Man Character

- 제작자: `manoeldarochadeoliveira`
- 원본 이름: `Business Man - Low Polygon game character`
- 사용 위치:
  - `Assets/@models/Player01/Player_01.fbx`
  - `Assets/@Materials/Player01/Textures/`
  - `Assets/@Textures/T_business_man_*.png`
- 수정 사항: Unity Humanoid 리그 설정, 플레이어 크기 조정, URP Lit 머티리얼 연결과 런타임 애니메이션 연동 작업을 진행했습니다.

Attribution: “Business Man - Low Polygon game character” by manoeldarochadeoliveira, licensed under CC BY 4.0. Unity 임포트, 머티리얼, 크기와 애니메이션 연결은 `BLUES WITH YOU`에 맞게 수정했습니다.

### Noto Sans KR

- 저작권: Copyright 2014-2021 Adobe, with Reserved Font Name `Source`
- 사용 위치: `Assets/Font/NotoSansKR-Regular SDF.asset`
- 사용 방식: 타이틀과 선택지에서 한국어를 표시하기 위한 TextMesh Pro SDF 에셋으로 변환했습니다.
- 라이선스 원문: `Assets/Font/LICENSE-NotoSansKR-OFL.txt`

### Adobe Mixamo

- 사용 위치: `Assets/@Animations/Player01/*.fbx`, `Assets/@models/Player01/Walking.fbx`
- 사용 방식: 플레이어 걷기 애니메이션으로 사용합니다.
- 공개 정책: 게임 안에서 사용하는 것은 가능하지만 다운로드한 원본 FBX를 공개 저장소에서 다시 배포할 권한까지 명확하다고 판단하지 않았습니다. 따라서 로컬 프로젝트에는 유지하고 Git 추적에서는 제외했습니다.
- 참고: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html

### BGM & Rain Ambience

- 출처: `Splice`
- 라이선스: `Splice Sounds License`
- 사용 위치: `Assets/@SE/*.wav`, `Assets/Resources/Audio/*.wav`
- 현재 사용하는 파일: 비 환경음, 타이틀 음악, 인게임 피아노 음악
- 공개 정책: 게임 안에서 사용했지만 원본 WAV 파일을 개별적으로 다시 배포하지 않도록 공개 저장소에서는 제외했습니다.

### Vending Machine

- 제작자: `Valentin Laffitte`
- 원본 이름: `Retro Vending Machine 3D Asset`
- 원본 링크: https://valentin-laffitte.itch.io/retro-vending-machine-3d-asset
- 라이선스: CC0 (Public Domain)
- 사용 위치: `Assets/@models/VendingMachine.fbx`
- 사용 방식: 자판기 단서와 포탈 해제 퍼즐에 사용합니다.
- 수정 사항: Unity 씬에 맞춰 크기와 위치를 조정하고 BoxCollider와 상호작용 스크립트를 추가했습니다.
- 공개 정책: 제작자가 CC0로 배포한 에셋이므로 FBX를 공개 저장소에 포함합니다. 출처 표기는 필수가 아니지만 제작자를 확인할 수 있도록 이 문서에 기록했습니다.

## Known Issue
