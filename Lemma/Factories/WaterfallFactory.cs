using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using Lemma.IO;

namespace Lemma.Factories
{
	public class WaterfallFactory : Factory<Main>
	{
		public WaterfallFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Waterfall");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			ModelAlpha model = entity.GetOrCreate<ModelAlpha>("Model");
			model.Filename.Value = "AlphaModels\\waterfall";
			model.Distortion.Value = true;

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			entity.Add("Scale", model.Scale);
			entity.Add("Color", model.Color);
			entity.Add("Alpha", model.Alpha);

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			Property<Vector2> uvScale = model.GetVector2Parameter("UVScale");
			model.Add(new Binding<Vector2, Vector3>(uvScale, x => new Vector2(x.X, x.Y), model.Scale));

			Property<Vector3> soundPosition = new Property<Vector3>();
			AkGameObjectTracker.Attach(entity, soundPosition);

			if (!main.EditorEnabled && !model.Suspended)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WATERFALL_LOOP, entity);

			Action stopSound = delegate()
			{
				AkSoundEngine.PostEvent(AK.EVENTS.STOP_WATERFALL_LOOP, entity);
			};
			model.Add(new CommandBinding(model.OnSuspended, stopSound));
			model.Add(new CommandBinding(model.Delete, stopSound));
			model.Add(new CommandBinding(model.OnResumed, delegate()
			{
				if (!main.EditorEnabled)
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WATERFALL_LOOP, entity);
			}));
			
			Property<Vector2> offset = model.GetVector2Parameter("Offset");
			Updater updater = new Updater
			{
				delegate(float dt)
				{
					offset.Value = new Vector2(0, main.TotalTime * -0.6f);
					Vector3 relativeCamera = Vector3.Transform(main.Camera.Position, Matrix.Invert(transform.Matrix));
					Vector3 bounds = model.Scale.Value * 0.5f;
					relativeCamera.X = MathHelper.Clamp(relativeCamera.X, -bounds.X, bounds.X);
					relativeCamera.Y = Math.Min(0, relativeCamera.Y);
					relativeCamera.Z = MathHelper.Clamp(relativeCamera.Z, -bounds.Z, bounds.Z);
					soundPosition.Value = Vector3.Transform(relativeCamera, transform.Matrix);
				}
			};
			updater.EnabledInEditMode = true;
			entity.Add(updater);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model editorModel = entity.Get<Model>("EditorModel");
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => Editor.EditorModelsVisible && !entity.EditorSelected, Editor.EditorModelsVisible, entity.EditorSelected));
			VoxelAttachable.AttachEditorComponents(entity, main, editorModel.Color);
		}
	}
}
