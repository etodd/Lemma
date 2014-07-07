using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using Lemma.Util;
using Microsoft.Xna.Framework.Content;
using Lemma.Factories;
using Lemma.IO;
using System.IO;

namespace Lemma.Components
{
	[XmlInclude(typeof(Material))]
	[XmlInclude(typeof(Property<Material>))]
	[XmlInclude(typeof(ListProperty<Material>))]
	public class Model : Component<Main>, IDrawableComponent
	{
#if MONOGAME
		public const string SamplerPostfix = "Sampler";
#else
		public const string SamplerPostfix = "Texture";
#endif

		protected struct InstanceVertex
		{
			public Matrix Transform;
			public Matrix LastTransform;
			public int InstanceIndex;
		}

		public float GetDistance(Vector3 camera)
		{
			Vector3 translation = this.Transform.Value.Translation;
			if (this.boundingBoxValid && this.CullBoundingBox)
			{
				BoundingBox box = this.BoundingBox;
				translation += (box.Min + box.Max) * 0.5f;
			}
			return (translation - camera).LengthSquared();
		}

		public bool IsVisible(BoundingFrustum frustum)
		{
			return !this.boundingBoxValid || !this.CullBoundingBox || frustum.Intersects(this.BoundingBox.Value.Transform(this.Transform));
		}

		public static int DrawCallCounter;
		public static int TriangleCounter;

		protected Microsoft.Xna.Framework.Graphics.Model model;

		public Property<string> Filename = new Property<string>();
		public Property<string> EffectFile = new Property<string>();
		public Property<string> TechniquePostfix = new Property<string> { Value = "" };
		protected Matrix lastWorldViewProjection;
		protected Matrix lastTransform;
		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix> { Value = Matrix.Identity };
		public Property<Vector3> Scale = new Property<Vector3> { Value = Vector3.One };
		public Property<Vector3> Color = new Property<Vector3> { Value = Vector3.One };

		public virtual void EditorProperties()
		{
			this.Entity.Add("Scale", this.Scale);
			this.Entity.Add("Color", this.Color);
			this.Entity.Add("Filename", this.Filename, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "Models", Path.Combine(MapLoader.MapDirectory, "Models") }),
			});
		}

		public struct Material
		{
			public static Material Unlit = new Material();
			public float SpecularPower;
			public float SpecularIntensity;
		}
		public Material[] Materials = new []
		{
			new Material
			{
				SpecularPower = 1.0f,
				SpecularIntensity = 0.0f,
			},
			Material.Unlit,
		};
		private int[] materialIds = new int[2];

		public Property<bool> IsInstanced = new Property<bool>();
		public Property<bool> DisableCulling = new Property<bool>();

		[XmlIgnore]
		public Property<bool> IsValid = new Property<bool>();

		public Property<bool> MapContent = new Property<bool>();

		protected Texture2D normalMap;
		public Property<string> NormalMap = new Property<string>();

		[XmlIgnore]
		public Property<BoundingBox> BoundingBox = new Property<BoundingBox>();
		private bool boundingBoxValid = false;
		public Property<bool> CullBoundingBox = new Property<bool> { Value = true };

		protected Texture2D diffuseTexture;
		public Property<string> DiffuseTexture = new Property<string>();

		[XmlIgnore]
		public ListProperty<Matrix> Instances = new ListProperty<Matrix>();

		private bool instancesChanged = true;
		private bool lastInstancesChanged;

		protected InstanceVertex[] instanceVertexData;
		protected DynamicVertexBuffer instanceVertexBuffer;
		[XmlIgnore]
		public ListProperty<Technique> UnsupportedTechniques = new ListProperty<Technique>();

		protected Effect effect;

		[XmlIgnore]
		public Effect InternalEffect
		{
			get
			{
				return this.effect;
			}
		}

		// To store instance transform matrices in a vertex buffer, we use this custom
		// vertex type which encodes 4x4 matrices as a set of four Vector4 values.
		protected static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
			new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 4),
			new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 5),
			new VertexElement(96, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 6),
			new VertexElement(112, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 7),
			new VertexElement(128, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0)
		);

