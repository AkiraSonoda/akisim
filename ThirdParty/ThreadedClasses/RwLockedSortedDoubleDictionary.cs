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
    public class RwLockedSortedDoubleDictionary<TKey1, TKey2, TValue>
    {
        public class ChangeKeyFailedException : Exception
        {
            public ChangeKeyFailedException(string message)
                : base(message)
            {

            }
        }
        SortedDictionary<TKey1, KeyValuePair<TKey2, TValue>> m_Dictionary_K1;
        SortedDictionary<TKey2, KeyValuePair<TKey1, TValue>> m_Dictionary_K2;
        ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedSortedDoubleDictionary()
        {
            m_Dictionary_K1 = new SortedDictionary<TKey1, KeyValuePair<TKey2, TValue>>();
            m_Dictionary_K2 = new SortedDictionary<TKey2, KeyValuePair<TKey1, TValue>>();
        }

        public RwLockedSortedDoubleDictionary(IComparer<TKey1> comparer1, IComparer<TKey2> comparer2)
        {
            m_Dictionary_K1 = new SortedDictionary<TKey1, KeyValuePair<TKey2, TValue>>(comparer1);
            m_Dictionary_K2 = new SortedDictionary<TKey2, KeyValuePair<TKey1, TValue>>(comparer2);
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Dictionary_K1.ContainsKey(key1))
                {
                    if (!m_Dictionary_K2.ContainsKey(key2))
                        throw new ArgumentException("key1 exists in the dictionary but not key2");
                }
                else if (m_Dictionary_K2.ContainsKey(key2))
                {
                    if (!m_Dictionary_K1.ContainsKey(key1))
                        throw new ArgumentException("key2 exists in the dictionary but not key1");
                }

                m_Dictionary_K1[key1] = new KeyValuePair<TKey2, TValue>(key2, value);
                m_Dictionary_K2[key2] = new KeyValuePair<TKey1, TValue>(key1, value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool Remove(TKey1 key1, TKey2 key2)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                KeyValuePair<TKey2, TValue> kvp;
                if (m_Dictionary_K1.TryGetValue(key1, out kvp))
                {
                    if (!kvp.Key.Equals(key2))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                m_Dictionary_K1.Remove(key1);
                m_Dictionary_K2.Remove(key2);
                return true;
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool Remove(TKey1 key1)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                KeyValuePair<TKey2, TValue> kvp;
                if (m_Dictionary_K1.TryGetValue(key1, out kvp))
                {
                    m_Dictionary_K1.Remove(key1);
                    m_Dictionary_K2.Remove(kvp.Key);
                    return true;
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return false;
        }

        public bool Remove(TKey2 key2)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                KeyValuePair<TKey1, TValue> kvp;
                if (m_Dictionary_K2.TryGetValue(key2, out kvp))
                {
                    m_Dictionary_K1.Remove(kvp.Key);
                    m_Dictionary_K2.Remove(key2);
                    return true;
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return false;
        }

        public bool Remove(TKey1 key1, out TValue val)
        {
            val = default(TValue);
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                KeyValuePair<TKey2, TValue> kvp;
                if (m_Dictionary_K1.TryGetValue(key1, out kvp))
                {
                    m_Dictionary_K1.Remove(key1);
                    m_Dictionary_K2.Remove(kvp.Key);
                    val = kvp.Value;
                    return true;
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return false;
        }

        public bool Remove(TKey2 key2, out TValue val)
        {
            val = default(TValue);
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                KeyValuePair<TKey1, TValue> kvp;
                if (m_Dictionary_K2.TryGetValue(key2, out kvp))
                {
                    m_Dictionary_K1.Remove(kvp.Key);
                    m_Dictionary_K2.Remove(key2);
                    val = kvp.Value;
                    return true;
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return false;
        }

        public void Clear()
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary_K1.Clear();
                m_Dictionary_K2.Clear();
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public int Count
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary_K1.Count;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public bool ContainsKey(TKey1 key)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary_K1.ContainsKey(key);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool ContainsKey(TKey2 key)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary_K2.ContainsKey(key);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool TryGetValue(TKey1 key, out TValue value)
        {
            value = default(TValue);
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                KeyValuePair<TKey2, TValue> kvp;
                bool success = m_Dictionary_K1.TryGetValue(key, out kvp);
                if (success)
                {
                    value = kvp.Value;
                }
                return success;
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool TryGetValue(TKey2 key, out TValue value)
        {
            value = default(TValue);
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                KeyValuePair<TKey1, TValue> kvp;
                bool success = m_Dictionary_K2.TryGetValue(key, out kvp);
                if (success)
                {
                    value = kvp.Value;
                }
                return success;
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public TValue this[TKey1 key]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary_K1[key].Value;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_RwLock.AcquireWriterLock(-1);
                try
                {
                    KeyValuePair<TKey2, TValue> kvp = m_Dictionary_K1[key];
                    m_Dictionary_K2[kvp.Key] = new KeyValuePair<TKey1, TValue>(key, value);
                    m_Dictionary_K1[key] = new KeyValuePair<TKey2, TValue>(kvp.Key, value);
                }
                finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }
        }

        public TValue this[TKey2 key]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary_K2[key].Value;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_RwLock.AcquireWriterLock(-1);
                try
                {
                    KeyValuePair<TKey1, TValue> kvp = m_Dictionary_K2[key];
                    m_Dictionary_K2[key] = new KeyValuePair<TKey1, TValue>(kvp.Key, value);
                    m_Dictionary_K1[kvp.Key] = new KeyValuePair<TKey2, TValue>(key, value);
                }
                finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }
        }

        public void CopyTo(out Dictionary<TKey1, TValue> result)
        {
            result = new Dictionary<TKey1, TValue>();

            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey1, KeyValuePair<TKey2, TValue>> kvp in m_Dictionary_K1)
                {
                    result.Add(kvp.Key, kvp.Value.Value);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void CopyTo(out Dictionary<TKey2, TValue> result)
        {
            result = new Dictionary<TKey2, TValue>();

            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey2, KeyValuePair<TKey1, TValue>> kvp in m_Dictionary_K2)
                {
                    result.Add(kvp.Key, kvp.Value.Value);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void ChangeKey(TKey1 newKey, TKey1 oldKey)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Dictionary_K1.ContainsKey(newKey))
                {
                    throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
                }
                KeyValuePair<TKey2, TValue> kvp = m_Dictionary_K1[oldKey];
                m_Dictionary_K1.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K2[kvp.Key] = new KeyValuePair<TKey1, TValue>(newKey, kvp.Value);
                m_Dictionary_K1[newKey] = new KeyValuePair<TKey2, TValue>(kvp.Key, kvp.Value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void ChangeKey(TKey2 newKey, TKey2 oldKey)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Dictionary_K2.ContainsKey(newKey))
                {
                    throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
                }
                KeyValuePair<TKey1, TValue> kvp = m_Dictionary_K2[oldKey];
                m_Dictionary_K2.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K1[kvp.Key] = new KeyValuePair<TKey2, TValue>(newKey, kvp.Value);
                m_Dictionary_K2[newKey] = new KeyValuePair<TKey1, TValue>(kvp.Key, kvp.Value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void ForEach(Action<TValue> d)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey2, TValue> kvp in m_Dictionary_K1.Values)
                {
                    d(kvp.Value);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void ForEach(Action<KeyValuePair<TKey1, TValue>> d)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey1, KeyValuePair<TKey2, TValue>> kvp in m_Dictionary_K1)
                {
                    d(new KeyValuePair<TKey1, TValue>(kvp.Key, kvp.Value.Value));
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void ForEach(Action<KeyValuePair<TKey2, TValue>> d)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey2, KeyValuePair<TKey1, TValue>> kvp in m_Dictionary_K2)
                {
                    d(new KeyValuePair<TKey2, TValue>(kvp.Key, kvp.Value.Value));
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public List<TValue> Values
        {
            get
            {
                List<TValue> result = new List<TValue>();
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    foreach (KeyValuePair<TKey2, TValue> kvp in m_Dictionary_K1.Values)
                    {
                        result.Add(kvp.Value);
                    }

                    return result;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public List<TValue> ValuesByKey1
        {
            get
            {
                return Values;
            }
        }

        public List<TValue> ValuesByKey2
        {
            get
            {
                List<TValue> result = new List<TValue>();
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    foreach (KeyValuePair<TKey1, TValue> kvp in m_Dictionary_K2.Values)
                    {
                        result.Add(kvp.Value);
                    }

                    return result;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }
    }
}
