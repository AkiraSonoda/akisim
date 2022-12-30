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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ThreadedClasses
{
    public class ExpiringCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        const double CACHE_PURGE_HZ = 1.0;
        private System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromSeconds(CACHE_PURGE_HZ).TotalMilliseconds);

        class CacheItem
        {
            public TValue m_Value;
            public DateTime m_ExpiringDate;
            public CacheItem(TValue val, DateTime expiringDate)
            {
                m_Value = val;
                m_ExpiringDate = expiringDate;
            }
        }
        private ReaderWriterLock m_RwLock = new ReaderWriterLock();
        private Dictionary<TKey, CacheItem> m_Dictionary = new Dictionary<TKey, CacheItem>();
        private TimeSpan m_ExpirationTime;

        private object purgeLock = new object();
        const int MAX_LOCK_WAIT = 5000; // milliseconds

        public ExpiringCache(double expirationSeconds)
        {
            m_ExpirationTime = TimeSpan.FromSeconds(expirationSeconds);
            timer.Elapsed += ExpireTimer;
            timer.Start();
        }

        public ExpiringCache(TimeSpan timeSpan)
        {
            m_ExpirationTime = timeSpan;
            timer.Elapsed += ExpireTimer;
            timer.Start();
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        public int Count
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary.Count;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary[key].m_Value;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    CacheItem ci = m_Dictionary[key];
                    ci.m_Value = value;
                    /* limited locking here so we work with a ReaderLock only here */
                    lock (ci)
                    {
                        ci.m_ExpiringDate = DateTime.UtcNow + m_ExpirationTime;
                    }
                }
                catch(KeyNotFoundException)
                {
                    LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                    try
                    {
                        m_Dictionary[key] = new CacheItem(value, DateTime.UtcNow + m_ExpirationTime);
                    }
                    finally
                    {
                        m_RwLock.DowngradeFromWriterLock(ref lc);
                    }
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary.Add(key, 
                    new CacheItem(
                        value, DateTime.UtcNow + m_ExpirationTime
                        ));
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void AddOrUpdate(TKey key, TValue value, double expirationSeconds)
        {
            AddOrUpdate(key, value, TimeSpan.FromSeconds(expirationSeconds));
        }

        public void AddOrUpdate(TKey key, TValue value, TimeSpan expirationTime)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                CacheItem ci = m_Dictionary[key];
                ci.m_Value = value;
                /* limited locking here so we work with a ReaderLock only here */
                lock (ci)
                {
                    ci.m_ExpiringDate = DateTime.UtcNow + expirationTime;
                }
            }
            catch(KeyNotFoundException)
            {
                LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                try
                {
                    m_Dictionary[key] = new CacheItem(value, DateTime.UtcNow + expirationTime);
                }
                finally
                {
                    m_RwLock.DowngradeFromWriterLock(ref lc);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public delegate TValue AddValueDelegate();
        public TValue GetOrAdd(TKey key, AddValueDelegate addDelegate, double expirationSeconds)
        {
            return GetOrAdd(key, addDelegate, TimeSpan.FromSeconds(expirationSeconds));
        }

        public TValue GetOrAdd(TKey key, AddValueDelegate addDelegate, TimeSpan expirationTime)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                CacheItem ci = m_Dictionary[key];
                return ci.m_Value;
            }
            catch (KeyNotFoundException)
            {
                TValue v = addDelegate();
                LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                try
                {
                    m_Dictionary[key] = new CacheItem(v, DateTime.UtcNow + expirationTime);
                    return v;
                }
                finally
                {
                    m_RwLock.DowngradeFromWriterLock(ref lc);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void Update(TKey key, TValue value, double expirationSeconds)
        {
            Update(key, value, TimeSpan.FromSeconds(expirationSeconds));
        }
        public void Update(TKey key, TValue value, TimeSpan expirationTimespan)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                CacheItem ci = m_Dictionary[key];
                ci.m_Value = value;
                /* limited locking here so we work with a ReaderLock only here */
                lock (ci)
                {
                    ci.m_ExpiringDate = DateTime.UtcNow + expirationTimespan;
                }
            }
            catch (KeyNotFoundException)
            {
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void Add(TKey key, TValue value, double expirationSeconds)
        {
            Add(key, value, TimeSpan.FromSeconds(expirationSeconds));
        }
        public void Add(TKey key, TValue value, TimeSpan expirationTimespan)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary[key] = new CacheItem(value, DateTime.UtcNow + expirationTimespan);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> kvp)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary.Add(kvp.Key,
                    new CacheItem(
                        kvp.Value, DateTime.UtcNow + m_ExpirationTime
                        ));
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> kvp, double expirationSeconds)
        {
            Add(kvp, TimeSpan.FromSeconds(expirationSeconds));
        }
        public void Add(KeyValuePair<TKey, TValue> kvp, TimeSpan expirationTime)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary.Add(kvp.Key,
                    new CacheItem(
                        kvp.Value, DateTime.UtcNow + expirationTime
                        ));
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void Clear()
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary.Clear();
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> kvp)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary.ContainsKey(kvp.Key);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool Contains(TKey key, TValue value)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary.ContainsKey(key);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary.ContainsKey(key);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> kvp)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                return m_Dictionary.Remove(kvp.Key);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool Remove(TKey key)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                return m_Dictionary.Remove(key);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                CacheItem ci;
                bool success = m_Dictionary.TryGetValue(key, out ci);
                if(success)
                {
                    value = ci.m_Value;
                }
                return success;
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary.Keys;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    List<TValue> result = new List<TValue>();
                    foreach (CacheItem ci in m_Dictionary.Values)
                    {
                        result.Add(ci.m_Value);
                    }
                    return result;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array,
            int arrayIndex)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey, CacheItem> kvp in m_Dictionary)
                {
                    array[arrayIndex++] = new KeyValuePair<TKey,TValue>(kvp.Key, kvp.Value.m_Value);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Dictionary<TKey, TValue> outList = new Dictionary<TKey, TValue>();
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey, CacheItem> kvp in m_Dictionary)
                {
                    outList.Add(kvp.Key, kvp.Value.m_Value);
                }
                return outList.GetEnumerator();
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ExpireTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            /* only one timer tick thread should ever enter here */
            if (!Monitor.TryEnter(purgeLock))
                return;
            try
            {

                DateTime signalTime = DateTime.UtcNow;

                List<TKey> expireList = new List<TKey>();

                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    foreach(KeyValuePair<TKey, CacheItem> kvp in m_Dictionary)
                    {
                        lock (kvp.Value)
                        {
                            if (kvp.Value.m_ExpiringDate < signalTime)
                            {
                                expireList.Add(kvp.Key);
                            }
                        }
                    }

                    LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                    try
                    {
                        foreach(TKey key in expireList)
                        {
                            /* recheck since we are doing limited locking in cache update code */
                            if(m_Dictionary[key].m_ExpiringDate < signalTime)
                            {
                                m_Dictionary.Remove(key);
                            }
                        }
                    }
                    finally
                    {
                        m_RwLock.DowngradeFromWriterLock(ref lc);
                    }
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
            finally
            {
                Monitor.Exit(purgeLock);
            }
        }

        /* support for non-copy enumeration */
        public void ForEach(Action<KeyValuePair<TKey, TValue>> action)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey, CacheItem> kvp in m_Dictionary)
                {
                    action(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.m_Value));
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }
    }
}