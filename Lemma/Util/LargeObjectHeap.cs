using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Util
{
	public class LargeObjectHeap<Type>
	{
		public static float GrowthFactor = 1.5f;

		private Dictionary<int, Queue<Type>> free = new Dictionary<int, Queue<Type>>();

		private static LargeObjectHeap<Type> instance;

		public static LargeObjectHeap<Type> Get(Func<int, Type> constructor)
		{
			return LargeObjectHeap<Type>.Get(constructor, x => x > 1000);
		}

		public static LargeObjectHeap<Type> Get(Func<int, Type> constructor, Func<int, bool> reuse)
		{
			if (LargeObjectHeap<Type>.instance == null)
			{
				LargeObjectHeap<Type>.instance = new LargeObjectHeap<Type>();
				LargeObjectHeap<Type>.instance.constructor = constructor;
				LargeObjectHeap<Type>.instance.reuse = reuse;
			}
			return LargeObjectHeap<Type>.instance;
		}
			
		private Func<int, Type> constructor;
		private Func<int, bool> reuse;

		private LargeObjectHeap()
		{
		}

		public Type Get(int size)
		{
			Type t;
			Queue<Type> queue;
			if (reuse(size) && this.free.TryGetValue(size, out queue) && queue.Count > 0)
				t = queue.Dequeue();
			else
				t = this.constructor(size);
			return t;
		}

		public void Free(int size, Type t)
		{
			if (this.reuse(size))
			{
				Queue<Type> queue;
				if (!this.free.TryGetValue(size, out queue))
					queue = this.free[size] = new Queue<Type>();
				queue.Enqueue(t);
			}
		}
	}
}
