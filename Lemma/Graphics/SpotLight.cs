using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class SpotLight : Component<Main>
	{
		public static readonly List<SpotLight> All = new List<SpotLight>();

		public Property<Vector3> Color = new Property<Vector3> { Value = Vector3.One };
		public Property<Quaternion> Orientation = new Property<Quaternion>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<bool> Shadowed = new Property<bool> { Value = true };
		public Property<float> FieldOfView = new Property<float> { Value = (float)Math.PI * 0.25f };
		public Property<float> Attenuation = new Property<float> { Value = 10.0f };

		[XmlIgnore]
		public Property<BoundingFrustum> BoundingFrustum = new Property<BoundingFrustum>();

		[XmlIgnore]
		public Property<Matrix> ViewProjection = new Property<Matrix>();

		[XmlIgnore]
		public Property<Matrix> View = new Property<Matrix>();

		[XmlIgnore]
		public Property<Matrix> Projection = new Property<Matrix>();

		public Property<string> CookieTextureFile = new Property<string> { Value = "Cookies\\default" };

		[XmlIgnore]
		public Property<Texture2D> CookieTexture = new Property<Texture2D>();

		public override void Awake()
		{
			base.Awake();
			this.Add(new SetBinding<float>(this.FieldOfView, delegate(float value)
			{
				if (value < 0.01f || value > (float)Math.PI - 0.01f)
					this.FieldOfView.Value = Math.Max(0.01f, Math.Min((float)Math.PI - 0.01f, value));
			}));

			this.Add(new Binding<Matrix>(this.View, delegate()
			{
				return Matrix.Invert(Matrix.CreateFromQuaternion(this.Orientation) * Matrix.CreateTranslation(this.Position));
			},
			this.Position, this.Orientation));

			this.Add(new Binding<Matrix>(this.Projection, delegate()
			{
				return Matrix.CreatePerspectiveFieldOfView(this.FieldOfView, 1.0f, 1.0f, Math.Max(2.0f, this.Attenuation));
			},
			this.Attenuation, this.FieldOfView));

			this.Add(new Binding<Matrix>(this.ViewProjection, delegate()
			{
				return this.View.Value * this.Projection;
			}, this.View, this.Projection));

			this.Add(new Binding<Texture2D, string>(this.CookieTexture, delegate(string value)
				{
					try
					{
						return this.main.Content.Load<Texture2D>(value);
					}
					catch
					{
						return null;
					}
				}, this.CookieTextureFile));

			this.Add(new Binding<BoundingFrustum, Matrix>(this.BoundingFrustum, x => new BoundingFrustum(x), this.ViewProjection));

			SpotLight.All.Add(this);
		}

		public override void delete()
		{
			base.delete();
			SpotLight.All.Remove(this);
		}

		public void LoadContent(bool reload)
		{
			if (reload)
			{
				try
				{
					this.CookieTexture.Value = this.main.Content.Load<Texture2D>(this.CookieTextureFile);
				}
				catch (Exception)
				{
					this.CookieTexture.Value = null;
				}
			}
		}
	}
}
