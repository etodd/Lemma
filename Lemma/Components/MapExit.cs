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
		public EditorProperty<string> NextMap = new EditorProperty<string>();
		public EditorProperty<string> StartSpawnPoint = new EditorProperty<string>();

		public void Go()
		{
			MapLoader.Transition(main, this.NextMap, this.StartSpawnPoint);
		}
	}
}
