using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Nhà cung cấp Font Tiếng Việt toàn cục cho TextMeshPro.
    /// Khắc phục triệt để hiện tượng thiếu glyphs tiếng Việt dẫn đến Unity tự fallback

    /// </summary>
    public static class VietnameseFontProvider
    {
        private static TMP_FontAsset vietnameseFontAsset;
        private static bool isInitialized = false;

        public static TMP_FontAsset VietnameseFont
        {
            get
            {
                if (!isInitialized)
                {
                    Initialize();
                }
                return vietnameseFontAsset;
            }
        }

        private static void Initialize()
        {
            isInitialized = true;

            // 1. Thử nạp Font Segoe UI / Arial từ Resources
            Font ttf = Resources.Load<Font>("Fonts/segoeuib");
            if (ttf == null)
            {
                ttf = Resources.Load<Font>("segoeuib");
            }

            if (ttf != null)
            {
                vietnameseFontAsset = TMP_FontAsset.CreateFontAsset(ttf);
                if (vietnameseFontAsset != null)
                {
                    vietnameseFontAsset.name = "SegoeUI_Vietnamese_Dynamic_SDF";
                    return;
                }
            }

            // 2. Thử nạp LiberationSans SDF chuẩn của TextMeshPro
            TMP_FontAsset libSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans != null)
            {
                vietnameseFontAsset = libSans;
            }
        }

        /// <summary>
        /// Đảm bảo một TextMeshProUGUI hiển thị tiếng Việt hoàn hảo,
        /// bổ sung font tiếng Việt vào fallback table của font hiện tại.
        /// </summary>
        public static void EnsureVietnameseFont(TMP_Text textComponent)
        {
            if (textComponent == null) return;

            TMP_FontAsset viFont = VietnameseFont;
            if (viFont == null) return;

            if (textComponent.font == null)
            {
                textComponent.font = viFont;
                return;
            }

            // Nếu font hiện tại không phải là font tiếng Việt, thêm font tiếng Việt vào bảng Fallback
            if (textComponent.font != viFont)
            {
                if (textComponent.font.fallbackFontAssetTable == null)
                {
                    textComponent.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                if (!textComponent.font.fallbackFontAssetTable.Contains(viFont))
                {
                    textComponent.font.fallbackFontAssetTable.Add(viFont);
                }
            }
        }

        /// <summary>
        /// Quét đệ quy toàn bộ cây GameObject và áp dụng bảo vệ font tiếng Việt cho tất cả TMP_Text.
        /// </summary>
        public static void EnsureAllVietnameseFonts(GameObject rootObject)
        {
            if (rootObject == null) return;

            TMP_Text[] allTexts = rootObject.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
            {
                EnsureVietnameseFont(t);
            }
        }
    }
}
