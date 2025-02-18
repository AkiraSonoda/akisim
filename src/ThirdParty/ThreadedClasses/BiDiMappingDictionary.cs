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
    public class RwLockedBiDiMappingDictionary<TKey1, TKey2>
    {
        public class ChangeKeyFailedException : Exception
        {
            public ChangeKeyFailedException(string message)
                : base(message)
            {

            }
        }
        Dictionary<TKey1, TKey2> m_Dictionary_K1;
        Dictionary<TKey2, TKey1> m_Dictionary_K2;
        ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedBiDiMappingDictionary()
        {
            m_Dictionary_K1 = new Dictionary<TKey1, TKey2>();
            m_Dictionary_K2 = new Dictionary<TKey2, TKey1>();
        }

        public RwLockedBiDiMappingDictionary(int capacity)
        {
            m_Dictionary_K1 = new Dictionary<TKey1, TKey2>(capacity);
            m_Dictionary_K2 = new Dictionary<TKey2, TKey1>(capacity);
        }

        public void Add(TKey1 key1, TKey2 key2)
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

                m_Dictionary_K1[key1] = key2;
                m_Dictionary_K2[key2] = key1;
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
                TKey2 kvp;
                if (m_Dictionary_K1.TryGetValue(key1, out kvp))
                {
                    if (!kvp.Equals(key2))
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
                TKey2 kvp;
                if (m_Dictionary_K1.TryGetValue(key1, out kvp))
                {
                    m_Dictionary_K1.Remove(key1);
                    m_Dictionary_K2.Remove(kvp);
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
                TKey1 kvp;
                if (m_Dictionary_K2.TryGetValue(key2, out kvp))
                {
                    m_Dictionary_K1.Remove(kvp);
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

        public bool TryGetValue(TKey1 key, out TKey2 value)
        {
            value = default(TKey2);
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary_K1.TryGetValue(key, out value);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool TryGetValue(TKey2 key, out TKey1 value)
        {
            value = default(TKey1);
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary_K2.TryGetValue(key, out value);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public TKey2 this[TKey1 key]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary_K1[key];
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public TKey1 this[TKey2 key]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary_K2[key];
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public void CopyTo(out Dictionary<TKey1, TKey2> result)
        {
            result = new Dictionary<TKey1, TKey2>();

            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey1, TKey2> kvp in m_Dictionary_K1)
                {
                    result.Add(kvp.Key, kvp.Value);
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
                TKey2 kvp = m_Dictionary_K1[oldKey];
                m_Dictionary_K1.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K2[kvp] = newKey;
                m_Dictionary_K1[newKey] = kvp;
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
                TKey1 kvp = m_Dictionary_K2[oldKey];
                m_Dictionary_K2.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K1[kvp] = newKey;
                m_Dictionary_K2[newKey] = kvp;
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void ForEach(Action<TKey1> d)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (TKey1 kvp in m_Dictionary_K1.Keys)
                {
                    d(kvp);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void ForEach(Action<TKey2> d)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (TKey2 kvp in m_Dictionary_K2.Keys)
                {
                    d(kvp);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public List<TKey1> Keys1
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return new List<TKey1>(m_Dictionary_K1.Keys);
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public List<TKey2> Keys2
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return new List<TKey2>(m_Dictionary_K2.Keys);
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }
    }
}
