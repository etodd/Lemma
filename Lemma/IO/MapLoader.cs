using System;
using System.Linq.Expressions;
using BEPUphysics.UpdateableSystems.ForceFields;
using ComponentBind;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.IO.IsolatedStorage;
using Lemma.Components;
using System.Xml.Serialization;
using Lemma.Factories;
using Lemma.Util;
using ICSharpCode.SharpZipLib.GZip;

namespace Lemma.IO
{
	public class MapLoader
	{
		public const string MapDirectory = "Game";
		public const string MapExtension = ".map";

		private static string[] persistentTypes = new[] { "PlayerData", };

		private static bool entityIsPersistent(Entity entity)
		{
			return MapLoader.persistentTypes.Contains(entity.Type);
		}

		public static Type[] IncludedTypes = new[]
		{
			typeof(PlayerTrigger),
			typeof(Trigger),
			typeof(PlayerCylinderTrigger),
			typeof(Voxel),
			typeof(DynamicVoxel),
			typeof(PlayerSpawn),
			typeof(Timer),
			typeof(Model),
			typeof(AnimatedModel),
			typeof(Water),
			typeof(TextElement),
			typeof(AmbientLight),
			typeof(DirectionalLight),
			typeof(PointLight),
			typeof(SpotLight),
			typeof(Script),
			typeof(ModelInstance),
			typeof(Zone),
			typeof(PhysicsBlock),
			typeof(Player),
			typeof(ParticleEmitter),
			typeof(PhysicsSphere),
			typeof(ModelAlpha),
			typeof(EnemyBase),
			typeof(Agent),
			typeof(AI),
			typeof(PID),
			typeof(PID3),
			typeof(RespawnLocation),
			typeof(ListProperty<RespawnLocation>),
			typeof(Property<RespawnLocation>),
			typeof(VoxelChaseAI),
			typeof(Phone),
			typeof(RaycastAI),
			typeof(SignalTower),
			typeof(Property<int>),
			typeof(Property<int>),
			typeof(Property<float>),
			typeof(Property<Direction>),
			typeof(Property<UInt32>),
			typeof(Propagator),
			typeof(RotationController),
			typeof(PlayerData),
			typeof(Rift),
			typeof(Collectible),
			typeof(CameraStop),
			typeof(AmbientSound),
			typeof(Sound),
			typeof(Counter),
			typeof(Ticker),
			typeof(RandomTicker),
			typeof(Setter),
			typeof(AnimatedSetter),
			typeof(World),
			typeof(Note),
			typeof(VoxelAttachable),
			typeof(Cloud),
			typeof(DialogueFile),
			typeof(EffectBlock),
			typeof(BlockCloud),
			typeof(SceneryBlock),
			typeof(FallingTower),
			typeof(Joint),
			typeof(Slider),
			typeof(Spinner),
			typeof(Levitator),
			typeof(RaycastAIMovement),
			typeof(Exploder),
			typeof(MapExit),
			typeof(ImplodeBlock),
			typeof(TimeTrial),
			typeof(Rain),
			typeof(Rotator),
			typeof(Skybox),
			typeof(Smoke),
			typeof(Snake),
			typeof(Switch),
			typeof(Turret),
			typeof(VoxelFill),
			typeof(Data),
			typeof(Starter),
			typeof(Explosion),
			typeof(Lemma.Components.Binder),
			typeof(Constant),
			typeof(MessageDisplayer),
			typeof(ParticleWind),
			typeof(VoxelSetter),
			typeof(ConsoleCommand),
			typeof(SliderCommon),
			typeof(StaticSlider),
			typeof(AnimatedProp),
			typeof(PowerBlockSocket),
			typeof(Sequence),
			typeof(Updater),
			typeof(LogicGate),
			typeof(Bouncer),
			typeof(VoxelTrigger),
			typeof(Block),
			typeof(SoundBank),
		};

		public static XmlSerializer Serializer;

		static MapLoader()
		{
			MapLoader.Serializer = new XmlSerializer(typeof(List<Entity>), MapLoader.IncludedTypes);
		}

		public static void LoadKeepEditor(Main main, string filename)
		{
			MapLoader.load(main, filename, false, null);
		}

		public static void Load(Main main, string filename)
		{
			MapLoader.load(main, filename, true, null);
		}

		public static void Load(Main main, string filename, Entity playerData)
		{
			MapLoader.load(main, filename, true, playerData);
		}

		private static void load(Main main, string filename, bool deleteEditor, Entity playerData)
		{
			if (filename == null)
				MapLoader.load(main, (Stream)null, deleteEditor, playerData);
			else
			{
				// Don't try to load the menu from a save game
				string directory;
				if (main.CurrentSave.Value == null || filename == Main.MenuMap)
					directory = main.MapDirectory;
				else
					directory = Path.Combine(main.SaveDirectory, main.CurrentSave);

				string filenameWithExtension = filename;
				if (!filenameWithExtension.EndsWith(MapLoader.MapExtension))
					filenameWithExtension += MapLoader.MapExtension;

				string fullFilename = Path.IsPathRooted(filenameWithExtension) ? filenameWithExtension : Path.Combine(directory, filenameWithExtension);

				if (main.CurrentSave.Value != null && !File.Exists(fullFilename))
					File.Copy(Path.Combine(main.MapDirectory, filenameWithExtension), fullFilename);

				main.LoadingMap.Execute(filename);

				main.MapFile.Value = filename;

				using (Stream fs = File.OpenRead(fullFilename))
				{
					using (Stream stream = new GZipInputStream(fs))
						MapLoader.load(main, stream, deleteEditor, playerData);
				}
			}
		}

