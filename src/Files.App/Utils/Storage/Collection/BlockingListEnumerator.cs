// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Storage
{
	public sealed class BlockingListEnumerator<T> : IEnumerator<T>
	{
		private readonly IList<T> m_Inner;

		private readonly object m_Lock;

		private T m_Current;

		private int m_Pos;

		public T Current
		{
			get
			{
				lock (m_Lock)
					return m_Current;
			}
		}

		object IEnumerator.Current
			=> Current;

		public BlockingListEnumerator(IList<T> inner, object @lock)
		{
			m_Inner = inner;
			m_Lock = @lock;
			m_Pos = -1;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			lock (m_Lock)
			{
				m_Pos++;
				var hasNext = m_Pos < m_Inner.Count;
				if (hasNext)
				{
					m_Current = m_Inner[m_Pos];
				}
				return hasNext;
			}
		}

		public void Reset()
		{
			lock (m_Lock)
			{
				m_Pos = -1;
			}
		}
	}
}
