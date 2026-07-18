# BLUES WITH YOU

<p align="center">
  <img src="Assets/%40Textures/Title_A.png" alt="BLUES WITH YOU title artwork" width="100%">
</p>

> A rain-soaked alley. A borrowed light. A rose left behind.

`BLUES WITH YOU` is a third-person atmospheric light-puzzle game set in a single rainy alley. The player follows light, reflections, and environmental reactions instead of on-screen explanations.

## Highlights

- A short four-choice title/tutorial sequence that leads directly into the alley
- Third-person exploration and character-centered camera control
- A vending-machine clue connected to the portal puzzle
- Wet-road reflections, rain, world-space fog, particles, and custom URP shaders
- Unity Web build target using URP Forward rendering

## Controls

| Input | Action |
|---|---|
| `WASD` | Move |
| Mouse | Look |
| `Left Shift` | Run |
| `E` | Interact / focus light-puzzle elements |
| Right mouse button | Target |
| Left mouse button | Activate the selected target |
| `Esc` | Release or restore cursor control |

Title choices support mouse clicks and keyboard input.

## Project

| Item | Value |
|---|---|
| Unity | `6000.0.79f1` |
| Render pipeline | Universal Render Pipeline `17.0.4` |
| Renderer | Forward |
| Primary target | Web |
| Entry scene | `Assets/Scenes/Scene-1 Title.unity` |
| Gameplay scene | `Assets/Scenes/Scene-TestLevel.unity` |

Open the repository with the Unity version above. The two scenes are already registered in Build Settings in title-to-gameplay order.

## Asset and license summary

| Resource | Source | License / status | Public repository |
|---|---|---|---|
| Game scripts, scenes, prefabs, custom shaders and materials | Project work | Project-specific work | Included |
| Title/key art and headset prompt | Project work | Project-specific artwork | Included |
| Wet-road and rain textures | [YNL Effect](https://github.com/YNL-Collection/YNL-Effect) by Yunasawa Studio | MIT | Included with license notice |
| Business Man character and textures | [manoeldarochadeoliveira on Sketchfab](https://sketchfab.com/3d-models/business-man-low-polygon-game-character-b6f6740f883b4749abac47af0045a9dd) | CC BY 4.0 | Included with attribution |
| Noto Sans KR TMP SDF | [Noto fonts](https://github.com/notofonts/noto-cjk) | SIL Open Font License 1.1 | Included with license notice |
| Unity/TMP template resources | Unity Technologies | Unity package/companion terms | Included for Unity use |
| Walking animation FBX | Adobe Mixamo | Royalty-free game use; raw public redistribution not asserted | Excluded |
| Music and ambience WAV files | Private development assets | Redistribution permission not documented | Excluded |
| Vending-machine source FBX | External source not yet documented | License pending verification | Excluded |

Detailed paths, attribution, and the private-asset policy are recorded in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Public repository policy

Restricted or unverified source assets are excluded by `.gitignore` and removed from Git tracking, while remaining on the developer machine for local builds. They must not be copied from this repository or redistributed separately. A playable Web build may contain compiled/embedded versions only where the applicable license permits it.

## Repository structure

```text
Assets/
├── Scenes/          # Title and gameplay scenes
├── Scripts/         # Gameplay, camera, interaction, and setup code
├── @Prefabs/        # Hand-placed level and puzzle prefabs
├── @FX/             # Custom shaders, materials, meshes, and VFX textures
├── @Materials/      # Environment, player, and portal materials
├── @models/         # Redistributable model sources
├── @Textures/       # Project textures and title artwork
└── Font/            # TextMesh Pro Korean font asset
```

## Third-party notices

This repository does not grant rights to third-party assets beyond their original licenses. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before redistributing or modifying the project.
