using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbandonedAsylum.UI;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class UIHierarchyBindingTests
    {
        private GameObject canvasObj;
        private BloodlinesUIRootView rootView;

        [SetUp]
        public void SetUp()
        {
            canvasObj = new GameObject("BloodlinesUI_Canvas");
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            rootView = canvasObj.AddComponent<BloodlinesUIRootView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasObj != null)
            {
                Object.DestroyImmediate(canvasObj);
            }
        }

        [Test]
        public void Test_BloodlinesUIRootView_AutoBindsAllViewsFromPanelNames()
        {
            GameObject hudPanel = new GameObject("HUD_Panel");
            hudPanel.transform.SetParent(canvasObj.transform, false);

            GameObject mainMenuPanel = new GameObject("MainMenu_Panel");
            mainMenuPanel.transform.SetParent(canvasObj.transform, false);

            GameObject pausePanel = new GameObject("Pause_Panel");
            pausePanel.transform.SetParent(canvasObj.transform, false);

            GameObject gameOverPanel = new GameObject("GameOver_Panel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);

            GameObject victoryPanel = new GameObject("Victory_Panel");
            victoryPanel.transform.SetParent(canvasObj.transform, false);

            rootView.BindAllViews();

            Assert.IsNotNull(rootView.HUD, "HUDView phải được tự động gắn kết nối vào HUD_Panel");
            Assert.IsNotNull(rootView.MainMenu, "MainMenuView phải được tự động gắn kết nối vào MainMenu_Panel");
            Assert.IsNotNull(rootView.PauseMenu, "PauseMenuView phải được tự động gắn kết nối vào Pause_Panel");
            Assert.IsNotNull(rootView.GameOver, "GameOverView phải được tự động gắn kết nối vào GameOver_Panel");
            Assert.IsNotNull(rootView.Victory, "VictoryView phải được tự động gắn kết nối vào Victory_Panel");
        }

        [Test]
        public void Test_MainMenuView_StartButtonClick_InvokesOnStartClicked()
        {
            GameObject mainMenuPanel = new GameObject("MainMenu_Panel");
            mainMenuPanel.transform.SetParent(canvasObj.transform, false);
            MainMenuView mainMenuView = mainMenuPanel.AddComponent<MainMenuView>();

            GameObject startBtnObj = new GameObject("StartButton");
            startBtnObj.transform.SetParent(mainMenuPanel.transform, false);
            Button startBtn = startBtnObj.AddComponent<Button>();

            mainMenuView.BindReferencesIfMissing();

            bool wasClicked = false;
            mainMenuView.OnStartClicked += () => { wasClicked = true; };

            startBtn.onClick.Invoke();

            Assert.IsTrue(wasClicked, "Sự kiện OnStartClicked phải được kích hoạt khi StartButton được click");
        }

        [Test]
        public void Test_BloodlinesButtonHoverFX_AutoAttachedToAllButtons()
        {
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObj.transform, false);

            GameObject btn1 = new GameObject("Btn1");
            btn1.transform.SetParent(panel.transform, false);
            btn1.AddComponent<Button>();

            GameObject btn2 = new GameObject("Btn2");
            btn2.transform.SetParent(panel.transform, false);
            btn2.AddComponent<Button>();

            rootView.BindAllViews();

            Assert.IsNotNull(btn1.GetComponent<BloodlinesButtonHoverFX>(), "Btn1 phải được tự động gắn BloodlinesButtonHoverFX");
            Assert.IsNotNull(btn2.GetComponent<BloodlinesButtonHoverFX>(), "Btn2 phải được tự động gắn BloodlinesButtonHoverFX");
        }

        [Test]
        public void Test_GameOverView_StatsText_DoesNotOverwriteTitle()
        {
            GameObject gameOverPanel = new GameObject("GameOver_Panel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            GameOverView gameOverView = gameOverPanel.AddComponent<GameOverView>();

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(gameOverPanel.transform, false);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "NHIỆM VỤ THẤT BẠI";

            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(gameOverPanel.transform, false);
            var subtitleTmp = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleTmp.text = "SUBTITLE";

            GameObject statsBoxObj = new GameObject("StatsBox");
            statsBoxObj.transform.SetParent(gameOverPanel.transform, false);

            GameObject statsTextObj = new GameObject("StatsText");
            statsTextObj.transform.SetParent(statsBoxObj.transform, false);
            var statsTmp = statsTextObj.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "OLD STATS";

            gameOverView.BindReferencesIfMissing();
            gameOverView.SetStats(1, 45, 2, 3, 14);

            // Title phải được bảo toàn nguyên vẹn
            Assert.AreEqual("NHIỆM VỤ THẤT BẠI", titleTmp.text);
            // StatsText phải chứa thông tin chính xác, phân tách gọn gàng
            Assert.IsTrue(statsTmp.text.Contains("01:45"), "StatsText phải chứa thời gian sinh tồn");
            Assert.IsTrue(statsTmp.text.Contains("2 / 3"), "StatsText phải chứa số lượng cổng dị thường");
            Assert.IsTrue(statsTmp.text.Contains("14 Quái vật"), "StatsText phải chứa số dị biến đã tiêu diệt");
        }

        [Test]
        public void Test_VictoryView_StatsText_DoesNotOverwriteTitle()
        {
            GameObject victoryPanel = new GameObject("Victory_Panel");
            victoryPanel.transform.SetParent(canvasObj.transform, false);
            VictoryView victoryView = victoryPanel.AddComponent<VictoryView>();

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(victoryPanel.transform, false);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "PHONG TỎA THÀNH CÔNG";

            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(victoryPanel.transform, false);
            var subtitleTmp = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleTmp.text = "SUBTITLE";

            GameObject statsBoxObj = new GameObject("StatsBox");
            statsBoxObj.transform.SetParent(victoryPanel.transform, false);

            GameObject statsTextObj = new GameObject("StatsText");
            statsTextObj.transform.SetParent(statsBoxObj.transform, false);
            var statsTmp = statsTextObj.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "OLD STATS";

            victoryView.BindReferencesIfMissing();
            victoryView.SetStats(3, 12, 42);

            // Title phải được bảo toàn nguyên vẹn
            Assert.AreEqual("PHONG TỎA THÀNH CÔNG", titleTmp.text);
            // StatsText phải chứa thông tin kết quả chiến thắng
            Assert.IsTrue(statsTmp.text.Contains("03:12"), "StatsText phải chứa thời gian hoàn thành");
            Assert.IsTrue(statsTmp.text.Contains("42 Quái vật"), "StatsText phải chứa số lượng dị biến thanh trừng");
        }
    }
}
