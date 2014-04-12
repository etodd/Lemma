// Stolen and adapted from http://www.codeguru.com/csharp/csharp/cs_misc/designtechniques/article.php/c12527

using System; using ComponentBind;
using System.Collections;
using System.Collections.Generic;

namespace Lemma.Util
{
	public class PriorityQueue<T>
	{
		protected List<T> list = new List<T>();
		protected IComparer<T> comparer;

		public PriorityQueue()
		{
			this.comparer = Comparer<T>.Default;
		}

		public PriorityQueue(IComparer<T> comparer)
		{
			this.comparer = comparer;
		}

		public PriorityQueue(IComparer<T> comparer, int capacity)
		{
			this.comparer = comparer;
			this.list.Capacity = capacity;
		}

		protected void SwitchElements(int i, int j)
		{
			T h = this.list[i];
			this.list[i] = this.list[j];
			this.list[j] = h;
		}

		protected virtual int OnCompare(int i, int j)
		{
			return this.comparer.Compare(list[i], list[j]);
		}

		/// <summary>
		/// Push an object onto the PQ
		/// </summary>
		/// <param name="O">The new object</param>
		/// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
		public int Push(T item)
		{
			int p = this.list.Count, p2;
			this.list.Add(item); // E[p] = O
			do
			{
				if (p == 0)
					break;
				p2 = (p - 1) / 2;
				if (this.OnCompare(p, p2) < 0)
				{
					this.SwitchElements(p, p2);
					p = p2;
				}
				else
					break;
			} while (true);
			return p;
		}

		/// <summary>
		/// Get the smallest object and remove it.
		/// </summary>
		/// <returns>The smallest object</returns>
		public T Pop()
		{
			T result = this.list[0];
			int p = 0, p1, p2, pn;
			this.list[0] = this.list[this.list.Count - 1];
			this.list.RemoveAt(this.list.Count - 1);
			do
			{
				pn = p;
				p1 = 2 * p + 1;
				p2 = 2 * p + 2;
				if (this.list.Count > p1 && this.OnCompare(p, p1) > 0) // links kleiner
					p = p1;
				if (this.list.Count > p2 && this.OnCompare(p, p2) > 0) // rechts noch kleiner
					p = p2;

				if (p == pn)
					break;
				this.SwitchElements(p, pn);
			} while (true);

			return result;
		}

		/// <summary>
		/// Notify the PQ that the object at position i has changed
		/// and the PQ needs to restore order.
		/// Since you dont have access to any indexes (except by using the
		/// explicit IList.this) you should not call this function without knowing exactly
		/// what you do.
		/// </summary>
		/// <param name="i">The index of the changed object.</param>
		public void Update(T item)
		{
			int i = this.list.IndexOf(item);
			int p = i, pn;
			int p1, p2;
			do	// aufsteigen
			{
				if (p == 0)
					break;
				p2 = (p - 1) / 2;
				if (this.OnCompare(p, p2) < 0)
				{
					this.SwitchElements(p, p2);
					p = p2;
				}
				else
					break;
			} while (true);
			if (p < i)
				return;
			do	   // absteigen
			{
				pn = p;
				p1 = 2 * p + 1;
				p2 = 2 * p + 2;
				if (this.list.Count > p1 && this.OnCompare(p, p1) > 0) // links kleiner
					p = p1;
				if (this.list.Count > p2 && this.OnCompare(p, p2) > 0) // rechts noch kleiner
					p = p2;

				if (p == pn)
					break;
				this.SwitchElements(p, pn);
			} while (true);
		}

		/// <summary>
		/// Get the smallest object without removing it.
		/// </summary>
		/// <returns>The smallest object</returns>
		public T Peek()
		{
			if (this.list.Count > 0)
				return this.list[0];
			return default(T);
		}

		public void Clear()
		{
			this.list.Clear();
		}

		public int Count
		{
			get { return this.list.Count; }
		}

		public T this[int index]
		{
			get { return this.list[index]; }
			set
			{
				this.list[index] = value;
				this.Update(value);
			}
		}
	}
}