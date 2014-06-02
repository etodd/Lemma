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
			Property<string> nextMap = entity.GetOrMakeProperty<string>("NextMap", true);
			Property<string> startSpawnPoint = entity.GetOrMakeProperty<string>("SpawnPoint", true);

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				MapLoader.Transition(main, nextMap, startSpawnPoint);
			}));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);
		}
	}
}