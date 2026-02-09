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
    public class RwLockedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
		protected ReaderWriterLock m_RwLock = new ReaderWriterLock();
        protected Dictionary<TKey, TValue> m_Dictionary;

        public class KeyAlreadyExistsException : Exception
        {
            public KeyAlreadyExistsException(string message)
                : base(message)
            {

            }
        }

        public RwLockedDictionary()
        {
            m_Dictionary = new Dictionary<TKey, TValue>();
        }

        public RwLockedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public RwLockedDictionary(IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(comparer);
        }

        public RwLockedDictionary(int capacity)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(capacity);
        }

        public RwLockedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(dictionary, comparer);
        }

        public RwLockedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(capacity, comparer);
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
                    return m_Dictionary[key];
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
                    m_Dictionary[key] = value;
                }
				finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }
        }

        public delegate TValue CreateValueDelegate();
        public TValue AddIfNotExists(TKey key, CreateValueDelegate del)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if(m_Dictionary.ContainsKey(key))
                {
                    throw new KeyAlreadyExistsException("Key \"" + key.ToString() + "\" already exists");
                }
                TValue res = del();
                m_Dictionary.Add(key, res);
                return res;
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public TValue GetOrAddIfNotExists(TKey key, CreateValueDelegate del)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_Dictionary[key];
            }
            catch(KeyNotFoundException)
            {
                LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                try
                {
                    if(m_Dictionary.ContainsKey(key))
                    {
                        return m_Dictionary[key];
                    }
                    return m_Dictionary[key] = del();
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

        public void Add(TKey key, TValue value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary.Add(key, value);
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
                m_Dictionary.Add(kvp.Key, kvp.Value);
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

        public bool ContainsValue(TValue value)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach(KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                { 
                    if(kvp.Value.Equals(value))
                    {
                        return true;
                    }
                }
                return false;
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

        public bool Remove(TKey key, out TValue val)
        {
            val = default(TValue);
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Dictionary.ContainsKey(key))
                {
                    val = m_Dictionary[key];
                }
                return m_Dictionary.Remove(key);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
			m_RwLock.AcquireReaderLock(-1);
			try
            {
				return m_Dictionary.TryGetValue(key, out value);
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
                    return new List<TKey>(m_Dictionary.Keys);
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
                    return new List<TValue>(m_Dictionary.Values);
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
                foreach(KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    array[arrayIndex++] = kvp;
                } 
            }
			finally
            {
				m_RwLock.ReleaseReaderLock();
			}
		}

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return (new Dictionary<TKey, TValue>(m_Dictionary)).GetEnumerator();
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

        /* support for non-copy enumeration */
        public void ForEach(Action<KeyValuePair<TKey, TValue>> action)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    action(kvp);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        /* support for non-copy enumeration */
        public void ForEach(Action<TKey> action)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    action(kvp.Key);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        /* support for non-copy enumeration */
        public void ForEach(Action<TValue> action)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    action(kvp.Value);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public TKey[] GetKeyStrings()
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                TKey[] __keys = new TKey[m_Dictionary.Count];
                m_Dictionary.Keys.CopyTo(__keys, 0);
                return __keys;
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public delegate bool CheckIfRemove(TValue value);

        public bool RemoveIf(TKey key, CheckIfRemove del)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Dictionary.ContainsKey(key))
                {
                    if (!del(m_Dictionary[key]))
                    {
                        return false;
                    }
                }
                return m_Dictionary.Remove(key);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool RemoveIf(TKey key, CheckIfRemove del, out KeyValuePair<TKey, TValue> kvp)
        {
            kvp = default(KeyValuePair<TKey, TValue>);
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Dictionary.ContainsKey(key))
                {
                    TValue val = m_Dictionary[key];
                    if (!del(val))
                    {
                        return false;
                    }
                    kvp = new KeyValuePair<TKey, TValue>(key, val);
                }
                return m_Dictionary.Remove(key);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public delegate bool FindIfRemove(TKey key, TValue value);
        public bool FindRemoveIf(FindIfRemove del, out KeyValuePair<TKey, TValue> kvpout)
        {
            kvpout = default(KeyValuePair<TKey, TValue>);
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach(KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    if (!del(kvp.Key, kvp.Value))
                    {
                        continue;
                    }
                    LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                    try
                    {
                        if(m_Dictionary.ContainsKey(kvp.Key))
                        {
                            m_Dictionary.Remove(kvp.Key);
                            kvpout = kvp;
                            return true;
                        }
                    }
                    finally
                    {
                        m_RwLock.DowngradeFromWriterLock(ref lc);
                    }
                }
                return false;
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }


        public void Remove(IEnumerable<TKey> keys)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                foreach(TKey key in keys)
                {
                    m_Dictionary.Remove(key);
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void Replace(IDictionary<TKey, TValue> dictionary)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_Dictionary = new Dictionary<TKey,TValue>(dictionary);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public delegate bool CheckReplaceDelegate(TValue value);

        public bool AddOrReplaceValueIf(TKey key, TValue value, CheckReplaceDelegate del)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                TValue checkval;
                if(m_Dictionary.TryGetValue(key, out checkval))
                {
                    if(!del(checkval))
                    {
                        return false;
                    }
                }
                m_Dictionary[key] = value;
                return true;
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                foreach(KeyValuePair<TKey, TValue> kvp in items)
                {
                    m_Dictionary.Add(kvp.Key, kvp.Value);
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }
    }

    public class RwLockedDictionaryAutoAdd<TKey, TValue> : RwLockedDictionary<TKey, TValue>
    {
        private CreateValueDelegate m_AutoAddDelegate;
        public RwLockedDictionaryAutoAdd(CreateValueDelegate autoAddDelegate)
            : base()
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(IDictionary<TKey, TValue> dictionary, CreateValueDelegate autoAddDelegate)
            : base(dictionary)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(IEqualityComparer<TKey> comparer, CreateValueDelegate autoAddDelegate)
            : base(comparer)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(int capacity, CreateValueDelegate autoAddDelegate)
            : base(capacity)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, CreateValueDelegate autoAddDelegate)
            : base(dictionary, comparer)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(int capacity, IEqualityComparer<TKey> comparer, CreateValueDelegate autoAddDelegate)
            : base(capacity, comparer)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public new TValue this[TKey key]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dictionary[key];
                }
                catch(KeyNotFoundException)
                {
                    LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                    try
                    {
                        return m_Dictionary[key] = m_AutoAddDelegate();
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
            set
            {
                m_RwLock.AcquireWriterLock(-1);
                try
                {
                    m_Dictionary[key] = value;
                }
                finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }
        }
    }
}