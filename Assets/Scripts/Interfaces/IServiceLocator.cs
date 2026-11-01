using System;

namespace AbandonedAsylum.Interfaces
{
    /// <summary>
    /// Contract cho Service Locator Container theo chuẩn Clean Architecture.
    /// Quản lý đăng ký, hủy đăng ký và truy xuất service an toàn kiểu (Type-Safe).
    /// </summary>
    public interface IServiceLocator
    {
        void Register<T>(T service) where T : class;
        void Unregister<T>() where T : class;
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T service) where T : class;
        bool Contains<T>() where T : class;
        void Reset();
    }
}
