using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using GeeUI.Views;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Binder : Component<Main>
	{
		public Property<Setter.PropType> PropertyType = new Property<Setter.PropType>();

		public Property<Entity.Handle> InTarget = new Property<Entity.Handle>();
		public Property<Entity.Handle> OutTarget = new Property<Entity.Handle>();

		public Property<string> TargetInProperty = new Property<string>();
		public Property<string> TargetOutProperty = new Property<string>();

		public Property<bool> TwoWay = new Property<bool>();
		public Property<bool> SetOnBind = new Property<bool>();

		[XmlIgnore]
		public Command Bind = new Command();

		[XmlIgnore]
		public Command UnBind = new Command();

		[XmlIgnore]
		public Command Set = new Command();

		private IProperty targetInProperty;
		private IProperty targetOutProperty;

		private NotifyBinding _inChanged;
		private NotifyBinding _outChanged;

		public override void Awake()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Bind.Action = () =>
			{
				this.FindProperties();
				if (this.targetInProperty != null)
				{
					if (this._inChanged == null)
						_inChanged = new NotifyBinding(InChanged, this.Enabled, targetInProperty);
					this.Add(_inChanged);
					if (SetOnBind) InChanged();
				}

				if (this.targetOutProperty != null && this.TwoWay)
				{
					if (this._outChanged == null)
						_outChanged = new NotifyBinding(OutChanged, this.Enabled, targetOutProperty);
					this.Add(_outChanged);
				}
			};

			this.UnBind.Action = () =>
			{
				if (this._inChanged != null) this.Remove(_inChanged);
				if (this._outChanged != null) this.Remove(_outChanged);
			};

			this.Set.Action = () =>
			{
				this.FindProperties();
				if (this.targetInProperty != null)
				{
					InChanged();
				}
			};
			base.Awake();
		}

		private object GetPropertyValue(ref IProperty prop)
		{
			if (prop == null) return null;
			switch (this.PropertyType.Value)
			{
				case Setter.PropType.Bool:
					return ((Property<bool>)prop).Value;
					break;
				case Setter.PropType.Int:
					return ((Property<int>)prop).Value;
					break;
				case Setter.PropType.Float:
					return ((Property<float>)prop).Value;
					break;
				case Setter.PropType.Vector2:
					return ((Property<Vector2>)prop).Value;
					break;
				case Setter.PropType.Vector3:
					return ((Property<Vector3>)prop).Value;
					break;
				case Setter.PropType.Vector4:
					return ((Property<Vector4>)prop).Value;
					break;
				case Setter.PropType.Coord:
					return ((Property<Voxel.Coord>)prop).Value;
					break;
				case Setter.PropType.Direction:
					return ((Property<Direction>)prop).Value;
					break;
				case Setter.PropType.String:
					return ((Property<string>)prop).Value;
					break;
				default:
					return null;
			}
		}

		private void SetPropertyValue(ref IProperty prop, object value)
		{
			if (prop == null) return;
			switch (this.PropertyType.Value)
			{
				case Setter.PropType.Bool:
					((Property<bool>)prop).Value = (bool)value;
					break;
				case Setter.PropType.Int:
					((Property<int>)prop).Value = (int)value;
					break;
				case Setter.PropType.Float:
					((Property<float>)prop).Value = (float)value;
					break;
				case Setter.PropType.Vector2:
					((Property<Vector2>)prop).Value = (Vector2)value;
					break;
				case Setter.PropType.Vector3:
					((Property<Vector3>)prop).Value = (Vector3)value;
					break;
				case Setter.PropType.Vector4:
					((Property<Vector4>)prop).Value = (Vector4)value;
					break;
				case Setter.PropType.Coord:
					((Property<Voxel.Coord>)prop).Value = (Voxel.Coord)value;
					break;
				case Setter.PropType.Direction:
					((Property<Direction>)prop).Value = (Direction)value;
					break;
				case Setter.PropType.String:
					((Property<string>)prop).Value = (string)value;
					break;
			}
		}

		private void InChanged()
		{
			if (this.targetOutProperty != null)
			{
				object val = GetPropertyValue(ref targetInProperty);
				if (val != null)
					SetPropertyValue(ref targetOutProperty, val);
			}
		}

		private void OutChanged()
		{
			if (this.targetInProperty != null && this.TwoWay)
			{
				object val = GetPropertyValue(ref targetOutProperty);
				object inVal = GetPropertyValue(ref targetInProperty);

				//Avoid stack overflow
				if (val != null && !val.Equals(inVal))
					SetPropertyValue(ref targetInProperty, val);
			}
		}

		public override void Start()
		{
			this.FindProperties();
		}

		private void FindProperty(ref IProperty prop, Entity entity, string name)
		{
			if (entity != null && entity.Active)
			{
				switch (this.PropertyType.Value)
				{
					case Setter.PropType.Bool:
						prop = entity.GetProperty<bool>(name);
						break;
					case Setter.PropType.Int:
						prop = entity.GetProperty<int>(name);
						break;
					case Setter.PropType.Float:
						prop = entity.GetProperty<float>(name);
						break;
					case Setter.PropType.Vector2:
						prop = entity.GetProperty<Vector2>(name);
						break;
					case Setter.PropType.Vector3:
						prop = entity.GetProperty<Vector3>(name);
						break;
					case Setter.PropType.Vector4:
						prop = entity.GetProperty<Vector4>(name);
						break;
					case Setter.PropType.Coord:
						prop = entity.GetProperty<Voxel.Coord>(name);
						break;
					case Setter.PropType.Direction:
						prop = entity.GetProperty<Direction>(name);
						break;
					case Setter.PropType.String:
						prop = entity.GetProperty<string>(name);
						break;
				}
			}
		}

		public void FindProperties()
		{
			if (this.targetInProperty == null)
			{
				Entity entity = this.InTarget.Value.Target;
				FindProperty(ref targetInProperty, entity, TargetInProperty);
			}
			if (this.targetOutProperty == null)
			{
				Entity entity = this.OutTarget.Value.Target;
				FindProperty(ref targetOutProperty, entity, TargetOutProperty);
			}
		}
	}
}
