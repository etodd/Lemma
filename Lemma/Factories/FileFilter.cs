using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ComponentBind;

namespace Lemma.Factories
{
	public class FileFilter
	{
		private struct Entry
		{
			public string Root;
			public string[] Directories;
			public string Extension;
		}

		private static Dictionary<Entry, ListProperty<string>> cache = new Dictionary<Entry, ListProperty<string>>();

		private static bool setupBinding = false;

		public static ListProperty<string> Get(Main main, string root, string[] directories, string extension = null)
		{
			if (!setupBinding)
			{
				new CommandBinding(main.MapLoaded, (Action)cache.Clear);
				setupBinding = true;
			}

			if (main.EditorEnabled)
			{
				Entry entry = new Entry { Root = root, Directories = directories, Extension = extension };
				ListProperty<string> result;
				if (!cache.TryGetValue(entry, out result))
				{
					result = cache[entry] = new ListProperty<string>();
					result.Add(null);
					int contentRootDirectoryIndex = root.Length + 1;
					if (directories == null)
						directories = new[] { "" };
					foreach (string dir in directories)
					{
						string fullDir = Path.Combine(root ?? "", dir);
						if (Directory.Exists(fullDir))
						{
							foreach (string f in Directory.GetFiles(fullDir))
							{
								int extensionIndex = f.LastIndexOf('.');
								string ext = f.Substring(extensionIndex);
								if (extension == null || ext == extension)
								{
									// Remove content root directory and extension
									string stripped = f.Substring(contentRootDirectoryIndex, extensionIndex - contentRootDirectoryIndex);
									if (ext != ".xnb" || !stripped.EndsWith("_0")) // Exclude subordinate xnb files
										result.Add(stripped);
								}
							}
						}
					}
				}
				return result;
			}
			else
				return null;
		}
	}
}
