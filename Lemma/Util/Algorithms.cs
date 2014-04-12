using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Util
{
	public static class Algorithms
	{
		public static void InsertionSort<T>(this IList<T> list, Comparison<T> comparison)
		{
			int count = list.Count;
			for (int j = 1; j < count; j++)
			{
				T key = list[j];

				int i = j - 1;
				for (; i >= 0 && comparison(list[i], key) > 0; i--)
					list[i + 1] = list[i];
				if (i != j - 1)
					list[i + 1] = key;
			}
		}
	}
}
