using System; using ComponentBind;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.IO.IsolatedStorage;
using Lemma.Components;
using System.Xml.Serialization;
using Lemma.Factories;

namespace Lemma.IO
{
	public class MapLoader
	{
		public const string MapDirectory = "Content\\Game";
		public const string MapExtension = "map";

		public static Type[] IncludedTypes = new[]
		{
			typeof(PlayerTrigger),
			typeof(Trigger),
			typeof(PlayerCylinderTrigger),
			typeof(Map),
			typeof(DynamicMap),
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
			typeof(ModelInstance.ModelInstanceSystem),
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
			typeof(PlayerFactory.RespawnLocation),
			typeof(ListProperty<PlayerFactory.RespawnLocation>),
			typeof(WorldFactory.ScheduledBlock),
			typeof(ListProperty<WorldFactory.ScheduledBlock>),
			typeof(VoxelChaseAI),
			typeof(FillMapFactory.CoordinateEntry),
			typeof(ListProperty<FillMapFactory.CoordinateEntry>),
			typeof(Phone),
			typeof(RaycastAI),
			typeof(SignalTower),
		};

		public static XmlSerializer Serializer;

		static MapLoader()
		{
			MapLoader.Serializer = new XmlSerializer(typeof(List<Entity>), MapLoader.IncludedTypes);
		}

		public static void Load(Main main, string directory, string filename, bool deleteEditor = true)
		{
			main.LoadingMap.Execute(filename);

			// HACK HACK HACK
			main.MapFile.InternalValue = filename;

			bool inAppPackage = false;

			if (directory == null)
			{
				inAppPackage = true;
				filename = Path.Combine(MapLoader.MapDirectory, filename);
			}
			else
				filename = Path.Combine(directory, filename);

			filename += "." + MapLoader.MapExtension;

			using (Stream stream = inAppPackage ? TitleContainer.OpenStream(filename) : File.OpenRead(filename))
				MapLoader.Load(main, stream, deleteEditor);
		}

		private static void Load(Main main, Stream stream, bool deleteEditor = true)
		{
			main.Camera.Position.Value = new Vector3(0, -10000, 0);
			main.IsLoadingMap = true;
			main.ClearEntities(deleteEditor);

			List<Entity> entities = null;
			try
			{
				entities = (List<Entity>)MapLoader.Serializer.Deserialize(stream);
			}
			catch (InvalidOperationException e)
			{
				throw new Exception("Failed to deserialize file stream.", e);
			}

			foreach (Entity entity in entities)
			{
				Factory<Main> factory = Factory<Main>.Get(entity.Type);
				factory.Bind(entity, main);
				main.Add(entity);
			}

			main.IsLoadingMap = false;
			main.MapLoaded.Execute();
		}

		public static void Save(Main main, string directory, string filename)
		{
			if (directory == null)
				filename = Path.Combine(MapLoader.MapDirectory, filename);
			else
				filename = Path.Combine(directory, filename);

			filename += "." + MapLoader.MapExtension;

			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					MapLoader.Save(main, ms);
					ms.Seek(0, SeekOrigin.Begin);
					using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
						ms.CopyTo(stream);
				}
			}
			catch (Exception e)
			{
				throw new Exception("Failed to save map.", e);
			}
		}

		public static void Save(Main main, Stream stream)
		{
			MapLoader.Serializer.Serialize(stream, main.Entities.Where(x => x.Serialize && x.Active).ToList());
		}

		public static void Reload(Main main, bool deleteEditor = true)
		{
			main.LoadingMap.Execute(main.MapFile);
			using (Stream stream = new MemoryStream())
			{
				MapLoader.Serializer.Serialize(stream, main.Entities.Where(x => x.Serialize).ToList());

				main.ClearEntities(deleteEditor);

				stream.Seek(0, SeekOrigin.Begin);

				List<Entity> entities = (List<Entity>)Serializer.Deserialize(stream);

				foreach (Entity entity in entities)
				{
					Factory<Main> factory = Factory<Main>.Get(entity.Type);
					factory.Bind(entity, main);
					main.Add(entity);
				}
			}
			main.MapLoaded.Execute();
		}
	}
}