using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;

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
			Entity result = new Entity(main, "DialogueFile");

			result.Add("Transform", new Transform());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			result.CannotSuspend = true;

			Property<string> name = result.GetOrMakeProperty<string>("Name", true);

			if (!main.EditorEnabled)
			{
				result.Add(new PostInitialization
				{
					delegate()
					{
						if (!string.IsNullOrEmpty(name))
						{
							Phone phone = Factory<Main>.Get<PlayerDataFactory>().Instance(main).GetOrCreate<Phone>("Phone");
							try
							{
								DialogueForest forest = new DialogueForest(File.ReadAllText(Path.Combine(main.Content.RootDirectory, "Game", name + ".dlz")));
								phone.Load(forest);
								WorldFactory.Get().GetListProperty<DialogueForest>().Add(forest);
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
