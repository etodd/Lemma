using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ComponentBind;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class Updater : Component<Main>, IUpdateableComponent
	{
		[XmlIgnore]
		public Action<float> Action;

		public override void delete()
		{
			base.delete();
			this.Action = null;
		}

		public Updater()
		{

		}

		public Updater(Action<float> action)
		{
			this.Action = action;
		}

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledInEditMode = false;
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
			}
		}

		public void Update(float elapsedTime)
		{
			this.Action(elapsedTime);
		}
	}
}
