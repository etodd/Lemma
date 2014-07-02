
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

		private FileInfo MapPath;
		private FileInfo MetaPath;

		public static MapManifest FromMapPath(string mapPath)
		{
			FileInfo mapFile = new FileInfo(mapPath);
			if (!mapFile.Exists) return null; //maaaybe throw
			string mapNameOnly = mapPath.EndsWith(".map", true, null) ? (mapPath.Substring(0, mapPath.Length - 4)) : mapPath;
			string metaPath = mapNameOnly;
			if (!metaPath.EndsWith(".meta")) metaPath += ".meta";

			if (!File.Exists(metaPath))
			{
				return CreateManifest(mapPath);
			}

			TextReader tr = new StreamReader(metaPath);
			string content = tr.ReadToEnd();
			tr.Close();

			MapManifest ret = JsonConvert.DeserializeObject<MapManifest>(content);
			ret.MapPath = mapFile;
			ret.MetaPath = new FileInfo(metaPath);
			return ret;
		}

		public static MapManifest CreateManifest(string mapPath, MapManifest values = null)
		{
			FileInfo mapFile = new FileInfo(mapPath);
			if (!mapFile.Exists) return null; //maaaybe throw
			string mapNameOnly = mapPath.EndsWith(".map", true, null) ? (mapPath.Substring(0, mapPath.Length - 4)) : mapPath;
			string metaPath = mapNameOnly;
			if (!metaPath.EndsWith(".meta")) metaPath += ".meta";
			if (File.Exists(metaPath)) return null;

			if (values == null)
			{
				values = new MapManifest { MapName = mapFile.Name.Replace(".map", ""), MapPath = mapFile, MetaPath = new FileInfo(metaPath)};
			}
			values.Save();
			return values;
		}

		public void Save()
		{
			if (!File.Exists(MetaPath.FullName))
			{
				//TODO: decide if this is an acceptable case.
			}

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