#if !MONOGAME
		static Dictionary<Microsoft.Xna.Framework.Graphics.Model, BoundingBox> boundingBoxCache = new Dictionary<Microsoft.Xna.Framework.Graphics.Model, BoundingBox>();
#endif

		public Model()
		{
			this.EnabledWhenPaused = true;
		}

		public override void Awake()
		{
			base.Awake();
			// Make sure all the parameters come before the model and effect
			this.NormalMap.Set = delegate(string value)
			{
				this.NormalMap.InternalValue = value;
				this.normalMap = string.IsNullOrEmpty(value) ? null : (this.MapContent ? this.main.MapContent : this.main.Content).Load<Texture2D>(value);
				if (this.effect != null && this.normalMap != null)
				{
					EffectParameter param = this.effect.Parameters["NormalMap" + Model.SamplerPostfix];
					if (param != null)
						param.SetValue(this.normalMap);
				}
			};
			this.DiffuseTexture.Set = delegate(string value)
			{
				this.DiffuseTexture.InternalValue = value;
				try
				{
					this.diffuseTexture = string.IsNullOrEmpty(value) ? null : (this.MapContent ? this.main.MapContent : this.main.Content).Load<Texture2D>(value);
				}
				catch (ContentLoadException)
				{
					this.diffuseTexture = null;
				}
				if (this.effect != null && this.diffuseTexture != null)
				{
					EffectParameter param = this.effect.Parameters["Diffuse" + Model.SamplerPostfix];
					if (param != null)
						param.SetValue(this.diffuseTexture);
				}
			};
			this.Color.Set = delegate(Vector3 value)
			{
				this.Color.InternalValue = value;
				if (this.effect != null)
				{
					EffectParameter param = this.effect.Parameters["DiffuseColor"];
					if (param != null)
						param.SetValue(value);
				}
			};

			this.BoundingBox.Set = delegate(BoundingBox value)
			{
				this.BoundingBox.InternalValue = value;
				this.boundingBoxValid = true;
			};

			this.Filename.Set = delegate(string value)
			{
				if (value == this.Filename.InternalValue && this.model != null)
					return;
				this.boundingBoxValid = false;
				this.loadModel(value, false);
				this.Filename.InternalValue = value;
#if !MONOGAME
				if (this.model != null)
				{
					// TODO: Fix bounding box calculation
					BoundingBox boundingBox = new BoundingBox();
					if (!Model.boundingBoxCache.TryGetValue(this.model, out boundingBox))
					{
						// Create variables to hold min and max xyz values for the model. Initialise them to extremes
						Vector3 modelMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
						Vector3 modelMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

						foreach (ModelMesh mesh in this.model.Meshes)
						{
							//Create variables to hold min and max xyz values for the mesh. Initialise them to extremes
							Vector3 meshMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
							Vector3 meshMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

							// There may be multiple parts in a mesh (different materials etc.) so loop through each
							foreach (ModelMeshPart part in mesh.MeshParts)
							{
								// The stride is how big, in bytes, one vertex is in the vertex buffer
								// We have to use this as we do not know the make up of the vertex
								int stride = part.VertexBuffer.VertexDeclaration.VertexStride;

								byte[] vertexData = new byte[stride * part.NumVertices];
								part.VertexBuffer.GetData(part.VertexOffset * stride, vertexData, 0, part.NumVertices, 1);

								// Find minimum and maximum xyz values for this mesh part
								// We know the position will always be the first 3 float values of the vertex data
								Vector3 vertPosition = new Vector3();
								for (int ndx = 0; ndx < vertexData.Length; ndx += stride)
								{
									vertPosition.X = BitConverter.ToSingle(vertexData, ndx);
									vertPosition.Y = BitConverter.ToSingle(vertexData, ndx + sizeof(float));
									vertPosition.Z = BitConverter.ToSingle(vertexData, ndx + sizeof(float) * 2);

									// update our running values from this vertex
									meshMin = Vector3.Min(meshMin, vertPosition);
									meshMax = Vector3.Max(meshMax, vertPosition);
								}
							}

							// Expand model extents by the ones from this mesh
							modelMin = Vector3.Min(modelMin, meshMin);
							modelMax = Vector3.Max(modelMax, meshMax);
						}
						boundingBox = new BoundingBox(modelMin, modelMax);
						Model.boundingBoxCache[this.model] = boundingBox;
					}
					this.BoundingBox.Value = boundingBox;
				}
#endif
			};

			this.EffectFile.Set = delegate(string value)
			{
				if (value == this.EffectFile.InternalValue && this.effect != null)
					return;
				this.EffectFile.InternalValue = value;
				this.loadEffect(value);
			};

			this.Instances.ItemRemoved += delegate(int index, Matrix matrix)
			{
				if (this.instanceVertexData != null)
				{
					for (int i = index; i < Math.Min(this.Instances.Length, this.instanceVertexData.Length) - 1; i++)
						this.instanceVertexData[i] = this.instanceVertexData[i + 1];
				}
				this.instancesChanged = true;
			};
			this.Instances.ItemAdded += delegate(int index, Matrix matrix)
			{
				if (this.instanceVertexData != null && index < this.instanceVertexData.Length)
				{
					this.instanceVertexData[index].LastTransform = matrix;
					this.instanceVertexData[index].Transform = matrix;
				}
				this.instancesChanged = true;
			};
			this.Instances.ItemChanged += delegate(int index, Matrix old, Matrix newValue)
			{
				this.instancesChanged = true;
			};
			this.Instances.Cleared += delegate()
			{
				this.instancesChanged = true;
			};
		}

		public virtual void LoadContent(bool reload)
		{
			if (reload)
			{
				this.instanceVertexBuffer = null;
				this.loadModel(this.Filename, true);
				this.loadEffect(this.EffectFile);
			}
		}

		protected virtual void loadEffect(string file)
		{
			this.effect = null;
			if (file == null)
			{
				if (this.model != null)
				{
					this.effect = this.model.Meshes.FirstOrDefault().Effects.FirstOrDefault().Clone();
					if (this.effect.IsDisposed)
						this.effect = null;
				}
			}
			else
				this.effect = this.main.Content.Load<Effect>(file).Clone();

			if (this.effect != null)
			{
				// Reset parameters

				foreach (IProperty property in this.properties.Values)
					property.Reset();

				this.Color.Reset();
				this.DiffuseTexture.Reset();
				this.NormalMap.Reset();
			}
		}

		protected virtual void loadModel(string file, bool reload)
		{
			if (!reload)
				this.UnsupportedTechniques.Clear();
			if (string.IsNullOrEmpty(file))
			{
				this.model = null;
				this.effect = null;
				this.IsValid.Value = false;
			}
			else
			{
				try
				{
					ContentManager content = this.MapContent ? this.main.MapContent : this.main.Content;
					this.model = content.Load<Microsoft.Xna.Framework.Graphics.Model>(file);
					if (this.EffectFile.Value == null)
						this.loadEffect(null);
					this.IsValid.Value = true;
				}
				catch (Exception e)
				{
					Log.d(e.ToString());
					this.model = null;
					this.effect = null;
					this.IsValid.Value = false;
				}
			}
		}

		public override void delete()
		{
			base.delete();
			if (this.effect != null)
				this.effect.Dispose();
		}

		private Dictionary<string, IProperty> properties = new Dictionary<string, IProperty>();

		public Property<bool> GetBoolParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<bool> property = new Property<bool>();
				property.Set = delegate(bool value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<bool>)result;
		}

		public Property<bool[]> GetBoolArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<bool[]> property = new Property<bool[]>();
				property.Set = delegate(bool[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<bool[]>)result;
		}

		public Property<int> GetIntParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<int> property = new Property<int>();
				property.Set = delegate(int value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<int>)result;
		}

		public Property<int[]> GetIntArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<int[]> property = new Property<int[]>();
				property.Set = delegate(int[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<int[]>)result;
		}

		public Property<float> GetFloatParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<float> property = new Property<float>();
				property.Set = delegate(float value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<float>)result;
		}

		public Property<float[]> GetFloatArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<float[]> property = new Property<float[]>();
				property.Set = delegate(float[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<float[]>)result;
		}

		public Property<Vector2> GetVector2Parameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Vector2> property = new Property<Vector2>();
				property.Set = delegate(Vector2 value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Vector2>)result;
		}

		public Property<Vector2[]> GetVector2ArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Vector2[]> property = new Property<Vector2[]>();
				property.Set = delegate(Vector2[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Vector2[]>)result;
		}

		public Property<Vector3> GetVector3Parameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Vector3> property = new Property<Vector3>();
				property.Set = delegate(Vector3 value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Vector3>)result;
		}

		public Property<Vector3[]> GetVector3ArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Vector3[]> property = new Property<Vector3[]>();
				property.Set = delegate(Vector3[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Vector3[]>)result;
		}

		public Property<Vector4> GetVector4Parameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Vector4> property = new Property<Vector4>();
				property.Set = delegate(Vector4 value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Vector4>)result;
		}

		public Property<Vector4[]> GetVector4ArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Vector4[]> property = new Property<Vector4[]>();
				property.Set = delegate(Vector4[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Vector4[]>)result;
		}

		public Property<Matrix> GetMatrixParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Matrix> property = new Property<Matrix>();
				property.Set = delegate(Matrix value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Matrix>)result;
		}

		public Property<Matrix[]> GetMatrixArrayParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Matrix[]> property = new Property<Matrix[]>();
				property.Set = delegate(Matrix[] value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null)
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Matrix[]>)result;
		}

		public Property<Texture2D> GetTexture2DParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<Texture2D> property = new Property<Texture2D>();
				property.Set = delegate(Texture2D value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null && (value == null || !value.IsDisposed))
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<Texture2D>)result;
		}

		public Property<RenderTarget2D> GetRenderTarget2DParameter(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				Property<RenderTarget2D> property = new Property<RenderTarget2D>();
				property.Set = delegate(RenderTarget2D value)
				{
					property.InternalValue = value;
					if (this.effect != null)
					{
						EffectParameter param = this.effect.Parameters[name];
						if (param != null && (value == null || !value.IsDisposed))
							param.SetValue(value);
					}
				};
				this.properties[name] = property;
				result = property;
			}
			return (Property<RenderTarget2D>)result;
		}

		protected virtual bool setParameters(Matrix transform, RenderParameters parameters)
		{
			if (this.UnsupportedTechniques.Contains(parameters.Technique))
				return false;

			EffectTechnique technique = this.effect.Techniques[parameters.Technique.ToString() + this.TechniquePostfix];
			if (technique == null)
			{
				this.UnsupportedTechniques.Add(parameters.Technique);
				return false;
			}
			else
				this.effect.CurrentTechnique = technique;
			
			if (parameters.Technique == Technique.Clip)
				this.effect.Parameters["ClipPlanes"].SetValue(parameters.ClipPlaneData);
			EffectParameter parameter = this.effect.Parameters["LastFrameWorldMatrix"];
			if (parameter != null)
				parameter.SetValue(this.lastTransform);
			parameter = this.effect.Parameters["LastFrameWorldViewProjectionMatrix"];
			if (parameter != null)
				parameter.SetValue(this.lastWorldViewProjection);
			parameter = this.effect.Parameters["WorldMatrix"];
			if (parameter != null)
				parameter.SetValue(transform);
			parameter = this.effect.Parameters["WorldViewMatrix"];
			if (parameter != null)
				parameter.SetValue(transform * parameters.Camera.View);
			parameter = this.effect.Parameters["Time"];
			if (parameter != null)
				parameter.SetValue(this.main.TotalTime);
			parameters.Camera.SetParameters(this.effect);

			if (this.materialIds.Length < this.Materials.Length)
				this.materialIds = new int[this.Materials.Length];
			for (int i = 0; i < this.Materials.Length; i++)
				this.materialIds[i] = this.main.LightingManager.GetMaterialIndex(this.Materials[i]);

			this.effect.Parameters["Materials"].SetValue(this.materialIds);
			return true;
		}

		public virtual void Draw(GameTime time, RenderParameters parameters)
		{
			Matrix transform = Matrix.CreateScale(this.Scale) * this.Transform;
			if (!this.IsInstanced)
				this.draw(parameters, transform);
			else
				this.drawInstances(parameters, transform);
		}

		/// <summary>
		/// Draws a single mesh using the given world matrix.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="transform"></param>
		protected virtual void draw(RenderParameters parameters, Matrix transform)
		{
			if (this.model != null)
			{
				if (this.setParameters(transform, parameters))
				{
					this.main.LightingManager.SetRenderParameters(this.effect, parameters);

					RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
					RasterizerState noCullState = null;
					if (this.DisableCulling)
					{
						noCullState = new RasterizerState { CullMode = CullMode.None };
						this.main.GraphicsDevice.RasterizerState = noCullState;
					}

					foreach (ModelMesh mesh in this.model.Meshes)
					{
						foreach (ModelMeshPart part in mesh.MeshParts)
						{
							if (part.NumVertices > 0)
							{
								// Draw all the instance copies in a single call.
								this.effect.CurrentTechnique.Passes[0].Apply();
								this.main.GraphicsDevice.SetVertexBuffer(part.VertexBuffer, part.VertexOffset);
								this.main.GraphicsDevice.Indices = part.IndexBuffer;
								this.main.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
								Model.DrawCallCounter++;
								Model.TriangleCounter += part.PrimitiveCount;
							}
						}
					}

					if (noCullState != null)
						this.main.GraphicsDevice.RasterizerState = originalState;

					if (parameters.IsMainRender)
					{
						this.lastTransform = transform;
						this.lastWorldViewProjection = transform * parameters.Camera.ViewProjection;
					}
				}
			}
		}

		/// <summary>
		/// Draws a collection of instances. Requires an HLSL effect designed for hardware instancing.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="camera"></param>
		/// <param name="instances"></param>
		protected virtual void drawInstances(RenderParameters parameters, Matrix transform)
		{
			if (this.Instances.Length == 0)
				return;

			bool recalculate = this.instanceVertexBuffer == null || this.instanceVertexBuffer.IsContentLost || (parameters.IsMainRender && (this.instancesChanged || this.lastInstancesChanged));
			if (recalculate)
			{
				// If we have more instances than room in our vertex buffer, grow it to the neccessary size.
				if (this.instanceVertexBuffer == null || this.instanceVertexBuffer.IsContentLost || this.Instances.Length > this.instanceVertexBuffer.VertexCount)
				{
					if (this.instanceVertexBuffer != null)
						this.instanceVertexBuffer.Dispose();

					int bufferSize = (int)Math.Pow(2.0, Math.Ceiling(Math.Log(this.Instances.Length, 2.0)));

					this.instanceVertexBuffer = new DynamicVertexBuffer
					(
						this.main.GraphicsDevice,
						Model.instanceVertexDeclaration,
						bufferSize,
						BufferUsage.WriteOnly
					);

					InstanceVertex[] newData = new InstanceVertex[bufferSize];
					if (this.instanceVertexData != null)
					{
						Array.Copy(this.instanceVertexData, newData, Math.Min(bufferSize, this.instanceVertexData.Length));
						for (int i = this.instanceVertexData.Length; i < this.Instances.Length; i++)
							newData[i].LastTransform = this.Instances[i];
					}
					this.instanceVertexData = newData;
				}
			
				for (int i = 0; i < this.Instances.Length; i++)
				{
					this.instanceVertexData[i].LastTransform = this.instanceVertexData[i].Transform;
					this.instanceVertexData[i].Transform = this.Instances[i];
					this.instanceVertexData[i].InstanceIndex = i;
				}

				// Transfer the latest instance transform matrices into the instanceVertexBuffer.
				this.instanceVertexBuffer.SetData<InstanceVertex>(this.instanceVertexData, 0, this.Instances.Length, SetDataOptions.Discard);

				this.lastInstancesChanged = this.instancesChanged;
				this.instancesChanged = false;
			}
			
#if !MONOGAME // TODO: enable hardware instancing for MonoGame
			// Set up the instance rendering effect.
			if (this.setParameters(transform, parameters))
			{
				this.main.LightingManager.SetRenderParameters(this.effect, parameters);

				RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
				RasterizerState noCullState = null;
				if (recalculate && this.DisableCulling)
				{
					noCullState = new RasterizerState { CullMode = CullMode.None };
					this.main.GraphicsDevice.RasterizerState = noCullState;
				}

				foreach (ModelMesh mesh in this.model.Meshes)
				{
					foreach (ModelMeshPart meshPart in mesh.MeshParts)
					{
						// Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.

						// TODO: Monogame support for GraphicsDevice.SetVertexBuffers()
						this.main.GraphicsDevice.SetVertexBuffers
						(
							new VertexBufferBinding(meshPart.VertexBuffer, meshPart.VertexOffset, 0),
							new VertexBufferBinding(instanceVertexBuffer, 0, 1)
						);
						
						this.main.GraphicsDevice.Indices = meshPart.IndexBuffer;

						// Draw all the instance copies in a single call.
						this.effect.CurrentTechnique.Passes[0].Apply();
						// TODO: Monogame support for GraphicsDevice.DrawInstancedPrimitives()
						this.main.GraphicsDevice.DrawInstancedPrimitives
						(
							PrimitiveType.TriangleList,
							0,
							0,
							meshPart.NumVertices,
							meshPart.StartIndex,
							meshPart.PrimitiveCount,
							this.Instances.Length
						);
						Model.DrawCallCounter++;
						Model.TriangleCounter += meshPart.PrimitiveCount * this.Instances.Length;
					}
				}

				if (noCullState != null)
					this.main.GraphicsDevice.RasterizerState = originalState;
			}
#endif

			if (parameters.IsMainRender)
			{
				this.lastTransform = transform;
				this.lastWorldViewProjection = transform * parameters.Camera.ViewProjection;
			}
		}
	}

	public class ModelAlpha : Model, IDrawableAlphaComponent
	{
		public Property<float> Alpha = null;
		public Property<int> DrawOrder { get; set; }
		public Property<bool> Distortion = new Property<bool>();

		public ModelAlpha()
		{
			this.Alpha = this.GetFloatParameter("Alpha");
			this.Alpha.Value = 1.0f;
			this.DrawOrder = new Property<int>();
		}

		public override void EditorProperties()
		{
			this.Entity.Add("Scale", this.Scale);
			this.Entity.Add("Color", this.Color);
			this.Entity.Add("Filename", this.Filename, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "AlphaModels", Path.Combine(MapLoader.MapDirectory, "AlphaModels") }),
			});
			this.Entity.Add("Alpha", this.Alpha);
			this.Entity.Add("DrawOrder", this.DrawOrder);
		}

		public override void Awake()
		{
			base.Awake();

			float alpha = this.Alpha;
			this.Alpha = this.GetFloatParameter("Alpha");
			this.Alpha.Value = alpha;
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
			{
				this.effect.Parameters["Depth" + Model.SamplerPostfix].SetValue(parameters.DepthBuffer);
				if (this.Distortion)
					this.effect.Parameters["Frame" + Model.SamplerPostfix].SetValue(parameters.FrameBuffer);
			}
			return result;
		}
	}
}