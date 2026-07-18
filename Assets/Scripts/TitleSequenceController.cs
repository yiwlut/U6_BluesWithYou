using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DoorPuzzle
{
    /// <summary>
    /// A self-contained title and four-choice prologue. The woman is represented
    /// only by a rose; the cigarette in the foreground belongs to the male player.
    /// </summary>
    public sealed class TitleSequenceController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "Scene-TestLevel";
        [SerializeField] private AudioClip rainyAtmo;
        [SerializeField] private AudioClip titleTutorialBgm;
        [SerializeField] private Material wetStreetMaterial;
        [SerializeField] private GameObject rainyAlleyAtmospherePrefab;
        [SerializeField] private GameObject lightingRigPrefab;
        [SerializeField] private GameObject titleStreetPrefab;
        [SerializeField] private Sprite headsetPromptSprite;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.52f;
        [SerializeField, Range(0f, 1f)] private float titleMusicVolume = 0.58f;
        [SerializeField, Min(0.1f)] private float titleMusicFadeInDuration = 6f;
        [SerializeField, Min(0.05f)] private float headsetPromptFadeDuration = 0.75f;
        [SerializeField, Min(0f)] private float headsetPromptHoldDuration = 1.8f;
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.35f;

        private static readonly string[] Questions =
        {
            "“불 좀 빌려줄래요?”",
            "“비가 그칠 때까지만, 같이 있어도 돼요?”",
            "“당신도 누군가를 기다리고 있어요?”",
            "“내가 사라져도, 오늘 밤을 기억해 줄래요?”"
        };

        private static readonly string[] FirstChoices =
        {
            "라이터를 건넨다",
            "그래요",
            "누군가를 기다린다",
            "기억할게요"
        };

        private static readonly string[] SecondChoices =
        {
            "모른 척한다",
            "곧 갈 겁니다",
            "갈 곳이 없을 뿐이다",
            "잊고 싶어요"
        };

        private int questionIndex = -1;
        private int selectedChoice;
        private bool loading;
        private bool transitioning;
        private Transform titleCamera;
        private Transform rose;
        private Vector3 roseBasePosition;
        private CanvasGroup titleGroup;
        private CanvasGroup questionGroup;
        private CanvasGroup headsetPromptGroup;
        private Image fadeOverlay;
        private TMP_Text questionText;
        private TMP_Text firstChoiceText;
        private TMP_Text secondChoiceText;
        private Button beginButton;
        private Button firstChoiceButton;
        private Button secondChoiceButton;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            BuildEnvironment();
            PlayAmbience();
            StartCoroutine(PlayTitleMusic());
            BuildInterface();
            StartCoroutine(InitialFadeIn());
        }

        private void Update()
        {
            AnimateEnvironment();
            if (loading || transitioning) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                SelectChoice(0);
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                SelectChoice(1);

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                if (questionIndex < 0) beginButton.onClick.Invoke();
                else if (selectedChoice == 0) firstChoiceButton.onClick.Invoke();
                else secondChoiceButton.onClick.Invoke();
            }
        }

        private void BuildEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.025f, 0.045f, 0.08f);
            RenderSettings.ambientEquatorColor = new Color(0.012f, 0.022f, 0.04f);
            RenderSettings.ambientGroundColor = new Color(0.004f, 0.007f, 0.014f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.012f, 0.022f, 0.04f);
            RenderSettings.fogDensity = 0.026f;

            var visualRoot = new GameObject("Rainy Alley Title Visuals").transform;
            visualRoot.SetParent(transform, false);

            var cameraObject = new GameObject("Title Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(visualRoot, false);
            cameraObject.transform.position = new Vector3(0f, 1.72f, -8.2f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.05f, 4.2f) - cameraObject.transform.position);
            titleCamera = cameraObject.transform;
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.003f, 0.006f, 0.012f);
            camera.fieldOfView = 43f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 70f;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cameraObject.AddComponent<AudioListener>();

            if (titleStreetPrefab != null)
            {
                var titleStreet = Instantiate(titleStreetPrefab, visualRoot, false);
                titleStreet.name = "Title Street from Scene-TestLevel";
                titleStreet.transform.localPosition = Vector3.zero;
                titleStreet.transform.localRotation = Quaternion.identity;
                titleStreet.transform.localScale = Vector3.one;
                PrepareTitleStreet(titleStreet);

                CreateRose(visualRoot);
                CreatePlayerCigarette(titleCamera);
                CreatePostProcessing(visualRoot);
                return;
            }

            Debug.LogWarning("[DoorPuzzle] Title street prefab is not assigned. Using the legacy title alley fallback.");

            var darkWall = CreateLitMaterial("Title Alley Wall", new Color(0.018f, 0.025f, 0.035f), Color.black, 0.14f, 0.05f);
            var trim = CreateLitMaterial("Title Alley Trim", new Color(0.035f, 0.045f, 0.055f), Color.black, 0.72f, 0.35f);
            var ground = CreateBox("Wet Alley", new Vector3(0f, -0.12f, 4f), new Vector3(9.5f, 0.22f, 29f), wetStreetMaterial, visualRoot);
            ground.isStatic = true;
            CreateBox("Left Alley Wall", new Vector3(-5.1f, 2.6f, 4f), new Vector3(0.7f, 5.4f, 29f), darkWall, visualRoot);
            CreateBox("Right Alley Wall", new Vector3(5.1f, 2.6f, 4f), new Vector3(0.7f, 5.4f, 29f), darkWall, visualRoot);
            CreateBox("Alley End", new Vector3(0f, 2.6f, 17.9f), new Vector3(10f, 5.4f, 0.7f), darkWall, visualRoot);
            CreateBox("Left Curb", new Vector3(-4.35f, 0.05f, 4f), new Vector3(0.42f, 0.3f, 29f), trim, visualRoot);
            CreateBox("Right Curb", new Vector3(4.35f, 0.05f, 4f), new Vector3(0.42f, 0.3f, 29f), trim, visualRoot);

            CreateAlleyArchitecture(visualRoot);

            if (lightingRigPrefab != null)
            {
                var lightingRig = Instantiate(lightingRigPrefab, visualRoot, false);
                lightingRig.name = "Title Street Lighting";
                lightingRig.transform.localPosition = Vector3.zero;
                lightingRig.transform.localRotation = Quaternion.identity;

                // Keep the authored lamp visuals and values, but fit their positions to this narrower title alley.
                foreach (Transform child in lightingRig.transform)
                {
                    var position = child.localPosition;
                    position.x *= 0.66f;
                    child.localPosition = position;
                }
            }
            else
            {
                var moonObject = new GameObject("Cold Rain Light");
                moonObject.transform.SetParent(visualRoot, false);
                moonObject.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
                var moon = moonObject.AddComponent<Light>();
                moon.type = LightType.Directional;
                moon.color = new Color(0.28f, 0.43f, 0.68f);
                moon.intensity = 1.25f;
                moon.shadows = LightShadows.Soft;

                CreatePointLight("Blue Alley Bounce", new Vector3(-3.8f, 1.4f, -2f), new Color(0.08f, 0.32f, 0.72f), 2.6f, 8f, visualRoot);
            }

            CreatePointLight("Rose Lamp", new Vector3(0f, 3.2f, 4.3f), new Color(1f, 0.42f, 0.12f), 6.2f, 9f, visualRoot);

            CreateRose(visualRoot);
            CreatePlayerCigarette(titleCamera);
            CreatePostProcessing(visualRoot);
            if (rainyAlleyAtmospherePrefab != null)
            {
                var atmosphere = Instantiate(rainyAlleyAtmospherePrefab, visualRoot, false);
                atmosphere.name = "Title Rainy Alley Atmosphere";
                atmosphere.transform.localPosition = Vector3.zero;
                atmosphere.transform.localRotation = Quaternion.identity;
            }
            else
            {
                RainVFX.Build(visualRoot);
            }
        }

        private static void PrepareTitleStreet(GameObject titleStreet)
        {
            foreach (var child in titleStreet.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Center Rose" || child.name == "Player")
                    child.gameObject.SetActive(false);
            }

            foreach (var behaviour in titleStreet.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var behaviourNamespace = behaviour.GetType().Namespace;
                if (behaviourNamespace == "DoorPuzzle" ||
                    (behaviourNamespace != null && behaviourNamespace.StartsWith("DoorPuzzle.")))
                    behaviour.enabled = false;
            }
        }

        private static void CreateAlleyArchitecture(Transform parent)
        {
            var facade = CreateLitMaterial("Title Facade", new Color(0.014f, 0.019f, 0.026f), Color.black, 0.08f, 0.24f);
            var frame = CreateLitMaterial("Title Window Frames", new Color(0.025f, 0.031f, 0.038f), Color.black, 0.64f, 0.42f);
            var warmGlass = CreateLitMaterial("Title Warm Windows", new Color(0.11f, 0.035f, 0.008f), new Color(2.4f, 0.48f, 0.055f), 0.05f, 0.78f);
            var coolGlass = CreateLitMaterial("Title Cool Windows", new Color(0.008f, 0.035f, 0.055f), new Color(0.02f, 0.42f, 0.72f), 0.08f, 0.82f);

            for (var index = 0; index < 4; index++)
            {
                var z = -3.4f + index * 6.1f;
                CreateBox($"Left Facade {index + 1}", new Vector3(-4.72f, 2.45f, z), new Vector3(0.12f, 4.7f, 5.3f), facade, parent);
                CreateBox($"Right Facade {index + 1}", new Vector3(4.72f, 2.45f, z), new Vector3(0.12f, 4.7f, 5.3f), facade, parent);

                var leftWindow = CreateBox($"Left Window {index + 1}", new Vector3(-4.64f, 2.05f, z + 0.45f), new Vector3(0.045f, 1.45f, 1.75f), index % 2 == 0 ? warmGlass : coolGlass, parent);
                var rightWindow = CreateBox($"Right Window {index + 1}", new Vector3(4.64f, 2.15f, z - 0.55f), new Vector3(0.045f, 1.55f, 1.65f), index % 2 == 0 ? coolGlass : warmGlass, parent);
                leftWindow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                rightWindow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                CreateBox($"Left Window Header {index + 1}", new Vector3(-4.58f, 2.87f, z + 0.45f), new Vector3(0.09f, 0.12f, 2.05f), frame, parent);
                CreateBox($"Right Window Header {index + 1}", new Vector3(4.58f, 3.02f, z - 0.55f), new Vector3(0.09f, 0.12f, 1.95f), frame, parent);
            }

            CreateBox("Distant Warm Window Left", new Vector3(-2.35f, 2.35f, 17.5f), new Vector3(1.45f, 1.65f, 0.06f), warmGlass, parent);
            CreateBox("Distant Cool Window Center", new Vector3(0f, 3.05f, 17.5f), new Vector3(1.15f, 1.35f, 0.06f), coolGlass, parent);
            CreateBox("Distant Warm Window Right", new Vector3(2.35f, 2.15f, 17.5f), new Vector3(1.35f, 1.5f, 0.06f), warmGlass, parent);

            var cableMaterial = CreateLitMaterial("Title Utility Cables", new Color(0.006f, 0.007f, 0.009f), Color.black, 0.15f, 0.16f);
            for (var index = 0; index < 3; index++)
            {
                var cable = CreatePrimitive($"Overhead Cable {index + 1}", PrimitiveType.Cylinder,
                    new Vector3(0f, 4.55f - index * 0.26f, 2.2f + index * 5.4f),
                    new Vector3(0.018f, 4.65f, 0.018f), cableMaterial, parent);
                cable.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        private void PlayAmbience()
        {
            if (rainyAtmo == null) return;
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = rainyAtmo;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = ambienceVolume;
            source.dopplerLevel = 0f;
            source.Play();
        }

        private IEnumerator PlayTitleMusic()
        {
            if (titleTutorialBgm == null)
                titleTutorialBgm = Resources.Load<AudioClip>("Audio/bgm_titleTutorial");
            if (titleTutorialBgm == null)
            {
                Debug.LogWarning("[DoorPuzzle] Scene 1 title tutorial BGM is not assigned and the Resources fallback is missing.");
                yield break;
            }

            if (titleTutorialBgm.loadState == AudioDataLoadState.Unloaded)
                titleTutorialBgm.LoadAudioData();
            while (titleTutorialBgm.loadState == AudioDataLoadState.Loading)
                yield return null;
            if (titleTutorialBgm.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogError("[DoorPuzzle] Scene 1 title tutorial BGM failed to load.");
                yield break;
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = titleTutorialBgm;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.dopplerLevel = 0f;
            source.Play();

            var elapsed = 0f;
            while (elapsed < titleMusicFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / titleMusicFadeInDuration);
                source.volume = Mathf.SmoothStep(0f, titleMusicVolume, normalized);
                yield return null;
            }
            source.volume = titleMusicVolume;
        }

        private void BuildInterface()
        {
            if (koreanFont == null)
                koreanFont = TMP_Settings.defaultFontAsset;

            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("Title Event System");
                eventSystemObject.AddComponent<EventSystem>();
                var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }

            var canvasObject = new GameObject("Title Tutorial UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateImage("Top Letterbox", canvasObject.transform, Color.black,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 58f));
            CreateImage("Bottom Letterbox", canvasObject.transform, Color.black,
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 58f));

            titleGroup = CreateGroup("Title", canvasObject.transform);
            CreateText("Game Title", titleGroup.transform, "BLUES WITH YOU", 72f, FontStyles.Bold,
                new Color(0.82f, 0.9f, 1f), new Vector2(0.5f, 0.72f), new Vector2(900f, 110f));
            CreateText("Subtitle", titleGroup.transform, "A rainy alley. A borrowed light. A rose left behind.", 24f, FontStyles.Italic,
                new Color(0.58f, 0.68f, 0.78f), new Vector2(0.5f, 0.64f), new Vector2(900f, 60f));
            beginButton = CreateButton("Begin Button", titleGroup.transform, "BEGIN", new Vector2(0.5f, 0.19f), new Vector2(300f, 72f), out _);
            beginButton.onClick.AddListener(BeginPrologue);
            CreateText("Begin Hint", titleGroup.transform, "ENTER / CLICK", 18f, FontStyles.Normal,
                new Color(0.48f, 0.57f, 0.68f), new Vector2(0.5f, 0.115f), new Vector2(520f, 40f));

            questionGroup = CreateGroup("Tutorial Questions", canvasObject.transform);
            questionGroup.alpha = 0f;
            questionGroup.interactable = false;
            questionGroup.blocksRaycasts = false;
            CreateImage("Dialogue Backdrop", questionGroup.transform, new Color(0.002f, 0.005f, 0.012f, 0.88f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 170f), new Vector2(0f, 340f));
            questionText = CreateText("Question", questionGroup.transform, string.Empty, 36f, FontStyles.Normal,
                new Color(0.93f, 0.95f, 1f), new Vector2(0.5f, 0.255f), new Vector2(1320f, 104f));
            firstChoiceButton = CreateButton("First Choice", questionGroup.transform, string.Empty,
                new Vector2(0.335f, 0.115f), new Vector2(480f, 72f), out firstChoiceText);
            secondChoiceButton = CreateButton("Second Choice", questionGroup.transform, string.Empty,
                new Vector2(0.665f, 0.115f), new Vector2(480f, 72f), out secondChoiceText);
            firstChoiceButton.onClick.AddListener(() => SelectAnswer(true));
            secondChoiceButton.onClick.AddListener(() => SelectAnswer(false));
            CreateText("Choice Hint", questionGroup.transform, "A / D  ·  ← / →  ·  ENTER", 18f, FontStyles.Normal,
                new Color(0.48f, 0.57f, 0.68f), new Vector2(0.5f, 0.045f), new Vector2(620f, 36f));

            var firstNavigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = secondChoiceButton,
                selectOnRight = secondChoiceButton
            };
            var secondNavigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = firstChoiceButton,
                selectOnRight = firstChoiceButton
            };
            firstChoiceButton.navigation = firstNavigation;
            secondChoiceButton.navigation = secondNavigation;

            fadeOverlay = CreateImage("Scene Fade", canvasObject.transform, Color.black,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fadeOverlay.raycastTarget = false;
            fadeOverlay.transform.SetAsLastSibling();

            headsetPromptGroup = CreateGroup("Headset Prompt", canvasObject.transform);
            headsetPromptGroup.alpha = 0f;
            headsetPromptGroup.interactable = false;
            headsetPromptGroup.blocksRaycasts = false;

            var headsetImage = CreateImage("Headset Image", headsetPromptGroup.transform, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(240f, 240f));
            headsetImage.sprite = headsetPromptSprite;
            headsetImage.preserveAspect = true;
            headsetImage.enabled = headsetPromptSprite != null;

            var headsetText = CreateText("Headset Prompt Text", headsetPromptGroup.transform, "헤드셋을 착용해주세요", 38f, FontStyles.Normal,
                new Color(0.92f, 0.95f, 1f), new Vector2(0.5f, 0.5f), new Vector2(900f, 80f));
            headsetText.rectTransform.anchoredPosition = new Vector2(0f, -105f);
            headsetPromptGroup.transform.SetAsLastSibling();

            SetFade(1f);
            EventSystem.current.SetSelectedGameObject(beginButton.gameObject);
        }

        private void CreateRose(Transform parent)
        {
            rose = new GameObject("The Rose").transform;
            rose.SetParent(parent, false);
            rose.position = new Vector3(0f, 0.03f, 4.25f);
            roseBasePosition = rose.position;

            var stemMaterial = CreateLitMaterial("Rose Stem", new Color(0.025f, 0.13f, 0.055f), Color.black, 0f, 0.28f);
            var petalMaterial = CreateLitMaterial("Rose Petals", new Color(0.28f, 0.008f, 0.018f), new Color(0.5f, 0.008f, 0.015f), 0f, 0.58f);
            CreatePrimitive("Stem", PrimitiveType.Cylinder, new Vector3(0f, 0.53f, 0f), new Vector3(0.032f, 0.5f, 0.032f), stemMaterial, rose);

            for (var i = 0; i < 9; i++)
            {
                var angle = i * Mathf.PI * 2f / 9f;
                var radius = i < 3 ? 0.07f : 0.14f;
                var petal = CreatePrimitive("Petal", PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(angle) * radius, 1.08f + (i % 2) * 0.035f, Mathf.Sin(angle) * radius),
                    new Vector3(0.16f, 0.09f, 0.21f), petalMaterial, rose);
                petal.transform.localRotation = Quaternion.Euler(18f, -angle * Mathf.Rad2Deg, 12f * Mathf.Sin(angle));
            }

            var leafLeft = CreatePrimitive("Leaf", PrimitiveType.Sphere, new Vector3(-0.12f, 0.53f, 0f), new Vector3(0.16f, 0.025f, 0.07f), stemMaterial, rose);
            leafLeft.transform.localRotation = Quaternion.Euler(0f, 20f, -28f);
            var leafRight = CreatePrimitive("Leaf", PrimitiveType.Sphere, new Vector3(0.11f, 0.35f, 0.02f), new Vector3(0.14f, 0.022f, 0.06f), stemMaterial, rose);
            leafRight.transform.localRotation = Quaternion.Euler(0f, -20f, 25f);
        }

        private void CreatePlayerCigarette(Transform cameraTransform)
        {
            var paper = CreateLitMaterial("Cigarette Paper", new Color(0.68f, 0.63f, 0.54f), Color.black, 0f, 0.32f);
            var emberMaterial = CreateLitMaterial("Cigarette Ember", new Color(0.3f, 0.025f, 0.005f), new Color(2.5f, 0.16f, 0.015f), 0f, 0.7f);
            var cigarette = CreatePrimitive("Player Cigarette", PrimitiveType.Cylinder, new Vector3(0.54f, -0.38f, 1.22f), new Vector3(0.018f, 0.16f, 0.018f), paper, cameraTransform);
            cigarette.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreatePrimitive("Cigarette Ember", PrimitiveType.Sphere, new Vector3(0.71f, -0.38f, 1.22f), Vector3.one * 0.035f, emberMaterial, cameraTransform);

            var emberLightObject = new GameObject("Ember Light");
            emberLightObject.transform.SetParent(cameraTransform, false);
            emberLightObject.transform.localPosition = new Vector3(0.71f, -0.38f, 1.22f);
            var emberLight = emberLightObject.AddComponent<Light>();
            emberLight.type = LightType.Point;
            emberLight.color = new Color(1f, 0.16f, 0.025f);
            emberLight.intensity = 0.75f;
            emberLight.range = 1.8f;
            emberLight.shadows = LightShadows.None;
        }

        private static void CreatePostProcessing(Transform parent)
        {
            var volumeObject = new GameObject("Title Rain Grade");
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = volume.profile.Add<Bloom>();
            bloom.intensity.Override(0.72f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.62f);
            var vignette = volume.profile.Add<Vignette>();
            vignette.intensity.Override(0.36f);
            vignette.smoothness.Override(0.78f);
            vignette.color.Override(new Color(0.002f, 0.004f, 0.012f));
            var color = volume.profile.Add<ColorAdjustments>();
            color.contrast.Override(18f);
            color.saturation.Override(-12f);
            color.colorFilter.Override(new Color(0.78f, 0.86f, 1f));
        }

        private void AnimateEnvironment()
        {
            var time = Time.unscaledTime;
            if (rose != null)
            {
                rose.position = roseBasePosition + Vector3.up * (Mathf.Sin(time * 0.72f) * 0.012f);
                rose.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 0.22f) * 4f, 0f);
            }

            if (titleCamera != null)
            {
                var basePosition = new Vector3(0f, 1.72f, -8.2f);
                titleCamera.position = basePosition + new Vector3(Mathf.Sin(time * 0.13f) * 0.045f, Mathf.Sin(time * 0.19f) * 0.018f, 0f);
                titleCamera.rotation = Quaternion.LookRotation(new Vector3(0f, 1.05f, 4.2f) - titleCamera.position);
            }
        }

        private void BeginPrologue()
        {
            if (loading || transitioning || questionIndex >= 0) return;
            StartCoroutine(TransitionToQuestion(0));
        }

        private void SelectAnswer(bool firstChoice)
        {
            if (loading || transitioning || questionIndex < 0 || questionIndex >= Questions.Length) return;
            if (questionIndex == Questions.Length - 1)
            {
                StartCoroutine(LoadGameplay());
                return;
            }

            StartCoroutine(TransitionToQuestion(questionIndex + 1));
        }

        private IEnumerator InitialFadeIn()
        {
            transitioning = true;
            yield return FadeCanvasGroup(headsetPromptGroup, 1f, headsetPromptFadeDuration);
            if (headsetPromptHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(headsetPromptHoldDuration);
            yield return FadeCanvasGroup(headsetPromptGroup, 0f, headsetPromptFadeDuration);
            headsetPromptGroup.gameObject.SetActive(false);

            yield return FadeTo(0f, transitionDuration * 2.4f);
            transitioning = false;
            EventSystem.current.SetSelectedGameObject(beginButton.gameObject);
        }

        private IEnumerator TransitionToQuestion(int nextQuestion)
        {
            transitioning = true;
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
            questionGroup.interactable = false;
            questionGroup.blocksRaycasts = false;

            yield return FadeTo(1f, transitionDuration);

            questionIndex = nextQuestion;
            questionText.text = Questions[questionIndex];
            firstChoiceText.text = FirstChoices[questionIndex];
            secondChoiceText.text = SecondChoices[questionIndex];
            titleGroup.alpha = 0f;
            questionGroup.alpha = 1f;
            selectedChoice = 0;

            yield return FadeTo(0f, transitionDuration);

            questionGroup.interactable = true;
            questionGroup.blocksRaycasts = true;
            transitioning = false;
            SelectChoice(0);
        }

        private void SelectChoice(int index)
        {
            if (questionIndex < 0 || transitioning || loading) return;
            selectedChoice = Mathf.Clamp(index, 0, 1);
            var target = selectedChoice == 0 ? firstChoiceButton : secondChoiceButton;
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private IEnumerator LoadGameplay()
        {
            loading = true;
            transitioning = true;
            questionGroup.interactable = false;
            questionGroup.blocksRaycasts = false;
            yield return FadeTo(1f, transitionDuration * 1.5f);

            AsyncOperation operation = null;
            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                operation = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            }
            else if (SceneManager.sceneCountInBuildSettings > 1)
            {
                Debug.LogWarning($"[DoorPuzzle] Gameplay scene '{gameplaySceneName}' is not in Build Settings. Loading build index 1 instead.");
                operation = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            }

            if (operation == null)
            {
                Debug.LogError("[DoorPuzzle] Gameplay scene could not be loaded. Check Build Settings.");
                yield return FadeTo(0f, transitionDuration);
                questionGroup.interactable = true;
                questionGroup.blocksRaycasts = true;
                loading = false;
                transitioning = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

#if false // Legacy IMGUI title UI retained only as migration reference; runtime uses TMP UI below.
        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 58,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.82f, 0.9f, 1f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Italic
            };
            subtitleStyle.normal.textColor = new Color(0.58f, 0.68f, 0.78f);
            questionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                wordWrap = true
            };
            questionStyle.normal.textColor = new Color(0.9f, 0.93f, 0.98f);
            choiceStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            choiceStyle.normal.textColor = new Color(0.78f, 0.86f, 0.94f);
            choiceStyle.hover.textColor = Color.white;
            choiceStyle.active.textColor = Color.white;
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            hintStyle.normal.textColor = new Color(0.42f, 0.5f, 0.6f);
        }

        private void OnGUI()
        {
            EnsureStyles();
            var scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1.45f);
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            DrawRect(new Rect(0f, height - 300f, width, 300f), new Color(0.002f, 0.005f, 0.012f, 0.82f));
            DrawRect(new Rect(0f, 0f, width, 56f), Color.black);
            DrawRect(new Rect(0f, height - 56f, width, 56f), Color.black);

            if (questionIndex < 0 && !loading)
            {
                GUI.Label(new Rect(width * 0.5f - 380f, height * 0.18f, 760f, 90f), "BLUES WITH YOU", titleStyle);
                GUI.Label(new Rect(width * 0.5f - 340f, height * 0.18f + 82f, 680f, 36f), "A rainy alley. A borrowed light. A rose left behind.", subtitleStyle);
                if (GUI.Button(new Rect(width * 0.5f - 110f, height - 188f, 220f, 58f), "BEGIN", choiceStyle))
                    BeginPrologue();
                GUI.Label(new Rect(width * 0.5f - 220f, height - 116f, 440f, 28f), "ENTER / CLICK", hintStyle);
            }
            else if (!loading)
            {
                GUI.Label(new Rect(width * 0.5f - 430f, height - 272f, 860f, 112f), Questions[questionIndex], questionStyle);
                DrawChoice(width * 0.5f - 250f, height - 142f, 0, "YES", true);
                DrawChoice(width * 0.5f + 30f, height - 142f, 1, "NO", false);
                GUI.Label(new Rect(width * 0.5f - 260f, height - 78f, 520f, 28f), "A / D choose    ·    ENTER confirm", hintStyle);
            }
            else
            {
                var ending = finalAnswer ? "The rose remembers." : "The rose remains in the rain.";
                GUI.Label(new Rect(width * 0.5f - 400f, height - 210f, 800f, 90f), ending, questionStyle);
            }

            if (fade > 0.001f) DrawRect(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, fade));
            if (loadFade > 0.001f) DrawRect(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, loadFade));
            GUI.matrix = oldMatrix;
        }

        private void DrawChoice(float x, float y, int index, string label, bool answer)
        {
            var rect = new Rect(x, y, 220f, 58f);
            if (selectedChoice == index)
            {
                DrawRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), new Color(0.28f, 0.62f, 0.9f, 0.72f));
            }
            if (GUI.Button(rect, label, choiceStyle))
            {
                selectedChoice = index;
                SelectAnswer(answer);
            }
        }
