using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class WorldFactory : Factory<Main>
	{
		private Random random = new Random();

		private static Entity instance;

		public WorldFactory()
		{
			this.Color = new Vector3(0.1f, 0.1f, 0.1f);
			this.EditorCanSpawn = false;
		}

		public static Entity Instance
		{
			get
			{
				if (WorldFactory.instance == null)
					return null;

				if (!WorldFactory.instance.Active)
					WorldFactory.instance = null;

				return WorldFactory.instance;
			}
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "World");
			entity.Add("Transform", new Transform());
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			entity.EditorCanDelete = false;

			World world = entity.GetOrCreate<World>("World");

			// Zone management
			entity.GetOrCreate<Propagator>("Propagator");

			this.SetMain(entity, main);
			WorldFactory.instance = entity;
			AkSoundEngine.DefaultGameObject = entity;

			entity.Add("OverlayTexture", world.OverlayTexture, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "Textures" }),
			});
			entity.Add("OverlayTiling", world.OverlayTiling);
			entity.Add("LightRampTexture", world.LightRampTexture, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "LightRamps" }),
			});
			entity.Add("EnvironmentMap", world.EnvironmentMap, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "EnvironmentMaps" }),
			});
			entity.Add("EnvironmentColor", world.EnvironmentColor);
			entity.Add("BackgroundColor", world.BackgroundColor);
			entity.Add("FarPlaneDistance", world.FarPlaneDistance);
			entity.Add("Gravity", world.Gravity);
			entity.Add("ThumbnailCamera", world.ThumbnailCamera);
		}
	}
}
