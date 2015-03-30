using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ComponentBind;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class PostInitialization : Component<Main>
	{
		public PostInitialization()
		{
			
		}

		public PostInitialization(Action a)
		{
			this.Action = a;
		}

		[XmlIgnore]
		public Action Action;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
		}

		public override void Start()
		{
			this.Action();
			this.Delete.Execute();
		}
	}
}
