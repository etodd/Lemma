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
		public enum PropType { Bool, Int, Float, Direction, String, Vector2, Vector3, Vector4, Coord }
		public Property<bool> Bool = new Property<bool>();
		public Property<int> Int = new Property<int>();
		public Property<float> Float = new Property<float>();
		public Property<Direction> Direction = new Property<Direction>();
		public Property<string> String = new Property<string>();
		public Property<Vector2> Vector2 = new Property<Vector2>();
		public Property<Vector3> Vector3 = new Property<Vector3>();
		public Property<Vector4> Vector4 = new Property<Vector4>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public Property<PropType> PropertyType = new Property<PropType>();

		public static Dictionary<PropType, Type> TypeMapping = new Dictionary<PropType,Type>
		{
			{ PropType.Bool, typeof(bool) },
			{ PropType.Int, typeof(int) },
			{ PropType.Float, typeof(float) },
			{ PropType.Direction, typeof(Direction) },
			{ PropType.String, typeof(string) },
			{ PropType.Vector2, typeof(Vector2) },
			{ PropType.Vector3, typeof(Vector3) },
			{ PropType.Vector4, typeof(Vector4) },
			{ PropType.Coord, typeof(Voxel.Coord) },
		};

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
				this.FindProperties();
				if (this.targetProperty != null)
				{
					switch (this.PropertyType.Value)
					{
						case PropType.Bool:
							((Property<bool>)this.targetProperty).Value = this.Bool;
							break;
						case PropType.Int:
							((Property<int>)this.targetProperty).Value = this.Int;
							break;
						case PropType.Float:
							((Property<float>)this.targetProperty).Value = this.Float;
							break;
						case PropType.Vector2:
							((Property<Vector2>)this.targetProperty).Value = this.Vector2;
							break;
						case PropType.Vector3:
							((Property<Vector3>)this.targetProperty).Value = this.Vector3;
							break;
						case PropType.Vector4:
							((Property<Vector4>)this.targetProperty).Value = this.Vector4;
							break;
						case PropType.Coord:
							((Property<Voxel.Coord>)this.targetProperty).Value = this.Coord;
							break;
						case PropType.Direction:
							((Property<Direction>)this.targetProperty).Value = this.Direction;
							break;
						case PropType.String:
							((Property<string>)this.targetProperty).Value = this.String;
							break;
					}
				}
			};
			base.Awake();
		}

		public override void Start()
		{
			this.FindProperties();
		}

		public void FindProperties()
		{
			if (this.targetProperty == null)
			{
				Entity entity = this.Target.Value.Target;
				if (entity != null && entity.Active)
				{
					switch (this.PropertyType.Value)
					{
						case PropType.Bool:
							this.targetProperty = entity.GetProperty<bool>(this.TargetProperty);
							break;
						case PropType.Int:
							this.targetProperty = entity.GetProperty<int>(this.TargetProperty);
							break;
						case PropType.Float:
							this.targetProperty = entity.GetProperty<float>(this.TargetProperty);
							break;
						case PropType.Vector2:
							this.targetProperty = entity.GetProperty<Vector2>(this.TargetProperty);
							break;
						case PropType.Vector3:
							this.targetProperty = entity.GetProperty<Vector3>(this.TargetProperty);
							break;
						case PropType.Vector4:
							this.targetProperty = entity.GetProperty<Vector4>(this.TargetProperty);
							break;
						case PropType.Coord:
							this.targetProperty = entity.GetProperty<Voxel.Coord>(this.TargetProperty);
							break;
						case PropType.Direction:
							this.targetProperty = entity.GetProperty<Direction>(this.TargetProperty);
							break;
						case PropType.String:
							this.targetProperty = entity.GetProperty<string>(this.TargetProperty);
							break;
					}
				}
			}
		}
	}
}
