using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ComponentBind;

namespace Lemma.Util
{
	public class WwisePicker
	{
		private static ListProperty<KeyValuePair<uint, string>> list;
		public static ListProperty<KeyValuePair<uint, string>> Get(Main main)
		{
			if (main.EditorEnabled)
			{
				if (list == null)
				{
					list = new ListProperty<KeyValuePair<uint, string>>();
					list.Add(new KeyValuePair<uint, string>(0, "[null]"));
					FieldInfo[] members = typeof(AK.EVENTS).GetFields(BindingFlags.Static | BindingFlags.Public);
					for (int i = 0; i < members.Length; i++)
					{
						FieldInfo field = members[i];
						list.Add(new KeyValuePair<uint, string>((uint)field.GetValue(null), field.Name));
					}
				}
				return list;
			}
			else
				return null;
		}
	}
}
