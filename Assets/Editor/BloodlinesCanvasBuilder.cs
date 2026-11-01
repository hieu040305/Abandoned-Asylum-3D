using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using AbandonedAsylum.UI;

namespace AbandonedAsylum.Editor
{
    public static class BloodlinesCanvasBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/BloodlinesUI_Canvas.prefab";
        private const string AltPrefabPath = "Assets/Prefabs/UI/BloodlinesUI_Canvas.prefab";

        [InitializeOnLoadMethod]
        private static void AutoEnsurePrefabOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PrefabPath))
                {
                    Debug.Log("[BloodlinesCanvasBuilder] Auto-generating Vietnamese Bloodlines UI Canvas Prefab...");
                    BuildCanvasPrefab(false);
                }
            };
        }

        [MenuItem("Tools/Abandoned Asylum/Build Bloodlines UI Prefab", false, 10)]
        public static void BuildCanvasPrefabMenu()
        {
            BuildCanvasPrefab(true);
        }

        [MenuItem("Tools/Abandoned Asylum/Add Bloodlines UI To Active Scene", false, 11)]
        public static void AddCanvasToActiveScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = BuildCanvasPrefab(false);
            }

            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không thể tìm thấy hoặc build Bloodlines UI Canvas Prefab!", "OK");
                return;
            }

            UIManager existing = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Đã Tồn Tại", "UIManager / Bloodlines UI Canvas đã có sẵn trong scene!", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "BloodlinesUI_Canvas";
            Undo.RegisterCreatedObjectUndo(instance, "Add Bloodlines UI Canvas");
            Selection.activeGameObject = instance;
            EditorUtility.DisplayDialog("Thành Công", "Bloodlines UI Canvas tiếng Việt đã được thêm vào scene!", "OK");
        }

        public static GameObject BuildCanvasPrefab(bool selectAfterBuild)
        {
            Directory.CreateDirectory("Assets/Resources/UI");
            Directory.CreateDirectory("Assets/Prefabs/UI");

            // Nạp Sprite Bloodlines
            Sprite frameMainMenu = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Frame/Frame_main_menu.png");
            Sprite frameMainMenuRed = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Frame/Frame_main_menu_red.png");
            Sprite frameBackground = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Frame/Frame_background.png");
            Sprite frameOutline = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Frame/Frame_outline.png");
            Sprite frameOutlineRed = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Frame/Frame_outline_red.png");

            Sprite btnRedDef = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Button/Button1/Status_Red_Default.png");
            Sprite btnRedHov = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Button/Button1/Status_Red_Hover.png");
            Sprite btnRedPress = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Button/Button1/Status_Pressed.png");
            Sprite btnRedDis = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Button/Button1/Status_Disable.png");

            Sprite btnGreyDef = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Button/Button1/Status_Grey_Default.png");
            Sprite btnGreyHov = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Button/Button1/Status_Grey_Hover.png");

            Sprite barEmpty = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_empty_v1.png");
            Sprite barFull = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v1.png");

            Sprite iconAmmo = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Icon/Icons-1.png");
            Sprite iconHeart = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Icon/Icons-17.png");
            Sprite iconSkull = LoadSprite("Assets/Alebardium/Bloodlines UI/Textures/Icon/Icons-18.png");

            // Tạo/Nạp font hỗ trợ 100% tiếng Việt
            TMP_FontAsset fontTitle = null;
            TMP_FontAsset fontBody = null;

            Font viFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/segoeuib.ttf");
            if (viFont != null)
            {
                fontTitle = TMP_FontAsset.CreateFontAsset(viFont);
                fontBody = fontTitle;
            }
            if (fontTitle == null)
            {
                fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Alebardium/Bloodlines UI/Fonts/MedievalSharp SDF.asset");
                fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Alebardium/Bloodlines UI/Fonts/ManufacturingConsent SDF.asset");
            }

            AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Alebardium/Bloodlines UI/Audio/Click Button SFX.wav");
            AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Alebardium/Bloodlines UI/Audio/Hover Button SFX.wav");

            // Root Canvas
            GameObject root = new GameObject("BloodlinesUI_Canvas");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = root.AddComponent<GraphicRaycaster>();

            // Blood Hurt Vignette
            GameObject vignetteObj = CreateRect("BloodHurtVignette", root.transform);
            StretchFull(vignetteObj.GetComponent<RectTransform>());
            Image hurtImg = vignetteObj.AddComponent<Image>();
            hurtImg.color = new Color(0.85f, 0.05f, 0.05f, 0f);
            hurtImg.raycastTarget = false;
            if (frameBackground != null)
            {
                hurtImg.sprite = frameBackground;
                hurtImg.type = Image.Type.Sliced;
            }

            // === HUD Panel ===
            GameObject hudPanel = CreateRect("HUD_Panel", root.transform);
            StretchFull(hudPanel.GetComponent<RectTransform>());

            // Health Widget
            GameObject hpWidget = CreateRect("HealthWidget", hudPanel.transform);
            RectTransform hpRect = hpWidget.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0, 1); hpRect.anchorMax = new Vector2(0, 1);
            hpRect.pivot = new Vector2(0, 1);
            hpRect.anchoredPosition = new Vector2(40, -40);
            hpRect.sizeDelta = new Vector2(420, 58);

            Image hpBg = hpWidget.AddComponent<Image>();
            if (barEmpty != null) { hpBg.sprite = barEmpty; hpBg.type = Image.Type.Simple; }
            else { hpBg.color = new Color(0.08f, 0.08f, 0.10f, 0.85f); }

            GameObject delObj = CreateRect("DelayedSlider", hpWidget.transform);
            RectTransform delRect = delObj.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(0.08f, 0.18f); delRect.anchorMax = new Vector2(0.92f, 0.82f);
            delRect.offsetMin = Vector2.zero; delRect.offsetMax = Vector2.zero;
            Slider delSlider = delObj.AddComponent<Slider>();
            delSlider.interactable = false;
            delSlider.transition = Selectable.Transition.None;
            GameObject delFill = CreateRect("Fill", delObj.transform);
            StretchFull(delFill.GetComponent<RectTransform>());
            Image delFillImg = delFill.AddComponent<Image>();
            delFillImg.color = new Color(0.92f, 0.55f, 0.12f, 0.85f);
            delSlider.fillRect = delFill.GetComponent<RectTransform>();
            delSlider.targetGraphic = delFillImg;

            GameObject hpSliderObj = CreateRect("HealthSlider", hpWidget.transform);
            RectTransform hpSRect = hpSliderObj.GetComponent<RectTransform>();
            hpSRect.anchorMin = new Vector2(0.08f, 0.18f); hpSRect.anchorMax = new Vector2(0.92f, 0.82f);
            hpSRect.offsetMin = Vector2.zero; hpSRect.offsetMax = Vector2.zero;
            Slider hpSlider = hpSliderObj.AddComponent<Slider>();
            hpSlider.interactable = false;
            hpSlider.transition = Selectable.Transition.None;
            GameObject hpFill = CreateRect("Fill", hpSliderObj.transform);
            StretchFull(hpFill.GetComponent<RectTransform>());
            Image hpFillImg = hpFill.AddComponent<Image>();
            if (barFull != null)
            {
                hpFillImg.sprite = barFull;
                hpFillImg.type = Image.Type.Filled;
                hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            }
            else
            {
                hpFillImg.color = new Color(0.85f, 0.12f, 0.12f, 0.95f);
            }
            hpSlider.fillRect = hpFill.GetComponent<RectTransform>();
            hpSlider.targetGraphic = hpFillImg;

            Image hpIcon = null;
            if (iconHeart != null)
            {
                GameObject hIconObj = CreateRect("HealthIcon", hpWidget.transform);
                RectTransform hiRect = hIconObj.GetComponent<RectTransform>();
                hiRect.anchorMin = new Vector2(0, 0.5f); hiRect.anchorMax = new Vector2(0, 0.5f);
                hiRect.pivot = new Vector2(0.5f, 0.5f);
                hiRect.anchoredPosition = new Vector2(-15, 0);
                hiRect.sizeDelta = new Vector2(36, 36);
                hpIcon = hIconObj.AddComponent<Image>();
                hpIcon.sprite = iconHeart;
                hpIcon.color = new Color(0.95f, 0.25f, 0.25f, 0.95f);
            }

            TMP_Text hpText = AddText(hpWidget.transform, "HP_Text", "100 / 100 HP", 20, TextAlignmentOptions.Center, Color.white, fontTitle);
            StretchFull(hpText.GetComponent<RectTransform>());

            // Objective Card Tiếng Việt
            GameObject objCard = CreateRect("ObjectiveCard", hudPanel.transform);
            RectTransform objRect = objCard.GetComponent<RectTransform>();
            objRect.anchorMin = new Vector2(1, 1); objRect.anchorMax = new Vector2(1, 1);
            objRect.pivot = new Vector2(1, 1);
            objRect.anchoredPosition = new Vector2(-40, -40);
            objRect.sizeDelta = new Vector2(360, 85);
            Image objBg = objCard.AddComponent<Image>();
            if (frameOutline != null) { objBg.sprite = frameOutline; objBg.type = Image.Type.Sliced; }
            objBg.color = new Color(0.06f, 0.06f, 0.08f, 0.92f);

            Image objIcon = null;
            if (iconSkull != null)
            {
                GameObject sIconObj = CreateRect("SkullIcon", objCard.transform);
                RectTransform siRect = sIconObj.GetComponent<RectTransform>();
                siRect.anchorMin = new Vector2(0, 0.5f); siRect.anchorMax = new Vector2(0, 0.5f);
                siRect.pivot = new Vector2(0.5f, 0.5f);
                siRect.anchoredPosition = new Vector2(25, 0);
                siRect.sizeDelta = new Vector2(32, 32);
                objIcon = sIconObj.AddComponent<Image>();
                objIcon.sprite = iconSkull;
                objIcon.color = new Color(0.90f, 0.85f, 0.75f, 0.95f);
            }

            TMP_Text objText = AddText(objCard.transform, "ObjectiveText", "CỔNG DỊ THƯỜNG: 0 / 3", 18, TextAlignmentOptions.TopLeft, new Color(0.95f, 0.90f, 0.85f), fontTitle);
            RectTransform otRect = objText.GetComponent<RectTransform>();
            otRect.anchoredPosition = new Vector2(52, -14); otRect.sizeDelta = new Vector2(290, 26);

            TMP_Text timeText = AddText(objCard.transform, "TimeText", "THỜI GIAN: 00:00", 17, TextAlignmentOptions.BottomLeft, new Color(0.85f, 0.75f, 0.55f), fontBody);
            RectTransform ttRect = timeText.GetComponent<RectTransform>();
            ttRect.anchoredPosition = new Vector2(52, -48); ttRect.sizeDelta = new Vector2(290, 24);

            // Ammo Card Tiếng Việt
            GameObject ammoCard = CreateRect("AmmoCard", hudPanel.transform);
            RectTransform ammoRect = ammoCard.GetComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(1, 0); ammoRect.anchorMax = new Vector2(1, 0);
            ammoRect.pivot = new Vector2(1, 0);
            ammoRect.anchoredPosition = new Vector2(-40, 40);
            ammoRect.sizeDelta = new Vector2(280, 95);
            Image ammoBg = ammoCard.AddComponent<Image>();
            if (frameOutline != null) { ammoBg.sprite = frameOutline; ammoBg.type = Image.Type.Sliced; }
            ammoBg.color = new Color(0.06f, 0.06f, 0.08f, 0.92f);

            Image amIcon = null;
            if (iconAmmo != null)
            {
                GameObject aIconObj = CreateRect("AmmoIcon", ammoCard.transform);
                RectTransform aiRect = aIconObj.GetComponent<RectTransform>();
                aiRect.anchorMin = new Vector2(0, 0.5f); aiRect.anchorMax = new Vector2(0, 0.5f);
                aiRect.pivot = new Vector2(0.5f, 0.5f);
                aiRect.anchoredPosition = new Vector2(28, 5);
                aiRect.sizeDelta = new Vector2(34, 34);
                amIcon = aIconObj.AddComponent<Image>();
                amIcon.sprite = iconAmmo;
                amIcon.color = new Color(0.95f, 0.85f, 0.65f, 0.95f);
            }

            TMP_Text wNameText = AddText(ammoCard.transform, "WeaponName", "SÚNG TRƯỜNG TẤN CÔNG", 14, TextAlignmentOptions.TopLeft, new Color(0.70f, 0.72f, 0.78f), fontTitle);
            RectTransform wnRect = wNameText.GetComponent<RectTransform>();
            wnRect.anchoredPosition = new Vector2(58, -12); wnRect.sizeDelta = new Vector2(210, 20);

            TMP_Text ammoDigits = AddText(ammoCard.transform, "AmmoText", "40 / 40", 32, TextAlignmentOptions.Left, new Color(0.95f, 0.90f, 0.85f), fontBody);
            RectTransform adRect = ammoDigits.GetComponent<RectTransform>();
            adRect.anchoredPosition = new Vector2(58, -42); adRect.sizeDelta = new Vector2(210, 42);

            // Reload Bar
            GameObject reloadObj = CreateRect("ReloadBar", ammoCard.transform);
            RectTransform rBarRect = reloadObj.GetComponent<RectTransform>();
            rBarRect.anchorMin = new Vector2(0.05f, 0.06f); rBarRect.anchorMax = new Vector2(0.95f, 0.16f);
            rBarRect.offsetMin = Vector2.zero; rBarRect.offsetMax = Vector2.zero;
            Slider rSlider = reloadObj.AddComponent<Slider>();
            rSlider.interactable = false;
            rSlider.transition = Selectable.Transition.None;
            GameObject rFill = CreateRect("Fill", reloadObj.transform);
            StretchFull(rFill.GetComponent<RectTransform>());
            Image rFillImg = rFill.AddComponent<Image>();
            rFillImg.color = new Color(0.95f, 0.65f, 0.15f, 0.95f);
            rSlider.fillRect = rFill.GetComponent<RectTransform>();
            reloadObj.SetActive(false);

            // Crosshair
            GameObject crossRootObj = CreateRect("CrosshairRoot", hudPanel.transform);
            RectTransform crossRoot = crossRootObj.GetComponent<RectTransform>();
            crossRoot.anchorMin = new Vector2(0.5f, 0.5f); crossRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crossRoot.pivot = new Vector2(0.5f, 0.5f);
            crossRoot.sizeDelta = new Vector2(40, 40);

            Image[] crossParts = new Image[5];
            crossParts[0] = CreateTick(crossRoot, new Vector2(0, 8), new Vector2(2, 8));
            crossParts[1] = CreateTick(crossRoot, new Vector2(0, -8), new Vector2(2, 8));
            crossParts[2] = CreateTick(crossRoot, new Vector2(-8, 0), new Vector2(8, 2));
            crossParts[3] = CreateTick(crossRoot, new Vector2(8, 0), new Vector2(8, 2));
            crossParts[4] = CreateTick(crossRoot, Vector2.zero, new Vector2(3, 3));

            // === Main Menu Panel (Tiếng Việt) ===
            GameObject mainMenu = CreateRect("MainMenu_Panel", root.transform);
            StretchFull(mainMenu.GetComponent<RectTransform>());
            Image mDarkBg = mainMenu.AddComponent<Image>();
            mDarkBg.color = new Color(0.02f, 0.02f, 0.04f, 0.94f);

            GameObject mFrameObj = CreateRect("MenuFrame", mainMenu.transform);
            RectTransform mfRect = mFrameObj.GetComponent<RectTransform>();
            mfRect.anchorMin = new Vector2(0.5f, 0.5f); mfRect.anchorMax = new Vector2(0.5f, 0.5f);
            mfRect.pivot = new Vector2(0.5f, 0.5f);
            mfRect.sizeDelta = new Vector2(780, 640);
            Image mfImg = mFrameObj.AddComponent<Image>();
            if (frameMainMenu != null) { mfImg.sprite = frameMainMenu; mfImg.type = Image.Type.Sliced; }
            else { mfImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f); }

            TMP_Text mTitle = AddText(mFrameObj.transform, "Title", "VIỆN TÂM THẦN BỎ HOANG", 52, TextAlignmentOptions.Center, new Color(0.85f, 0.12f, 0.10f), fontTitle);
            RectTransform mtRect = mTitle.GetComponent<RectTransform>();
            mtRect.anchoredPosition = new Vector2(0, 200); mtRect.sizeDelta = new Vector2(700, 68);

            TMP_Text mSub = AddText(mFrameObj.transform, "Subtitle", "DỊCH BỆNH BÙNG PHÁT TẠI KARLHEINZ", 20, TextAlignmentOptions.Center, new Color(0.80f, 0.75f, 0.65f), fontBody);
            RectTransform msRect = mSub.GetComponent<RectTransform>();
            msRect.anchoredPosition = new Vector2(0, 145); msRect.sizeDelta = new Vector2(700, 32);

            GameObject bBoxObj = CreateRect("BriefingBox", mFrameObj.transform);
            RectTransform bbRect = bBoxObj.GetComponent<RectTransform>();
            bbRect.anchoredPosition = new Vector2(0, 25); bbRect.sizeDelta = new Vector2(640, 155);
            Image bbImg = bBoxObj.AddComponent<Image>();
            if (frameOutline != null) { bbImg.sprite = frameOutline; bbImg.type = Image.Type.Sliced; }
            bbImg.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);

            TMP_Text bText = AddText(bBoxObj.transform, "BriefingText",
                "Ba cánh cổng không gian dị thường đã xé rách viện tâm thần, liên tục triệu hồi dị biến ký sinh.\n" +
                "Đột nhập vào các hành lang tối tăm và tiêu diệt toàn bộ 3 cổng để phong tỏa hoàn toàn ổ dịch.\n\n" +
                "<color=#D4AF37>[W A S D]</color> Di chuyển   |   <color=#D4AF37>[Shift]</color> Chạy nhanh   |   <color=#D4AF37>[Chuột Phải]</color> Ngắm bắn\n" +
                "<color=#D4AF37>[Chuột Trái]</color> Khai hỏa   |   <color=#D4AF37>[R]</color> Nạp đạn   |   <color=#D4AF37>[ESC]</color> Tạm dừng",
                16, TextAlignmentOptions.Center, new Color(0.85f, 0.82f, 0.78f), fontBody);
            StretchFull(bText.GetComponent<RectTransform>());
            bText.margin = new Vector4(20, 15, 20, 15);

            AddButton(mFrameObj.transform, "StartButton", "BẮT ĐẦU NHIỆM VỤ", new Vector2(0, -115), new Vector2(300, 80), btnRedDef, btnRedHov, btnRedPress, btnRedDis, fontTitle);
            AddButton(mFrameObj.transform, "ExitButton", "THOÁT TRÒ CHƠI", new Vector2(0, -210), new Vector2(300, 80), btnGreyDef, btnGreyHov, btnRedPress, btnRedDis, fontTitle);

            // === Game Over Panel (Tiếng Việt) ===
            GameObject gameOver = CreateRect("GameOver_Panel", root.transform);
            StretchFull(gameOver.GetComponent<RectTransform>());
            Image goDarkBg = gameOver.AddComponent<Image>();
            goDarkBg.color = new Color(0.12f, 0.02f, 0.02f, 0.95f);

            GameObject goFrameObj = CreateRect("GameOverFrame", gameOver.transform);
            RectTransform gofRect = goFrameObj.GetComponent<RectTransform>();
            gofRect.anchorMin = new Vector2(0.5f, 0.5f); gofRect.anchorMax = new Vector2(0.5f, 0.5f);
            gofRect.pivot = new Vector2(0.5f, 0.5f);
            gofRect.sizeDelta = new Vector2(760, 600);
            Image gofImg = goFrameObj.AddComponent<Image>();
            if (frameMainMenuRed != null) { gofImg.sprite = frameMainMenuRed; gofImg.type = Image.Type.Sliced; }
            else { gofImg.color = new Color(0.15f, 0.04f, 0.04f, 0.95f); }

            TMP_Text goTitle = AddText(goFrameObj.transform, "Title", "NHIỆM VỤ THẤT BẠI", 52, TextAlignmentOptions.Center, new Color(0.95f, 0.15f, 0.12f), fontTitle);
            RectTransform gotRect = goTitle.GetComponent<RectTransform>();
            gotRect.anchoredPosition = new Vector2(0, 185); gotRect.sizeDelta = new Vector2(680, 65);

            TMP_Text goSub = AddText(goFrameObj.transform, "Subtitle", "BẠN ĐÃ BỊ KÝ SINH TRÙNG VÀ DỊ BIẾN NUỐT CHỬNG", 18, TextAlignmentOptions.Center, new Color(0.85f, 0.70f, 0.70f), fontBody);
            RectTransform gosRect = goSub.GetComponent<RectTransform>();
            gosRect.anchoredPosition = new Vector2(0, 135); gosRect.sizeDelta = new Vector2(680, 30);

            GameObject goStatsBox = CreateRect("StatsBox", goFrameObj.transform);
            RectTransform gosbRect = goStatsBox.GetComponent<RectTransform>();
            gosbRect.anchoredPosition = new Vector2(0, 20); gosbRect.sizeDelta = new Vector2(620, 140);
            Image gosbImg = goStatsBox.AddComponent<Image>();
            if (frameOutline != null) { gosbImg.sprite = frameOutline; gosbImg.type = Image.Type.Sliced; }
            gosbImg.color = new Color(0.06f, 0.02f, 0.02f, 0.85f);

            TMP_Text goStatsText = AddText(goStatsBox.transform, "StatsText", "ĐANG PHÂN TÍCH HỒ SƠ TÁC CHIẾN...", 18, TextAlignmentOptions.Center, Color.white, fontBody);
            StretchFull(goStatsText.GetComponent<RectTransform>());

            AddButton(goFrameObj.transform, "RetryButton", "THỬ LẠI", new Vector2(0, -115), new Vector2(300, 78), btnRedDef, btnRedHov, btnRedPress, btnRedDis, fontTitle);
            AddButton(goFrameObj.transform, "MenuButton", "VỀ MENU CHÍNH", new Vector2(0, -205), new Vector2(300, 78), btnGreyDef, btnGreyHov, btnRedPress, btnRedDis, fontTitle);
            gameOver.SetActive(false);

            // === Victory Panel (Tiếng Việt) ===
            GameObject victory = CreateRect("Victory_Panel", root.transform);
            StretchFull(victory.GetComponent<RectTransform>());
            Image vDarkBg = victory.AddComponent<Image>();
            vDarkBg.color = new Color(0.02f, 0.08f, 0.04f, 0.95f);

            GameObject vFrameObj = CreateRect("VictoryFrame", victory.transform);
            RectTransform vfRect = vFrameObj.GetComponent<RectTransform>();
            vfRect.anchorMin = new Vector2(0.5f, 0.5f); vfRect.anchorMax = new Vector2(0.5f, 0.5f);
            vfRect.pivot = new Vector2(0.5f, 0.5f);
            vfRect.sizeDelta = new Vector2(760, 600);
            Image vfImg = vFrameObj.AddComponent<Image>();
            if (frameMainMenu != null) { vfImg.sprite = frameMainMenu; vfImg.type = Image.Type.Sliced; }
            else { vfImg.color = new Color(0.04f, 0.12f, 0.06f, 0.95f); }

            TMP_Text vTitle = AddText(vFrameObj.transform, "Title", "PHONG TỎA THÀNH CÔNG", 52, TextAlignmentOptions.Center, new Color(0.85f, 0.75f, 0.20f), fontTitle);
            RectTransform vtRect = vTitle.GetComponent<RectTransform>();
            vtRect.anchoredPosition = new Vector2(0, 185); vtRect.sizeDelta = new Vector2(680, 65);

            TMP_Text vSub = AddText(vFrameObj.transform, "Subtitle", "TOÀN BỘ CỔNG DỊ THƯỜNG ĐÃ ĐƯỢC THANH TRỪNG HOÀN TOÀN", 18, TextAlignmentOptions.Center, new Color(0.70f, 0.85f, 0.70f), fontBody);
            RectTransform vsRect = vSub.GetComponent<RectTransform>();
            vsRect.anchoredPosition = new Vector2(0, 135); vsRect.sizeDelta = new Vector2(680, 30);

            GameObject vStatsBox = CreateRect("StatsBox", vFrameObj.transform);
            RectTransform vsbRect = vStatsBox.GetComponent<RectTransform>();
            vsbRect.anchoredPosition = new Vector2(0, 20); vsbRect.sizeDelta = new Vector2(620, 140);
            Image vsbImg = vStatsBox.AddComponent<Image>();
            if (frameOutline != null) { vsbImg.sprite = frameOutline; vsbImg.type = Image.Type.Sliced; }
            vsbImg.color = new Color(0.02f, 0.06f, 0.03f, 0.85f);

            TMP_Text vStatsText = AddText(vStatsBox.transform, "StatsText", "ĐANG TÍNH TOÁN KẾT QUẢ...", 18, TextAlignmentOptions.Center, Color.white, fontBody);
            StretchFull(vStatsText.GetComponent<RectTransform>());

            AddButton(vFrameObj.transform, "PlayAgainButton", "CHƠI LẠI", new Vector2(0, -115), new Vector2(300, 78), btnRedDef, btnRedHov, btnRedPress, btnRedDis, fontTitle);
            AddButton(vFrameObj.transform, "MenuButton", "VỀ MENU CHÍNH", new Vector2(0, -205), new Vector2(300, 78), btnGreyDef, btnGreyHov, btnRedPress, btnRedDis, fontTitle);
            victory.SetActive(false);

            // === Pause Panel (Tiếng Việt) ===
            GameObject pause = CreateRect("Pause_Panel", root.transform);
            StretchFull(pause.GetComponent<RectTransform>());
            Image pDarkBg = pause.AddComponent<Image>();
            pDarkBg.color = new Color(0.02f, 0.02f, 0.04f, 0.85f);

            GameObject pFrameObj = CreateRect("PauseFrame", pause.transform);
            RectTransform pfRect = pFrameObj.GetComponent<RectTransform>();
            pfRect.anchorMin = new Vector2(0.5f, 0.5f); pfRect.anchorMax = new Vector2(0.5f, 0.5f);
            pfRect.pivot = new Vector2(0.5f, 0.5f);
            pfRect.sizeDelta = new Vector2(620, 520);
            Image pfImg = pFrameObj.AddComponent<Image>();
            if (frameMainMenu != null) { pfImg.sprite = frameMainMenu; pfImg.type = Image.Type.Sliced; }
            else { pfImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f); }

            TMP_Text pTitle = AddText(pFrameObj.transform, "Title", "TẠM DỪNG TRÒ CHƠI", 48, TextAlignmentOptions.Center, new Color(0.95f, 0.85f, 0.70f), fontTitle);
            RectTransform ptRect = pTitle.GetComponent<RectTransform>();
            ptRect.anchoredPosition = new Vector2(0, 150); ptRect.sizeDelta = new Vector2(560, 60);

            AddButton(pFrameObj.transform, "ResumeButton", "TIẾP TỤC", new Vector2(0, 45), new Vector2(280, 72), btnRedDef, btnRedHov, btnRedPress, btnRedDis, fontTitle);
            AddButton(pFrameObj.transform, "RestartButton", "CHƠI LẠI NHIỆM VỤ", new Vector2(0, -45), new Vector2(280, 72), btnGreyDef, btnGreyHov, btnRedPress, btnRedDis, fontTitle);
            AddButton(pFrameObj.transform, "QuitButton", "VỀ MENU CHÍNH", new Vector2(0, -135), new Vector2(280, 72), btnGreyDef, btnGreyHov, btnRedPress, btnRedDis, fontTitle);
            pause.SetActive(false);

            // Setup MVP Views on Panels
            hudPanel.AddComponent<HUDView>().BindReferencesIfMissing();
            mainMenu.AddComponent<MainMenuView>().BindReferencesIfMissing();
            pause.AddComponent<PauseMenuView>().BindReferencesIfMissing();
            gameOver.AddComponent<GameOverView>().BindReferencesIfMissing();
            victory.AddComponent<VictoryView>().BindReferencesIfMissing();

            BloodlinesUIRootView rootView = root.AddComponent<BloodlinesUIRootView>();
            rootView.BindAllViews();

            // Save Prefabs
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, AltPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BloodlinesCanvasBuilder] Successfully built and saved Vietnamese Bloodlines UI Canvas to {PrefabPath} and {AltPrefabPath}!");
            if (selectAfterBuild)
            {
                Selection.activeObject = savedPrefab;
            }
            return savedPrefab;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TMP_Text AddText(Transform parent, string name, string content, int fontSize, TextAlignmentOptions align, Color color, TMP_FontAsset font)
        {
            GameObject go = CreateRect(name, parent);
            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static Image CreateTick(Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject tick = CreateRect("Tick", parent);
            RectTransform rect = tick.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image img = tick.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.85f);
            return img;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Sprite def, Sprite hov, Sprite press, Sprite dis, TMP_FontAsset font)
        {
            GameObject btnObj = CreateRect(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = def;
            img.type = Image.Type.Simple;

            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState
            {
                highlightedSprite = hov,
                pressedSprite = press,
                disabledSprite = dis
            };
            btn.spriteState = ss;
            btnObj.AddComponent<BloodlinesButtonHoverFX>();

            TMP_Text txt = AddText(btnObj.transform, "Label", label, 20, TextAlignmentOptions.Center, new Color(0.95f, 0.90f, 0.85f), font);
            StretchFull(txt.GetComponent<RectTransform>());

            return btn;
        }
    }
}
