using System.Collections.Generic;
using UnityEngine;

namespace Util {
    /// <summary>
    /// Manages a pool for a particular object type <see cref="T"/>. Can request instances of <see cref="T"/> from the pool,
    /// and the pool handles instances and keeping track of those.
    /// </summary>
    public class GameObjectPool<T> where T : Component {
        private readonly int _poolCapacity;
        private readonly Transform _poolParent;
        private readonly T _prefab;

        private readonly List<T> _pool = new List<T>();
    
        public GameObjectPool(T prefab, Transform poolParent, int poolCapacity = 100) {
            _poolCapacity = poolCapacity;
            _prefab = prefab;
            _poolParent = poolParent;
        
            FillPool();
        }

        public T GetObject() {
            // Remove all that were destroyed in some unexpected way
            _pool.RemoveAll(e => !e);

            if (_pool.Count > 0) {
                T entry = _pool[_pool.Count - 1];
                _pool.Remove(entry);
                return entry;
            }

            return CreateObject();
        }

        private void FillPool() {
            while (_pool.Count < _poolCapacity) {
                AddAndHideObject(CreateObject());
            }
        }

        private T CreateObject() {
            T newObject = Object.Instantiate(_prefab, _poolParent);
        
            // Set up the new object
            ResetObject(newObject);
            return newObject;
        }

        /// <summary>
        /// Returns the object to a default state of being hidden and not rotated and stuff
        /// </summary>
        private void ResetObject(T obj) {
            Transform t = obj.transform;
            t.SetParent(_poolParent);
            t.rotation = Quaternion.identity;
            obj.gameObject.SetActive(false);
        }
    
        /// <summary>
        /// Add an object to the pool, and also hide it and set default transform values
        /// </summary>
        public void AddAndHideObject(T newObject) {
            if (!newObject) return;
            if (!_poolParent) {
                Object.Destroy(newObject);
                return;
            }

            _pool.Add(newObject);
        
            ResetObject(newObject);

            // Remove the first-added entry if the pool is full now
            if (_pool.Count > _poolCapacity) {
                T entry = _pool[0];
                _pool.RemoveAt(0);
                Object.Destroy(entry.gameObject);
            }
        }
    }
}