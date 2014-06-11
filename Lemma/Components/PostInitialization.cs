using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ComponentBind;

namespace Lemma.Components
{
	public class PostInitialization : Component<Main>, IEnumerable<Action>
	{
		protected List<Action> actions = new List<Action>();

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
		}

		public override Entity Entity
		{
			get
			{
				return base.Entity;
			}
			set
			{
				base.Entity = value;
				this.EnabledWhenPaused = false;
				this.Suspended.Set = delegate(bool v)
				{
					this.Suspended.InternalValue = false;
				};
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.actions.GetEnumerator();
		}

		IEnumerator<Action> IEnumerable<Action>.GetEnumerator()
		{
			return this.actions.GetEnumerator();
		}

		public void Add(Action action)
		{
			this.actions.Add(action);
		}

		public override void Start()
		{
			foreach (Action action in this.actions)
				action();
			this.Delete.Execute();
		}
	}
}
