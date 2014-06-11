using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using Lemma.Util;

namespace Lemma.Factories
{
	public class DialogueFileFactory : Factory<Main>
	{
		public DialogueFileFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "DialogueFile");

			entity.Add("Transform", new Transform());

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);
			entity.CannotSuspend = true;

			Property<string> name = entity.GetOrMakeProperty<string>("Name", true);

			if (!main.EditorEnabled)
			{
				entity.Add(new PostInitialization
				{
					delegate()
					{
						if (!string.IsNullOrEmpty(name))
						{
							Phone phone = PlayerDataFactory.Instance.GetOrCreate<Phone>("Phone");
							try
							{
								DialogueForest forest = WorldFactory.Instance.Get<World>().DialogueForest;
								IEnumerable<DialogueForest.Node> nodes = forest.Load(File.ReadAllText(Path.Combine(main.Content.RootDirectory, "Game", name + ".dlz")));
								phone.Load(forest, nodes);
							}
							catch (IOException)
							{
								Log.d("Failed to load dialogue file: " + name);
							}
						}
					}
				});
			}
		}
	}
}
