using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Lemma.Components
{
	public class PostInitialization : Component, IEnumerable<Action>, IUpdateableComponent
	{
		protected List<Action> actions = new List<Action>();

		public override void InitializeProperties()
		{
			base.InitializeProperties();
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
				this.EnabledWhenPaused.Value = false;
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

		public void Update(float elapsedTime)
		{
			foreach (Action action in this.actions)
				action();
			this.Delete.Execute();
		}
	}
}
