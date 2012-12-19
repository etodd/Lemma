using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class AI : Component, IUpdateableComponent
	{
		public override void InitializeProperties()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
		}

		public void Update(float elapsedTime)
		{

		}
	}
}
