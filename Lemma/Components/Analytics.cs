using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Specialized;
using System.Net;
using ComponentBind;
using System.Management;
using ICSharpCode.SharpZipLib.GZip;

namespace Lemma.Components
{
	public class Session
	{
		public class ContinuousProperty
		{
			public string Name;
			public bool Independent = true;

			private List<float> data = new List<float>();

			private float interval;

			public void Initialize(Session session)
			{
				this.interval = session.Interval;
			}

			public float this[int index]
			{
				get
				{
					return this.data.Count == 0 ? 0.0f : this.data[Math.Min(index, this.data.Count - 1)];
				}
			}

			public float this[float time]
			{
				get
				{
					int index = (int)Math.Floor(time / this.interval);
					float blend = (time - (index * this.interval)) / this.interval;
					return (this[index] * (1.0f - blend)) + (this[index + 1] * blend);
				}
			}

			public int Count
			{
				get
				{
					return this.data.Count;
				}
			}

			public void Record(float value)
			{
				data.Add(value);
			}

			public unsafe static string serialize(List<float> data)
			{
				byte[] result = new byte[data.Count * 4];
				for (int i = 0; i < data.Count; i++)
				{
					float value = data[i];
					int intValue = *((int*)&value);
					int j = i * 4;
					result[j] = (byte)(intValue >> 24);
					result[j + 1] = (byte)(intValue >> 16);
					result[j + 2] = (byte)(intValue >> 8);
					result[j + 3] = (byte)intValue;
				}
				return System.Convert.ToBase64String(result);
			}

			public unsafe static List<float> deserialize(string input)
			{
				byte[] temp = System.Convert.FromBase64String(input);
				List<float> result = new List<float>();
				for (int i = 0; i < temp.Length / 4; i++)
				{
					int j = i * 4;
					int intValue = (temp[j] << 24)
						| (temp[j + 1] << 16)
						| (temp[j + 2] << 8)
						| temp[j + 3];
					result.Add(*((float*)&intValue));
				}
				return result;
			}

			public string Data
			{
				get
				{
					return serialize(this.data);
				}
				set
				{
					this.data = deserialize(value);
				}
			}

			public void Clear()
			{
				this.data.Clear();
			}
		}

		public class PositionProperty
		{
			private ContinuousProperty[] coordinates;

			public string Name;

			private float interval;

			public void Initialize(Session session)
			{
				this.interval = session.Interval;
				if (!session.continuousProperties.ContainsKey(this.Name + "X"))
				{
					this.coordinates = new ContinuousProperty[3];
					this.coordinates[0] = session.continuousProperties[this.Name + "X"] = new ContinuousProperty
					{
						Name = this.Name + "X",
						Independent = false,
					};
					this.coordinates[1] = session.continuousProperties[this.Name + "Y"] = new ContinuousProperty
					{
						Name = this.Name + "Y",
						Independent = false,
					};
					this.coordinates[2] = session.continuousProperties[this.Name + "Z"] = new ContinuousProperty
					{
						Name = this.Name + "Z",
						Independent = false,
					};
				}
				else
				{
					this.coordinates = new[]
					{
						session.continuousProperties[this.Name + "X"],
						session.continuousProperties[this.Name + "Y"],
						session.continuousProperties[this.Name + "Z"],
					};
				}
			}

			public Vector3 this[float time]
			{
				get
				{
					int index = (int)Math.Floor(time / this.interval);
					float blend = (time - (index * this.interval)) / this.interval;
					return Vector3.Lerp(this[index], this[index + 1], blend);
				}
			}

			public Vector3 this[int index]
			{
				get
				{
					return new Vector3
					(
						this.coordinates[0][index],
						this.coordinates[1][index],
						this.coordinates[2][index]
					);
				}
			}

			public Vector3 GetLastRecordedPosition(float time)
			{
				int index = (int)Math.Floor(time / this.interval);
				return this[index];
			}

			public void Record(Vector3 value)
			{
				this.coordinates[0].Record(value.X);
				this.coordinates[1].Record(value.Y);
				this.coordinates[2].Record(value.Z);
			}
		}

		public class EventList
		{
			[XmlIgnore]
			public Session Session;
			public string Name;
			public List<Event> Events = new List<Event>();
		}

		public class Event
		{
			public float Time;
			public string Data;
		}

		public ContinuousProperty GetContinuousProperty(string name)
		{
			ContinuousProperty result = null;
			this.continuousProperties.TryGetValue(name, out result);
			return result;
		}

		public ContinuousProperty[] ContinuousProperties
		{
			get
			{
				return this.continuousProperties.Values.ToArray();
			}
			set
			{
				this.continuousProperties.Clear();
				foreach (ContinuousProperty prop in value)
					this.continuousProperties.Add(prop.Name, prop);
			}
		}

		public PositionProperty[] PositionProperties
		{
			get
			{
				return this.positionProperties.Values.ToArray();
			}
			set
			{
				this.positionProperties.Clear();
				foreach (PositionProperty prop in value)
					this.positionProperties.Add(prop.Name, prop);
			}
		}

		public EventList[] Events
		{
			get
			{
				return this.events.Values.ToArray();
			}
			set
			{
				this.events.Clear();
				foreach (EventList el in value)
					this.events.Add(el.Name, el);
			}
		}

		public float TotalTime;

		public string Map;

		public float Interval;

