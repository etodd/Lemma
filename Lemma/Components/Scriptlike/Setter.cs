using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Setter : Component<Main>
	{
		public enum Type { Bool, Int, Float, Direction, String, Vector2, Vector3, Vector4, Coord }
		public Property<bool> Bool = new Property<bool>();
		public Property<int> Int = new Property<int>();
		public Property<float> Float = new Property<float>();
		public Property<Direction> Direction = new Property<Direction>();
		public Property<string> String = new Property<string>();
		public Property<Vector2> Vector2 = new Property<Vector2>();
		public Property<Vector3> Vector3 = new Property<Vector3>();
		public Property<Vector4> Vector4 = new Property<Vector4>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public Property<Type> PropertyType = new Property<Type>();

		public Property<Entity.Handle> Target = new Property<Entity.Handle>();

		public Property<string> TargetProperty = new Property<string>();

		[XmlIgnore]
		public Command Set = new Command();

		private IProperty targetProperty;

		public override void Awake()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Set.Action = () =>
			{
				if (this.targetProperty != null)
				{
					switch (this.PropertyType.Value)
					{
						case Type.Bool:
							((Property<bool>)this.targetProperty).Value = this.Bool;
							break;
						case Type.Int:
							((Property<int>)this.targetProperty).Value = this.Int;
							break;
						case Type.Float:
							((Property<float>)this.targetProperty).Value = this.Float;
							break;
						case Type.Vector2:
							((Property<Vector2>)this.targetProperty).Value = this.Vector2;
							break;
						case Type.Vector3:
							((Property<Vector3>)this.targetProperty).Value = this.Vector3;
							break;
						case Type.Vector4:
							((Property<Vector4>)this.targetProperty).Value = this.Vector4;
							break;
						case Type.Coord:
							((Property<Voxel.Coord>)this.targetProperty).Value = this.Coord;
							break;
						case Type.Direction:
							((Property<Direction>)this.targetProperty).Value = this.Direction;
							break;
						case Type.String:
							((Property<string>)this.targetProperty).Value = this.String;
							break;
					}
				}
			};
			base.Awake();
		}

		public override void Start()
		{
			
		}

		public void FindProperties(string component, string property)
		{
			if (this.targetProperty == null)
			{
				Entity entity = this.Target.Value.Target;
				if (entity != null && entity.Active)
				{
					switch (this.PropertyType.Value)
					{
						case Type.Bool:
							this.targetProperty = entity.GetProperty<bool>(this.TargetProperty);
							break;
						case Type.Int:
							this.targetProperty = entity.GetProperty<int>(this.TargetProperty);
							break;
						case Type.Float:
							this.targetProperty = entity.GetProperty<float>(this.TargetProperty);
							break;
						case Type.Vector2:
							this.targetProperty = entity.GetProperty<Vector2>(this.TargetProperty);
							break;
						case Type.Vector3:
							this.targetProperty = entity.GetProperty<Vector3>(this.TargetProperty);
							break;
						case Type.Vector4:
							this.targetProperty = entity.GetProperty<Vector4>(this.TargetProperty);
							break;
						case Type.Coord:
							this.targetProperty = entity.GetProperty<Voxel.Coord>(this.TargetProperty);
							break;
						case Type.Direction:
							this.targetProperty = entity.GetProperty<Direction>(this.TargetProperty);
							break;
						case Type.String:
							this.targetProperty = entity.GetProperty<string>(this.TargetProperty);
							break;
					}
				}
			}
		}
	}
}
