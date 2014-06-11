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
			Entity entity = new Entity(main, "PlayerSpawn");

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Enabled.Value = false;
			entity.Add("Trigger", trigger);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			VoxelAttachable.MakeAttachable(entity, main);

			entity.CannotSuspendByDistance = true;

			this.SetMain(entity, main);

			if (main.EditorEnabled)
			{
				entity.Add("Spawn Here", new Command
				{
					Action = delegate()
					{
						main.Spawner.StartSpawnPoint.Value = entity.ID;
						Editor editor = main.Get("Editor").First().Get<Editor>();
						if (editor.NeedsSave)
							editor.Save.Execute();
						main.EditorEnabled.Value = false;
						IO.MapLoader.Load(main, null, main.MapFile);
					},
					ShowInEditor = true,
				});
			}

			PlayerSpawn spawn = entity.GetOrCreate<PlayerSpawn>("PlayerSpawn");
			spawn.Add(new TwoWayBinding<Vector3>(transform.Position, spawn.Position));
			spawn.Add(new Binding<float, Vector3>(spawn.Rotation, x => ((float)Math.PI * -0.5f) - (float)Math.Atan2(x.Z, x.X), transform.Forward));

			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("Trigger");
			trigger.Enabled.Editable = true;
			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate() { spawn.Activate.Execute(); }));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Color.Value = this.Color;
			model.Editable = false;
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);
			VoxelAttachable.AttachEditorComponents(entity, main);
		}
	}
}