		public static Session Load(string path)
		{
			Session s;
			using (Stream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				Stream stream = null;
				try
				{
					stream = new GZipInputStream(fs);
					s = (Session)new XmlSerializer(typeof(Session)).Deserialize(stream);
				}
				catch (GZipException)
				{
					fs.Seek(0, SeekOrigin.Begin);
					s = (Session)new XmlSerializer(typeof(Session)).Deserialize(fs);
				}
				finally
				{
					if (stream != null)
						stream.Dispose();
				}
			}
			foreach (ContinuousProperty prop in s.continuousProperties.Values)
				prop.Initialize(s);
			foreach (PositionProperty prop in s.positionProperties.Values)
				prop.Initialize(s);
			foreach (EventList el in s.events.Values)
				el.Session = s;
			return s;
		}

		private Dictionary<string, ContinuousProperty> continuousProperties = new Dictionary<string, ContinuousProperty>();

		private Dictionary<string, PositionProperty> positionProperties = new Dictionary<string, PositionProperty>();

		private Dictionary<string, EventList> events = new Dictionary<string, EventList>();

		public DateTime Date;

		public int Build;

		public string UUID;

		public string OS;

		public string CPU;

		public string GPU;

		public bool Is64BitOS;

		public Point ScreenSize;

		public bool VR;

		public bool IsFullscreen;

		public int Memory;

		public string ID;

		public string LastSession;

		public class Recorder : Component<Main>, IUpdateableComponent
		{
			public static void Event(Main main, string name, string data = null)
			{
#if ANALYTICS
				main.SessionRecorder.RecordEvent(name, data);
#endif
			}

			public static void UploadSession(string file)
			{
				string url = "http://powerful-dusk-6047.herokuapp.com/" + Path.GetFileName(file);
				new WebClient().UploadData(url, "PUT", File.ReadAllBytes(file));
				File.Delete(file);
			}

			public const float Interval = 0.25f;

			private float intervalTime = 0.0f;

			private Session data = new Session();

			public Recorder()
			{
				this.data.Date = DateTime.Now;
				this.data.Interval = Interval;
				this.data.Build = Main.Build;
				this.data.OS = Environment.OSVersion.VersionString;
				this.data.Is64BitOS = Environment.Is64BitOperatingSystem;

#if WINDOWS
				this.data.Memory = (int)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (ulong)1048576);
				ManagementObject cpu = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
				this.data.CPU = string.Format("{0} {1}", cpu["Name"], cpu["Caption"]);
				ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_DisplayConfiguration");
				foreach (ManagementObject mo in searcher.Get())
				{
					foreach (PropertyData property in mo.Properties)
					{
						if (property.Name == "Description")
						{
							this.data.GPU = property.Value.ToString();
							break;
						}
					}
				}
#endif
				this.EnabledWhenPaused = true;
				this.EnabledInEditMode = true;
			}

			public void Save(string path, int build, string map, float totalTime)
			{
				this.data.Map = string.IsNullOrEmpty(map) ? null : Path.GetFileNameWithoutExtension(map);
				this.data.TotalTime = totalTime;
				this.data.Map = map;
				this.data.UUID = this.main.Settings.UUID;
				Point screenSize;
#if VR
				this.data.VR = this.main.VR;
				if (this.main.VR)
					screenSize = this.main.VRActualScreenSize;
				else
#endif
					screenSize = this.main.ScreenSize;

				this.data.ScreenSize = screenSize;
				this.data.IsFullscreen = this.main.Settings.Fullscreen;

				string filename = string.Format("{0}-{1}-{2}.xml.gz", build, this.data.Map == null ? "null" : this.data.Map, this.data.ID);
				using (Stream fs = new FileStream(Path.Combine(path, filename), FileMode.Create, FileAccess.Write, FileShare.None))
				using (Stream stream = new GZipOutputStream(fs))
					new XmlSerializer(typeof(Session)).Serialize(stream, this.data);
			}

			public void Reset()
			{
				foreach (ContinuousProperty prop in this.data.continuousProperties.Values)
					prop.Clear();
				this.data.LastSession = this.data.ID;
				this.data.ID = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32);
				this.data.events.Clear();
				this.data.Date = DateTime.Now;
			}

			private List<Action> recordActions = new List<Action>();

			public void Add(string name, Func<float> get)
			{
				ContinuousProperty prop;
				if (!this.data.continuousProperties.TryGetValue(name, out prop))
				{
					prop = this.data.continuousProperties[name] = new ContinuousProperty
					{
						Name = name,
					};
					prop.Initialize(this.data);
				}
				this.recordActions.Add(delegate()
				{
					prop.Record(get());
				});
			}

			public void Add(string name, Func<Vector3> get)
			{
				PositionProperty prop;
				if (!this.data.positionProperties.TryGetValue(name, out prop))
				{
					prop = this.data.positionProperties[name] = new PositionProperty
					{
						Name = name,
					};
					prop.Initialize(this.data);
				}
				this.recordActions.Add(delegate()
				{
					prop.Record(get());
				});
			}

			public void RecordEvent(string name, string data = null)
			{
				EventList eventList;
				if (!this.data.events.TryGetValue(name, out eventList))
				{
					eventList = this.data.events[name] = new EventList
					{
						Name = name,
						Session = this.data,
					};
				}
				eventList.Events.Add(new Session.Event
				{
					Time = main.TotalTime,
					Data = data,
				});
			}

			public void Update(float dt)
			{
				this.intervalTime += dt;
				while (this.intervalTime > Interval)
				{
					foreach (Action action in this.recordActions)
						action();
					this.intervalTime -= Interval;
				}
			}
		}
	}
}