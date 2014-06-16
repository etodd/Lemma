using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.IO;

namespace Lemma.Components
{
	public class MapExit : Component<Main>
	{
		public Property<string> NextMap = new Property<string>();
		public Property<string> StartSpawnPoint = new Property<string>();

		public void Go()
		{
			MapLoader.Transition(main, this.NextMap, this.StartSpawnPoint);
		}
	}
}
