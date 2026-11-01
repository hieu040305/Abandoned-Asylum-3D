namespace AbandonedAsylum.Interfaces
{
    /// <summary>
    /// Giao diện chuẩn cho Object Pooling Pattern trong hệ thống game:
    /// - Quản lý chu trình tái sử dụng đối tượng nhàn rỗi.
    /// - Loại bỏ hoàn toàn chi phí Heap GC Alloc và Instantiate trong runtime gameplay.
    /// </summary>
    public interface IObjectPool<T>
    {
        T Get();
        void Release(T element);
        void Clear();
        int CountActive { get; }
        int CountInactive { get; }
    }
}
