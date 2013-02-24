using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class MapExitFactory : Factory
	{
		public MapExitFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "MapExit");

			Transform position = new Transform();

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 10.0f;
			result.Add("PlayerTrigger", trigger);

			result.Add("Transform", position);

			result.Add("NextMap", new Property<string> { Editable = true });
			result.Add("SpawnPoint", new Property<string> { Editable = true });

			return result;
		}

		private static string[] persistentTypes = new[] { "Player", "PlayerData", };
		private static string[] attachedTypes = new[] { "Pistol", "Headlamp", };

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

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			Property<string> nextMap = result.GetProperty<string>("NextMap");
			Property<string> startSpawnPoint = result.GetProperty<string>("SpawnPoint");

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(List<Entity>));

				Container notification = new Container();
				notification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				notification.Opacity.Value = 0.5f;
				TextElement notificationText = new TextElement();
				notificationText.Name.Value = "Text";
				notificationText.FontFile.Value = "Font";
				notificationText.Text.Value = "Loading...";
				notification.Children.Add(notificationText);
				((GameMain)main).UI.Root.GetChildByName("Notifications").Children.Add(notification);

				Stream stream = new MemoryStream();
				main.AddComponent(new Animation
				(
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						// We are exiting the map; just save the state of the map without the player.
						// So save the player and pistol and then delete them.
						List<Entity> persistentEntities = main.Entities.Where((Func<Entity, bool>)MapExitFactory.isPersistent).ToList();

						serializer.Serialize(stream, persistentEntities);

						foreach (Entity e in persistentEntities)
							e.Delete.Execute();

						((GameMain)main).StartSpawnPoint = startSpawnPoint;
					}),
					new Animation.Execute(((GameMain)main).SaveCurrentMap),
					new Animation.Set<string>(main.MapFile, nextMap),
					new Animation.Execute(delegate()
					{
						notification.Visible.Value = false;
						stream.Seek(0, SeekOrigin.Begin);
						List<Entity> entities = (List<Entity>)serializer.Deserialize(stream);
						foreach (Entity entity in entities)
						{
							Factory factory = Factory.Get(entity.Type);
							factory.Bind(entity, main);
							main.Add(entity);
						}
						stream.Dispose();
					}),
					new Animation.Delay(1.1f),
					new Animation.Set<string>(notificationText.Text, "Saving..."),
					new Animation.Set<bool>(notification.Visible, true),
					new Animation.Delay(0.01f),
					new Animation.Execute(((GameMain)main).Save),
					new Animation.Set<string>(notificationText.Text, "Saved"),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(notification.Opacity, 0.0f, 1.0f),
						new Animation.FloatMoveTo(notificationText.Opacity, 0.0f, 1.0f)
					),
					new Animation.Execute(notification.Delete)
				));
			}));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}