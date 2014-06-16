using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class CameraStopFactory : Factory<Main>
	{
		public CameraStopFactory()
		{
			this.Color = new Vector3(0.4f, 1.5f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "CameraStop");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			CameraStop cameraStop = entity.GetOrCreate<CameraStop>("CameraStop");

			entity.CannotSuspendByDistance = true;

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();
			
			if (main.EditorEnabled)
			{
				entity.Add("Preview", new Command
				{
					Action = delegate()
					{
						ulong id = entity.GUID;
						Editor editor = main.Get("Editor").First().Get<Editor>();
						if (editor.NeedsSave)
							editor.Save.Execute();
						main.EditorEnabled.Value = false;
						IO.MapLoader.Load(main, null, main.MapFile);

						main.Spawner.CanSpawn = false;
						main.Renderer.Brightness.Value = 0.0f;
						main.Renderer.InternalGamma.Value = 0.0f;
						main.IsMouseVisible.Value = false;
						
						main.AddComponent(new PostInitialization
						{
							delegate()
							{
								// We have to squirrel away the ID and get a new entity
								// becuase OUR entity got wiped out by the MapLoader.
								main.GetByGUID(id).Get<CameraStop>().Go.Execute();
							}
						});
					},
				}, Command.Perms.Executable);
			}

			entity.Add("Go", cameraStop.Go);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Color.Value = this.Color;
			model.Serialize = false;
			model.Scale.Value = new Vector3(1, 1, -1);
			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
			entity.Add("EditorModel", model);

			VoxelAttachable.AttachEditorComponents(entity, main);

			Model offsetModel = new Model();
			offsetModel.Filename.Value = "Models\\cone";
			offsetModel.Add(new Binding<Vector3>(offsetModel.Color, model.Color));

			CameraStop cameraStop = entity.Get<CameraStop>();

			offsetModel.Add(new Binding<bool>(offsetModel.Enabled, () => entity.EditorSelected && cameraStop.Offset != 0, entity.EditorSelected, cameraStop.Offset));
			offsetModel.Add(new Binding<Vector3, float>(offsetModel.Scale, x => new Vector3(1, 1, x), cameraStop.Offset));
			offsetModel.Add(new Binding<Matrix>(offsetModel.Transform, model.Transform));
			offsetModel.Serialize = false;
			entity.Add("EditorModel3", offsetModel);

			EntityConnectable.AttachEditorComponents(entity, cameraStop.Next);
		}
	}
}
