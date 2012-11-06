using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Lemma.Components
{
	public class Updater : Component, IEnumerable<Action<float>>, IUpdateableComponent
	{
		protected List<Action<float>> actions = new List<Action<float>>();

		public Updater()
		{
			this.EnabledInEditMode.Value = false;
		}

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

		IEnumerator<Action<float>> IEnumerable<Action<float>>.GetEnumerator()
		{
			return this.actions.GetEnumerator();
		}

		public void Add(Action<float> action)
		{
			this.actions.Add(action);
		}

		public void Update(float elapsedTime)
		{
			foreach (Action<float> action in this.actions)
				action(elapsedTime);
		}
	}
}
