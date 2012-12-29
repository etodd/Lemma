using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lemma.Util
{
	public class BlockingQueue<T>
	{
		private readonly Queue<T> queue = new Queue<T>();

		public int Count
		{
			get
			{
				return this.queue.Count;
			}
		}

		private readonly int maxSize;
		public BlockingQueue(int maxSize)
		{
			this.maxSize = maxSize;
		}

		public void Enqueue(T item)
		{
			lock (this.queue)
			{
				while (this.queue.Count >= this.maxSize)
					Monitor.Wait(this.queue);
				this.queue.Enqueue(item);
				if (this.queue.Count == 1)
				{
					// wake up any blocked dequeue
					Monitor.PulseAll(this.queue);
				}
			}
		}

		public T Dequeue()
		{
			lock (this.queue)
			{
				while (this.queue.Count == 0)
					Monitor.Wait(this.queue);
				T item = this.queue.Dequeue();
				if (this.queue.Count == maxSize - 1)
				{
					// wake up any blocked enqueue
					Monitor.PulseAll(this.queue);
				}
				return item;
			}
		}
	}
}
