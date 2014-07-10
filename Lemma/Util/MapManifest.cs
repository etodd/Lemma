
using System.IO;
using Newtonsoft.Json;

namespace Lemma.Util
{
	public class MapManifest
	{
		public float BestPersonalTimeTrialTime { get; set; }
		public float LastPersonalTimeTrialTime { get; set; }
		public int TimesPlayed { get; set; }
		public string MapName { get; set; }
		public string MapPchName { get; set; }

		private FileInfo MapPath;
		private FileInfo MetaPath;

		public static MapManifest FromMapPath(Main main, string mapPath)
		{
			if (!mapPath.EndsWith(IO.MapLoader.MapExtension))
				mapPath += IO.MapLoader.MapExtension;
			if (Path.IsPathRooted(mapPath))
				return FromAbsolutePath(mapPath);
			else
				return FromAbsolutePath(Path.GetFullPath(Path.Combine(main.Content.RootDirectory, IO.MapLoader.MapDirectory, mapPath)));
		}

		public static MapManifest FromAbsolutePath(string mapPath)
		{
			FileInfo mapFile = new FileInfo(mapPath);
			string metaPath = Path.ChangeExtension(mapPath, ".meta");

			if (!File.Exists(metaPath))
			{
				return createManifest(mapPath);
			}

			TextReader tr = new StreamReader(metaPath);
			string content = tr.ReadToEnd();
			tr.Close();

			MapManifest ret = JsonConvert.DeserializeObject<MapManifest>(content);
			ret.MapPath = mapFile;
			ret.MetaPath = new FileInfo(metaPath);
			return ret;
		}

		private static MapManifest createManifest(string mapPath, MapManifest values = null)
		{
			FileInfo mapFile = new FileInfo(mapPath);
			string metaPath = Path.ChangeExtension(mapPath, ".meta");

			if (values == null)
			{
				values = new MapManifest { MapName = Path.GetFileNameWithoutExtension(mapFile.Name), MapPath = mapFile, MetaPath = new FileInfo(metaPath)};
			}
			values.Save();
			return values;
		}

		public void Save()
		{
			if (!File.Exists(MapPath.FullName))
			{
				//This is DEFINITELY not acceptable.
				throw new FileNotFoundException("MapManifest cannot save if map does not exist: " + MapPath.FullName);
			}

			TextWriter tw = new StreamWriter(MetaPath.FullName, false);
			tw.Write(JsonConvert.SerializeObject(this));
			tw.Close();
		}
	}
}
