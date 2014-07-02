using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;
using Lemma.IO;

namespace Lemma.Factories
{
	public class MapExitFactory : Factory<Main>
	{
		public MapExitFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "MapExit");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");
			this.SetMain(entity, main);

			MapExit mapExit = entity.GetOrCreate<MapExit>("MapExit");

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, (Action)mapExit.Go));

			trigger.EditorProperties();
			entity.Add("Enable", trigger.Enable);
			entity.Add("Enabled", trigger.Enabled);
			entity.Add("Disable", trigger.Disable);
			ListProperty<string> options = FileFilter.Get(main, Path.Combine(main.Content.RootDirectory, MapLoader.MapDirectory), null, MapLoader.MapExtension);
			if (options != null && !options.Contains(Main.MenuMap))
				options.Add(Main.MenuMap);
			entity.Add("NextMap", mapExit.NextMap, null, null, options);
			entity.Add("StartSpawnPoint", mapExit.StartSpawnPoint);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);
		}
	}
}