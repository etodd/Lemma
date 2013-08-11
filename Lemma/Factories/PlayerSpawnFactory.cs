using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PlayerSpawnFactory : Factory
	{
		public PlayerSpawnFactory()
		{
			this.Color = new Vector3(0.8f, 0.4f, 1.5f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "PlayerSpawn");

			result.Add("PlayerSpawn", new PlayerSpawn());

			Transform transform = new Transform();
			result.Add("Transform", transform);

			result.Add("Trigger", new PlayerTrigger());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			result.CannotSuspendByDistance = true;

			this.SetMain(result, main);

			if (main.EditorEnabled)
			{
				result.Add("Spawn Here", new Command
				{
					Action = delegate()
					{
						((GameMain)main).StartSpawnPoint.Value = result.ID;
						Editor editor = main.Get("Editor").First().Get<Editor>();
						if (editor.NeedsSave)
							editor.Save.Execute();
						main.EditorEnabled.Value = false;
						IO.MapLoader.Load(main, null, main.MapFile);
					},
					ShowInEditor = true,
				});
			}

			Transform transform = result.Get<Transform>();

			PlayerSpawn spawn = result.Get<PlayerSpawn>();
			spawn.Add(new TwoWayBinding<Vector3>(transform.Position, spawn.Position));
			spawn.Add(new Binding<float, Vector3>(spawn.Rotation, x => ((float)Math.PI * -0.5f) - (float)Math.Atan2(x.Z, x.X), transform.Forward));

			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			trigger.Enabled.Editable = true;
			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player) { spawn.Activate.Execute(); }));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Color.Value = this.Color;
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));

			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
			MapAttachable.AttachEditorComponents(result, main);
		}
	}
}
