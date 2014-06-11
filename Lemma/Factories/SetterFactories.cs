using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class SetterFactory<T> : Factory<Main>
	{
		public SetterFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Setter");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
			EntityConnectable.AttachEditorComponents(entity, entity.Get<Setter<T>>().ConnectedEntities);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Position");
			Setter<T> setter = entity.GetOrCreate<Setter<T>>("Setter");
			BindCommand(entity, setter.Set, "Set");

			base.Bind(entity, main, creating);

		}
	}

	public class BoolSetterFactory : SetterFactory<bool> { }
	public class IntSetterFactory : SetterFactory<int> { }
	public class FloatSetterFactory : SetterFactory<float> { }
	public class DirectionSetterFactory : SetterFactory<Direction> { }
	public class StringSetterFactory : SetterFactory<string> { }
	public class Vector2SetterFactory : SetterFactory<Vector2> { }
	public class Vector3SetterFactory : SetterFactory<Vector3> { }
	public class Vector4SetterFactory : SetterFactory<Vector4> { }
	public class CoordSetterFactory : SetterFactory<Voxel.Coord> { }
}
