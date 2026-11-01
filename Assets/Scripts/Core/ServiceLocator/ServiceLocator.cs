using System;
using System.Collections.Generic;
using UnityEngine;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Core
{
    /// <summary>
    /// Service Locator Container quản lý đăng ký và truy xuất các dịch vụ dùng chung.
    /// Hỗ trợ Dependency Inversion và thuận tiện cho việc viết Unit Test / Mocking.
    /// </summary>
    public class ServiceLocator : IServiceLocator
    {
        private static IServiceLocator current;
        public static IServiceLocator Current => current ??= new ServiceLocator();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticDomain()
        {
            if (current != null)
            {
                current.Reset();
            }
            current = null;
        }

        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private readonly object lockObject = new object();

        #region Static Facade Shortcuts
        public static void RegisterService<T>(T service) where T : class
        {
            Current.Register(service);
        }

        public static void UnregisterService<T>() where T : class
        {
            Current.Unregister<T>();
        }

        public static T Get<T>() where T : class
        {
            return Current.Resolve<T>();
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            return Current.TryResolve<T>(out service);
        }

        public static bool IsRegistered<T>() where T : class
        {
            return Current.Contains<T>();
        }

        public static void ResetAll()
        {
            Current.Reset();
        }
        #endregion

        #region IServiceLocator Implementation
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), $"[ServiceLocator] Không thể đăng ký null service cho Type '{typeof(T).Name}'.");
            }

            lock (lockObject)
            {
                Type type = typeof(T);
                if (services.ContainsKey(type))
                {
                    Debug.LogWarning($"[ServiceLocator] Ghi đè service đã tồn tại cho Type '{type.Name}'.");
                    services[type] = service;
                }
                else
                {
                    services.Add(type, service);
                }
            }
        }

        public void Unregister<T>() where T : class
        {
            lock (lockObject)
            {
                Type type = typeof(T);
                if (services.ContainsKey(type))
                {
                    services.Remove(type);
                }
            }
        }

        public T Resolve<T>() where T : class
        {
            if (TryResolve(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"[ServiceLocator] Không tìm thấy Service được đăng ký cho Type '{typeof(T).Name}'. Hãy kiểm tra thứ tự khởi tạo.");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            Type type = typeof(T);

            lock (lockObject)
            {
                if (services.TryGetValue(type, out object obj))
                {
                    service = (T)obj;
                    return true;
                }
            }

            service = null;
            return false;
        }

        public bool Contains<T>() where T : class
        {
            lock (lockObject)
            {
                return services.ContainsKey(typeof(T));
            }
        }

        public void Reset()
        {
            lock (lockObject)
            {
                services.Clear();
            }
        }
        #endregion
    }
}
