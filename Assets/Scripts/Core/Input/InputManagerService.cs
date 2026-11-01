using UnityEngine;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Core.Input
{
    /// <summary>
    /// Điều phối Input toàn cục và đăng ký IInputService vào ServiceLocator.
    /// Tự động khởi tạo ngay khi game tải (RuntimeInitializeOnLoadMethod).
    /// </summary>
    public static class InputManagerService
    {
        private static IInputService activeService;
        public static IInputService Active => activeService ??= InitializeInputService();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeService = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void AutoRegisterInputService()
        {
            if (activeService == null)
            {
                activeService = InitializeInputService();
            }

            ServiceLocator.RegisterService<IInputService>(activeService);
        }

        private static IInputService InitializeInputService()
        {
#if ENABLE_INPUT_SYSTEM
            Debug.Log("[InputManagerService] Khởi tạo Unity New Input System Service.");
            return new NewInputSystemService();
#else
            Debug.Log("[InputManagerService] Khởi tạo Legacy Input Service (New Input System Fallback Strategy).");
            return new LegacyInputService();
#endif
        }

        public static void SetCustomInputService(IInputService customService)
        {
            activeService = customService;
            ServiceLocator.RegisterService<IInputService>(customService);
        }
    }
}
