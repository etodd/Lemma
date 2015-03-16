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

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			MapExit mapExit = entity.GetOrCreate<MapExit>("MapExit");

			AmbientSound sound = entity.GetOrCreate<AmbientSound>();
			sound.Add(new Binding<Vector3>(sound.Position, transform.Position));
			sound.PlayCue.Value = AK.EVENTS.PLAY_DOOR_AMBIENCE;
			sound.StopCue.Value = AK.EVENTS.STOP_DOOR_AMBIENCE;
			sound.Add(new Binding<bool>(sound.Enabled, trigger.Enabled));

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, (Action)mapExit.Go));

			trigger.EditorProperties();

			entity.Add("Enable", trigger.Enable);
			entity.Add("Disable", trigger.Disable);
			entity.Add("OnEnter", trigger.PlayerEntered);

			entity.Add("Enabled", trigger.Enabled);
			entity.Add("NextMap", mapExit.NextMap, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.MapDirectory, null, MapLoader.MapExtension, delegate()
				{
					return new[] { Main.MenuMap };
				}),
			});
			entity.Add("StartSpawnPoint", mapExit.StartSpawnPoint);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>("EditorModel").Color);
		}
	}
}