using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core.Input;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using AbandonedAsylum.Core;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Điều phối giao diện người dùng theo mô hình MVP (Model - View - Presenter):
    /// Quản lý hiển thị các màn hình HUD, Menu chính, Tạm dừng, Kết thúc trận và Chiến thắng,
    /// kết nối sự kiện giữa trạng thái màn chơi và các thành phần hiển thị.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class UIManager : MonoBehaviour
    {
        private IInputService inputService;
        private IInputService InputService => inputService ??= (ServiceLocator.TryGet(out IInputService svc) ? svc : InputManagerService.Active);

        private static UIManager instance;
        public static UIManager Instance => instance;

        [Header("Prefab-Driven UI Architecture (MVP)")]
        [SerializeField] private BloodlinesUIRootView rootView;
        [SerializeField] private GameObject canvasPrefab;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip buttonHoverClip;

        private AudioSource audioSource;
        private IPlayerStatsNotifier playerStats;
        private IGameStateService gameStateService;
        private IGameStateService GameStateService
        {
            get
            {
                if (gameStateService != null) return gameStateService;
                if (ServiceLocator.TryGet(out IGameStateService svc))
                {
                    gameStateService = svc;
                    return gameStateService;
                }
                gameStateService = GameManager.Instance;
                return gameStateService;
            }
        }

        private float targetHealthValue = 100f;
        private float displayedDelayedHealth = 100f;
        private float lastHealthValue = 100f;
        private float hurtVignetteAlpha = 0f;
        private bool isGamePaused = false;
        public bool IsGamePaused => isGamePaused;

        private float nextSceneRefSearchTime = 0.0f;
        private int cachedSurvivalSeconds = -1;
        private int cachedAmmoCount = -1;
        private int cachedMaxAmmo = -1;

        public BloodlinesUIRootView RootView => rootView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureUIManagerExists()
        {
            if (instance == null && FindFirstObjectByType<UIManager>() == null)
            {
                GameObject uiObj = new GameObject("[UIManager]", typeof(UIManager));
                DontDestroyOnLoad(uiObj);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[UIManager] Duplicate instance on '{gameObject.name}'. Preserving GameObject and removing duplicate component.");
                Destroy(this);
                return;
            }
            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            LoadAudioClips();
            EnsureCanvasHierarchy();
            FindSceneReferences();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            isGamePaused = false;
            GameManager.IsGamePaused = false;
            Time.timeScale = 1.0f;

            UnbindPlayerStats();
            FindSceneReferences();

            EnsureCanvasHierarchy();

            GameState currentState = GameStateService != null ? GameStateService.CurrentState : GameState.MainMenu;
            ApplyPanelVisibility(currentState, false);

            if (GameStateService != null)
            {
                GameStateService.OnGameStateChanged -= HandleGameStateChanged;
                GameStateService.OnPortalProgressChanged -= HandlePortalProgressChanged;
                GameStateService.OnGameStateChanged += HandleGameStateChanged;
                GameStateService.OnPortalProgressChanged += HandlePortalProgressChanged;

                HandleGameStateChanged(GameStateService.CurrentState);
                HandlePortalProgressChanged(GameStateService.PortalsDestroyed, GameStateService.TargetPortalsToDestroy);
            }



            cachedSurvivalSeconds = -1;
            cachedAmmoCount = -1;
            cachedMaxAmmo = -1;
            hurtVignetteAlpha = 0f;
            if (rootView != null && rootView.HUD != null)
            {
                rootView.HUD.SetHurtVignetteAlpha(0f);
            }
        }

        private void Start()
        {
            FindSceneReferences();

            GameState initialState = GameStateService != null ? GameStateService.CurrentState : GameState.MainMenu;
            ApplyPanelVisibility(initialState, false);

            if (GameStateService != null)
            {
                GameStateService.OnGameStateChanged -= HandleGameStateChanged;
                GameStateService.OnPortalProgressChanged -= HandlePortalProgressChanged;

                GameStateService.OnGameStateChanged += HandleGameStateChanged;
                GameStateService.OnPortalProgressChanged += HandlePortalProgressChanged;

                HandleGameStateChanged(GameStateService.CurrentState);
                HandlePortalProgressChanged(GameStateService.PortalsDestroyed, GameStateService.TargetPortalsToDestroy);
            }


        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (GameStateService != null)
            {
                GameStateService.OnGameStateChanged -= HandleGameStateChanged;
                GameStateService.OnPortalProgressChanged -= HandlePortalProgressChanged;
            }

            UnbindPlayerStats();
        }

        private void FindSceneReferences()
        {
            if (playerStats == null && ServiceLocator.TryGet(out IPlayerStatsNotifier notifier))
            {
                BindPlayerStats(notifier);
            }
        }

        private void BindPlayerStats(IPlayerStatsNotifier notifier)
        {
            if (playerStats != null) UnbindPlayerStats();
            playerStats = notifier;
            if (playerStats != null)
            {
                playerStats.OnHealthChanged += HandleHealthChanged;
                playerStats.OnAmmoChanged += HandleAmmoChanged;
                playerStats.OnReloadProgressChanged += HandleReloadProgressChanged;
                playerStats.OnWeaponChanged += HandleWeaponChanged;

                targetHealthValue = playerStats.CurrentHealth;
                displayedDelayedHealth = targetHealthValue;
                lastHealthValue = targetHealthValue;

                if (rootView != null && rootView.HUD != null)
                {
                    rootView.HUD.UpdateHealth(targetHealthValue, playerStats.MaxHealth);
                    rootView.HUD.UpdateAmmo(playerStats.CurrentAmmo, playerStats.MaxAmmo);
                    rootView.HUD.UpdateWeaponName(playerStats.CurrentWeaponName);
                }
            }
        }

        private void UnbindPlayerStats()
        {
            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= HandleHealthChanged;
                playerStats.OnAmmoChanged -= HandleAmmoChanged;
                playerStats.OnReloadProgressChanged -= HandleReloadProgressChanged;
                playerStats.OnWeaponChanged -= HandleWeaponChanged;
                playerStats = null;
            }
        }

        private void HandleWeaponChanged(string weaponName)
        {
            if (rootView != null && rootView.HUD != null)
            {
                rootView.HUD.UpdateWeaponName(weaponName);
            }
        }

        private void HandleAmmoChanged(int current, int max)
        {
            cachedAmmoCount = current;
            cachedMaxAmmo = max;
            if (rootView != null && rootView.HUD != null)
            {
                rootView.HUD.UpdateAmmo(current, max);
            }
        }

        private void HandleReloadProgressChanged(float elapsed, float duration, bool isReloading)
        {
            if (rootView != null && rootView.HUD != null)
            {
                rootView.HUD.UpdateReloadProgress(elapsed, duration, isReloading);
            }
        }

        private void LoadAudioClips()
        {
            if (buttonClickClip == null) buttonClickClip = Resources.Load<AudioClip>("Bloodlines/Audio/Click Button SFX");
            if (buttonHoverClip == null) buttonHoverClip = Resources.Load<AudioClip>("Bloodlines/Audio/Hover Button SFX");
        }

        #region Prefab-Driven Hierarchy & Event Binding
        private void EnsureCanvasHierarchy()
        {
            // 1. Đảm bảo EventSystem luôn tồn tại và tương thích với Input System đang kích hoạt
            EventSystem existingEs = FindFirstObjectByType<EventSystem>();
            if (existingEs == null)
            {
                GameObject es = new GameObject("EventSystem");
                existingEs = es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                existingEs.gameObject.AddComponent<InputSystemUIInputModule>();
#else
                existingEs.gameObject.AddComponent<StandaloneInputModule>();
#endif
                DontDestroyOnLoad(es);
            }
            else
            {
                existingEs.enabled = true;
#if ENABLE_INPUT_SYSTEM
                StandaloneInputModule sim = existingEs.GetComponent<StandaloneInputModule>();
                if (sim != null)
                {
                    // Tránh xung đột làm tê liệt raycast click khi New Input System kích hoạt
                    Destroy(sim);
                }
                if (existingEs.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existingEs.gameObject.AddComponent<InputSystemUIInputModule>();
                }
#else
                StandaloneInputModule sim = existingEs.GetComponent<StandaloneInputModule>();
                if (sim != null) sim.enabled = true;
                else existingEs.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }

            // 2. Tìm hoặc nạp Canvas từ Prefab chuẩn
            if (rootView == null)
            {
                rootView = FindFirstObjectByType<BloodlinesUIRootView>(FindObjectsInactive.Include);
            }

            if (rootView == null)
            {
                GameObject existingCanvas = GameObject.Find("BloodlinesUI_Canvas");
                if (existingCanvas != null)
                {
                    rootView = existingCanvas.GetComponent<BloodlinesUIRootView>() ?? existingCanvas.AddComponent<BloodlinesUIRootView>();
                }
            }

            if (rootView == null)
            {
                GameObject prefab = canvasPrefab;
                if (prefab == null)
                {
                    prefab = Resources.Load<GameObject>("UI/BloodlinesUI_Canvas");
                }

                if (prefab != null)
                {
                    // Instantiate Canvas ra root scene độc lập để giữ trọn vẹn RectTransform và tỷ lệ chuẩn 1920x1080
                    GameObject canvasObj = Instantiate(prefab);
                    canvasObj.name = "BloodlinesUI_Canvas";
                    DontDestroyOnLoad(canvasObj);

                    // Gỡ bỏ ngay lập tức UIManager duplicate nếu có gắn trên prefab cũ
                    UIManager dupUI = canvasObj.GetComponent<UIManager>();
                    if (dupUI != null && dupUI != this)
                    {
                        DestroyImmediate(dupUI);
                    }

                    rootView = canvasObj.GetComponent<BloodlinesUIRootView>();
                    if (rootView == null)
                    {
                        rootView = canvasObj.AddComponent<BloodlinesUIRootView>();
                    }
                }
                else
                {
                    Debug.LogWarning("[UIManager] Không thể nạp prefab 'UI/BloodlinesUI_Canvas' từ Resources!");
                }
            }

            if (rootView != null)
            {
                rootView.BindAllViews();
                BindPresenterEvents();
            }
        }

        private void BindPresenterEvents()
        {
            if (rootView == null) return;

            // Main Menu Events
            if (rootView.MainMenu != null)
            {
                rootView.MainMenu.OnStartClicked -= OnClickStartMission;
                rootView.MainMenu.OnQuitClicked -= OnClickExitGame;

                rootView.MainMenu.OnStartClicked += OnClickStartMission;
                rootView.MainMenu.OnQuitClicked += OnClickExitGame;
            }

            // Pause Menu Events
            if (rootView.PauseMenu != null)
            {
                rootView.PauseMenu.OnResumeClicked -= TogglePause;
                rootView.PauseMenu.OnRestartClicked -= OnClickRetry;
                rootView.PauseMenu.OnReturnMenuClicked -= OnClickReturnToMainMenu;

                rootView.PauseMenu.OnResumeClicked += TogglePause;
                rootView.PauseMenu.OnRestartClicked += OnClickRetry;
                rootView.PauseMenu.OnReturnMenuClicked += OnClickReturnToMainMenu;
            }

            // Game Over Events
            if (rootView.GameOver != null)
            {
                rootView.GameOver.OnRetryClicked -= OnClickRetry;
                rootView.GameOver.OnReturnMenuClicked -= OnClickReturnToMainMenu;
                rootView.GameOver.OnQuitClicked -= OnClickExitGame;

                rootView.GameOver.OnRetryClicked += OnClickRetry;
                rootView.GameOver.OnReturnMenuClicked += OnClickReturnToMainMenu;
                rootView.GameOver.OnQuitClicked += OnClickExitGame;
            }

            // Victory Events
            if (rootView.Victory != null)
            {
                rootView.Victory.OnRetryClicked -= OnClickRetry;
                rootView.Victory.OnReturnMenuClicked -= OnClickReturnToMainMenu;
                rootView.Victory.OnQuitClicked -= OnClickExitGame;

                rootView.Victory.OnRetryClicked += OnClickRetry;
                rootView.Victory.OnReturnMenuClicked += OnClickReturnToMainMenu;
                rootView.Victory.OnQuitClicked += OnClickExitGame;
            }
        }
        #endregion

        private void Update()
        {
            if (playerStats == null)
            {
                if (Time.time >= nextSceneRefSearchTime)
                {
                    nextSceneRefSearchTime = Time.time + 1.0f;
                    FindSceneReferences();
                }
            }

            // Toggle Pause bằng phím ESC
            GameState currentState = GameStateService != null ? GameStateService.CurrentState : GameState.Playing;
            if (currentState == GameState.Playing && InputService.IsPausePressed)
            {
                TogglePause();
            }

            // Smooth hurt vignette fade
            if (rootView != null && rootView.HUD != null)
            {
                float baseLowHealthAlpha = (playerStats != null && playerStats.CurrentHealth < 30f)
                    ? Mathf.PingPong(Time.unscaledTime * 1.5f, 0.35f) + 0.15f
                    : 0f;

                hurtVignetteAlpha = Mathf.MoveTowards(hurtVignetteAlpha, baseLowHealthAlpha, Time.unscaledDeltaTime * 1.8f);
                rootView.HUD.SetHurtVignetteAlpha(hurtVignetteAlpha);
            }

            if (currentState == GameState.Playing && !isGamePaused)
            {
                UpdateInGameHUD();
            }
        }

        private void UpdateInGameHUD()
        {
            if (rootView == null || rootView.HUD == null) return;
            HUDView hud = rootView.HUD;

            // 1. Smooth delayed health interpolation
            if (displayedDelayedHealth > targetHealthValue)
            {
                displayedDelayedHealth = Mathf.MoveTowards(displayedDelayedHealth, targetHealthValue, Time.deltaTime * 35f);
                hud.UpdateDelayedHealth(displayedDelayedHealth);
            }
            else
            {
                displayedDelayedHealth = targetHealthValue;
                hud.UpdateDelayedHealth(displayedDelayedHealth);
            }



            // 3. Throttled Survival Time Update (1 lần/giây)
            if (GameStateService != null)
            {
                float seconds = GameStateService.ElapsedSurvivalTime;
                int totalSecs = (int)seconds;
                if (totalSecs != cachedSurvivalSeconds)
                {
                    cachedSurvivalSeconds = totalSecs;
                    hud.UpdateSurvivalTime(totalSecs / 60, totalSecs % 60);
                }
            }

            // 3. Dynamic Crosshair Spread
            bool isAiming = InputService.IsAimPressed;
            bool isMoving = InputService.MoveInput.sqrMagnitude > 0.05f;
            hud.UpdateCrosshairDynamics(isAiming, isMoving);
        }

        public void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            if (currentHealth < lastHealthValue)
            {
                hurtVignetteAlpha = 0.65f;
                if (rootView != null && rootView.HUD != null)
                {
                    rootView.HUD.SetHurtVignetteAlpha(hurtVignetteAlpha);
                }
            }
            lastHealthValue = currentHealth;
            targetHealthValue = currentHealth;

            if (rootView != null && rootView.HUD != null)
            {
                rootView.HUD.UpdateHealth(currentHealth, maxHealth);
            }
        }

        public void HandlePortalProgressChanged(int destroyed, int total)
        {
            if (rootView != null && rootView.HUD != null)
            {
                rootView.HUD.UpdateObjective(destroyed, total);
            }
        }

        public void HandleGameStateChanged(GameState newState)
        {
            if (newState != GameState.Playing)
            {
                isGamePaused = false;
            }

            ApplyPanelVisibility(newState, isGamePaused);

            if (newState == GameState.GameOver)
            {
                PopulateGameOverStats();
            }
            else if (newState == GameState.Victory)
            {
                PopulateVictoryStats();
            }
        }

        public void ApplyPanelVisibility(GameState state, bool paused)
        {
            // Cursor control luôn được cập nhật chính xác tuyệt đối dù rootView có null hay không
            bool needsCursor = (state != GameState.Playing) || paused;
            Cursor.lockState = needsCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = needsCursor;

            if (rootView == null)
            {
                EnsureCanvasHierarchy();
                if (rootView == null) return;
            }

            if (rootView.HUD != null) rootView.HUD.SetVisible(state == GameState.Playing && !paused);
            if (rootView.MainMenu != null) rootView.MainMenu.SetVisible(state == GameState.MainMenu);
            if (rootView.GameOver != null) rootView.GameOver.SetVisible(state == GameState.GameOver);
            if (rootView.Victory != null) rootView.Victory.SetVisible(state == GameState.Victory);
            if (rootView.PauseMenu != null) rootView.PauseMenu.SetVisible(paused && state == GameState.Playing);
        }

        private void PopulateGameOverStats()
        {
            if (rootView == null || rootView.GameOver == null || GameStateService == null) return;
            float time = GameStateService.ElapsedSurvivalTime;
            rootView.GameOver.SetStats(
                (int)(time / 60),
                (int)(time % 60),
                GameStateService.PortalsDestroyed,
                GameStateService.TargetPortalsToDestroy,
                GameStateService.ZombiesKilled
            );
        }

        private void PopulateVictoryStats()
        {
            if (rootView == null || rootView.Victory == null || GameStateService == null) return;
            float time = GameStateService.ElapsedSurvivalTime;
            rootView.Victory.SetStats(
                (int)(time / 60),
                (int)(time % 60),
                GameStateService.ZombiesKilled
            );
        }

        #region User Actions & Navigation
        public void OnClickStartMission()
        {
            PlayButtonClickSFX();
            isGamePaused = false;
            GameManager.IsGamePaused = false;
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (rootView != null && rootView.MainMenu != null) rootView.MainMenu.SetVisible(false);

            if (GameStateService != null)
            {
                GameStateService.StartMission();
            }
            else
            {
                HandleGameStateChanged(GameState.Playing);
            }
        }

        public void OnClickRetry()
        {
            PlayButtonClickSFX();
            isGamePaused = false;
            GameManager.IsGamePaused = false;
            Time.timeScale = 1.0f;

            if (GameStateService != null)
            {
                GameStateService.RetryMission();
            }
        }

        public void OnClickReturnToMainMenu()
        {
            PlayButtonClickSFX();
            isGamePaused = false;
            GameManager.IsGamePaused = false;
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (GameStateService != null)
            {
                GameStateService.ReturnToMainMenu();
            }
            else
            {
                HandleGameStateChanged(GameState.MainMenu);
            }
        }

        public void OnClickExitGame()
        {
            PlayButtonClickSFX();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void TogglePause()
        {
            isGamePaused = !isGamePaused;
            GameManager.IsGamePaused = isGamePaused;
            Time.timeScale = isGamePaused ? 0.0f : 1.0f;

            if (rootView != null)
            {
                if (rootView.PauseMenu != null) rootView.PauseMenu.SetVisible(isGamePaused);
                if (rootView.HUD != null) rootView.HUD.SetVisible(!isGamePaused);
            }

            Cursor.lockState = isGamePaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isGamePaused;
        }

        public void PlayButtonClickSFX()
        {
            if (audioSource != null && buttonClickClip != null)
            {
                audioSource.PlayOneShot(buttonClickClip, 0.85f);
            }
        }

        public void PlayButtonHoverSFX()
        {
            if (audioSource != null && buttonHoverClip != null)
            {
                audioSource.PlayOneShot(buttonHoverClip, 0.45f);
            }
        }
        #endregion
    }
}
