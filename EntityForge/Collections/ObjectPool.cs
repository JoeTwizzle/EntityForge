using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Collections;

sealed class ObjectPool<T> : IDisposable
{
    readonly List<T> pool;
    readonly Func<T> _creationFunc;
    readonly Func<T, T> _resetFunc;
    readonly Action<T> _cleanupAction;
    public ObjectPool(Func<T> creationFunc, Func<T, T> resetFunc, Action<T> cleanupAction)
    {
        pool = new();
        _creationFunc = creationFunc;
        _resetFunc = resetFunc;
        _cleanupAction = cleanupAction;
    }

    public void Dispose()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            _cleanupAction(pool[i]);
        }
        pool.Clear();
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            var item = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return _resetFunc(item);
        }

        return _creationFunc();
    }

    public void Return(T item)
    {
        pool.Add(item);
    }
}
