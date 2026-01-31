using System.Collections.Generic;
using UnityEngine;

namespace DodgeDots.Core
{
    /// <summary>
    /// 通用对象池，用于复用游戏对象以提升性能
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool;
        private readonly int _maxCapacity;

        public ObjectPool(T prefab, int initialSize = 10, int maxCapacity = 100, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            _maxCapacity = maxCapacity;
            _pool = new Queue<T>(initialSize);

            // 预创建对象
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);

            // 如果池已满，销毁对象
            if (_pool.Count >= _maxCapacity)
            {
                Object.Destroy(obj.gameObject);
                return;
            }

            _pool.Enqueue(obj);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
        }

        private T CreateNewObject()
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
