using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Tầng View của Màn hình Tạm dừng Trò chơi (Pause Menu) theo mô hình MVP (Model - View - Presenter).
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnMenuButton;

        public event Action OnResumeClicked;
        public event Action OnRestartClicked;
        public event Action OnReturnMenuClicked;

        private void Awake()
        {
            BindReferencesIfMissing();
        }

        public void BindReferencesIfMissing()
        {
            if (resumeButton == null) resumeButton = FindButtonByName("ResumeButton", "Resume", "Btn_Resume", "BtnResume");
            if (restartButton == null) restartButton = FindButtonByName("RestartButton", "Restart", "RetryButton", "Retry", "Btn_Restart");
            if (returnMenuButton == null) returnMenuButton = FindButtonByName("QuitButton", "MenuButton", "ReturnButton", "ExitButton", "Return", "Menu", "Btn_Menu");

            SetupListeners();
        }

        private void SetupListeners()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(() => OnResumeClicked?.Invoke());
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
            }

            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.RemoveAllListeners();
                returnMenuButton.onClick.AddListener(() => OnReturnMenuClicked?.Invoke());
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private Button FindButtonByName(params string[] names)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    if (buttons[i].name.Equals(names[n], StringComparison.OrdinalIgnoreCase)) return buttons[i];
                }
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    if (buttons[i].name.IndexOf(names[n], StringComparison.OrdinalIgnoreCase) >= 0) return buttons[i];
                }
            }

            return null;
        }
    }
}