#endif

        private IEnumerator FadeTo(float target, float duration)
        {
            var start = fadeOverlay.color.a;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            SetFade(target);
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
        {
            if (group == null) yield break;

            var start = group.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = target;
        }

        private void SetFade(float alpha)
        {
            var color = fadeOverlay.color;
            color.a = Mathf.Clamp01(alpha);
            fadeOverlay.color = color;
        }

        private static CanvasGroup CreateGroup(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            value.transform.SetParent(parent, false);
            Stretch(value.GetComponent<RectTransform>());
            return value.GetComponent<CanvasGroup>();
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style,
            Color color, Vector2 anchor, Vector2 dimensions)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = dimensions;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = koreanFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 dimensions, out TMP_Text labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = dimensions;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.035f, 0.075f, 0.12f, 0.94f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.32f, 0.4f, 0.5f, 1f),
                highlightedColor = new Color(0.62f, 0.82f, 1f, 1f),
                pressedColor = new Color(0.85f, 0.94f, 1f, 1f),
                selectedColor = new Color(0.36f, 0.7f, 1f, 1f),
                disabledColor = new Color(0.18f, 0.2f, 0.24f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };

            labelText = CreateText("Label", buttonObject.transform, label, 27f, FontStyles.Normal,
                new Color(0.94f, 0.97f, 1f), new Vector2(0.5f, 0.5f), dimensions - new Vector2(28f, 10f));
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            return CreatePrimitive(name, PrimitiveType.Cube, position, scale, material, parent);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            var renderer = value.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            var collider = value.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return value;
        }

        private static Material CreateLitMaterial(string name, Color baseColor, Color emission, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }

        private static Light CreatePointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
