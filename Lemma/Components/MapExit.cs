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

		public static List<MapExit> All = new List<MapExit>();

		public void Go()
		{
			MapLoader.Transition(main, this.NextMap, this.StartSpawnPoint);
		}

		public override void Awake()
		{
			base.Awake();
			MapExit.All.Add(this);
		}

		public override void delete()
		{
			MapExit.All.Remove(this);
			base.delete();
		}
	}
}
