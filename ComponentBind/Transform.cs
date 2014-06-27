using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace ComponentBind
{
	public class Transform : Component<BaseMain>
	{
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Quaternion> Quaternion = new Property<Quaternion>();

		[XmlIgnore]
		public Property<Matrix> Matrix = new Property<Matrix> { Value = Microsoft.Xna.Framework.Matrix.Identity };

		[XmlIgnore]
		public Property<bool> Selectable = new Property<bool> { Value = true };

		public void EditorProperties()
		{
			this.Entity.Add("Position", this.Position);
			this.Entity.Add("Quaternion", this.Quaternion);
		}

		public override void Awake()
		{
			base.Awake();
			this.Add(new TwoWayBinding<Vector3, Matrix>(
				this.Position,
				delegate(Matrix value)
				{
					return value.Translation;
				},
				this.Matrix,
				delegate(Vector3 value)
				{
					Matrix matrix = this.Matrix.InternalValue;
					matrix.Translation = value;
					return matrix;
				}));

			this.Add(new TwoWayBinding<Quaternion, Matrix>(
				this.Quaternion,
				delegate(Matrix value)
				{
					Vector3 scale, translation;
					Quaternion rotation;
					value.Decompose(out scale, out rotation, out translation);
					return rotation;
				},
				this.Matrix,
				delegate(Quaternion value)
				{
					Matrix original = this.Matrix;
					Matrix result = Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(value);
					result.Translation = original.Translation;
					return result;
				}));
		}
	}
}
