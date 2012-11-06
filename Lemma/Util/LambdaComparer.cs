using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Util
{
	public class LambdaComparer<T> : IComparer<T>
	{
		private readonly Func<T, T, int> lambda;

		public LambdaComparer(Func<T, T, int> _lambda)
		{
			if (_lambda == null)
				throw new ArgumentNullException("_lambda");

			this.lambda = _lambda;
		}

		public int Compare(T x, T y)
		{
			return this.lambda(x, y);
		}
	}
}
