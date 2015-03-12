using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Reflection;

namespace Lemma.Components
{
	public class ModelInstance : Component<Main>
	{
		public class ModelInstanceSystem : Model
		{
			public string Key;
			protected ListProperty<ModelInstance> instances = new ListProperty<ModelInstance>();
			protected IListBinding<ModelInstance> instanceBinding;

			public override void Awake()
			{
				base.Awake();
				this.Serialize = false;
				this.CullBoundingBox.Value = false;
				this.IsInstanced.Value = true;
				this.instanceBinding = new ListBinding<Model.Instance, ModelInstance>(this.Instances, this.instances, x => new Model.Instance { Param = x.Param, Transform = x.Transform });
				this.Add(this.instanceBinding);
			}

			public void Add(ModelInstance instance)
			{
				this.instances.Add(instance);
			}

			public void Remove(ModelInstance instance)
			{
				this.instances.Remove(instance);
			}

			protected override void drawInstances(RenderParameters parameters, Matrix transform)
			{
				if (parameters.IsMainRender)
				{
					for (int i = 0; i < this.instances.Length; i++)
						this.instances.Changed(i, this.instances[i]);
				}
				base.drawInstances(parameters, transform);
			}

			public override void delete()
			{
				base.delete();
				this.instanceBinding = null;
			}
		}

		public class ModelInstanceSystemAlpha : ModelInstanceSystem, IDrawableAlphaComponent
		{
			public Property<float> Alpha = null;
			public Property<int> DrawOrder { get; set; }

			public ModelInstanceSystemAlpha()
			{
				this.Alpha = this.GetFloatParameter("Alpha");
				this.Alpha.Value = 1.0f;
				this.DrawOrder = new Property<int>();
			}

			public override void Awake()
			{
				base.Awake();
				this.Add(new NotifyBinding(this.main.AlphaDrawablesModified, this.DrawOrder));
			}

			public override void Draw(GameTime time, RenderParameters parameters)
			{

			}

			void IDrawableAlphaComponent.DrawAlpha(GameTime time, RenderParameters parameters)
			{
				if (this.Alpha > 0.0f)
					base.Draw(time, parameters);
			}

			protected override bool setParameters(Matrix transform, RenderParameters parameters)
			{
				bool result = base.setParameters(transform, parameters);
				if (result)
					this.effect.Parameters["DepthBuffer"].SetValue(parameters.DepthBuffer);
				return result;
			}
		}

		protected ModelInstanceSystem model;

		public Property<string> Filename = new Property<string>();
		
		public Property<Matrix> Transform = new Property<Matrix> { Value = Matrix.Identity };

		public Property<bool> EnableAlpha = new Property<bool> { Value = false };

		public Property<int> InstanceKey = new Property<int>();

		public Property<Vector3> Param = new Property<Vector3>();

		[XmlIgnore]
		public Property<string> FullInstanceKey = new Property<string>();

		[XmlIgnore]
		public bool IsFirstInstance;

		[XmlIgnore]
		public Model Model
		{
			get
			{
				return this.model;
			}
		}

		protected void refreshModel()
		{
			if (!string.IsNullOrEmpty(this.Filename))
			{
				string key = string.Format("_{0}:{1}+{2}", (this.EnableAlpha ? "Alpha" : ""), this.Filename.Value, this.InstanceKey.Value);

				Entity world = Lemma.Factories.WorldFactory.Instance;
				
				ModelInstanceSystem newModel = this.EnableAlpha ? world.Get<ModelInstanceSystemAlpha>(key) : world.Get<ModelInstanceSystem>(key);

				bool foundExistingModel = newModel != null;

				if (!foundExistingModel)
				{
					newModel = this.EnableAlpha ? new ModelInstanceSystemAlpha() : new ModelInstanceSystem();
					newModel.Filename.Value = this.Filename;
					newModel.Key = key;
					world.Add(key, newModel);
				}

				if (newModel != this.model)
				{
					this.IsFirstInstance = !foundExistingModel;
					newModel.Add(this);
					if (this.model != null)
						this.model.Remove(this);
					this.model = newModel;
				}

				this.FullInstanceKey.Value = key;
			}
		}

		public void Setup(string filename, int instanceKey, bool alpha = false)
		{
			bool refresh = this.Filename.Value != filename;
			refresh |= this.InstanceKey.Value != instanceKey;
			if (refresh)
			{
				this.Filename.SetStealthy(filename);
				this.InstanceKey.SetStealthy(instanceKey);
				this.EnableAlpha.SetStealthy(alpha);
				this.refreshModel();
				this.Filename.Changed();
				this.InstanceKey.Changed();
				this.EnableAlpha.Changed();
			}
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new SetBinding<string>(this.Filename, delegate(string value)
			{
				this.refreshModel();
			}));
			this.Add(new SetBinding<int>(this.InstanceKey, delegate(int value)
			{
				this.refreshModel();
			}));
			this.Add(new SetBinding<bool>(this.EnableAlpha, delegate(bool value)
			{
				this.refreshModel();
			}));
		}
		public override void delete()
		{
			base.delete();
			if (this.model != null)
			{
				this.model.Remove(this);
				this.model = null;
			}
		}
	}
}