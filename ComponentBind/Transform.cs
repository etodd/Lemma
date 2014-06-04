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
		public EditorProperty<Vector3> Position = new EditorProperty<Vector3>();
		public EditorProperty<Quaternion> Quaternion = new EditorProperty<Quaternion>();

		[XmlIgnore]
		public Property<Matrix> Orientation = new Property<Matrix>();
		[XmlIgnore]
		public Property<Vector3> Forward = new Property<Vector3>();

		[XmlIgnore]
		public Property<Matrix> Matrix = new Property<Matrix>();

		public Transform()
		{
			this.Matrix.Value = Microsoft.Xna.Framework.Matrix.Identity;
			this.Editable = false;
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
				x => Microsoft.Xna.Framework.Quaternion.CreateFromRotationMatrix(x),
				this.Orientation,
				x => Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(x)));

			this.Add(new TwoWayBinding<Matrix, Matrix>(
				this.Orientation,
				delegate(Matrix value)
				{
					Vector3 scale, translation;
					Quaternion rotation;
					value.Decompose(out scale, out rotation, out translation);
					return Microsoft.Xna.Framework.Matrix.CreateFromQuaternion(rotation);
				},
				this.Matrix,
				delegate(Matrix value)
				{
					Matrix original = this.Matrix;
					Matrix result = Microsoft.Xna.Framework.Matrix.Identity;
					result.Forward = Vector3.Normalize(value.Forward) * original.Forward.Length();
					result.Up = Vector3.Normalize(value.Up) * original.Up.Length();
					result.Left = Vector3.Normalize(value.Left) * original.Left.Length();
					result.Translation = original.Translation;
					return result;
				}));

			this.Add(new TwoWayBinding<Matrix, Vector3>(
				this.Orientation,
				x => Microsoft.Xna.Framework.Matrix.CreateLookAt(Vector3.Zero, x, Vector3.Up),
				this.Forward,
				x => Vector3.TransformNormal(Vector3.Forward, x)));
		}
	}
}
