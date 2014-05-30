using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;

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

		private static string[] persistentTypes = new[] { "PlayerData", };
		private static string[] attachedTypes = new string[] { };

		private static bool isPersistent(Entity entity)
		{
			if (MapExitFactory.persistentTypes.Contains(entity.Type))
				return true;

			if (MapExitFactory.attachedTypes.Contains(entity.Type))
			{
				Property<bool> attached = entity.GetProperty<bool>("Attached");
				if (attached != null && attached)
					return true;
			}
			return false;
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
				Container notification = new Container();
				TextElement notificationText = new TextElement();

				Stream stream = new MemoryStream();

				Animation anim = new Animation
				(
					main.Spawner.FlashAnimation(),
					new Animation.Execute(delegate()
					{
						notification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
						notification.Opacity.Value = 0.5f;
						notificationText.Name.Value = "Text";
						notificationText.FontFile.Value = "Font";
						notificationText.Text.Value = "Loading...";
						notification.Children.Add(notificationText);
						main.UI.Root.GetChildByName("Notifications").Children.Add(notification);
					}),
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						// We are exiting the map; just save the state of the map without the player.
						ListProperty<RespawnLocation> respawnLocations = PlayerDataFactory.Instance.Get<PlayerData>().RespawnLocations;
						respawnLocations.Clear();

						List<Entity> persistentEntities = main.Entities.Where((Func<Entity, bool>)MapExitFactory.isPersistent).ToList();

						IO.MapLoader.Serializer.Serialize(stream, persistentEntities);

						foreach (Entity e in persistentEntities)
							e.Delete.Execute();

						main.Spawner.StartSpawnPoint.Value = startSpawnPoint;

						if (PlayerFactory.Instance != null)
							PlayerFactory.Instance.Delete.Execute();

						main.SaveCurrentMap();
						main.MapFile.Value = nextMap;

						notification.Visible.Value = false;
						stream.Seek(0, SeekOrigin.Begin);
						List<Entity> entities = (List<Entity>)IO.MapLoader.Serializer.Deserialize(stream);
						foreach (Entity e in entities)
						{
							Factory<Main> factory = Factory<Main>.Get(e.Type);
							factory.Bind(e, main);
							main.Add(e);
						}
						stream.Dispose();
					}),
					new Animation.Delay(1.5f),
					new Animation.Execute(delegate()
					{
						main.Screenshot.Take(main.ScreenSize);
					}),
					new Animation.Delay(0.01f),
					new Animation.Set<string>(notificationText.Text, "Saving..."),
					new Animation.Set<bool>(notification.Visible, true),
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						main.SaveOverwrite();
					}),
					new Animation.Set<string>(notificationText.Text, "Saved"),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(notification.Opacity, 0.0f, 1.0f),
						new Animation.FloatMoveTo(notificationText.Opacity, 0.0f, 1.0f)
					),
					new Animation.Execute(notification.Delete)
				);
				anim.EnabledWhenPaused = false;
				main.AddComponent(anim);
			}));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);
		}
	}
}