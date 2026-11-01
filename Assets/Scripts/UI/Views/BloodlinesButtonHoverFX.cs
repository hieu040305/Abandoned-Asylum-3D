using UnityEngine;
using UnityEngine.EventSystems;

namespace AbandonedAsylum.UI
{
    /// <summary>
    /// Component hỗ trợ âm thanh khi rê chuột qua nút, chỉ xử lý IPointerEnterHandler,
    /// không đụng chạm PointerDown hay PointerClick để tránh chặn sự kiện Button của Unity.
    /// </summary>
    public class BloodlinesButtonHoverFX : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            UIManager.Instance?.PlayButtonHoverSFX();
        }
    }
}
