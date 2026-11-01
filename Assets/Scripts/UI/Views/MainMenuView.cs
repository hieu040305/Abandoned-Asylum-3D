using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Tầng View của Màn hình Menu Chính (Main Menu) theo mô hình MVP (Model - View - Presenter).
    /// Chịu trách nhiệm hiển thị panel Menu chính và phát sự kiện click nút bắt đầu/thoát game.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        public event Action OnStartClicked;
        public event Action OnQuitClicked;

        private void Awake()
        {
            BindReferencesIfMissing();
        }

        public void BindReferencesIfMissing()
        {
            if (startButton == null) startButton = FindButtonByName("StartButton", "Start", "Btn_Start", "BtnStart");
            if (quitButton == null) quitButton = FindButtonByName("ExitButton", "QuitButton", "Exit", "Quit", "Btn_Quit", "Btn_Exit");

            SetupListeners();
        }

        private void SetupListeners()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => OnStartClicked?.Invoke());
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(() => OnQuitClicked?.Invoke());
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
