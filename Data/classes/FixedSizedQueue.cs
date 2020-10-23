using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace RhAutoHSOTP.classes
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public void Enqueue(T obj)
        {
            queue.Enqueue(obj);

            while (queue.Count > Size)
            {
                T outObj;
                queue.TryDequeue(out outObj);
            }
        }
    }
}
