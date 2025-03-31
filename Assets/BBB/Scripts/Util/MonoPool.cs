using System.Collections.Generic;
using UnityEngine;

namespace BBB.Scripts.Util
{
    /**
     * This class is a pool for MonoBehaviours that implement the IPoolable interface.
     */
    public class MonoPool<T> : MonoSingleton<MonoPool<T>> where T : MonoBehaviour, IPoolable
    {
        [SerializeField] private int initialPoolSize = 1;
        [SerializeField] private T prefab;
        [SerializeField] private Transform poolParent;
        private Stack<T> _availableObjects;

        private void Awake()
        {
            _availableObjects = new Stack<T>();
            AddItemsToPool();
        }

        // Adds items to the pool.
        private void AddItemsToPool()
        {
            for (var i = 0; i < initialPoolSize; i++)
            {
                var obj = Instantiate(prefab, poolParent, true);
                obj.gameObject.SetActive(false);
                _availableObjects.Push(obj);
            }
        }

        /**
         * Gets an object from the pool. If the pool is empty, it adds more items to the pool.
         */
        public T Get()
        {
            if (_availableObjects.Count == 0)
                AddItemsToPool();
            
            var obj = _availableObjects.Pop();
            obj.gameObject.SetActive(true);
            obj.Reset();
            return obj;
        }

        /**
         * Returns an object to the pool. The object is deactivated and added to the pool.
         */
        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _availableObjects.Push(obj);
        }
    }
}