using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Excel;
using Lemma.Components;

namespace Lemma
{
	public class Strings
	{
		private Dictionary<string, Dictionary<string, string>> data = new Dictionary<string,Dictionary<string, string>>();

		public Property<string> Language = new Property<string> { Value = "en" };
		
		public void Load(string filename)
		{
			using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
				reader.IsFirstRowAsColumnNames = true;

				reader.Read(); // Read first row

				// Initialize languages
				Dictionary<string, Dictionary<string, string>> languages = new Dictionary<string, Dictionary<string, string>>();
				List<Dictionary<string, string>> languageList = new List<Dictionary<string,string>>();
				for (int i = 1; i < reader.FieldCount; i++)
				{
					string language = reader.GetString(i);
					Dictionary<string, string> dict;
					if (!this.data.TryGetValue(language, out dict))
					{
						dict = new Dictionary<string, string>();
						this.data[language] = dict;
					}
					languageList.Add(dict);
				}

				while (reader.Read())
				{
					string key = reader.GetString(0);
					for (int i = 0; i < Math.Min(languageList.Count, reader.FieldCount); i++)
						languageList[i][reader.GetString(0)] = reader.GetString(i + 1);
				}
			}
		}

		public string Get(string key)
		{
			Dictionary<string, string> language;
			if (!this.data.TryGetValue(this.Language, out language))
				return null;

			string result;
			language.TryGetValue(key, out result);
			return result;
		}
	}
}
