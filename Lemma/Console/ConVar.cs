using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ComponentBind;
using Lemma.Components;

namespace Lemma.Console
{
	public class ConVar
	{
		public Property<string> Name = new Property<string>();
		public Property<string> Description = new Property<string>();
		public Property<string> Value = new Property<string>();
		public Property<Action<string>> OnChanged = new Property<Action<string>>();
		public Func<object, bool> Validate = o => true;

		public Type TypeConstraint;

		public Type OutCastConstraint
		{
			get
			{
				if (TypeConstraint == null) return typeof(string);
				return TypeConstraint;
			}
		}

		public ConVar(string name, string description, Action<string> onChanged, string defaultValue)
		{
			this.Name.Value = name;
			this.Description.Value = description;
			this.OnChanged.Value = onChanged;
			this.Value.Value = defaultValue;
		}

		public ConVar(string name, string description, string defaultValue)
			: this(name, description, s => { }, defaultValue)
		{
		}

		public ConVar(string name, string description, Action<string> onChanged)
			: this(name, description, onChanged, "")
		{

		}

		public ConVar(string name, string description)
			: this(name, description, s => { }, "")
		{

		}

		public object GetCastedValue(string input = null)
		{
			string toConvert = input ?? Value.Value;
			Type T = OutCastConstraint;
			var typeConverter = TypeDescriptor.GetConverter(T);
			if (typeConverter.CanConvertFrom(typeof(string)) && typeConverter.IsValid(toConvert))
			{
				return typeConverter.ConvertFromString(toConvert);
			}
			return default(object);
		}

		public bool IsGoodValue(string input)
		{
			Type T = TypeConstraint;
			if (T == null) return true;
			var typeConverter = TypeDescriptor.GetConverter(T);
			return typeConverter.CanConvertFrom(typeof(string)) && IsGood(T, input) && Validate(GetCastedValue(input));
		}

		public bool IsGood(Type T, string input)
		{
			if (T == typeof(bool))
			{
				string[] good = new string[] { "true", "false"};
				return good.Contains(input.ToLower());
			}
			return true;
		}

	}
}