		public static void New(Main main, string filename, string templateMap)
		{
			main.LoadingMap.Execute(filename);

			main.MapFile.Value = filename;

			if (!filename.EndsWith(MapLoader.MapExtension))
				filename += MapLoader.MapExtension;

			main.ClearEntities(false);

			if (!templateMap.EndsWith(MapLoader.MapExtension))
				templateMap += MapLoader.MapExtension;

			using (Stream fs = File.OpenRead(templateMap))
			{
				using (Stream stream = new GZipInputStream(fs))
					MapLoader.load(main, stream, false, null);
			}
			WorldFactory.Instance.Get<World>().NewUUID();
		}

		private static void load(Main main, Stream stream, bool deleteEditor, Entity playerData)
		{
			main.Camera.Position.Value = new Vector3(0, -1000, 0);
			main.IsLoadingMap = true;
			main.ClearEntities(deleteEditor);

			if (stream == null)
				main.DefaultLighting(); // There's no World entity to set the correct lighting, so set the defaults
			else
			{
				List<Entity> entities = null;
				try
				{
					entities = (List<Entity>)MapLoader.Serializer.Deserialize(stream);
				}
				catch (InvalidOperationException e)
				{
					throw new Exception("Failed to deserialize file stream.", e);
				}

				if (playerData != null)
					entities.Add(playerData);

				foreach (Entity entity in entities)
				{
					Factory<Main> factory = Factory<Main>.Get(entity.Type);
					if (factory != null)
					{
						factory.Bind(entity, main);
						main.Add(entity);
					}
				}
			}

			main.IsLoadingMap = false;
			main.MapLoaded.Execute();
		}

		public static void Save(Main main, string directory, string filename)
		{
			if (directory == null)
				filename = Path.Combine(main.MapDirectory, filename);
			else
				filename = Path.Combine(directory, Path.GetFileName(filename));

			if (!filename.EndsWith(MapLoader.MapExtension))
				filename += MapLoader.MapExtension;

			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					MapLoader.Save(main, ms);
					ms.Seek(0, SeekOrigin.Begin);
					using (Stream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
					using (Stream stream = new GZipOutputStream(fs))
						ms.CopyTo(stream);
				}
			}
			catch (Exception e)
			{
				throw new Exception("Failed to save map.", e);
			}
			GC.Collect();
		}

		public static void Save(Main main, Stream stream)
		{
			List<Entity> entities = main.Entities.Where(x => x.Serialize && x.Active).ToList();
			MapLoader.Serializer.Serialize(stream, entities);
		}

		public static void LoadWithEntities(Main main, string nextMap, List<Entity> persistentEntities)
		{
			Stream stream = new MemoryStream();
			IO.MapLoader.Serializer.Serialize(stream, persistentEntities);

			MapLoader.Load(main, nextMap);

			stream.Seek(0, SeekOrigin.Begin);
			List<Entity> entities = (List<Entity>)IO.MapLoader.Serializer.Deserialize(stream);
			foreach (Entity e in entities)
			{
				Factory<Main> factory = Factory<Main>.Get(e.Type);
				e.GUID = 0;
				factory.Bind(e, main);
				main.Add(e);
			}
			stream.Dispose();
		}

		public static void Transition(Main main, string nextMap, string spawn = null)
		{
			Container loadingNotification = new Container();
			loadingNotification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			loadingNotification.Opacity.Value = UIFactory.Opacity;
			TextElement loadingNotificationText = new TextElement();
			loadingNotificationText.Name.Value = "Text";
			loadingNotificationText.FontFile.Value = main.Font;
			loadingNotificationText.Text.Value = "\\loading";
			loadingNotification.Children.Add(loadingNotificationText);

			Animation anim = new Animation
			(
				new Animation.Set<bool>(main.Menu.CanPause, false),
				main.Spawner.FlashAnimation(),
				new Animation.Execute(delegate()
				{
					main.UI.Root.GetChildByName("Notifications").Children.Add(loadingNotification);
				}),
				new Animation.Delay(0.01f),
				new Animation.Execute(delegate()
				{
#if DEMO
					if (nextMap == "forest")
					{
						main.Spawner.StartSpawnPoint.Value = "demo";
						MapLoader.Load(main, Main.MenuMap);
					}
					else
#endif
					{
						// We are exiting the map; just save the state of the map without the player.
						ListProperty<RespawnLocation> respawnLocations = PlayerDataFactory.Instance.Get<PlayerData>().RespawnLocations;
						respawnLocations.Clear();

						List<Entity> persistentEntities = main.Entities.Where((Func<Entity, bool>)MapLoader.entityIsPersistent).ToList();

						Stream stream = new MemoryStream();
						IO.MapLoader.Serializer.Serialize(stream, persistentEntities);

						foreach (Entity e in persistentEntities)
							e.Delete.Execute();

						main.Spawner.StartSpawnPoint.Value = spawn;

						if (PlayerFactory.Instance != null)
							PlayerFactory.Instance.Delete.Execute();

						main.SaveCurrentMap(null, default(Point));
						MapLoader.Load(main, nextMap);

						stream.Seek(0, SeekOrigin.Begin);
						List<Entity> entities = (List<Entity>)IO.MapLoader.Serializer.Deserialize(stream);
						foreach (Entity e in entities)
						{
							Factory<Main> factory = Factory<Main>.Get(e.Type);
							e.GUID = 0;
							factory.Bind(e, main);
							main.Add(e);
						}
						stream.Dispose();
					}
				}),
				new Animation.Delay(0.01f),
				new Animation.Execute(loadingNotification.Delete),
				new Animation.Set<bool>(main.Menu.CanPause, true),
				new Animation.Execute(main.ScheduleSave)
			);
			anim.EnabledWhenPaused = false;
			main.AddComponent(anim);
		}
	}
}