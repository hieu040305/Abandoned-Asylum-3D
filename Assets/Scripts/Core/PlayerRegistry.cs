using UnityEngine;

namespace AbandonedAsylum.Core
{
    /// <summary>
    /// Lưu trữ tham chiếu tập trung đến Transform và GameObject của người chơi,
    /// cho phép truy xuất trực tiếp và hạn chế việc tìm kiếm đối tượng trong Scene.
    /// </summary>
    public static class PlayerRegistry
    {
        public static Transform PlayerTransform { get; private set; }
        public static GameObject PlayerGameObject { get; private set; }
        public static bool HasPlayer => PlayerTransform != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerTransform = null;
            PlayerGameObject = null;
        }

        public static void Register(Transform transform, GameObject gameObject = null)
        {
            PlayerTransform = transform;
            PlayerGameObject = gameObject != null ? gameObject : (transform != null ? transform.gameObject : null);
        }

        public static void Register(Transform transform, Component component)
        {
            PlayerTransform = transform;
            PlayerGameObject = component != null ? component.gameObject : (transform != null ? transform.gameObject : null);
        }

        public static void Unregister(Transform transform)
        {
            if (PlayerTransform == transform)
            {
                PlayerTransform = null;
                PlayerGameObject = null;
            }
        }
    }
}
