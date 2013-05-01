using System;
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
		public const string MapDirectory = "Content\\Maps";
		public const string MapExtension = "map";

		public static void Load(Main main, string directory, string filename, bool deleteEditor = true)
		{
			main.LoadingMap.Execute(filename);

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
				entities = (List<Entity>)new XmlSerializer(typeof(List<Entity>)).Deserialize(stream);
			}
			catch (InvalidOperationException e)
			{
				throw new Exception("Failed to deserialize file stream.", e);
			}

			foreach (Entity entity in entities)
			{
				Factory factory = Factory.Get(entity.Type);
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
				using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
					MapLoader.Save(main, stream);
			}
			catch (Exception e)
			{
				throw new Exception("Failed to save map.", e);
			}
		}

		public static void Save(Main main, Stream stream)
		{
			new XmlSerializer(typeof(List<Entity>)).Serialize(stream, main.Entities.Where(x => x.Serialize && x.Active).ToList());
		}

		public static void Reload(Main main, bool deleteEditor = true)
		{
			main.LoadingMap.Execute(main.MapFile);
			using (Stream stream = new MemoryStream())
			{
				XmlSerializer serializer = new XmlSerializer(typeof(List<Entity>));
				serializer.Serialize(stream, main.Entities.Where(x => x.Serialize).ToList());

				main.ClearEntities(deleteEditor);

				stream.Seek(0, SeekOrigin.Begin);

				List<Entity> entities = (List<Entity>)serializer.Deserialize(stream);

				foreach (Entity entity in entities)
				{
					Factory factory = Factory.Get(entity.Type);
					factory.Bind(entity, main);
					main.Add(entity);
				}
			}
			main.MapLoaded.Execute();
		}
	}
}