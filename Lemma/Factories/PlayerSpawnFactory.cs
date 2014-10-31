using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PlayerSpawnFactory : Factory<Main>
	{
		public PlayerSpawnFactory()
		{
			this.Color = new Vector3(0.8f, 0.4f, 1.5f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "PlayerSpawn");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			entity.CannotSuspendByDistance = true;

			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("Trigger");
			PlayerSpawn spawn = entity.GetOrCreate<PlayerSpawn>("PlayerSpawn");
			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);

			this.SetMain(entity, main);

			attachable.EditorProperties();

			if (main.EditorEnabled)
			{
				entity.Add("Spawn Here", new Command
				{
					Action = delegate()
					{
						main.Spawner.StartSpawnPointGUID.Value = entity.GUID;
						Editor editor = main.Get("Editor").First().Get<Editor>();
						if (editor.NeedsSave)
							editor.Save.Execute();
						main.EditorEnabled.Value = false;
						IO.MapLoader.Load(main, main.MapFile);
					},
				}, Command.Perms.Executable);
			}

			spawn.Add(new TwoWayBinding<Vector3>(transform.Position, spawn.Position));
			spawn.Add(new Binding<float, Quaternion>(spawn.Rotation, delegate(Quaternion value)
			{
				Vector3 x = Vector3.Transform(Vector3.Forward, value);
				return ((float)Math.PI * -0.5f) - (float)Math.Atan2(x.Z, x.X);
			}, transform.Quaternion));
			spawn.EditorProperties();

			trigger.Enabled.Value = true;
			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, spawn.Activate));

			trigger.EditorProperties();
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\light";
			model.Color.Value = this.Color;
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
			model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);
			VoxelAttachable.AttachEditorComponents(entity, main);
		}
	}
}
