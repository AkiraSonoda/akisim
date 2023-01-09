/*
 * ThreadedClasses is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this library statically or dynamically with other modules is
 * making a combined work based on this library. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this library give you
 * permission to link this library with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this library. If you modify this library, you may extend
 * this exception to your version of the library, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using System;
using System.Threading;
using System.Collections.Generic;

namespace ThreadedClasses
{
    public class BlockingQueue<T> : Queue<T>
    {
        public class TimeoutException : Exception
        {
            public TimeoutException()
            {
            }
        }

        public BlockingQueue(IEnumerable<T> col)
            : base(col)
        {
        }

        public BlockingQueue(int capacity)
            : base(capacity)
        {
        }

        public BlockingQueue()
            : base()
        {
        }

        ~BlockingQueue()
        {
            lock(this)
            {
                base.Clear();
                Monitor.PulseAll(this);
            }
        }

        public new T Dequeue()
        {
            return Dequeue(Timeout.Infinite);
        }

        public T Dequeue(TimeSpan timeout)
        {
            return Dequeue(timeout.Milliseconds);
        }

        public T Dequeue(int timeout)
        {
            lock(this)
            {
                while(base.Count == 0)
                {
                    if(!Monitor.Wait(this, timeout))
                    {
                        throw new TimeoutException();
                    }
                }
                return base.Dequeue();
            }
        }

        public new void Enqueue(T obj)
        {
            lock (this)
            {
                base.Enqueue(obj);
                Monitor.Pulse(this);
            }
        }

        public new int Count
        {
            get
            {
                lock (this) return base.Count;
            }
        }

        public new bool Contains(T obj)
        {
            lock(this)
            {
                return base.Contains(obj);
            }
        }
    }
}