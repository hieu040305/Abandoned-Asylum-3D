using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Tầng View của Heads-Up Display (HUD) theo mô hình MVP (Model - View - Presenter).
    /// Chỉ chịu trách nhiệm hiển thị dữ liệu lên màn hình, không chứa logic nghiệp vụ game.
    /// Triển khai Zero-GC Hot Path cho TextMeshPro và dynamic crosshair.
    /// </summary>
    [DisallowMultipleComponent]
    public class HUDView : MonoBehaviour
    {
        [Header("Health Widgets")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider delayedHealthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TMP_Text healthText;

        [Header("Ammo & Weapon Widgets")]
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private Slider reloadSlider;

        [Header("Objective & Timer Widgets")]
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text survivalTimeText;

        [Header("Crosshair & Vignette")]
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private Image[] crosshairParts;
        [SerializeField] private Image hurtVignetteImage;

        public Image HurtVignetteImage => hurtVignetteImage;
        public Slider DelayedHealthSlider => delayedHealthSlider;

        private void Awake()
        {
            BindReferencesIfMissing();
        }

        public void BindReferencesIfMissing()
        {
            if (healthSlider == null) healthSlider = FindChildComponent<Slider>("HealthSlider");
            if (delayedHealthSlider == null) delayedHealthSlider = FindChildComponent<Slider>("DelayedSlider");
            if (healthText == null) healthText = FindChildComponent<TMP_Text>("HealthText") ?? FindChildComponent<TMP_Text>("HP_Text");
            if (ammoText == null) ammoText = FindChildComponent<TMP_Text>("AmmoText");
            if (weaponNameText == null) weaponNameText = FindChildComponent<TMP_Text>("WeaponName");
            if (reloadSlider == null) reloadSlider = FindChildComponent<Slider>("ReloadBar");
            if (objectiveText == null) objectiveText = FindChildComponent<TMP_Text>("ObjectiveText");
            if (survivalTimeText == null) survivalTimeText = FindChildComponent<TMP_Text>("TimeText");

            if (crosshairRoot == null)
            {
                Transform ch = transform.Find("CrosshairRoot") ?? transform.Find("Crosshair");
                if (ch != null) crosshairRoot = ch.GetComponent<RectTransform>();
            }

            if (crosshairParts == null || crosshairParts.Length == 0)
            {
                if (crosshairRoot != null)
                {
                    crosshairParts = crosshairRoot.GetComponentsInChildren<Image>(true);
                }
            }

            if (hurtVignetteImage == null)
            {
                Transform v = transform.parent != null ? transform.parent.Find("BloodHurtVignette") : null;
                if (v != null) hurtVignetteImage = v.GetComponent<Image>();
            }

            if (healthFillImage == null && healthSlider != null && healthSlider.fillRect != null)
            {
                healthFillImage = healthSlider.fillRect.GetComponent<Image>();
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateHealth(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }

            if (delayedHealthSlider != null)
            {
                delayedHealthSlider.maxValue = max;
            }

            if (healthText != null)
            {
                healthText.SetText("{0} / {1} HP", Mathf.CeilToInt(current), Mathf.CeilToInt(max));
            }

            if (healthFillImage != null)
            {
                float pct = Mathf.Clamp01(current / Mathf.Max(1f, max));
                healthFillImage.color = pct < 0.25f 
                    ? new Color(0.95f, 0.15f, 0.15f, 0.95f) 
                    : Color.white;
            }
        }

        public void UpdateDelayedHealth(float value)
        {
            if (delayedHealthSlider != null)
            {
                delayedHealthSlider.value = value;
            }
        }

        public void UpdateAmmo(int current, int max)
        {
            if (ammoText != null)
            {
                ammoText.SetText("{0} <size=65%>/ {1}</size>", current, max);
                ammoText.color = current <= 5 
                    ? new Color(0.95f, 0.22f, 0.18f) 
                    : new Color(0.95f, 0.90f, 0.85f);
            }
        }

        public void UpdateWeaponName(string name)
        {
            if (weaponNameText != null && !string.IsNullOrEmpty(name))
            {
                weaponNameText.text = name;
            }
        }

        public void UpdateReloadProgress(float elapsed, float duration, bool isReloading)
        {
            if (reloadSlider == null) return;

            if (isReloading && duration > 0.01f)
            {
                if (!reloadSlider.gameObject.activeSelf) reloadSlider.gameObject.SetActive(true);
                reloadSlider.maxValue = duration;
                reloadSlider.value = elapsed;
            }
            else if (reloadSlider.gameObject.activeSelf)
            {
                reloadSlider.gameObject.SetActive(false);
            }
        }

        public void UpdateSurvivalTime(int minutes, int seconds)
        {
            if (survivalTimeText != null)
            {
                survivalTimeText.SetText("THỜI GIAN: {0:D2}:{1:D2}", minutes, seconds);
            }
        }

        public void UpdateObjective(int destroyed, int total)
        {
            if (objectiveText != null)
            {
                objectiveText.SetText("CỔNG DỊ THƯỜNG: <color=#D4AF37>{0}</color> / {1}", destroyed, total);
            }
        }

        public void UpdateCrosshairDynamics(bool isAiming, bool isMoving)
        {
            if (crosshairRoot == null || crosshairParts == null || crosshairParts.Length < 4) return;

            float speed = isMoving ? 1.0f : 0.0f;
            float spread = isAiming ? 4f : (8f + speed * 12f);
            Color targetColor = isAiming ? new Color(0.95f, 0.25f, 0.20f, 0.95f) : new Color(1f, 1f, 1f, 0.85f);

            crosshairParts[0].rectTransform.anchoredPosition = new Vector2(0, spread);
            crosshairParts[1].rectTransform.anchoredPosition = new Vector2(0, -spread);
            crosshairParts[2].rectTransform.anchoredPosition = new Vector2(-spread, 0);
            crosshairParts[3].rectTransform.anchoredPosition = new Vector2(spread, 0);

            for (int i = 0; i < crosshairParts.Length; i++)
            {
                if (crosshairParts[i] != null) crosshairParts[i].color = targetColor;
            }
        }

        public void SetHurtVignetteAlpha(float alpha)
        {
            if (hurtVignetteImage != null)
            {
                Color c = hurtVignetteImage.color;
                c.a = alpha;
                hurtVignetteImage.color = c;
            }
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
    }
}
