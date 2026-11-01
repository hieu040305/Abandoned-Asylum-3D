using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Root View nằm trên Prefab Canvas giao diện theo kiến trúc MVP (Model - View - Presenter).
    /// Quản lý, liên kết và điều phối 5 tầng View thành phần:
    /// HUDView, MainMenuView, PauseMenuView, GameOverView, VictoryView.
    /// Đảm bảo tự động gắn kết nối (dynamic auto-binding) an toàn nếu view components chưa được gắn sẵn trên prefab panels.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class BloodlinesUIRootView : MonoBehaviour
    {
        [Header("Sub-Views (MVP Architecture)")]
        [SerializeField] private HUDView hudView;
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private PauseMenuView pauseMenuView;
        [SerializeField] private GameOverView gameOverView;
        [SerializeField] private VictoryView victoryView;

        public HUDView HUD => hudView;
        public MainMenuView MainMenu => mainMenuView;
        public PauseMenuView PauseMenu => pauseMenuView;
        public GameOverView GameOver => gameOverView;
        public VictoryView Victory => victoryView;

        private void Awake()
        {
            BindAllViews();
        }

        public void BindAllViews()
        {
            // 1. Gắn kết nối hoặc khởi tạo View component cho từng Panel trong hierarchy
            EnsureSubView(ref hudView, "HUD_Panel", "HUD", "Panel_HUD");
            if (hudView != null) hudView.BindReferencesIfMissing();

            EnsureSubView(ref mainMenuView, "MainMenu_Panel", "MainMenu", "Panel_MainMenu", "Menu_Panel");
            if (mainMenuView != null) mainMenuView.BindReferencesIfMissing();

            EnsureSubView(ref pauseMenuView, "Pause_Panel", "PauseMenu_Panel", "PauseMenu", "Pause");
            if (pauseMenuView != null) pauseMenuView.BindReferencesIfMissing();

            EnsureSubView(ref gameOverView, "GameOver_Panel", "GameOver", "Panel_GameOver");
            if (gameOverView != null) gameOverView.BindReferencesIfMissing();

            EnsureSubView(ref victoryView, "Victory_Panel", "Victory", "Panel_Victory");
            if (victoryView != null) victoryView.BindReferencesIfMissing();

            // 2. Tự động gắn hiệu ứng hover âm thanh cho toàn bộ Button trong toàn bộ Canvas
            var allButtons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i].GetComponent<BloodlinesButtonHoverFX>() == null)
                {
                    allButtons[i].gameObject.AddComponent<BloodlinesButtonHoverFX>();
                }
            }
        }

        private T EnsureSubView<T>(ref T viewRef, params string[] candidateNames) where T : Component
        {
            if (viewRef != null) return viewRef;

            viewRef = GetComponentInChildren<T>(true);
            if (viewRef != null) return viewRef;

            for (int i = 0; i < candidateNames.Length; i++)
            {
                Transform panel = FindChildRecursive(transform, candidateNames[i]);
                if (panel != null)
                {
                    viewRef = panel.GetComponent<T>() ?? panel.gameObject.AddComponent<T>();
                    return viewRef;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
