using AbandonedAsylum.Interfaces;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbandonedAsylum.Core
{

    public class GameManager : MonoBehaviour, IGameStateService
    {
        private static GameManager instance;
        private static bool isApplicationQuitting = false;

        public static bool IsGamePaused { get; set; } = false;
        public static event Action OnSceneResetRequested;
        public static event Action<int> OnCoordinatePortalsRequested;

        public static GameManager Instance
        {
            get
            {
                if (isApplicationQuitting) return null;
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return null;
#endif
                if (instance == null && Application.isPlaying)
                {
                    instance = FindFirstObjectByType<GameManager>();
                    if (instance == null && !isApplicationQuitting)
                    {
                        GameObject go = new GameObject("[GameManager]");
                        instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isApplicationQuitting = false;
            StartInMenuAfterReload = true;
            IsGamePaused = false;
            OnSceneResetRequested = null;
            OnCoordinatePortalsRequested = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeGameManager()
        {
            if (isApplicationQuitting) return;
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[GameManager]");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isSceneTransitioning = false;
            Time.timeScale = 1.0f;
            portalsDestroyed = 0;
            zombiesKilled = 0;
            survivalDuration = 0.0f;
            missionStartTime = Time.time;

            // Reset static state của DimensionalPortal để quái và cổng luôn được thiết lập chuẩn xác
            IsGamePaused = false;
            OnSceneResetRequested?.Invoke();

            bool shouldOpenMenu = StartInMenuAfterReload;
            StartInMenuAfterReload = true;

            if (shouldOpenMenu)
            {
                currentState = GameState.MainMenu;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                OnGameStateChanged?.Invoke(currentState);
                OnPortalProgressChanged?.Invoke(portalsDestroyed, targetPortalsToDestroy);
            }
            else
            {
                StartMission();
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                ServiceLocator.UnregisterService<IGameStateService>();
            }
        }

        public static bool StartInMenuAfterReload = true;

        [Header("Game Flow & Rules")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private int targetPortalsToDestroy = 3;

        private int portalsDestroyed = 0;
        private int zombiesKilled = 0;
        private float missionStartTime = 0.0f;
        private float survivalDuration = 0.0f;
        private bool isSceneTransitioning = false;

        

        public event Action<GameState> OnGameStateChanged;
        public event Action<int, int> OnPortalProgressChanged;

        public GameState CurrentState => currentState;
        public int TargetPortalsToDestroy => targetPortalsToDestroy;
        public int PortalsDestroyed => portalsDestroyed;
        public int ZombiesKilled => zombiesKilled;
        public float ElapsedSurvivalTime => currentState == GameState.Playing 
            ? (Time.time - missionStartTime) 
            : survivalDuration;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            ServiceLocator.RegisterService<IGameStateService>(this);

            currentState = StartInMenuAfterReload ? GameState.MainMenu : GameState.Playing;
            StartInMenuAfterReload = true;
            IsGamePaused = false;
        }

        private void Start()
        {
            if (currentState == GameState.Playing)
            {
                StartMission();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                OnGameStateChanged?.Invoke(currentState);
                OnPortalProgressChanged?.Invoke(portalsDestroyed, targetPortalsToDestroy);
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                survivalDuration = Time.time - missionStartTime;
            }
        }

        public void StartMission()
        {
            currentState = GameState.Playing;
            portalsDestroyed = 0;
            zombiesKilled = 0;
            missionStartTime = Time.time;
            survivalDuration = 0.0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnCoordinatePortalsRequested?.Invoke(targetPortalsToDestroy);

            OnGameStateChanged?.Invoke(currentState);
            OnPortalProgressChanged?.Invoke(portalsDestroyed, targetPortalsToDestroy);
        }

        public void OnPortalDestroyed()
        {
            if (currentState != GameState.Playing) return;

            portalsDestroyed++;
            OnPortalProgressChanged?.Invoke(portalsDestroyed, targetPortalsToDestroy);

            if (portalsDestroyed >= targetPortalsToDestroy)
            {
                TriggerVictory();
            }
        }

        public void OnZombieKilled()
        {
            zombiesKilled++;
        }

        public void OnPlayerDied()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.GameOver;
            survivalDuration = Time.time - missionStartTime;
            Time.timeScale = 1.0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnGameStateChanged?.Invoke(currentState);
        }

        public void TriggerVictory()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.Victory;
            survivalDuration = Time.time - missionStartTime;
            Time.timeScale = 1.0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnGameStateChanged?.Invoke(currentState);
        }

        public void RetryMission()
        {
            if (isSceneTransitioning) return;
            StartInMenuAfterReload = false;
            currentState = GameState.Playing;
            portalsDestroyed = 0;
            zombiesKilled = 0;
            survivalDuration = 0.0f;
            missionStartTime = Time.time;
            Time.timeScale = 1.0f;

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Karlheinz_Asylum_Main";
            StartCoroutine(DeferredSceneLoadRoutine(sceneName));
        }

        public void ReturnToMainMenu()
        {
            if (isSceneTransitioning) return;
            StartInMenuAfterReload = true;
            currentState = GameState.MainMenu;
            portalsDestroyed = 0;
            zombiesKilled = 0;
            survivalDuration = 0.0f;
            Time.timeScale = 1.0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Karlheinz_Asylum_Main";
            StartCoroutine(DeferredSceneLoadRoutine(sceneName));
        }

        private IEnumerator DeferredSceneLoadRoutine(string sceneName)
        {
            isSceneTransitioning = true;
            Time.timeScale = 1.0f;
            yield return null;
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = true;
                while (!asyncOp.isDone)
                {
                    yield return null;
                }
            }
            yield return null;
            isSceneTransitioning = false;
        }
    }
}
