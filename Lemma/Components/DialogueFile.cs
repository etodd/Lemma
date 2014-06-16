using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;

namespace Lemma.Components
{
	public class DialogueFile : Component<Main>
	{
		public Property<string> Name = new Property<string>();

		public override void Start()
		{
			if (!this.main.EditorEnabled && !string.IsNullOrEmpty(this.Name))
			{
				Phone phone = PlayerDataFactory.Instance.GetOrCreate<Phone>("Phone");
				try
				{
					DialogueForest forest = WorldFactory.Instance.Get<World>().DialogueForest;
					IEnumerable<DialogueForest.Node> nodes = forest.Load(File.ReadAllText(Path.Combine(main.Content.RootDirectory, "Game", this.Name + ".dlz")));
					phone.Load(forest, nodes);
				}
				catch (IOException)
				{
					Log.d("Failed to load dialogue file: " + this.Name);
				}
			}
		}
	}
}
