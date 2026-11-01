using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Tầng View của Màn hình Thất Bại (Game Over) theo mô hình MVP (Model - View - Presenter).
    /// Đảm bảo phân tách rõ ràng Title, Subtitle và StatsText trong StatsBox,
    /// triệt tiêu hoàn toàn lỗi chồng lấn chữ và tràn layout.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameOverView : MonoBehaviour
    {
        [Header("Text Components")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text statsText;

        [Header("Action Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button returnMenuButton;
        [SerializeField] private Button quitButton;

        public event Action OnRetryClicked;
        public event Action OnReturnMenuClicked;
        public event Action OnQuitClicked;

        private void Awake()
        {
            BindReferencesIfMissing();
        }

        public void BindReferencesIfMissing()
        {
            if (titleText == null) titleText = FindChildComponent<TMP_Text>("Title");
            if (subtitleText == null) subtitleText = FindChildComponent<TMP_Text>("Subtitle");

            // Tìm StatsText trong StatsBox chuyên biệt
            Transform statsBox = FindChildTransform("StatsBox");
            if (statsText == null && statsBox != null)
            {
                statsText = statsBox.GetComponentInChildren<TMP_Text>(true);
            }

            if (statsText == null)
            {
                statsText = FindChildComponent<TMP_Text>("StatsText") ?? FindChildComponent<TMP_Text>("Stats");
            }

            // Đảm bảo không bao giờ nhận nhầm Title hoặc Subtitle làm statsText
            if (statsText == null || statsText == titleText || statsText == subtitleText)
            {
                if (statsBox != null)
                {
                    GameObject go = new GameObject("StatsText", typeof(RectTransform));
                    go.transform.SetParent(statsBox, false);
                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.offsetMin = new Vector2(15, 10);
                    rt.offsetMax = new Vector2(-15, -10);
                    statsText = go.AddComponent<TextMeshProUGUI>();
                }
                else
                {
                    var allTmps = GetComponentsInChildren<TMP_Text>(true);
                    for (int i = 0; i < allTmps.Length; i++)
                    {
                        if (allTmps[i] != titleText && allTmps[i] != subtitleText)
                        {
                            statsText = allTmps[i];
                            break;
                        }
                    }
                }
            }

            if (retryButton == null) retryButton = FindButtonByName("RetryButton", "RestartButton", "Retry", "Restart", "Btn_Retry");
            if (returnMenuButton == null) returnMenuButton = FindButtonByName("MenuButton", "QuitButton", "ReturnButton", "Menu", "Return", "Btn_Menu");
            if (quitButton == null) quitButton = FindButtonByName("ExitButton", "QuitButton", "Exit", "Quit", "Btn_Quit");

            SetupListeners();
            VietnameseFontProvider.EnsureAllVietnameseFonts(gameObject);
        }

        private void SetupListeners()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            }

            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.RemoveAllListeners();
                returnMenuButton.onClick.AddListener(() => OnReturnMenuClicked?.Invoke());
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
            if (visible)
            {
                VietnameseFontProvider.EnsureAllVietnameseFonts(gameObject);
            }
        }

        public void SetStats(int mins, int secs, int portals, int totalPortals, int kills)
        {
            VietnameseFontProvider.EnsureAllVietnameseFonts(gameObject);

            if (titleText != null)
            {
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = 28f;
                titleText.fontSizeMax = 44f;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.SetText("NHIỆM VỤ THẤT BẠI");
                titleText.color = new Color(0.92f, 0.18f, 0.18f, 1f);
            }

            if (subtitleText != null)
            {
                subtitleText.enableAutoSizing = true;
                subtitleText.fontSizeMin = 13f;
                subtitleText.fontSizeMax = 17f;
                subtitleText.alignment = TextAlignmentOptions.Center;
                subtitleText.SetText("BẠN ĐÃ BỊ KÝ SINH TRÙNG VÀ DỊ BIẾN NUỐT CHỬNG");
                subtitleText.color = new Color(0.95f, 0.65f, 0.65f, 0.95f);
            }

            if (statsText != null)
            {
                statsText.enableAutoSizing = true;
                statsText.fontSizeMin = 12f;
                statsText.fontSizeMax = 15f;
                statsText.lineSpacing = 10f;
                statsText.margin = new Vector4(12f, 8f, 12f, 8f);
                statsText.alignment = TextAlignmentOptions.Center;

                statsText.SetText(
                    "<color=#CCCCCC>Thời Gian Sinh Tồn:</color> <color=#FFFFFF><b>{0:D2}:{1:D2}</b></color>   |   <color=#CCCCCC>Cổng Dị Thường:</color> <color=#E0D0B8><b>{2} / {3}</b></color>\n" +
                    "<color=#CCCCCC>Dị Biến Đã Tiêu Diệt:</color> <color=#FF6666><b>{4} Quái vật</b></color>\n\n" +
                    "<size=85%><color=#A0A0A0>Khu vực Karlheinz chưa được giải phóng. Dịch bệnh tiếp tục lây lan.</color></size>",
                    mins, secs, portals, totalPortals, kills);
            }
        }

        private Transform FindChildTransform(string childName)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.Equals(childName, StringComparison.OrdinalIgnoreCase)) return transforms[i];
            }
            return null;
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            var components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].name.Equals(childName, StringComparison.OrdinalIgnoreCase)) return components[i];
            }
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].name.IndexOf(childName, StringComparison.OrdinalIgnoreCase) >= 0) return components[i];
            }
            return null;
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