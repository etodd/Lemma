using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Util
{
	public class LargeObjectHeap<Type>
	{
		private static Dictionary<int, Queue<Type>> free = new Dictionary<int, Queue<Type>>();

		public static Type Get(int size, Func<int, Type> constructor)
		{
			Type t;
			Queue<Type> queue;
			if (LargeObjectHeap<Type>.free.TryGetValue(size, out queue) && queue.Count > 0)
				t = queue.Dequeue();
			else
				t = constructor(size);
			return t;
		}

		public static void Free(int size, Type t)
		{
			Queue<Type> queue;
			if (!LargeObjectHeap<Type>.free.TryGetValue(size, out queue))
				queue = LargeObjectHeap<Type>.free[size] = new Queue<Type>();
			queue.Enqueue(t);
		}
	}
}
