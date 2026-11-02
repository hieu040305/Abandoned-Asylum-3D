using System;
using System.Collections.Generic;
using UnityEngine;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Core
{
    /// <summary>
    /// Triển khai Object Pool chuẩn tối ưu cho các Unity Component:
    /// - Quản lý Stack đối tượng nhàn rỗi (LIFO để tận dụng CPU cache locality).
    /// - Quản lý HashSet các đối tượng đang hoạt động để tránh double-release.
    /// - Hỗ trợ Prewarm và tự động mở rộng an toàn khi cạn pool.
    /// </summary>
    public class ComponentPool<T> : IObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform rootContainer;
        private readonly Stack<T> inactiveStack;
        private readonly HashSet<T> activeSet;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;

        public int CountActive => activeSet.Count;
        public int CountInactive => inactiveStack.Count;
        public int CountAll => CountActive + CountInactive;

        public ComponentPool(
            T prefab,
            int defaultCapacity = 16,
            Transform rootContainer = null,
            Action<T> onGet = null,
            Action<T> onRelease = null)
        {
            this.prefab = prefab;
            this.rootContainer = rootContainer;
            this.inactiveStack = new Stack<T>(defaultCapacity);
            this.activeSet = new HashSet<T>();
            this.onGet = onGet;
            this.onRelease = onRelease;
        }

        public void Prewarm(int count)
        {
            if (prefab == null) return;
            for (int i = 0; i < count; i++)
            {
                T instance = UnityEngine.Object.Instantiate(prefab, rootContainer);
                instance.gameObject.SetActive(false);
                inactiveStack.Push(instance);
            }
        }

        public T Get()
        {
            T instance;
            if (inactiveStack.Count > 0)
            {
                instance = inactiveStack.Pop();
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(prefab, rootContainer);
            }

            activeSet.Add(instance);
            instance.gameObject.SetActive(true);
            onGet?.Invoke(instance);
            return instance;
        }

        public void Release(T element)
        {
            if (element == null) return;
            if (!activeSet.Remove(element))
            {
                // Tránh double release nếu đối tượng đã ở trong inactive stack
                return;
            }

            onRelease?.Invoke(element);
            element.gameObject.SetActive(false);
            if (rootContainer != null)
            {
                element.transform.SetParent(rootContainer);
            }
            inactiveStack.Push(element);
        }

        public void Clear()
        {
            foreach (var item in activeSet)
            {
                if (item != null && item.gameObject != null)
                {
                    item.gameObject.SetActive(false);
                    SafeDestroy(item.gameObject);
                }
            }
            activeSet.Clear();

            while (inactiveStack.Count > 0)
            {
                var item = inactiveStack.Pop();
                if (item != null && item.gameObject != null)
                {
                    item.gameObject.SetActive(false);
                    SafeDestroy(item.gameObject);
                }
            }
        }

        private static void SafeDestroy(UnityEngine.Object target)
        {
            if (target == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
