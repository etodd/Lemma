using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

using Lemma.Util;
using System.Xml.Serialization;
using BEPUphysics.Entities;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Lemma.Factories;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;
using BEPUphysics.Collidables.MobileCollidables;
using System.ComponentModel;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using System.Threading;
using System.Collections;

namespace Lemma.Components
{
	public class Map : Component
	{
		[XmlIgnore]
		public object Lock = new object();

		public static Command<Map, IEnumerable<Coordinate>, Map> GlobalCellsEmptied = new Command<Map, IEnumerable<Coordinate>, Map>();

		public static Command<Map, IEnumerable<Coordinate>, Map> GlobalCellsFilled = new Command<Map, IEnumerable<Coordinate>, Map>();

		[XmlIgnore]
		public Command<IEnumerable<Coordinate>, Map> CellsEmptied = new Command<IEnumerable<Coordinate>, Map>();

		[XmlIgnore]
		public Command<IEnumerable<Coordinate>, Map> CellsFilled = new Command<IEnumerable<Coordinate>, Map>();

		public struct MapVertex
		{
			public Vector3 Position;
			public Vector3 Normal;
			public Vector3 Binormal;
			public Vector3 Tangent;

			public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
			(
				new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
				new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
				new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0),
				new VertexElement(36, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0)
			);

			public const int SizeInBytes = 48;
		}

		private static Dictionary<int, Queue<Box[, ,]>> freeDataChunks = new Dictionary<int, Queue<Box[, ,]>>();
		private static Dictionary<int, Queue<Chunk[, ,]>> freeChunkArrays = new Dictionary<int, Queue<Chunk[, ,]>>();

		public class MapState
		{
			public List<Box> Boxes = new List<Box>();
			public List<Chunk> Chunks = new List<Chunk>();

			public MapState(IEnumerable<Chunk> chunks)
			{
				this.Add(chunks);
			}

			public void Add(IEnumerable<Chunk> chunks)
			{
				foreach (Chunk chunk in chunks)
				{
					if (!this.Chunks.Contains(chunk))
					{
						this.Chunks.Add(chunk);
						this.Boxes.AddRange(chunks.SelectMany(x => x.Boxes));
					}
				}
			}

			public CellState this[Coordinate coord]
			{
				get
				{
					foreach (Box box in this.Boxes)
					{
						if (box.Contains(coord))
							return box.Type;
					}
					return null;
				}
			}
		}

		public class CellState
		{
			public int ID;
			public bool Permanent;
			public string Name;
			public string DiffuseMap;
			public string NormalMap;
			public string FootstepCue;
			public string RubbleCue;
			public float SpecularPower;
			public float SpecularIntensity;
			public float Density;
			[DefaultValue(false)]
			public bool AllowAlpha;
			[DefaultValue(true)]
			public bool ShadowCast = true;
			[DefaultValue(false)]
			public bool Fake;
			[DefaultValue(false)]
			public bool Invisible;
			[DefaultValue(false)]
			public bool Glow;
			[DefaultValue(1.0f)]
			public float Tiling = 1.0f;
			public Vector3 Tint = Vector3.One;

			public void ApplyTo(Model model)
			{
				model.DiffuseTexture.Value = this.DiffuseMap;
				model.NormalMap.Value = this.NormalMap;
				model.SpecularIntensity.Value = this.SpecularIntensity;
				model.SpecularPower.Value = this.SpecularPower;
				model.DisableCulling.Value = this.AllowAlpha;
				model.Color.Value = this.Tint;
				string postfix = "";
				if (this.AllowAlpha)
					postfix += "Alpha";
				if (this.Glow)
					postfix += "Glow";
				model.TechniquePostfix.Value = postfix;
				model.GetFloatParameter("Tiling").Value = this.Tiling;
			}

			public void ApplyToBlock(Entity block)
			{
				block.GetProperty<string>("CollisionSoundCue").Value = this.RubbleCue;
				block.Get<PhysicsBlock>().Box.Mass = this.Density;
				this.ApplyToEffectBlock(block.Get<ModelInstance>());
			}

			public void ApplyToEffectBlock(ModelInstance modelInstance)
			{
				modelInstance.Setup("Models\\block", this.ID);
				if (modelInstance.IsFirstInstance)
				{
					Model model = modelInstance.Model;
					this.ApplyTo(model);
				}
			}
		}

		public class Surface
		{
			public int W;
			public int MinU, MaxU;
			public int MinV, MaxV;
			public bool HasArea;

			public Map.MapVertex[] Vertices;

			public void RefreshTransform(Box box, Direction normal)
			{
				this.HasArea = this.MaxU > this.MinU && this.MaxV > this.MinV;

				if (this.HasArea)
				{
					switch (normal)
					{
						case Direction.PositiveX:
							this.Vertices[0].Position = new Vector3(this.W, this.MaxU, this.MinV);
							this.Vertices[1].Position = new Vector3(this.W, this.MinU, this.MinV);
							this.Vertices[2].Position = new Vector3(this.W, this.MinU, this.MaxV);
							this.Vertices[3].Position = new Vector3(this.W, this.MaxU, this.MaxV);
							break;

						case Direction.NegativeX:
							this.Vertices[0].Position = new Vector3(this.W, this.MaxU, this.MinV);
							this.Vertices[1].Position = new Vector3(this.W, this.MaxU, this.MaxV);
							this.Vertices[2].Position = new Vector3(this.W, this.MinU, this.MaxV);
							this.Vertices[3].Position = new Vector3(this.W, this.MinU, this.MinV);
							break;

						case Direction.PositiveY:
							this.Vertices[0].Position = new Vector3(this.MaxU, this.W, this.MaxV);
							this.Vertices[1].Position = new Vector3(this.MinU, this.W, this.MaxV);
							this.Vertices[2].Position = new Vector3(this.MinU, this.W, this.MinV);
							this.Vertices[3].Position = new Vector3(this.MaxU, this.W, this.MinV);
							break;
							
						case Direction.NegativeY:
							this.Vertices[0].Position = new Vector3(this.MaxU, this.W, this.MinV);
							this.Vertices[1].Position = new Vector3(this.MinU, this.W, this.MinV);
							this.Vertices[2].Position = new Vector3(this.MinU, this.W, this.MaxV);
							this.Vertices[3].Position = new Vector3(this.MaxU, this.W, this.MaxV);
							break;

						case Direction.PositiveZ:
							this.Vertices[0].Position = new Vector3(this.MaxU, this.MinV, this.W);
							this.Vertices[1].Position = new Vector3(this.MinU, this.MinV, this.W);
							this.Vertices[2].Position = new Vector3(this.MinU, this.MaxV, this.W);
							this.Vertices[3].Position = new Vector3(this.MaxU, this.MaxV, this.W);
							break;

						default: // Direction.NegativeZ
							this.Vertices[0].Position = new Vector3(this.MaxU, this.MaxV, this.W);
							this.Vertices[1].Position = new Vector3(this.MinU, this.MaxV, this.W);
							this.Vertices[2].Position = new Vector3(this.MinU, this.MinV, this.W);
							this.Vertices[3].Position = new Vector3(this.MaxU, this.MinV, this.W);
							break;
					}
				}
			}
		}

		public class Chunk
		{
			public bool Active = false;
			public Map Map;
			public Box[, ,] Data;
			public int X, Y, Z;
			public ListProperty<Box> Boxes = new ListProperty<Box>();
			public BoundingBox RelativeBoundingBox;
			public int IndexX, IndexY, IndexZ;

			public List<Box> DataBoxes;

			public Chunk()
			{
				this.Boxes.ItemAdded += delegate(int index, Box t)
				{
					int chunkHalfSize = this.Map.chunkHalfSize;
					t.ChunkHash = t.Type.ID + (255 * (1
						+ (((int)((t.X - this.X) / chunkHalfSize) * 4)
						+ ((int)((t.Y - this.Y) / chunkHalfSize) * 2)
						+ (int)((t.Z - this.Z) / chunkHalfSize))));
					this.MarkDirty(t);
					t.Added = true;
					t.ChunkIndex = index;
				};

				this.Boxes.ItemChanged += delegate(int index, Box old, Box newValue)
				{
					this.MarkDirty(old);
					newValue.ChunkIndex = old.ChunkIndex;
				};

				this.Boxes.ItemRemoved += delegate(int index, Box t)
				{
					t.Added = false;
					for (int i = index; i < this.Boxes.Count; i++)
						this.Boxes[i].ChunkIndex = i;
				};
			}

			public virtual void Refresh()
			{

			}

			public void Instantiate()
			{
				foreach (Box b in this.DataBoxes)
					this.Map.addBoxWithoutAdjacency(b);

				foreach (Box box in this.DataBoxes)
				{
					for (int i = 0; i < 6; i++)
						box.Surfaces[i].RefreshTransform(box, (Direction)i);
					this.Boxes.Add(box);
				}

				this.DataBoxes.Clear();
				this.DataBoxes = null;

				if (!this.Map.main.EditorEnabled && !this.Map.EnablePhysics)
				{
					this.freeData();
					foreach (Box box in this.Boxes)
					{
						box.Adjacent.Clear();
						box.Adjacent = null;
					}
				}
			}

			public virtual void Activate()
			{
				this.Active = true;
			}

			public virtual void Deactivate()
			{
				this.Active = false;
			}

			private void freeData()
			{
				if (this.Data == null)
					return; // Already freed

				for (int u = 0; u < this.Map.chunkSize; u++)
				{
					for (int v = 0; v < this.Map.chunkSize; v++)
					{
						for (int w = 0; w < this.Map.chunkSize; w++)
							this.Data[u, v, w] = null;
					}
				}
				Queue<Box[, ,]> queue;
				if (!Map.freeDataChunks.TryGetValue(this.Map.chunkSize, out queue))
					queue = Map.freeDataChunks[this.Map.chunkSize] = new Queue<Box[, ,]>();
				queue.Enqueue(this.Data);
				this.Data = null;
			}

			public virtual void Delete()
			{
				this.Active = false;
				this.freeData();
				foreach (Box box in this.Boxes)
				{
					if (box.Adjacent != null)
					{
						box.Adjacent.Clear();
						box.Adjacent = null;
					}
				}
				this.Boxes.Clear();
				this.Boxes = null;
			}

			public virtual void MarkDirty(Box box)
			{

			}
		}

		public class StaticChunk : Chunk
		{
			private class MeshEntry
			{
				public StaticMesh Mesh;
				public bool Dirty;
				public bool Added;
			}

			private Dictionary<int, MeshEntry> meshes = new Dictionary<int, MeshEntry>();

			public override void MarkDirty(Box box)
			{
				if (this.Map.main.EditorEnabled || !this.Map.EnablePhysics || box.Type.Fake)
					return;

				MeshEntry entry = null;
				if (!this.meshes.TryGetValue(box.ChunkHash, out entry))
				{
					entry = new MeshEntry();
					this.meshes[box.ChunkHash] = entry;
				}
				entry.Dirty = true;
			}

			public override void Refresh()
			{
				Map.workQueue.Enqueue(new WorkItem { StaticChunk = this, StaticChunkAction = StaticChunkAction.Refresh });
			}

			public void RefreshImmediately()
			{
				if (this.Map.main.EditorEnabled || !this.Map.EnablePhysics)
					return;

				foreach (KeyValuePair<int, MeshEntry> pair in this.meshes)
				{
					if (!pair.Value.Dirty)
						continue;
					pair.Value.Dirty = false;

					if (pair.Value.Mesh != null && pair.Value.Added)
					{
						pair.Value.Added = false;
						this.Map.main.Space.SpaceObjectBuffer.Remove(pair.Value.Mesh);
					}

					List<Box> boxes = this.Boxes.Where(x => x.ChunkHash == pair.Key).ToList();
					Vector3[] vertices = new Vector3[boxes.Count * 8];
					int[] indices = new int[boxes.SelectMany(x => x.Surfaces).Count(x => x.HasArea) * 6];
					int vertexIndex = 0;
					int index = 0;
					foreach (Box box in boxes)
					{
						vertices[vertexIndex + 0] = new Vector3(box.X, box.Y, box.Z);
						vertices[vertexIndex + 1] = new Vector3(box.X, box.Y, box.Z + box.Depth);
						vertices[vertexIndex + 2] = new Vector3(box.X, box.Y + box.Height, box.Z);
						vertices[vertexIndex + 3] = new Vector3(box.X, box.Y + box.Height, box.Z + box.Depth);
						vertices[vertexIndex + 4] = new Vector3(box.X + box.Width, box.Y, box.Z);
						vertices[vertexIndex + 5] = new Vector3(box.X + box.Width, box.Y, box.Z + box.Depth);
						vertices[vertexIndex + 6] = new Vector3(box.X + box.Width, box.Y + box.Height, box.Z);
						vertices[vertexIndex + 7] = new Vector3(box.X + box.Width, box.Y + box.Height, box.Z + box.Depth);

						if (box.Surfaces[(int)Direction.NegativeX].HasArea)
						{
							indices[index++] = vertexIndex + 0;
							indices[index++] = vertexIndex + 1;
							indices[index++] = vertexIndex + 2;
							indices[index++] = vertexIndex + 2;
							indices[index++] = vertexIndex + 1;
							indices[index++] = vertexIndex + 3;
						}
						if (box.Surfaces[(int)Direction.PositiveX].HasArea)
						{
							indices[index++] = vertexIndex + 6;
							indices[index++] = vertexIndex + 5;
							indices[index++] = vertexIndex + 4;
							indices[index++] = vertexIndex + 7;
							indices[index++] = vertexIndex + 5;
							indices[index++] = vertexIndex + 6;
						}
						if (box.Surfaces[(int)Direction.NegativeY].HasArea)
						{
							indices[index++] = vertexIndex + 0;
							indices[index++] = vertexIndex + 4;
							indices[index++] = vertexIndex + 1;
							indices[index++] = vertexIndex + 1;
							indices[index++] = vertexIndex + 4;
							indices[index++] = vertexIndex + 5;
						}
						if (box.Surfaces[(int)Direction.PositiveY].HasArea)
						{
							indices[index++] = vertexIndex + 3;
							indices[index++] = vertexIndex + 6;
							indices[index++] = vertexIndex + 2;
							indices[index++] = vertexIndex + 7;
							indices[index++] = vertexIndex + 6;
							indices[index++] = vertexIndex + 3;
						}
						if (box.Surfaces[(int)Direction.NegativeZ].HasArea)
						{
							indices[index++] = vertexIndex + 2;
							indices[index++] = vertexIndex + 4;
							indices[index++] = vertexIndex + 0;
							indices[index++] = vertexIndex + 6;
							indices[index++] = vertexIndex + 4;
							indices[index++] = vertexIndex + 2;
						}
						if (box.Surfaces[(int)Direction.PositiveZ].HasArea)
						{
							indices[index++] = vertexIndex + 1;
							indices[index++] = vertexIndex + 5;
							indices[index++] = vertexIndex + 3;
							indices[index++] = vertexIndex + 3;
							indices[index++] = vertexIndex + 5;
							indices[index++] = vertexIndex + 7;
						}

						vertexIndex += 8;
					}

					Matrix transform = this.Map.Transform;
					pair.Value.Mesh = new StaticMesh(vertices, indices, new BEPUphysics.MathExtensions.AffineTransform(BEPUphysics.MathExtensions.Matrix3X3.CreateFromMatrix(transform), transform.Translation));
					pair.Value.Mesh.Tag = this.Map;
					pair.Value.Mesh.Sidedness = TriangleSidedness.Clockwise;
					if (this.Active)
					{
						pair.Value.Added = true;
						this.Map.main.Space.SpaceObjectBuffer.Add(pair.Value.Mesh);
					}
				}
			}

			public override void Activate()
			{
				if (!this.Active)
				{
					base.Activate();
					Map.workQueue.Enqueue(new WorkItem { StaticChunk = this, StaticChunkAction = StaticChunkAction.Activate });
				}
			}

			public void ActivateImmediately()
			{
				foreach (MeshEntry entry in this.meshes.Values)
				{
					if (entry.Mesh != null && !entry.Added)
					{
						entry.Added = true;
						this.Map.main.Space.SpaceObjectBuffer.Add(entry.Mesh);
					}
				}
			}

			public override void Deactivate()
			{
				if (this.Active)
				{
					base.Deactivate();
					Map.workQueue.Enqueue(new WorkItem { StaticChunk = this, StaticChunkAction = StaticChunkAction.Deactivate });
				}
			}

			public void DeactivateImmediately()
			{
				foreach (MeshEntry entry in this.meshes.Values)
				{
					if (entry.Mesh != null && entry.Added)
					{
						entry.Added = false;
						this.Map.main.Space.SpaceObjectBuffer.Remove(entry.Mesh);
					}
				}
			}

			public override void Delete()
			{
				if (this.Active)
				{
					foreach (MeshEntry entry in this.meshes.Values)
					{
						if (entry.Mesh != null && entry.Added)
						{
							entry.Added = false;
							this.Map.main.Space.SpaceObjectBuffer.Remove(entry.Mesh);
						}
					}
					this.meshes.Clear();
				}
				base.Delete();
			}
		}
		
		public struct Coordinate
		{
			public int X;
			public int Y;
			public int Z;
			public CellState Data;

			public Coordinate Move(Direction dir, int amount)
			{
				int x = this.X, y = this.Y, z = this.Z;
				switch (dir)
				{
					case Direction.NegativeX:
						x -= amount;
						break;
					case Direction.PositiveX:
						x += amount;
						break;
					case Direction.NegativeY:
						y -= amount;
						break;
					case Direction.PositiveY:
						y += amount;
						break;
					case Direction.NegativeZ:
						z -= amount;
						break;
					case Direction.PositiveZ:
						z += amount;
						break;
				}
				return new Coordinate { X = x, Y = y, Z = z, Data = this.Data };
			}

			public Coordinate Plus(Coordinate other)
			{
				return new Coordinate { X = this.X + other.X, Y = this.Y + other.Y, Z = this.Z + other.Z };
			}

			public Coordinate Minus(Coordinate other)
			{
				return new Coordinate { X = this.X - other.X, Y = this.Y - other.Y, Z = this.Z - other.Z };
			}

			// Expects every dimension of A to be smaller than every dimension of B.
			public bool Between(Coordinate a, Coordinate b)
			{
				return this.X >= a.X && this.X < b.X
					&& this.Y >= a.Y && this.Y < b.Y
					&& this.Z >= a.Z && this.Z < b.Z;
			}

			public IEnumerable<Coordinate> CoordinatesBetween(Coordinate b)
			{
				for (int x = this.X; x < b.X; x++)
				{
					for (int y = this.Y; y < b.Y; y++)
					{
						for (int z = this.Z; z < b.Z; z++)
						{
							yield return new Coordinate { X = x, Y = y, Z = z };
						}
					}
				}
			}

			public bool Equivalent(Coordinate coord)
			{
				return coord.X == this.X && coord.Y == this.Y && coord.Z == this.Z;
			}

			public Coordinate Move(int x, int y, int z)
			{
				return new Coordinate { X = this.X + x, Y = this.Y + y, Z = this.Z + z, Data = this.Data };
			}

			public Coordinate Move(Direction dir)
			{
				return this.Move(dir, 1);
			}

			public Coordinate Clone()
			{
				return new Coordinate { X = this.X, Y = this.Y, Z = this.Z, Data = this.Data };
			}

			public int GetComponent(Direction dir)
			{
				switch (dir)
				{
					case Direction.NegativeX:
						return -this.X;
					case Direction.PositiveX:
						return this.X;
					case Direction.NegativeY:
						return -this.Y;
					case Direction.PositiveY:
						return this.Y;
					case Direction.NegativeZ:
						return -this.Z;
					case Direction.PositiveZ:
						return this.Z;
					default:
						return 0;
				}
			}

			public void SetComponent(Direction dir, int value)
			{
				switch (dir)
				{
					case Direction.NegativeX:
						this.X = -value;
						break;
					case Direction.PositiveX:
						this.X = value;
						break;
					case Direction.NegativeY:
						this.Y = -value;
						break;
					case Direction.PositiveY:
						this.Y = value;
						break;
					case Direction.NegativeZ:
						this.Z = -value;
						break;
					case Direction.PositiveZ:
						this.Z = value;
						break;
					default:
						break;
				}
			}
		}

		public class Box
		{
			public int X;
			public int Y;
			public int Z;
			public int Width;
			public int Height;
			public int Depth;
			public CellState Type;

			[XmlIgnore]
			public bool Active = true;
			[XmlIgnore]
			public bool Added;
			[XmlIgnore]
			public int ChunkIndex;
			[XmlIgnore]
			public int ChunkHash;
			[XmlIgnore]
			public Chunk Chunk;
			[XmlIgnore]
			public List<Box> Adjacent = new List<Box>();
			[XmlIgnore]
			public Surface[] Surfaces = new[]
			{
				new Surface // PositiveX
				{
					Vertices = new MapVertex[]
					{
						new MapVertex { Normal = Vector3.Right, Binormal = Vector3.Up, Tangent = Vector3.Forward },
						new MapVertex { Normal = Vector3.Right, Binormal = Vector3.Up, Tangent = Vector3.Forward },
						new MapVertex { Normal = Vector3.Right, Binormal = Vector3.Up, Tangent = Vector3.Forward },
						new MapVertex { Normal = Vector3.Right, Binormal = Vector3.Up, Tangent = Vector3.Forward },
					}
				},
				new Surface // NegativeX
				{
					Vertices = new MapVertex[]
					{
						new MapVertex { Normal = Vector3.Left, Binormal = Vector3.Up, Tangent = Vector3.Backward },
						new MapVertex { Normal = Vector3.Left, Binormal = Vector3.Up, Tangent = Vector3.Backward },
						new MapVertex { Normal = Vector3.Left, Binormal = Vector3.Up, Tangent = Vector3.Backward },
						new MapVertex { Normal = Vector3.Left, Binormal = Vector3.Up, Tangent = Vector3.Backward },
					}
				},
				new Surface // PositiveY
				{
					Vertices = new MapVertex[]
					{
						new MapVertex { Normal = Vector3.Up, Binormal = Vector3.Right, Tangent = Vector3.Forward },
						new MapVertex { Normal = Vector3.Up, Binormal = Vector3.Right, Tangent = Vector3.Forward },
						new MapVertex { Normal = Vector3.Up, Binormal = Vector3.Right, Tangent = Vector3.Forward },
						new MapVertex { Normal = Vector3.Up, Binormal = Vector3.Right, Tangent = Vector3.Forward },
					}
				},
				new Surface // NegativeY
				{
					Vertices = new MapVertex[]
					{
						new MapVertex { Normal = Vector3.Down, Binormal = Vector3.Right, Tangent = Vector3.Backward },
						new MapVertex { Normal = Vector3.Down, Binormal = Vector3.Right, Tangent = Vector3.Backward },
						new MapVertex { Normal = Vector3.Down, Binormal = Vector3.Right, Tangent = Vector3.Backward },
						new MapVertex { Normal = Vector3.Down, Binormal = Vector3.Right, Tangent = Vector3.Backward },
					}
				},
				new Surface // PositiveZ
				{
					Vertices = new MapVertex[]
					{
						new MapVertex { Normal = Vector3.Backward, Binormal = Vector3.Up, Tangent = Vector3.Right },
						new MapVertex { Normal = Vector3.Backward, Binormal = Vector3.Up, Tangent = Vector3.Right },
						new MapVertex { Normal = Vector3.Backward, Binormal = Vector3.Up, Tangent = Vector3.Right },
						new MapVertex { Normal = Vector3.Backward, Binormal = Vector3.Up, Tangent = Vector3.Right },
					}
				},
				new Surface // NegativeZ
				{
					Vertices = new MapVertex[]
					{
						new MapVertex { Normal = Vector3.Forward, Binormal = Vector3.Up, Tangent = Vector3.Left },
						new MapVertex { Normal = Vector3.Forward, Binormal = Vector3.Up, Tangent = Vector3.Left },
						new MapVertex { Normal = Vector3.Forward, Binormal = Vector3.Up, Tangent = Vector3.Left },
						new MapVertex { Normal = Vector3.Forward, Binormal = Vector3.Up, Tangent = Vector3.Left },
					}
				}
			};

			public int GetComponent(Direction dir)
			{
				switch (dir)
				{
					case Direction.NegativeX:
						return -this.X;
					case Direction.PositiveX:
						return this.X;
					case Direction.NegativeY:
						return -this.Y;
					case Direction.PositiveY:
						return this.Y;
					case Direction.NegativeZ:
						return -this.Z;
					case Direction.PositiveZ:
						return this.Z;
					default:
						return 0;
				}
			}

			public IEnumerable<Map.Coordinate> GetCoords()
			{
				for (int x = this.X; x < this.X + this.Width; x++)
				{
					for (int y = this.Y; y < this.Y + this.Height; y++)
					{
						for (int z = this.Z; z < this.Z + this.Depth; z++)
						{
							yield return new Map.Coordinate { X = x, Y = y, Z = z, Data = this.Type };
						}
					}
				}
			}

			public int GetSizeComponent(Direction dir)
			{
				switch (dir)
				{
					case Direction.NegativeX:
					case Direction.PositiveX:
						return this.Width;
					case Direction.NegativeY:
					case Direction.PositiveY:
						return this.Height;
					case Direction.NegativeZ:
					case Direction.PositiveZ:
						return this.Depth;
					default:
						return 0;
				}
			}

			public bool Contains(Coordinate coord)
			{
				return coord.X >= this.X && coord.X < this.X + this.Width
					&& coord.Y >= this.Y && coord.Y < this.Y + this.Height
					&& coord.Z >= this.Z && coord.Z < this.Z + this.Depth;
			}

			public CompoundShapeEntry GetCompoundShapeEntry()
			{
				return new CompoundShapeEntry(new BoxShape(this.Width, this.Height, this.Depth), new Vector3(this.X + (this.Width * 0.5f), this.Y + (this.Height * 0.5f), this.Z + (this.Depth * 0.5f)), this.Type.Density * this.Width * this.Height * this.Depth);
			}
		}

		public static readonly List<Map> Maps = new List<Map>();

		public static IEnumerable<Map> ActivePhysicsMaps
		{
			get
			{
				return Map.Maps.Where(x => !x.Suspended && x.EnablePhysics);
			}
		}

		public static IEnumerable<Map> ActiveMaps
		{
			get
			{
				return Map.Maps.Where(x => !x.Suspended);
			}
		}

		public struct GlobalRaycastResult
		{
			public Map Map;
			public Map.Coordinate? Coordinate;
			public Vector3 Position;
			public Direction Normal;
			public float Distance;
		}

		public struct RaycastResult
		{
			public Map.Coordinate? Coordinate;
			public Vector3 Position;
			public Direction Normal;
			public float Distance;
		}

		public static GlobalRaycastResult GlobalRaycast(Vector3 start, Vector3 ray, float length, bool includeScenery = false)
		{
			// Voxel raycasting
			GlobalRaycastResult result = new GlobalRaycastResult();
			result.Distance = length;

			IEnumerable<Map> maps = includeScenery ? Map.ActiveMaps : Map.ActivePhysicsMaps;

			foreach (Map map in maps)
			{
				RaycastResult hit = map.Raycast(start, ray, result.Distance);
				if (hit.Coordinate != null && hit.Distance < result.Distance)
				{
					result.Map = map;
					result.Coordinate = hit.Coordinate;
					result.Normal = hit.Normal;
					result.Position = hit.Position;
					result.Distance = hit.Distance;
				}
			}
			return result;
		}

		public static GlobalRaycastResult GlobalRaycast(Vector3 start, Vector3 ray, float length, Func<Map, bool> filter, bool includeScenery = false)
		{
			// Voxel raycasting
			GlobalRaycastResult result = new GlobalRaycastResult();
			result.Distance = length;

			IEnumerable<Map> maps = includeScenery ? Map.ActiveMaps : Map.ActivePhysicsMaps;

			foreach (Map map in maps)
			{
				if (!filter(map))
					continue;
				RaycastResult hit = map.Raycast(start, ray, result.Distance);
				if (hit.Coordinate != null && hit.Distance < result.Distance)
				{
					result.Map = map;
					result.Coordinate = hit.Coordinate;
					result.Normal = hit.Normal;
					result.Position = hit.Position;
					result.Distance = hit.Distance;
				}
			}
			return result;
		}

		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false, Value = Matrix.Identity };

		public Property<string> Data = new Property<string> { Editable = false };

		protected int minX;
		protected int minY;
		protected int minZ;
		protected int maxX;
		protected int maxY;
		protected int maxZ;

		[XmlIgnore]
		public int MinX
		{
			get
			{
				return this.minX;
			}
		}

		[XmlIgnore]
		public int MinY
		{
			get
			{
				return this.minY;
			}
		}

		[XmlIgnore]
		public int MinZ
		{
			get
			{
				return this.minZ;
			}
		}

		[XmlIgnore]
		public int MaxX
		{
			get
			{
				return this.minX;
			}
		}

		[XmlIgnore]
		public int MaxY
		{
			get
			{
				return this.minY;
			}
		}

		[XmlIgnore]
		public int MaxZ
		{
			get
			{
				return this.minZ;
			}
		}

		protected int maxChunks;
		protected int chunkHalfSize;
		protected int chunkSize;

		public int ChunkSize
		{
			get
			{
				return this.chunkSize;
			}
		}

		[XmlIgnore]
		public Command CompletelyEmptied = new Command();

		[XmlIgnore]
		public ListProperty<Chunk> Chunks = new ListProperty<Chunk> { Editable = false };

		private Chunk[, ,] chunks;

		protected List<Box> additions = new List<Box>();
		protected List<Box> removals = new List<Box>();
		protected List<Coordinate> removalCoords = new List<Coordinate>();

		[XmlIgnore]
		public Property<Vector3> Offset = new Property<Vector3> { Editable = false };

		public Property<bool> EnablePhysics = new Property<bool> { Editable = true, Value = true };

		[DefaultValueAttribute(0)]
		public int OffsetX { get; set; }
		[DefaultValueAttribute(0)]
		public int OffsetY { get; set; }
		[DefaultValueAttribute(0)]
		public int OffsetZ { get; set; }

		public Map()
			: this(0, 0, 0)
		{
			
		}

		public Map(int offsetX, int offsetY, int offsetZ)
			: this(20, 40)
		{
			this.OffsetX = offsetX;
			this.OffsetY = offsetY;
			this.OffsetZ = offsetZ;
		}

		protected Map(int maxChunks, int chunkHalfSize)
		{
			this.chunkHalfSize = chunkHalfSize;
			this.chunkSize = chunkHalfSize * 2;
			this.maxChunks = maxChunks;
			this.chunks = new Chunk[maxChunks, maxChunks, maxChunks];
		}

		public virtual void updatePhysics()
		{
			if (!main.EditorEnabled && this.EnablePhysics)
			{
				foreach (Chunk chunk in this.Chunks)
				{
					StaticChunk staticChunk = chunk as StaticChunk;
					if (staticChunk != null)
						staticChunk.RefreshImmediately();
					else
						chunk.Refresh();
				}
			}
		}

		private void updateBounds()
		{
			int min = (-this.chunkHalfSize * this.maxChunks) - this.chunkHalfSize;
			int max = (this.chunkHalfSize * this.maxChunks) - this.chunkHalfSize;
			this.minX = this.OffsetX + min;
			this.minY = this.OffsetY + min;
			this.minZ = this.OffsetZ + min;
			this.maxX = this.OffsetX + max;
			this.maxY = this.OffsetY + max;
			this.maxZ = this.OffsetZ + max;
		}

		private struct BoxRelationship
		{
			public Box A;
			public Box B;
		}

		private static Updater spawner;
		private class SpawnGroup
		{
			public List<List<Box>> Islands;
			public Map Source;
			public Action<List<DynamicMap>> Callback;
		}
		private static List<SpawnGroup> spawns = new List<SpawnGroup>();

		public override void InitializeProperties()
		{
			this.updateBounds();

			if (Map.workThread == null)
			{
				Map.workThread = new Thread(new ThreadStart(Map.worker));
				Map.workThread.Start();
				this.main.Exiting += delegate(object a, EventArgs b)
				{
					Map.workThread.Abort();
				};
				Main m = this.main;
				Map.spawner = new Updater
				{
					delegate(float dt)
					{
						DynamicMapFactory factory = Factory.Get<DynamicMapFactory>();
						BlockFactory blockFactory = Factory.Get<BlockFactory>();
						List<SpawnGroup> spawns = null;
						lock (Map.spawns)
						{
							spawns = Map.spawns.ToList();
							Map.spawns.Clear();
						}
						foreach (SpawnGroup spawn in spawns)
						{
							List<DynamicMap> spawnedMaps = new List<DynamicMap>();
							foreach (List<Box> island in spawn.Islands)
							{
								Box firstBox = island.First();
								if (island.Count == 1 && firstBox.Width * firstBox.Height * firstBox.Depth == 1)
								{
									// Just create a temporary physics block instead of a full-blown map
									Coordinate coord = new Coordinate { X = firstBox.X, Y = firstBox.Y, Z = firstBox.Z };
									Entity block = blockFactory.CreateAndBind(main);
									block.Get<Transform>().Matrix.Value = this.Transform;
									block.Get<Transform>().Position.Value = this.GetAbsolutePosition(coord);
									firstBox.Type.ApplyToBlock(block);
									block.Get<ModelInstance>().GetVector3Parameter("Offset").Value = this.GetRelativePosition(coord);
									main.Add(block);
								}
								else
								{
									Entity newMap = factory.CreateAndBind(spawn.Source.main, firstBox.X, firstBox.Y, firstBox.Z);
									newMap.Get<Transform>().Matrix.Value = spawn.Source.Transform;
									DynamicMap newMapComponent = newMap.Get<DynamicMap>();
									newMapComponent.Offset.Value = spawn.Source.Offset;
									newMapComponent.BuildFromBoxes(island);
									spawn.Source.notifyEmptied(island.SelectMany(x => x.GetCoords()), newMapComponent);
									newMapComponent.notifyFilled(island.SelectMany(x => x.GetCoords()), spawn.Source);
									newMapComponent.Transform.Reset();
									if (spawn.Source is DynamicMap)
										newMapComponent.IsAffectedByGravity.Value = ((DynamicMap)spawn.Source).IsAffectedByGravity;
									spawn.Source.main.Add(newMap);
									spawnedMaps.Add(newMapComponent);
								}
							}
							if (spawn.Callback != null)
								spawn.Callback(spawnedMaps);
						}
					}
				};
				Map.spawner.EnabledInEditMode.Value = true;
				Map.spawner.EnabledWhenPaused.Value = true;
				this.main.AddComponent(Map.spawner);
			}

			this.Data.Get = delegate()
			{
				List<int> result = new List<int>();
				lock (this.mutationLock)
				{
					List<Box> boxes = this.Chunks.Where(x => x.Data != null).SelectMany(x => x.Boxes).ToList();
					bool[] modifications = this.simplify(boxes);
					this.simplify(boxes, modifications);
					this.applyChanges(boxes, modifications);

					boxes = this.Chunks.SelectMany(x => x.Boxes).ToList();

					result.Add(boxes.Count);

					Dictionary<Box, int> indexLookup = new Dictionary<Box, int>();

					int index = 0;
					foreach (Box box in boxes)
					{
						result.Add(box.X);
						result.Add(box.Y);
						result.Add(box.Z);
						result.Add(box.Width);
						result.Add(box.Height);
						result.Add(box.Depth);
						result.Add(box.Type.ID);
						for (int i = 0; i < 6; i++)
						{
							Surface surface = box.Surfaces[i];
							result.Add(surface.MinU);
							result.Add(surface.MinV);
							result.Add(surface.MaxU);
							result.Add(surface.MaxV);
						}
						indexLookup.Add(box, index);
						index++;
					}

					Dictionary<BoxRelationship, bool> relationships = new Dictionary<BoxRelationship, bool>();
					index = 0;
					foreach (Box box in boxes)
					{
						if (box.Adjacent == null)
							continue;
						foreach (Box adjacent in box.Adjacent)
						{
							if (box.Type.Permanent && adjacent.Type.Permanent)
								continue;

							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								result.Add(index);
								result.Add(indexLookup[adjacent]);
							}
						}
						index++;
					}
				}

				return Map.serializeData(result.ToArray());
			};

			this.Data.Set = delegate(string value)
			{
				int[] data = Map.deserializeData(value);

				int boxCount = data[0];

				Box[] boxes = new Box[boxCount];

				const int boxDataSize = 31;

				for (int i = 0; i < boxCount; i++)
				{
					// Format:
					// x
					// y
					// z
					// width
					// height
					// depth
					// type
					// MinU, MinV, MaxU, MaxV for each of six surfaces
					int index = 1 + (i * boxDataSize);
					if (data[index + 6] != 0)
					{
						CellState state = WorldFactory.States[data[index + 6]];
						int x = data[index], y = data[index + 1], z = data[index + 2], w = data[index + 3], h = data[index + 4], d = data[index + 5];
						int chunkX = this.minX + ((x - this.minX) / this.chunkSize) * this.chunkSize, chunkY = this.minY + ((y - this.minY) / this.chunkSize) * this.chunkSize, chunkZ = this.minZ + ((z - this.minZ) / this.chunkSize) * this.chunkSize;
						int nextChunkX = this.minX + ((x + w - this.minX) / this.chunkSize) * this.chunkSize, nextChunkY = this.minY + ((y + h - this.minY) / this.chunkSize) * this.chunkSize, nextChunkZ = this.minZ + ((z + d - this.minZ) / this.chunkSize) * this.chunkSize;
						for (int ix = chunkX; ix <= nextChunkX; ix += this.chunkSize)
						{
							for (int iy = chunkY; iy <= nextChunkY; iy += this.chunkSize)
							{
								for (int iz = chunkZ; iz <= nextChunkZ; iz += this.chunkSize)
								{
									int bx = Math.Max(ix, x), by = Math.Max(iy, y), bz = Math.Max(iz, z);
									Box box = new Box
									{
										X = bx,
										Y = by,
										Z = bz,
										Width = Math.Min(x + w, ix + this.chunkSize) - bx,
										Height = Math.Min(y + h, iy + this.chunkSize) - by,
										Depth = Math.Min(z + d, iz + this.chunkSize) - bz,
										Type = state,
										Active = true,
									};
									if (box.Width > 0 && box.Height > 0 && box.Depth > 0)
									{
										boxes[i] = box;
										Chunk chunk = this.GetChunk(bx, by, bz);
										if (chunk.DataBoxes == null)
											chunk.DataBoxes = new List<Box>();
										chunk.DataBoxes.Add(box);
										box.Chunk = chunk;
										for (int x1 = box.X - chunk.X; x1 < box.X + box.Width - chunk.X; x1++)
										{
											for (int y1 = box.Y - chunk.Y; y1 < box.Y + box.Height - chunk.Y; y1++)
											{
												for (int z1 = box.Z - chunk.Z; z1 < box.Z + box.Depth - chunk.Z; z1++)
													chunk.Data[x1, y1, z1] = box;
											}
										}
										box.Surfaces[(int)Direction.NegativeX].W = box.X;
										box.Surfaces[(int)Direction.PositiveX].W = box.X + box.Width;
										box.Surfaces[(int)Direction.NegativeY].W = box.Y;
										box.Surfaces[(int)Direction.PositiveY].W = box.Y + box.Height;
										box.Surfaces[(int)Direction.NegativeZ].W = box.Z;
										box.Surfaces[(int)Direction.PositiveZ].W = box.Z + box.Depth;
										for (int j = 0; j < 6; j++)
										{
											int baseIndex = index + (j * 4) + 7;
											Surface surface = box.Surfaces[j];
											surface.MinU = data[baseIndex + 0];
											surface.MinV = data[baseIndex + 1];
											surface.MaxU = data[baseIndex + 2];
											surface.MaxV = data[baseIndex + 3];
										}
									}
								}
							}
						}
					}
				}

				for (int i = 1 + (boxCount * boxDataSize); i < data.Length; i += 2)
				{
					Box box1 = boxes[data[i]], box2 = boxes[data[i + 1]];
					if (box1 != null && box2 != null)
					{
						box1.Adjacent.Add(box2);
						box2.Adjacent.Add(box1);
					}
				}

				this.postDeserialization();
			};
			Map.Maps.Add(this);
		}

		public IEnumerable<Chunk> GetChunksBetween(Map.Coordinate a, Map.Coordinate b)
		{
			a.X = Math.Max(this.minX, a.X);
			b.X = Math.Min(this.maxX - 1, b.X);
			a.Y = Math.Max(this.minY, a.Y);
			b.Y = Math.Min(this.maxY - 1, b.Y);
			a.Z = Math.Max(this.minX, a.Z);
			b.Z = Math.Min(this.maxX - 1, b.Z);
			if (b.X > a.X && b.Y > a.Y && b.Z > a.Z)
			{
				int chunkX = ((a.X - this.minX) / this.chunkSize), chunkY = ((a.Y - this.minY) / this.chunkSize), chunkZ = ((a.Z - this.minZ) / this.chunkSize);
				int nextChunkX = ((b.X - this.minX) / this.chunkSize), nextChunkY = ((b.Y - this.minY) / this.chunkSize), nextChunkZ = ((b.Z - this.minZ) / this.chunkSize);
				int numChunks = this.chunks.GetLength(0); // Same number of chunks in each dimension
				for (int ix = chunkX; ix <= nextChunkX; ix++)
				{
					for (int iy = chunkY; iy <= nextChunkY; iy++)
					{
						for (int iz = chunkZ; iz <= nextChunkZ; iz++)
						{
							Chunk chunk = this.chunks[ix, iy, iz];
							if (chunk != null)
								yield return chunk;
						}
					}
				}
			}
		}

		protected void postDeserialization()
		{
			foreach (Chunk c in this.Chunks)
				c.Instantiate();
			this.updatePhysics();
		}

		protected static string serializeData(int[] data)
		{
			byte[] result = new byte[data.Length * 4];
			for (int i = 0; i < data.Length; i++)
			{
				int value = data[i];
				int j = i * 4;
				result[j] = (byte)(value >> 24);
				result[j + 1] = (byte)(value >> 16);
				result[j + 2] = (byte)(value >> 8);
				result[j + 3] = (byte)value;
			}
			return System.Convert.ToBase64String(result);
		}

		protected static int[] deserializeData(string data)
		{
			byte[] temp = System.Convert.FromBase64String(data);
			int[] result = new int[temp.Length / 4];
			for (int i = 0; i < result.Length; i++)
			{
				int j = i * 4;
				result[i] = (temp[j] << 24)
					| (temp[j + 1] << 16)
					| (temp[j + 2] << 8)
					| temp[j + 3];
			}
			return result;
		}

		public Direction GetRelativeDirection(Direction dir)
		{
			return this.GetRelativeDirection(dir.GetVector());
		}

		public Direction GetRelativeDirection(Vector3 vector)
		{
			return DirectionExtensions.GetDirectionFromVector(this.GetRelativeVector(vector));
		}

		public Direction GetAbsoluteDirection(Direction dir)
		{
			return DirectionExtensions.GetDirectionFromVector(this.GetAbsoluteVector(dir.GetVector()));
		}

		public Vector3 GetRelativeVector(Vector3 vector)
		{
			return Vector3.TransformNormal(vector, Matrix.Invert(this.Transform));
		}

		public Vector3 GetAbsoluteVector(Vector3 vector)
		{
			return Vector3.TransformNormal(vector, this.Transform);
		}

		protected override void delete()
		{
			base.delete();
			lock (this.mutationLock)
			{
				foreach (Chunk chunk in this.Chunks)
					chunk.Delete();
				this.Chunks.Clear();

				for (int i = 0; i < this.maxChunks; i++)
				{
					for (int j = 0; j < this.maxChunks; j++)
					{
						for (int k = 0; k < this.maxChunks; k++)
							this.chunks[i, j, k] = null;
					}
				}

				Queue<Chunk[, ,]> chunkQueue;
				if (!Map.freeChunkArrays.TryGetValue(this.maxChunks, out chunkQueue))
				{
					chunkQueue = new Queue<Chunk[, ,]>();
					Map.freeChunkArrays[this.maxChunks] = chunkQueue;
				}
				chunkQueue.Enqueue(this.chunks);
			}
			Map.Maps.Remove(this);
		}

		public Chunk GetChunk(Coordinate coord, bool createIfNonExistent = true)
		{
			return this.GetChunk(coord.X, coord.Y, coord.Z, createIfNonExistent);
		}

		public Chunk GetChunk(int x, int y, int z, bool createIfNonExistent = true)
		{
			while (x < this.minX || x >= this.maxX || y < this.minY || y >= this.maxY || z < this.minZ || z >= this.maxZ)
			{
				if (createIfNonExistent)
				{
					int originalChunkArraySize = this.maxChunks;
					int oldMin = this.maxChunks / -2, oldMax = this.maxChunks / 2;
					this.maxChunks *= 2;
					int newMin = this.maxChunks / -2;

					Queue<Chunk[, ,]> chunkQueue;
					Map.freeChunkArrays.TryGetValue(this.maxChunks, out chunkQueue);

					Chunk[, ,] newChunks;
					if (chunkQueue != null && chunkQueue.Count > 0)
						newChunks = chunkQueue.Dequeue();
					else
						newChunks = new Chunk[this.maxChunks, this.maxChunks, this.maxChunks];

					for (int i = oldMin; i < oldMax; i++)
					{
						for (int j = oldMin; j < oldMax; j++)
						{
							for (int k = oldMin; k < oldMax; k++)
							{
								int i2 = i - oldMin, j2 = j - oldMin, k2 = k - oldMin;
								newChunks[i - newMin, j - newMin, k - newMin] = this.chunks[i2, j2, k2];
								this.chunks[i2, j2, k2] = null;
							}
						}
					}

					if (!Map.freeChunkArrays.TryGetValue(originalChunkArraySize, out chunkQueue))
					{
						chunkQueue = new Queue<Chunk[, ,]>();
						Map.freeChunkArrays[originalChunkArraySize] = chunkQueue;
					}
					chunkQueue.Enqueue(this.chunks);

					this.chunks = newChunks;
					this.updateBounds();
				}
				else
					return null;
			}

			int ix = (x - this.minX) / this.chunkSize, iy = (y - this.minY) / this.chunkSize, iz = (z - this.minZ) / this.chunkSize;
			Chunk chunk = this.chunks[ix, iy, iz];
			if (createIfNonExistent && chunk == null)
			{
				chunk = this.newChunk();
				chunk.Map = this;
				chunk.X = this.minX + (ix * this.chunkSize);
				chunk.Y = this.minY + (iy * this.chunkSize);
				chunk.Z = this.minZ + (iz * this.chunkSize);
				Queue<Box[, ,]> queue;
				if (Map.freeDataChunks.TryGetValue(this.chunkSize, out queue) && queue.Count > 0)
					chunk.Data = queue.Dequeue();
				else
					chunk.Data = new Box[this.chunkSize, this.chunkSize, this.chunkSize];
				chunk.IndexX = ix;
				chunk.IndexY = iy;
				chunk.IndexZ = iz;
				chunk.RelativeBoundingBox = new BoundingBox(new Vector3(chunk.X, chunk.Y, chunk.Z), new Vector3(chunk.X + this.chunkSize, chunk.Y + this.chunkSize, chunk.Z + this.chunkSize));
				this.chunks[ix, iy, iz] = chunk;
				this.Chunks.Add(chunk);
			}
			return chunk;
		}

		protected virtual Chunk newChunk()
		{
			Chunk chunk = !this.main.EditorEnabled && this.EnablePhysics ? new StaticChunk() : new Chunk();
			chunk.Map = this;
			return chunk;
		}

		public bool Contains(Coordinate coord)
		{
			return coord.X >= this.minX && coord.X < this.maxX
				&& coord.Y >= this.minY && coord.Y < this.maxY
				&& coord.Z >= this.minZ && coord.Z < this.maxZ;
		}

		public bool Fill(Vector3 pos, CellState state)
		{
			return this.Fill(this.GetCoordinate(pos), state);
		}

		public bool Fill(Coordinate start, Coordinate end, CellState state)
		{
			bool changed = false;
			for (int x = start.X; x < end.X; x++)
			{
				for (int y = start.Y; y < end.Y; y++)
				{
					for (int z = start.Z; z < end.Z; z++)
					{
						changed |= this.Fill(x, y, z, state);
					}
				}
			}
			return changed;
		}

		public bool Fill(Coordinate coord, CellState state)
		{
			return this.Fill(coord.X, coord.Y, coord.Z, state);
		}

		public bool Empty(Vector3 pos, Map transferringToNewMap = null)
		{
			return this.Empty(this.GetCoordinate(pos), transferringToNewMap);
		}

		public bool Empty(Coordinate coord, Map transferringToNewMap = null)
		{
			return this.Empty(coord.X, coord.Y, coord.Z, transferringToNewMap);
		}

		public bool Empty(Coordinate a, Coordinate b, Map transferringToNewMap = null)
		{
			int minY = Math.Min(a.Y, b.Y);
			int minZ = Math.Min(a.Z, b.Z);
			int maxX = Math.Max(a.X, b.X);
			int maxY = Math.Max(a.Y, b.Y);
			int maxZ = Math.Max(a.Z, b.Z);
			List<Map.Coordinate> coords = new List<Coordinate>();
			for (int x = Math.Min(a.X, b.X); x < maxX; x++)
			{
				for (int y = minY; y < maxY; y++)
				{
					for (int z = minZ; z < maxZ; z++)
					{
						coords.Add(new Map.Coordinate { X = x, Y = y, Z = z });
					}
				}
			}
			return this.Empty(coords, transferringToNewMap);
		}

		/// <summary>
		/// Fills the specified location. This change will not take effect until Generate() or Regenerate() is called.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public bool Fill(int x, int y, int z, CellState state)
		{
			if (state.ID == 0 || (!this.main.EditorEnabled && !this.EnablePhysics)) // 0 = empty
				return false;

			bool filled = false;
			lock (this.mutationLock)
			{
				Chunk chunk = this.GetChunk(x, y, z);
				if (chunk != null)
				{
					if (chunk.Data[x - chunk.X, y - chunk.Y, z - chunk.Z] == null)
					{
						this.addBox(new Box { Type = state, X = x, Y = y, Z = z, Depth = 1, Height = 1, Width = 1 });
						filled = true;
					}
				}
			}
			if (filled)
				this.notifyFilled(new Coordinate[] { new Coordinate { X = x, Y = y, Z = z, Data = state } }, null);
			return filled;
		}

		private void notifyFilled(IEnumerable<Coordinate> coords, Map transferredFromMap)
		{
			this.CellsFilled.Execute(coords, transferredFromMap);
			Map.GlobalCellsFilled.Execute(this, coords, transferredFromMap);
		}
		
		private void notifyEmptied(IEnumerable<Coordinate> coords, Map transferringToNewMap)
		{
			this.CellsEmptied.Execute(coords, transferringToNewMap);
			Map.GlobalCellsEmptied.Execute(this, coords, transferringToNewMap);

			bool completelyEmptied = true;
			if (this.additions.FirstOrDefault(x => x.Active) != null)
				completelyEmptied = false;
			else
			{
				foreach (Chunk chunk in this.Chunks)
				{
					foreach (Box box in chunk.Boxes)
					{
						if (box.Active)
						{
							completelyEmptied = false;
							break;
						}
					}
					if (!completelyEmptied)
						break;
				}
			}

			if (completelyEmptied)
				this.CompletelyEmptied.Execute();
		}

		public bool Empty(IEnumerable<Coordinate> coords, Map transferringToNewMap = null)
		{
			bool modified = false;
			List<Box> boxAdditions = new List<Box>();
			List<Coordinate> removed = new List<Coordinate>();
			lock (this.mutationLock)
			{
				foreach (Map.Coordinate coord in coords)
				{
					Chunk chunk = this.GetChunk(coord.X, coord.Y, coord.Z, false);

					if (chunk == null || (!this.main.EditorEnabled && !this.EnablePhysics))
						continue;

					Box box = chunk.Data[coord.X - chunk.X, coord.Y - chunk.Y, coord.Z - chunk.Z];
					if (box != null && (!box.Type.Permanent || this.main.EditorEnabled))
					{
						this.removalCoords.Add(coord);
						if (box != null)
						{
							this.removeBox(box);

							// Left
							if (coord.X > box.X)
							{
								Box newBox = new Box
								{
									X = box.X,
									Y = box.Y,
									Z = box.Z,
									Width = coord.X - box.X,
									Height = box.Height,
									Depth = box.Depth,
									Type = box.Type,
								};
								this.addBoxWithoutAdjacency(newBox);
								boxAdditions.Add(newBox);
							}

							// Right
							if (box.X + box.Width > coord.X + 1)
							{
								Box newBox = new Box
								{
									X = coord.X + 1,
									Y = box.Y,
									Z = box.Z,
									Width = box.X + box.Width - (coord.X + 1),
									Height = box.Height,
									Depth = box.Depth,
									Type = box.Type,
								};
								this.addBoxWithoutAdjacency(newBox);
								boxAdditions.Add(newBox);
							}

							// Bottom
							if (coord.Y > box.Y)
							{
								Box newBox = new Box
								{
									X = coord.X,
									Y = box.Y,
									Z = box.Z,
									Width = 1,
									Height = coord.Y - box.Y,
									Depth = box.Depth,
									Type = box.Type,
								};
								this.addBoxWithoutAdjacency(newBox);
								boxAdditions.Add(newBox);
							}

							// Top
							if (box.Y + box.Height > coord.Y + 1)
							{
								Box newBox = new Box
								{
									X = coord.X,
									Y = coord.Y + 1,
									Z = box.Z,
									Width = 1,
									Height = box.Y + box.Height - (coord.Y + 1),
									Depth = box.Depth,
									Type = box.Type,
								};
								this.addBoxWithoutAdjacency(newBox);
								boxAdditions.Add(newBox);
							}

							// Back
							if (coord.Z > box.Z)
							{
								Box newBox = new Box
								{
									X = coord.X,
									Y = coord.Y,
									Z = box.Z,
									Width = 1,
									Height = 1,
									Depth = coord.Z - box.Z,
									Type = box.Type,
								};
								this.addBoxWithoutAdjacency(newBox);
								boxAdditions.Add(newBox);
							}

							// Front
							if (box.Z + box.Depth > coord.Z + 1)
							{
								Box newBox = new Box
								{
									X = coord.X,
									Y = coord.Y,
									Z = coord.Z + 1,
									Width = 1,
									Height = 1,
									Depth = box.Z + box.Depth - (coord.Z + 1),
									Type = box.Type,
								};
								this.addBoxWithoutAdjacency(newBox);
								boxAdditions.Add(newBox);
							}

							removed.Add(new Map.Coordinate { X = coord.X, Y = coord.Y, Z = coord.Z, Data = box.Type });
							modified = true;
						}
					}
				}
				this.calculateAdjacency(boxAdditions.Where(x => x.Active));
			}

			if (modified)
				this.notifyEmptied(removed, transferringToNewMap);

			return modified;
		}

		/// <summary>
		/// If the specified location is currently filled, it is emptied.
		/// This change will not take effect until Generate() or Regenerate() is called.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public bool Empty(int x, int y, int z, Map transferringToNewMap = null)
		{
			bool modified = false;
			Map.Coordinate coord = new Coordinate { X = x, Y = y, Z = z, };
			lock (this.mutationLock)
			{
				Chunk chunk = this.GetChunk(x, y, z, false);

				if (chunk == null || (!this.main.EditorEnabled && !this.EnablePhysics))
					return false;

				Box box = chunk.Data[x - chunk.X, y - chunk.Y, z - chunk.Z];
				if (box != null && (!box.Type.Permanent || this.main.EditorEnabled))
				{
					List<Box> boxAdditions = new List<Box>();
					coord.Data = box.Type;
					this.removalCoords.Add(coord);
					this.removeBox(box);

					// Left
					if (coord.X > box.X)
					{
						Box newBox = new Box
						{
							X = box.X,
							Y = box.Y,
							Z = box.Z,
							Width = coord.X - box.X,
							Height = box.Height,
							Depth = box.Depth,
							Type = box.Type,
						};
						this.addBoxWithoutAdjacency(newBox);
						boxAdditions.Add(newBox);
					}

					// Right
					if (box.X + box.Width > coord.X + 1)
					{
						Box newBox = new Box
						{
							X = coord.X + 1,
							Y = box.Y,
							Z = box.Z,
							Width = box.X + box.Width - (coord.X + 1),
							Height = box.Height,
							Depth = box.Depth,
							Type = box.Type,
						};
						this.addBoxWithoutAdjacency(newBox);
						boxAdditions.Add(newBox);
					}

					// Bottom
					if (coord.Y > box.Y)
					{
						Box newBox = new Box
						{
							X = coord.X,
							Y = box.Y,
							Z = box.Z,
							Width = 1,
							Height = coord.Y - box.Y,
							Depth = box.Depth,
							Type = box.Type,
						};
						this.addBoxWithoutAdjacency(newBox);
						boxAdditions.Add(newBox);
					}

					// Top
					if (box.Y + box.Height > coord.Y + 1)
					{
						Box newBox = new Box
						{
							X = coord.X,
							Y = coord.Y + 1,
							Z = box.Z,
							Width = 1,
							Height = box.Y + box.Height - (coord.Y + 1),
							Depth = box.Depth,
							Type = box.Type,
						};
						this.addBoxWithoutAdjacency(newBox);
						boxAdditions.Add(newBox);
					}

					// Back
					if (coord.Z > box.Z)
					{
						Box newBox = new Box
						{
							X = coord.X,
							Y = coord.Y,
							Z = box.Z,
							Width = 1,
							Height = 1,
							Depth = coord.Z - box.Z,
							Type = box.Type,
						};
						this.addBoxWithoutAdjacency(newBox);
						boxAdditions.Add(newBox);
					}

					// Front
					if (box.Z + box.Depth > coord.Z + 1)
					{
						Box newBox = new Box
						{
							X = coord.X,
							Y = coord.Y,
							Z = coord.Z + 1,
							Width = 1,
							Height = 1,
							Depth = box.Z + box.Depth - (coord.Z + 1),
							Type = box.Type,
						};
						this.addBoxWithoutAdjacency(newBox);
						boxAdditions.Add(newBox);
					}
					modified = true;
					this.calculateAdjacency(boxAdditions.Where(a => a.Active));
				}
			}

			if (modified)
				this.notifyEmptied(new Coordinate[] { coord }, transferringToNewMap);

			return modified;
		}

		protected void addBoxWithoutAdjacency(Box box)
		{
			Chunk chunk = this.GetChunk(box.X, box.Y, box.Z);
			chunk.MarkDirty(box);

			box.Chunk = chunk;

			for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
			{
				for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
				{
					for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
					{
						chunk.Data[x, y, z] = box;
					}
				}
			}
		}

		protected void addBox(Box box)
		{
			this.addBoxWithoutAdjacency(box);

			this.additions.Add(box);

			Dictionary<Box, bool> adjacents = new Dictionary<Box, bool>();

			// Front face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(x, y, box.Z + box.Depth);
					if (adjacent != null)
					{
						if (!adjacents.ContainsKey(adjacent))
						{
							adjacents[adjacent] = true;
							box.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(box);
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Back face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(x, y, box.Z - 1);
					if (adjacent != null)
					{
						if (!adjacents.ContainsKey(adjacent))
						{
							adjacents[adjacent] = true;
							box.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(box);
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Right face
			for (int z = box.Z; z < box.Z + box.Depth; z++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(box.X + box.Width, y, z);
					if (adjacent != null)
					{
						if (!adjacents.ContainsKey(adjacent))
						{
							adjacents[adjacent] = true;
							box.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(box);
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Left face
			for (int z = box.Z; z < box.Z + box.Depth; z++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(box.X - 1, y, z);
					if (adjacent != null)
					{
						if (!adjacents.ContainsKey(adjacent))
						{
							adjacents[adjacent] = true;
							box.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(box);
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Top face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int z = box.Z; z < box.Z + box.Depth; )
				{
					Box adjacent = this.GetBox(x, box.Y + box.Height, z);
					if (adjacent != null)
					{
						if (!adjacents.ContainsKey(adjacent))
						{
							adjacents[adjacent] = true;
							box.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(box);
						}
						z = adjacent.Z + adjacent.Depth;
					}
					else
						z++;
				}
			}

			// Bottom face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int z = box.Z; z < box.Z + box.Depth; )
				{
					Box adjacent = this.GetBox(x, box.Y - 1, z);
					if (adjacent != null)
					{
						if (!adjacents.ContainsKey(adjacent))
						{
							adjacents[adjacent] = true;
							box.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(box);
						}
						z = adjacent.Z + adjacent.Depth;
					}
					else
						z++;
				}
			}
		}

		protected IEnumerable<Box> getAdjacentBoxes(Box box)
		{
			Dictionary<Box, bool> relationships = new Dictionary<Box, bool>();

			// Front face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(x, y, box.Z + box.Depth);
					if (adjacent != null)
					{
						if (!relationships.ContainsKey(adjacent))
						{
							relationships[adjacent] = true;
							yield return adjacent;
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Back face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(x, y, box.Z - 1);
					if (adjacent != null)
					{
						if (!relationships.ContainsKey(adjacent))
						{
							relationships[adjacent] = true;
							yield return adjacent;
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Right face
			for (int z = box.Z; z < box.Z + box.Depth; z++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(box.X + box.Width, y, z);
					if (adjacent != null)
					{
						if (!relationships.ContainsKey(adjacent))
						{
							relationships[adjacent] = true;
							yield return adjacent;
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Left face
			for (int z = box.Z; z < box.Z + box.Depth; z++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(box.X - 1, y, z);
					if (adjacent != null)
					{
						if (!relationships.ContainsKey(adjacent))
						{
							relationships[adjacent] = true;
							yield return adjacent;
						}
						y = adjacent.Y + adjacent.Height;
					}
					else
						y++;
				}
			}

			// Top face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int z = box.Z; z < box.Z + box.Depth; )
				{
					Box adjacent = this.GetBox(x, box.Y + box.Height, z);
					if (adjacent != null)
					{
						if (!relationships.ContainsKey(adjacent))
						{
							relationships[adjacent] = true;
							yield return adjacent;
						}
						z = adjacent.Z + adjacent.Depth;
					}
					else
						z++;
				}
			}

			// Bottom face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int z = box.Z; z < box.Z + box.Depth; )
				{
					Box adjacent = this.GetBox(x, box.Y - 1, z);
					if (adjacent != null)
					{
						if (!relationships.ContainsKey(adjacent))
						{
							relationships[adjacent] = true;
							yield return adjacent;
						}
						z = adjacent.Z + adjacent.Depth;
					}
					else
						z++;
				}
			}
		}

		protected void calculateAdjacency(IEnumerable<Box> boxes)
		{
			foreach (Box box in boxes)
				box.Adjacent = new List<Box>();

			Dictionary<BoxRelationship, bool> relationships = new Dictionary<BoxRelationship, bool>();

			foreach (Box box in boxes)
			{
				this.additions.Add(box);
				// Front face
				for (int x = box.X; x < box.X + box.Width; x++)
				{
					for (int y = box.Y; y < box.Y + box.Height; )
					{
						Box adjacent = this.GetBox(x, y, box.Z + box.Depth);
						if (adjacent != null)
						{
							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								box.Adjacent.Add(adjacent);
								adjacent.Adjacent.Add(box);
							}
							y = adjacent.Y + adjacent.Height;
						}
						else
							y++;
					}
				}

				// Back face
				for (int x = box.X; x < box.X + box.Width; x++)
				{
					for (int y = box.Y; y < box.Y + box.Height; )
					{
						Box adjacent = this.GetBox(x, y, box.Z - 1);
						if (adjacent != null)
						{
							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								box.Adjacent.Add(adjacent);
								adjacent.Adjacent.Add(box);
							}
							y = adjacent.Y + adjacent.Height;
						}
						else
							y++;
					}
				}

				// Right face
				for (int z = box.Z; z < box.Z + box.Depth; z++)
				{
					for (int y = box.Y; y < box.Y + box.Height; )
					{
						Box adjacent = this.GetBox(box.X + box.Width, y, z);
						if (adjacent != null)
						{
							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								box.Adjacent.Add(adjacent);
								adjacent.Adjacent.Add(box);
							}
							y = adjacent.Y + adjacent.Height;
						}
						else
							y++;
					}
				}

				// Left face
				for (int z = box.Z; z < box.Z + box.Depth; z++)
				{
					for (int y = box.Y; y < box.Y + box.Height; )
					{
						Box adjacent = this.GetBox(box.X - 1, y, z);
						if (adjacent != null)
						{
							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								box.Adjacent.Add(adjacent);
								adjacent.Adjacent.Add(box);
							}
							y = adjacent.Y + adjacent.Height;
						}
						else
							y++;
					}
				}

				// Top face
				for (int x = box.X; x < box.X + box.Width; x++)
				{
					for (int z = box.Z; z < box.Z + box.Depth; )
					{
						Box adjacent = this.GetBox(x, box.Y + box.Height, z);
						if (adjacent != null)
						{
							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								box.Adjacent.Add(adjacent);
								adjacent.Adjacent.Add(box);
							}
							z = adjacent.Z + adjacent.Depth;
						}
						else
							z++;
					}
				}

				// Bottom face
				for (int x = box.X; x < box.X + box.Width; x++)
				{
					for (int z = box.Z; z < box.Z + box.Depth; )
					{
						Box adjacent = this.GetBox(x, box.Y - 1, z);
						if (adjacent != null)
						{
							BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
							BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
							if (!relationships.ContainsKey(relationship1) && !relationships.ContainsKey(relationship2))
							{
								relationships[relationship1] = true;
								box.Adjacent.Add(adjacent);
								adjacent.Adjacent.Add(box);
							}
							z = adjacent.Z + adjacent.Depth;
						}
						else
							z++;
					}
				}
			}
		}

		protected bool regenerateSurfaces(Box box, bool firstTime = false)
		{
			bool permanent = box.Type.Permanent;
			if (permanent && !firstTime && box.Added && !main.EditorEnabled)
				return false;
			int x, y, z;
			Surface surface;

			foreach (Direction face in new[] { Direction.PositiveX, Direction.NegativeX })
			{
				surface = box.Surfaces[(int)face];

				surface.MinV = box.Z + box.Depth;
				surface.MaxV = box.Z;
				surface.MinU = box.Y + box.Height;
				surface.MaxU = box.Y;
				if (face == Direction.PositiveX)
					x = surface.W = box.X + box.Width;
				else
				{
					surface.W = box.X;
					x = box.X - 1;
				}
				for (y = box.Y; y < box.Y + box.Height; y++)
				{
					for (z = box.Z; z < box.Z + box.Depth; )
					{
						Box adjacent = this.GetBox(x, y, z);
						if (adjacent == null || adjacent.Type.AllowAlpha || (permanent && !adjacent.Type.Permanent))
						{
							surface.MinV = Math.Min(surface.MinV, z);
							surface.MaxV = Math.Max(surface.MaxV, z + 1);
							surface.MinU = Math.Min(surface.MinU, y);
							surface.MaxU = Math.Max(surface.MaxU, y + 1);
							z++;
						}
						else
							z = adjacent.Z + adjacent.Depth;
					}
				}
				surface.RefreshTransform(box, face);
			}

			foreach (Direction face in new[] { Direction.PositiveY, Direction.NegativeY })
			{
				surface = box.Surfaces[(int)face];
				surface.MinU = box.X + box.Width;
				surface.MaxU = box.X;
				surface.MinV = box.Z + box.Depth;
				surface.MaxV = box.Z;
				if (face == Direction.PositiveY)
					y = surface.W = box.Y + box.Height;
				else
				{
					surface.W = box.Y;
					y = box.Y - 1;
				}
				for (x = box.X; x < box.X + box.Width; x++)
				{
					for (z = box.Z; z < box.Z + box.Depth; )
					{
						Box adjacent = this.GetBox(x, y, z);
						if (adjacent == null || adjacent.Type.AllowAlpha || (permanent && !adjacent.Type.Permanent))
						{
							surface.MinV = Math.Min(surface.MinV, z);
							surface.MaxV = Math.Max(surface.MaxV, z + 1);
							surface.MinU = Math.Min(surface.MinU, x);
							surface.MaxU = Math.Max(surface.MaxU, x + 1);
							z++;
						}
						else
							z = adjacent.Z + adjacent.Depth;
					}
				}
				surface.RefreshTransform(box, face);
			}

			foreach (Direction face in new[] { Direction.PositiveZ, Direction.NegativeZ })
			{
				surface = box.Surfaces[(int)face];
				surface.MinU = box.X + box.Width;
				surface.MaxU = box.X;
				surface.MinV = box.Y + box.Height;
				surface.MaxV = box.Y;
				if (face == Direction.PositiveZ)
					z = surface.W = box.Z + box.Depth;
				else
				{
					surface.W = box.Z;
					z = box.Z - 1;
				}
				for (y = box.Y; y < box.Y + box.Height; y++)
				{
					for (x = box.X; x < box.X + box.Width; )
					{
						Box adjacent = this.GetBox(x, y, z);
						if (adjacent == null || adjacent.Type.AllowAlpha || (permanent && !adjacent.Type.Permanent))
						{
							surface.MinU = Math.Min(surface.MinU, x);
							surface.MaxU = Math.Max(surface.MaxU, x + 1);
							surface.MinV = Math.Min(surface.MinV, y);
							surface.MaxV = Math.Max(surface.MaxV, y + 1);
							x++;
						}
						else
							x = adjacent.X + adjacent.Width;
					}
				}
				surface.RefreshTransform(box, face);
			}

			if (box.Added)
			{
				box.Chunk.MarkDirty(box);
				return true;
			}
			return false;
		}

		protected void removeBox(Box box)
		{
			Chunk chunk = box.Chunk;
			for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
			{
				for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
				{
					for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
					{
						chunk.Data[x, y, z] = null;
					}
				}
			}
			this.removeBoxAdjacency(box);
			box.Active = false;
			chunk.MarkDirty(box);
			this.removals.Add(box);
		}

		protected void removeBoxAdjacency(Box box)
		{
			foreach (Box box2 in box.Adjacent)
				box2.Adjacent.Remove(box);
		}

		public void Regenerate(Action<List<DynamicMap>> callback = null)
		{
			workQueue.Enqueue(new WorkItem { Map = this, Callback = callback });
		}

		private enum StaticChunkAction { Refresh, Activate, Deactivate }

		private class WorkItem
		{
			public Map Map;
			public StaticChunk StaticChunk;
			public StaticChunkAction StaticChunkAction;
			public Action<List<DynamicMap>> Callback;
		}

		private static BlockingQueue<WorkItem> workQueue = new BlockingQueue<WorkItem>(8);

		private static void worker()
		{
			while (true)
			{
				WorkItem item = Map.workQueue.Dequeue();
				if (item.Map != null)
					item.Map.RegenerateImmediately(item.Callback);
				else
				{
					switch (item.StaticChunkAction)
					{
						case StaticChunkAction.Refresh:
							item.StaticChunk.RefreshImmediately();
							break;
						case StaticChunkAction.Activate:
							item.StaticChunk.ActivateImmediately();
							break;
						case StaticChunkAction.Deactivate:
							item.StaticChunk.DeactivateImmediately();
							break;
					}
				}
			}
		}

		private static Thread workThread;

		private object mutationLock = new object();

		/// <summary>
		/// Applies any changes made to the map.
		/// </summary>
		public void RegenerateImmediately(Action<List<DynamicMap>> callback = null)
		{
			List<DynamicMap> spawnedMaps = new List<DynamicMap>();

			if (!this.main.EditorEnabled && !this.EnablePhysics)
				return;

			lock (this.mutationLock)
			{
				if (!this.Active)
					return;

				if (!this.main.EditorEnabled)
				{
					// Spawn new maps for portions that have been cut off

					IEnumerable<IEnumerable<Box>> islands;
					bool foundPermanentBlock;
					this.GetAdjacentIslands(this.removalCoords, out islands, out foundPermanentBlock, null);

					List<List<Box>> finalIslands = new List<List<Box>>();

					foreach (IEnumerable<Box> island in islands)
					{
						finalIslands.Add(island.ToList());

						// Remove these boxes from the map
						foreach (Box adjacent in island)
							this.removeBox(adjacent);
					}

					if (finalIslands.Count > 0)
					{
						lock (Map.spawns)
						{
							Map.spawns.Add(new SpawnGroup
							{
								Source = this,
								Callback = callback,
								Islands = finalIslands
							});
						}
					}
				}
				this.removalCoords.Clear();

				// Figure out which blocks need updating

				// Update graphics
				Dictionary<Box, bool> regenerated = new Dictionary<Box, bool>();

				foreach (Box box in this.removals.Concat(this.additions.Where(x => x.Active)))
				{
					if (box.Active && !regenerated.ContainsKey(box))
						regenerated[box] = this.regenerateSurfaces(box);

					IEnumerable<Box> adjacentBoxes;

					if (box.Type.Permanent) // We probably don't have any adjacency info for it.
						adjacentBoxes = this.getAdjacentBoxes(box);
					else
						adjacentBoxes = box.Adjacent;
					foreach (Box adjacent in adjacentBoxes)
					{
						if (adjacent.Active && !regenerated.ContainsKey(adjacent))
							regenerated[adjacent] = this.regenerateSurfaces(adjacent);
					}
				}
				
				List<Box> boxes = regenerated.Keys.ToList();

				bool[] modifications = regenerated.Values.ToArray();
				this.simplify(boxes, modifications);
				this.simplify(boxes, modifications);

				this.applyChanges(boxes, modifications);
			}
		}

		private void applyChanges(List<Box> boxes, bool[] modifications)
		{
			lock (this.Lock)
			{
				foreach (Box box in this.removals)
				{
					if (box.Added)
						box.Chunk.Boxes.RemoveAt(box.ChunkIndex);
				}

				int i = 0;
				foreach (Box box in boxes)
				{
					if (box.Added)
					{
						if (box.Active)
						{
							if (modifications[i])
								box.Chunk.Boxes.Changed(box.ChunkIndex, box);
						}
						else
							box.Chunk.Boxes.RemoveAt(box.ChunkIndex);
					}
					i++;
				}

				foreach (Box box in this.additions)
				{
					if (box.Active && !box.Added)
						box.Chunk.Boxes.Add(box);
				}
			}

			this.removals.Clear();
			this.additions.Clear();

			this.updatePhysics();
		}

		public void BuildFromBoxes(IEnumerable<Box> boxes)
		{
			List<Box> boxAdditions = new List<Box>();
			foreach (Box source in boxes)
			{
				Chunk baseChunk = this.GetChunk(source.X, source.Y, source.Z);
				Chunk nextChunk = this.GetChunk(source.X + source.Width, source.Y + source.Height, source.Z + source.Depth);
				for (int ix = baseChunk.X; ix <= nextChunk.X; ix += this.chunkSize)
				{
					for (int iy = baseChunk.Y; iy <= nextChunk.Y; iy += this.chunkSize)
					{
						for (int iz = baseChunk.Z; iz <= nextChunk.Z; iz += this.chunkSize)
						{
							int bx = Math.Max(ix, source.X), by = Math.Max(iy, source.Y), bz = Math.Max(iz, source.Z);
							Box box = new Box
							{
								X = bx,
								Y = by,
								Z = bz,
								Width = Math.Min(source.X + source.Width, ix + this.chunkSize) - bx,
								Height = Math.Min(source.Y + source.Height, iy + this.chunkSize) - by,
								Depth = Math.Min(source.Z + source.Depth, iz + this.chunkSize) - bz,
								Type = source.Type,
							};
							if (box.Width > 0 && box.Height > 0 && box.Depth > 0)
							{
								this.addBoxWithoutAdjacency(box);
								boxAdditions.Add(box);
							}
						}
					}
				}
			}
			this.calculateAdjacency(boxAdditions);

			this.RegenerateImmediately(null);
		}

		public List<Box> GetContiguousByType(IEnumerable<Box> input)
		{
			CellState state = input.First().Type;
			Queue<Box> boxes = new Queue<Box>();

			foreach (Box box in input)
				boxes.Enqueue(box);

			List<Box> result = new List<Box>();
			Dictionary<Box, bool> alreadyVisited = new Dictionary<Box, bool>();

			while (boxes.Count > 0)
			{
				Box b = boxes.Dequeue();

				if (b.Type == state)
				{
					result.Add(b);
					IEnumerable<Box> adjacentBoxes;
					if (b.Type.Permanent) // We probably don't have any adjacency info for it.
						adjacentBoxes = this.getAdjacentBoxes(b);
					else
						adjacentBoxes = b.Adjacent;
					foreach (Box adjacent in adjacentBoxes)
					{
						if (!alreadyVisited.ContainsKey(adjacent))
						{
							boxes.Enqueue(adjacent);
							alreadyVisited.Add(adjacent, true);
						}
					}
				}
			}

			return result;
		}

		public void GetAdjacentIslands(IEnumerable<Coordinate> removals, out IEnumerable<IEnumerable<Box>> islands, out bool foundPermanentBlock, IEnumerable<Box> ignore)
		{
			List<Dictionary<Box, bool>> lists = new List<Dictionary<Box, bool>>();

			foundPermanentBlock = false;

			// Build adjacency lists
			foreach (Coordinate removal in removals)
			{
				if (this[removal].ID != 0) // A new block was subsequently filled in after removal. Forget about it.
					continue;

				foreach (Direction dir in DirectionExtensions.Directions)
				{
					Coordinate adjacentCoord = removal.Move(dir);
					Box box = this.GetBox(adjacentCoord);
					if (box == null)
						continue;
					bool alreadyFound = false;
					foreach (Dictionary<Box, bool> list in lists)
					{
						if (list.ContainsKey(box))
						{
							alreadyFound = true;
							break;
						}
					}
					if (alreadyFound)
						continue;
					Dictionary<Box, bool> newList = new Dictionary<Box, bool>();
					if (ignore != null)
					{
						foreach (Box b in ignore)
							newList.Add(b, true);
					}
					bool permanent = this.buildAdjacency(box, newList);
					if (ignore != null)
					{
						foreach (Box b in ignore)
							newList.Remove(b);
					}
					foundPermanentBlock |= permanent;
					if (!permanent && newList.Count > 0)
						lists.Add(newList);
				}
			}

			// Spawn the dynamic maps
			if (foundPermanentBlock)
				islands = lists.Select(x => x.Keys);
			else if (lists.Count > 1)
			{
				IEnumerable<Box> biggestList = null;
				int biggestSize = 0;

				foreach (IEnumerable<Box> list in lists.Select(x => x.Keys))
				{
					int size = list.Sum(x => x.Width * x.Height * x.Depth);
					if (size > biggestSize)
					{
						biggestList = list;
						biggestSize = size;
					}
				}

				islands = lists.Select(x => x.Keys).Except(new[] { biggestList });
			}
			else
				islands = new Box[][] { };
		}

		private bool adjacentToFilledCell(Coordinate coord)
		{
			return this[coord.Move(0, 0, 1)].ID != 0
			|| this[coord.Move(0, 1, 0)].ID != 0
			|| this[coord.Move(0, 1, 1)].ID != 0
			|| this[coord.Move(1, 0, 0)].ID != 0
			|| this[coord.Move(1, 0, 1)].ID != 0
			|| this[coord.Move(1, 1, 0)].ID != 0
			|| this[coord.Move(1, 1, 1)].ID != 0
			|| this[coord.Move(0, 0, -1)].ID != 0
			|| this[coord.Move(0, -1, 0)].ID != 0
			|| this[coord.Move(0, -1, -1)].ID != 0
			|| this[coord.Move(-1, 0, 0)].ID != 0
			|| this[coord.Move(-1, 0, 1)].ID != 0
			|| this[coord.Move(-1, -1, 0)].ID != 0
			|| this[coord.Move(-1, -1, -1)].ID != 0;
		}

		public Coordinate? FindClosestAStarCell(Coordinate coord, int maxDistance = 20)
		{
			Map.CellState s = this[coord];
			if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
				return coord;

			Vector3 pos = this.GetRelativePosition(coord);

			Coordinate? closestCoord = null;

			for (int radius = 1; radius < maxDistance; radius++)
			{
				float closestDistance = float.MaxValue;

				// Left
				for (int y = -radius; y <= radius; y++)
				{
					for (int z = -radius; z <= radius; z++)
					{
						Coordinate c = coord.Move(-radius, y, z);
						s = this[c];
						if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Right
				for (int y = -radius; y <= radius; y++)
				{
					for (int z = -radius; z <= radius; z++)
					{
						Coordinate c = coord.Move(radius, y, z);
						s = this[c];
						if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Bottom
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int z = -radius + 1; z < radius; z++)
					{
						Coordinate c = coord.Move(x, -radius, z);
						s = this[c];
						if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Top
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int z = -radius + 1; z < radius; z++)
					{
						Coordinate c = coord.Move(x, radius, z);
						s = this[c];
						if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Backward
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int y = -radius; y <= radius; y++)
					{
						Coordinate c = coord.Move(x, y, -radius);
						s = this[c];
						if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Forward
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int y = -radius; y <= radius; y++)
					{
						Coordinate c = coord.Move(x, y, radius);
						s = this[c];
						if ((s.ID != 0 || this.adjacentToFilledCell(coord)) && !s.Permanent)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				if (closestCoord.HasValue)
					break;
			}
			return closestCoord;
		}

		public Coordinate? FindClosestFilledCell(Coordinate coord, int maxDistance = 20)
		{
			if (this[coord].ID != 0)
				return coord;

			Vector3 pos = this.GetRelativePosition(coord);

			Coordinate? closestCoord = null;

			for (int radius = 1; radius < maxDistance; radius++)
			{
				float closestDistance = float.MaxValue;
				
				// Left
				for (int y = -radius; y <= radius; y++)
				{
					for (int z = -radius; z <= radius; z++)
					{
						Coordinate c = coord.Move(-radius, y, z);
						if (this[c].ID != 0)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Right
				for (int y = -radius; y <= radius; y++)
				{
					for (int z = -radius; z <= radius; z++)
					{
						Coordinate c = coord.Move(radius, y, z);
						if (this[c].ID != 0)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Bottom
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int z = -radius + 1; z < radius; z++)
					{
						Coordinate c = coord.Move(x, -radius, z);
						if (this[c].ID != 0)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Top
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int z = -radius + 1; z < radius; z++)
					{
						Coordinate c = coord.Move(x, radius, z);
						if (this[c].ID != 0)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Backward
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int y = -radius; y <= radius; y++)
					{
						Coordinate c = coord.Move(x, y, -radius);
						if (this[c].ID != 0)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				// Forward
				for (int x = -radius + 1; x < radius; x++)
				{
					for (int y = -radius; y <= radius; y++)
					{
						Coordinate c = coord.Move(x, y, radius);
						if (this[c].ID != 0)
						{
							float distance = (this.GetRelativePosition(c) - pos).LengthSquared();
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestCoord = c;
							}
						}
					}
				}

				if (closestCoord.HasValue)
					break;
			}
			return closestCoord;
		}

		private class AStarEntry
		{
			public AStarEntry Parent;
			public float SoFar;
			public float ToGoal;
			public Coordinate Coordinate;
		}

		private List<Coordinate> constructPath(AStarEntry entry)
		{
			List<Coordinate> result = new List<Coordinate>();
			result.Add(entry.Coordinate);
			while (entry.Parent != null)
			{
				entry = entry.Parent;
				result.Insert(0, entry.Coordinate);
			}
			return result;
		}

		// This isn't really A* at all. But whatevs.
		public List<Coordinate> CustomAStar(Coordinate start, Coordinate end, int iterationLimit = 200)
		{
			Dictionary<Coordinate, bool> closed = new Dictionary<Coordinate, bool>();
			Dictionary<Coordinate, AStarEntry> queueReverseLookup = new Dictionary<Coordinate, AStarEntry>();

			Coordinate? closestStart = this.FindClosestAStarCell(start, 10);
			Coordinate? closestEnd = this.FindClosestFilledCell(end);

			if (!closestStart.HasValue || !closestEnd.HasValue)
				return null;
			else
			{
				start = closestStart.Value;
				end = closestEnd.Value;
			}

			Vector3 endPos = this.GetRelativePosition(end);

			PriorityQueue<AStarEntry> queue = new PriorityQueue<AStarEntry>(new LambdaComparer<AStarEntry>((x, y) => x.ToGoal.CompareTo(y.ToGoal)));
			AStarEntry firstEntry = new AStarEntry { Coordinate = start, SoFar = 0, ToGoal = (this.GetRelativePosition(start) - endPos).Length() };
			queue.Push(firstEntry);
			queueReverseLookup.Add(start, firstEntry);
			int iteration = 0;
			while (queue.Count > 0)
			{
				AStarEntry entry = queue.Pop();

				if (iteration == iterationLimit
					|| (Math.Abs(entry.Coordinate.X - end.X) <= 1
					&& Math.Abs(entry.Coordinate.Y - end.Y) <= 1
					&& Math.Abs(entry.Coordinate.Z - end.Z) <= 1))
					return this.constructPath(entry);

				queueReverseLookup.Remove(entry.Coordinate);
				try
				{
					closed.Add(entry.Coordinate, true);
				}
				catch (ArgumentException)
				{
					continue;
				}

				foreach (Direction d in DirectionExtensions.Directions)
				{
					Coordinate next = entry.Coordinate.Move(d);
					if ((entry.Parent == null || !next.Equivalent(entry.Parent.Coordinate)) && !closed.ContainsKey(next))
					{
						Map.CellState state = this[next];
						if (state.ID == 0)
						{
							// This is an empty cell
							// We can still use it if it's adjacent to a full cell
							if (this[next.Move(0, 0, 1)].ID == 0
								&& this[next.Move(0, 1, 0)].ID == 0
								&& this[next.Move(0, 1, 1)].ID == 0
								&& this[next.Move(1, 0, 0)].ID == 0
								&& this[next.Move(1, 0, 1)].ID == 0
								&& this[next.Move(1, 1, 0)].ID == 0
								&& this[next.Move(1, 1, 1)].ID == 0
								&& this[next.Move(0, 0, -1)].ID == 0
								&& this[next.Move(0, -1, 0)].ID == 0
								&& this[next.Move(0, -1, -1)].ID == 0
								&& this[next.Move(-1, 0, 0)].ID == 0
								&& this[next.Move(-1, 0, 1)].ID == 0
								&& this[next.Move(-1, -1, 0)].ID == 0
								&& this[next.Move(-1, -1, -1)].ID == 0)
								continue;
						}
						else if (state.Permanent)
							continue;

						float tentativeGScore = entry.SoFar + 1;

						AStarEntry newEntry;
						if (queueReverseLookup.TryGetValue(next, out newEntry))
						{
							if (newEntry.SoFar < tentativeGScore)
								continue;
						}

						if (newEntry == null)
						{
							newEntry = new AStarEntry { Coordinate = next, Parent = entry, SoFar = tentativeGScore, ToGoal = (this.GetRelativePosition(next) - endPos).Length() };
							queue.Push(newEntry);
							queueReverseLookup.Add(next, newEntry);
						}
						else
							newEntry.SoFar = tentativeGScore;
					}
				}
				iteration++;
			}

			return null;
		}

		private bool buildAdjacency(Box box, Dictionary<Box, bool> list)
		{
			Queue<Box> boxes = new Queue<Box>();
			if (!list.ContainsKey(box))
			{
				boxes.Enqueue(box);
				list.Add(box, true);
			}

			while (boxes.Count > 0)
			{
				Box b = boxes.Dequeue();

				if (b.Type.Permanent)
					return true;
				
				foreach (Box adjacent in b.Adjacent)
				{
					if (!list.ContainsKey(adjacent))
					{
						boxes.Enqueue(adjacent);
						list.Add(adjacent, true);
					}
				}
			}
			return false;
		}

		private bool[] simplify(List<Box> list, bool[] modified = null)
		{
			if (modified == null)
				modified = new bool[list.Count];
			
			// Z
			int i = 0;
			foreach (Box baseBox in list)
			{
				if (!baseBox.Active)
				{
					i++;
					continue;
				}
				Chunk chunk = baseBox.Chunk;
				for (int z2 = baseBox.Z + baseBox.Depth - chunk.Z; z2 < this.chunkSize; )
				{
					Box box = chunk.Data[baseBox.X - chunk.X, baseBox.Y - chunk.Y, z2];
					if (box != null && box.X == baseBox.X && box.Y == baseBox.Y && box.Z == z2 + chunk.Z && box.Type == baseBox.Type && box.Width == baseBox.Width && box.Height == baseBox.Height)
					{
						box.Active = false;
						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent == baseBox)
								continue;
							baseBox.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(baseBox);
						}
						this.removeBoxAdjacency(box);
						this.removals.Add(box);
						baseBox.Depth += box.Depth;
						box.Chunk.MarkDirty(box);

						Surface baseSurface = baseBox.Surfaces[(int)Direction.PositiveZ], newSurface = box.Surfaces[(int)Direction.PositiveZ];
						baseSurface.MinU = newSurface.MinU;
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MinV = newSurface.MinV;
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.W = newSurface.W;
						baseSurface.RefreshTransform(baseBox, Direction.PositiveZ);

						baseSurface = baseBox.Surfaces[(int)Direction.NegativeX];
						newSurface = box.Surfaces[(int)Direction.NegativeX];
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MaxU = Math.Max(baseSurface.MaxU, newSurface.MaxU);
						baseSurface.MinU = Math.Min(baseSurface.MinU, newSurface.MinU);
						baseSurface.RefreshTransform(baseBox, Direction.NegativeX);

						baseSurface = baseBox.Surfaces[(int)Direction.PositiveX];
						newSurface = box.Surfaces[(int)Direction.PositiveX];
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MaxU = Math.Max(baseSurface.MaxU, newSurface.MaxU);
						baseSurface.MinU = Math.Min(baseSurface.MinU, newSurface.MinU);
						baseSurface.RefreshTransform(baseBox, Direction.PositiveX);

						baseSurface = baseBox.Surfaces[(int)Direction.NegativeY];
						newSurface = box.Surfaces[(int)Direction.NegativeY];
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MaxU = Math.Max(baseSurface.MaxU, newSurface.MaxU);
						baseSurface.MinU = Math.Min(baseSurface.MinU, newSurface.MinU);
						baseSurface.RefreshTransform(baseBox, Direction.NegativeY);

						baseSurface = baseBox.Surfaces[(int)Direction.PositiveY];
						newSurface = box.Surfaces[(int)Direction.PositiveY];
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MaxU = Math.Max(baseSurface.MaxU, newSurface.MaxU);
						baseSurface.MinU = Math.Min(baseSurface.MinU, newSurface.MinU);
						baseSurface.RefreshTransform(baseBox, Direction.PositiveY);

						for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
						{
							for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
							{
								for (z2 = box.Z - chunk.Z; z2 < box.Z + box.Depth - chunk.Z; z2++)
									chunk.Data[x, y, z2] = baseBox;
							}
						}
						modified[i] = true;
					}
					else
						break;
				}
				i++;
			}

			// X
			i = 0;
			foreach (Box baseBox in list)
			{
				if (!baseBox.Active)
				{
					i++;
					continue;
				}
				Chunk chunk = baseBox.Chunk;
				for (int x2 = baseBox.X + baseBox.Width - chunk.X; x2 < this.chunkSize; )
				{
					Box box = chunk.Data[x2, baseBox.Y - chunk.Y, baseBox.Z - chunk.Z];
					if (box != null && box.X == x2 + chunk.X && box.Y == baseBox.Y && box.Z == baseBox.Z && box.Type == baseBox.Type && box.Depth == baseBox.Depth && box.Height == baseBox.Height)
					{
						box.Active = false;
						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent == baseBox)
								continue;
							baseBox.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(baseBox);
						}
						this.removeBoxAdjacency(box);
						this.removals.Add(box);
						baseBox.Width += box.Width;
						box.Chunk.MarkDirty(box);

						Surface baseSurface = baseBox.Surfaces[(int)Direction.PositiveX], newSurface = box.Surfaces[(int)Direction.PositiveX];
						baseSurface.MinV = newSurface.MinV;
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MinU = newSurface.MinU;
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.W = newSurface.W;
						baseSurface.RefreshTransform(baseBox, Direction.PositiveX);

						baseSurface = baseBox.Surfaces[(int)Direction.NegativeZ];
						newSurface = box.Surfaces[(int)Direction.NegativeZ];
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MaxV = Math.Max(baseSurface.MaxV, newSurface.MaxV);
						baseSurface.MinV = Math.Min(baseSurface.MinV, newSurface.MinV);
						baseSurface.RefreshTransform(baseBox, Direction.NegativeZ);

						baseSurface = baseBox.Surfaces[(int)Direction.PositiveZ];
						newSurface = box.Surfaces[(int)Direction.PositiveZ];
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MaxV = Math.Max(baseSurface.MaxV, newSurface.MaxV);
						baseSurface.MinV = Math.Min(baseSurface.MinV, newSurface.MinV);
						baseSurface.RefreshTransform(baseBox, Direction.PositiveZ);

						baseSurface = baseBox.Surfaces[(int)Direction.NegativeY];
						newSurface = box.Surfaces[(int)Direction.NegativeY];
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MaxV = Math.Max(baseSurface.MaxV, newSurface.MaxV);
						baseSurface.MinV = Math.Min(baseSurface.MinV, newSurface.MinV);
						baseSurface.RefreshTransform(baseBox, Direction.NegativeY);

						baseSurface = baseBox.Surfaces[(int)Direction.PositiveY];
						newSurface = box.Surfaces[(int)Direction.PositiveY];
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MaxV = Math.Max(baseSurface.MaxV, newSurface.MaxV);
						baseSurface.MinV = Math.Min(baseSurface.MinV, newSurface.MinV);
						baseSurface.RefreshTransform(baseBox, Direction.PositiveY);

						for (x2 = box.X - chunk.X; x2 < box.X + box.Width - chunk.X; x2++)
						{
							for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
							{
								for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
									chunk.Data[x2, y, z] = baseBox;
							}
						}
						modified[i] = true;
					}
					else
						break;
				}
				i++;
			}
			// Y
			i = 0;
			foreach (Box baseBox in list)
			{
				if (!baseBox.Active)
				{
					i++;
					continue;
				}
				Chunk chunk = baseBox.Chunk;
				for (int y2 = baseBox.Y + baseBox.Height - chunk.Y; y2 < this.chunkSize; )
				{
					Box box = chunk.Data[baseBox.X - chunk.X, y2, baseBox.Z - chunk.Z];
					if (box != null && box.X == baseBox.X && box.Y == y2 + chunk.Y && box.Z == baseBox.Z && box.Type == baseBox.Type && box.Depth == baseBox.Depth && box.Width == baseBox.Width)
					{
						box.Active = false;
						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent == baseBox)
								continue;
							baseBox.Adjacent.Add(adjacent);
							adjacent.Adjacent.Add(baseBox);
						}
						this.removeBoxAdjacency(box);
						this.removals.Add(box);
						baseBox.Height += box.Height;
						box.Chunk.MarkDirty(box);

						Surface baseSurface = baseBox.Surfaces[(int)Direction.PositiveY], newSurface = box.Surfaces[(int)Direction.PositiveY];
						baseSurface.MinV = newSurface.MinV;
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MinU = newSurface.MinU;
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.W = newSurface.W;
						baseSurface.RefreshTransform(baseBox, Direction.PositiveY);

						baseSurface = baseBox.Surfaces[(int)Direction.NegativeZ];
						newSurface = box.Surfaces[(int)Direction.NegativeZ];
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MaxU = Math.Max(baseSurface.MaxU, newSurface.MaxU);
						baseSurface.MinU = Math.Min(baseSurface.MinU, newSurface.MinU);
						baseSurface.RefreshTransform(baseBox, Direction.NegativeZ);

						baseSurface = baseBox.Surfaces[(int)Direction.PositiveZ];
						newSurface = box.Surfaces[(int)Direction.PositiveZ];
						baseSurface.MaxV = newSurface.MaxV;
						baseSurface.MaxU = Math.Max(baseSurface.MaxU, newSurface.MaxU);
						baseSurface.MinU = Math.Min(baseSurface.MinU, newSurface.MinU);
						baseSurface.RefreshTransform(baseBox, Direction.PositiveZ);

						baseSurface = baseBox.Surfaces[(int)Direction.NegativeX];
						newSurface = box.Surfaces[(int)Direction.NegativeX];
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MaxV = Math.Max(baseSurface.MaxV, newSurface.MaxV);
						baseSurface.MinV = Math.Min(baseSurface.MinV, newSurface.MinV);
						baseSurface.RefreshTransform(baseBox, Direction.NegativeX);

						baseSurface = baseBox.Surfaces[(int)Direction.PositiveX];
						newSurface = box.Surfaces[(int)Direction.PositiveX];
						baseSurface.MaxU = newSurface.MaxU;
						baseSurface.MaxV = Math.Max(baseSurface.MaxV, newSurface.MaxV);
						baseSurface.MinV = Math.Min(baseSurface.MinV, newSurface.MinV);
						baseSurface.RefreshTransform(baseBox, Direction.PositiveX);

						for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
						{
							for (y2 = box.Y - chunk.Y; y2 < box.Y + box.Height - chunk.Y; y2++)
							{
								for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
									chunk.Data[x, y2, z] = baseBox;
							}
						}
						modified[i] = true;
					}
					else
						break;
				}

				i++;
			}

			return modified;
		}

		public RaycastResult Raycast(Coordinate start, Direction dir, int length)
		{
			return this.Raycast(start, start.Move(dir, length));
		}

		public RaycastResult Raycast(Coordinate start, Coordinate end)
		{
			return this.Raycast(this.GetRelativePosition(start), this.GetRelativePosition(end));
		}

		private Coordinate getChunkCoordinateFromCoordinate(Coordinate coord)
		{
			return new Coordinate { X = (coord.X - this.minX) / this.chunkSize, Y = (coord.Y - this.minY) / this.chunkSize, Z = (coord.Z - this.minZ) / this.chunkSize };
		}

		private IEnumerable<Chunk> rasterizeChunks(Vector3 startRelative, Vector3 endRelative)
		{
			// Adapted from PolyVox
			// http://www.volumesoffun.com/polyvox/documentation/library/doc/html/_raycast_8inl_source.html

			startRelative = (startRelative - new Vector3(this.minX, this.minY, this.minZ)) / this.chunkSize;
			endRelative = (endRelative - new Vector3(this.minX, this.minY, this.minZ)) / this.chunkSize;

			Coordinate startCoord = new Coordinate { X = (int)startRelative.X, Y = (int)startRelative.Y, Z = (int)startRelative.Z };
			Coordinate endCoord = new Coordinate { X = (int)endRelative.X, Y = (int)endRelative.Y, Z = (int)endRelative.Z };

			int dx = ((startRelative.X < endRelative.X) ? 1 : ((startRelative.X > endRelative.X) ? -1 : 0));
			int dy = ((startRelative.Y < endRelative.Y) ? 1 : ((startRelative.Y > endRelative.Y) ? -1 : 0));
			int dz = ((startRelative.Z < endRelative.Z) ? 1 : ((startRelative.Z > endRelative.Z) ? -1 : 0));

			float minx = startCoord.X, maxx = minx + 1.0f;
			float tx = ((startRelative.X > endRelative.X) ? (startRelative.X - minx) : (maxx - startRelative.X)) / Math.Abs(endRelative.X - startRelative.X);
			float miny = startCoord.Y, maxy = miny + 1.0f;
			float ty = ((startRelative.Y > endRelative.Y) ? (startRelative.Y - miny) : (maxy - startRelative.Y)) / Math.Abs(endRelative.Y - startRelative.Y);
			float minz = startCoord.Z, maxz = minz + 1.0f;
			float tz = ((startRelative.Z > endRelative.Z) ? (startRelative.Z - minz) : (maxz - startRelative.Z)) / Math.Abs(endRelative.Z - startRelative.Z);

			float deltatx = 1.0f / Math.Abs(endRelative.X - startRelative.X);
			float deltaty = 1.0f / Math.Abs(endRelative.Y - startRelative.Y);
			float deltatz = 1.0f / Math.Abs(endRelative.Z - startRelative.Z);

			Coordinate coord = startCoord.Clone();

			Direction xDirection = dx > 0 ? Direction.NegativeX : (dx < 0 ? Direction.PositiveX : Direction.None);
			Direction yDirection = dy > 0 ? Direction.NegativeY : (dy < 0 ? Direction.PositiveY : Direction.None);
			Direction zDirection = dz > 0 ? Direction.NegativeZ : (dz < 0 ? Direction.PositiveZ : Direction.None);

			for (; ; )
			{
				if (coord.X >= 0 && coord.X < this.maxChunks
					&& coord.Y >= 0 && coord.Y < this.maxChunks
					&& coord.Z >= 0 && coord.Z < this.maxChunks)
					yield return this.chunks[coord.X, coord.Y, coord.Z];

				if (tx <= ty && tx <= tz)
				{
					if (coord.X == endCoord.X)
						break;
					tx += deltatx;
					coord.X += dx;
				}
				else if (ty <= tz)
				{
					if (coord.Y == endCoord.Y)
						break;
					ty += deltaty;
					coord.Y += dy;
				}
				else
				{
					if (coord.Z == endCoord.Z)
						break;
					tz += deltatz;
					coord.Z += dz;
				}
			}
		}

		public IEnumerable<Coordinate> Rasterize(Vector3 start, Vector3 end)
		{
			start = this.GetRelativePosition(start);
			end = this.GetRelativePosition(end);

			Coordinate startCoord = this.GetCoordinateFromRelative(start);
			Coordinate endCoord = this.GetCoordinateFromRelative(end);

			foreach (Coordinate coord in this.rasterize(start, end, startCoord, endCoord))
				yield return coord;
		}

		public IEnumerable<Coordinate> Rasterize(Coordinate startCoord, Coordinate endCoord)
		{
			Vector3 start = this.GetRelativePosition(startCoord);
			Vector3 end = this.GetRelativePosition(endCoord);

			foreach (Coordinate coord in this.rasterize(start, end, startCoord, endCoord))
				yield return coord;
		}

		private IEnumerable<Coordinate> rasterize(Vector3 start, Vector3 end, Coordinate startCoord, Coordinate endCoord)
		{
			// Adapted from PolyVox
			// http://www.volumesoffun.com/polyvox/documentation/library/doc/html/_raycast_8inl_source.html

			int dx = ((start.X < end.X) ? 1 : ((start.X > end.X) ? -1 : 0));
			int dy = ((start.Y < end.Y) ? 1 : ((start.Y > end.Y) ? -1 : 0));
			int dz = ((start.Z < end.Z) ? 1 : ((start.Z > end.Z) ? -1 : 0));

			float minx = startCoord.X, maxx = minx + 1.0f;
			float tx = ((start.X > end.X) ? (start.X - minx) : (maxx - start.X)) / Math.Abs(end.X - start.X);
			float miny = startCoord.Y, maxy = miny + 1.0f;
			float ty = ((start.Y > end.Y) ? (start.Y - miny) : (maxy - start.Y)) / Math.Abs(end.Y - start.Y);
			float minz = startCoord.Z, maxz = minz + 1.0f;
			float tz = ((start.Z > end.Z) ? (start.Z - minz) : (maxz - start.Z)) / Math.Abs(end.Z - start.Z);

			float deltatx = 1.0f / Math.Abs(end.X - start.X);
			float deltaty = 1.0f / Math.Abs(end.Y - start.Y);
			float deltatz = 1.0f / Math.Abs(end.Z - start.Z);

			Coordinate coord = startCoord.Clone();

			Direction normal = Direction.None;

			Direction xDirection = dx > 0 ? Direction.NegativeX : (dx < 0 ? Direction.PositiveX : Direction.None);
			Direction yDirection = dy > 0 ? Direction.NegativeY : (dy < 0 ? Direction.PositiveY : Direction.None);
			Direction zDirection = dz > 0 ? Direction.NegativeZ : (dz < 0 ? Direction.PositiveZ : Direction.None);

			for (; ; )
			{
				yield return coord;

				if (tx <= ty && tx <= tz)
				{
					if (coord.X == endCoord.X)
						break;
					tx += deltatx;
					coord.X += dx;
					normal = xDirection;
				}
				else if (ty <= tz)
				{
					if (coord.Y == endCoord.Y)
						break;
					ty += deltaty;
					coord.Y += dy;
					normal = yDirection;
				}
				else
				{
					if (coord.Z == endCoord.Z)
						break;
					tz += deltatz;
					coord.Z += dz;
					normal = zDirection;
				}
			}
		}

		public RaycastResult Raycast(Vector3 start, Vector3 end)
		{
			if (!this.main.EditorEnabled && !this.EnablePhysics)
				return new RaycastResult();

			// Adapted from PolyVox
			// http://www.volumesoffun.com/polyvox/documentation/library/doc/html/_raycast_8inl_source.html

			Vector3 absoluteStart = start;
			start = this.GetRelativePosition(start);
			end = this.GetRelativePosition(end);

			Vector3 ray = end - start;

			foreach (Chunk c in this.rasterizeChunks(start, end))
			{
				if (c == null || !c.Active)
					continue;

				Vector3 min = new Vector3(c.X, c.Y, c.Z), max = new Vector3(c.X + this.chunkSize, c.Y + this.chunkSize, c.Z + this.chunkSize);

				Vector3[] intersections = new Vector3[2];
				int intersectionIndex = 0;

				bool startInChunk = c.RelativeBoundingBox.Contains(start) != ContainmentType.Disjoint, endInChunk = c.RelativeBoundingBox.Contains(end) != ContainmentType.Disjoint;

				int expectedIntersections = 0;

				if (startInChunk && endInChunk)
				{
					intersections[0] = start;
					intersections[1] = end;
					goto done;
				}
				else if (startInChunk && !endInChunk)
				{
					intersections[1] = start;
					expectedIntersections = 1;
				}
				else if (!startInChunk && endInChunk)
				{
					intersections[1] = end;
					expectedIntersections = 1;
				}
				else
					expectedIntersections = 2;

				// Negative X
				Vector3 intersection;
				float ratio = Vector3.Dot((min - start), Vector3.Left) / Vector3.Dot(ray, Vector3.Left);
				if (ratio > 0.0f && ratio <= 1.0f)
				{
					intersection = start + (ray * ratio);
					if (intersection.Y >= min.Y && intersection.Y <= max.Y
						&& intersection.Z >= min.Z && intersection.Z <= max.Z)
					{
						intersections[intersectionIndex] = intersection;
						intersectionIndex++;
						if (intersectionIndex == expectedIntersections)
							goto done;
					}
				}

				// Positive X
				ratio = Vector3.Dot((max - start), Vector3.Right) / Vector3.Dot(ray, Vector3.Right);
				if (ratio > 0.0f && ratio <= 1.0f)
				{
					intersection = start + (ray * ratio);
					if (intersection.Y >= min.Y && intersection.Y <= max.Y
						&& intersection.Z >= min.Z && intersection.Z <= max.Z)
					{
						intersections[intersectionIndex] = intersection;
						intersectionIndex++;
						if (intersectionIndex == expectedIntersections)
							goto done;
					}
				}

				// Negative Y
				ratio = Vector3.Dot((min - start), Vector3.Down) / Vector3.Dot(ray, Vector3.Down);
				if (ratio > 0.0f && ratio <= 1.0f)
				{
					intersection = start + (ray * ratio);
					if (intersection.X >= min.X && intersection.X <= max.X
						&& intersection.Z >= min.Z && intersection.Z <= max.Z)
					{
						intersections[intersectionIndex] = intersection;
						intersectionIndex++;
						if (intersectionIndex == expectedIntersections)
							goto done;
					}
				}

				// Positive Y
				ratio = Vector3.Dot((max - start), Vector3.Up) / Vector3.Dot(ray, Vector3.Up);
				if (ratio > 0.0f && ratio <= 1.0f)
				{
					intersection = start + (ray * ratio);
					if (intersection.X >= min.X && intersection.X <= max.X
						&& intersection.Z >= min.Z && intersection.Z <= max.Z)
					{
						intersections[intersectionIndex] = intersection;
						intersectionIndex++;
						if (intersectionIndex == 2)
							goto done;
					}
				}

				// Negative Z
				ratio = Vector3.Dot((min - start), Vector3.Forward) / Vector3.Dot(ray, Vector3.Forward);
				if (ratio > 0.0f && ratio <= 1.0f)
				{
					intersection = start + (ray * ratio);
					if (intersection.X >= min.X && intersection.X <= max.X
						&& intersection.Y >= min.Y && intersection.Y <= max.Y)
					{
						intersections[intersectionIndex] = intersection;
						intersectionIndex++;
						if (intersectionIndex == expectedIntersections)
							goto done;
					}
				}

				// Positive Z
				ratio = Vector3.Dot((max - start), Vector3.Backward) / Vector3.Dot(ray, Vector3.Backward);
				if (ratio > 0.0f && ratio <= 1.0f)
				{
					intersection = start + (ray * ratio);
					if (intersection.X >= min.X && intersection.X <= max.X
						&& intersection.Y >= min.Y && intersection.Y <= max.Y)
					{
						intersections[intersectionIndex] = intersection;
						intersectionIndex++;
						if (intersectionIndex == expectedIntersections)
							goto done;
					}
				}

			done:
				if (intersectionIndex == expectedIntersections)
				{
					if ((intersections[0] - start).LengthSquared() > (intersections[1] - start).LengthSquared())
					{
						// Swap intersections to the correct order.
						Vector3 tmp = intersections[1];
						intersections[1] = intersections[0];
						intersections[0] = tmp;
					}

					RaycastResult result = this.raycastChunk(intersections[0], intersections[1], c);
					if (result.Coordinate != null)
					{
						result.Distance = (result.Position - absoluteStart).Length();
						return result;
					}
				}
			}

			return new RaycastResult { Coordinate = null };
		}

		private RaycastResult raycastChunk(Vector3 start, Vector3 end, Chunk c)
		{
			Vector3 actualStart = start, actualEnd = end;

			start -= new Vector3(c.X, c.Y, c.Z);
			end -= new Vector3(c.X, c.Y, c.Z);

			Coordinate startCoord = new Coordinate { X = (int)start.X, Y = (int)start.Y, Z = (int)start.Z };
			Coordinate endCoord = new Coordinate { X = (int)end.X, Y = (int)end.Y, Z = (int)end.Z };

			int dx = ((start.X < end.X) ? 1 : ((start.X > end.X) ? -1 : 0));
			int dy = ((start.Y < end.Y) ? 1 : ((start.Y > end.Y) ? -1 : 0));
			int dz = ((start.Z < end.Z) ? 1 : ((start.Z > end.Z) ? -1 : 0));

			float minx = startCoord.X, maxx = minx + 1.0f;
			float tx = ((start.X > end.X) ? (start.X - minx) : (maxx - start.X)) / Math.Abs(end.X - start.X);
			float miny = startCoord.Y, maxy = miny + 1.0f;
			float ty = ((start.Y > end.Y) ? (start.Y - miny) : (maxy - start.Y)) / Math.Abs(end.Y - start.Y);
			float minz = startCoord.Z, maxz = minz + 1.0f;
			float tz = ((start.Z > end.Z) ? (start.Z - minz) : (maxz - start.Z)) / Math.Abs(end.Z - start.Z);

			float deltatx = 1.0f / Math.Abs(end.X - start.X);
			float deltaty = 1.0f / Math.Abs(end.Y - start.Y);
			float deltatz = 1.0f / Math.Abs(end.Z - start.Z);

			Coordinate coord = startCoord.Clone();

			Direction normal = Direction.None;

			Direction xDirection = dx > 0 ? Direction.NegativeX : (dx < 0 ? Direction.PositiveX : Direction.None);
			Direction yDirection = dy > 0 ? Direction.NegativeY : (dy < 0 ? Direction.PositiveY : Direction.None);
			Direction zDirection = dz > 0 ? Direction.NegativeZ : (dz < 0 ? Direction.PositiveZ : Direction.None);

			for (; ; )
			{
				if ((coord.X >= 0 && coord.X < this.chunkSize
					&& coord.Y >= 0 && coord.Y < this.chunkSize
					&& coord.Z >= 0 && coord.Z < this.chunkSize)
					&& c.Data[coord.X, coord.Y, coord.Z] != null)
				{
					Coordinate actualCoord = coord.Move(c.X, c.Y, c.Z);

					// Found intersection

					Vector3 ray = actualEnd - actualStart;

					Vector3 norm = normal == Direction.None ? -Vector3.Normalize(ray) : normal.GetVector();

					Vector3 planePosition = new Vector3(actualCoord.X + 0.5f, actualCoord.Y + 0.5f, actualCoord.Z + 0.5f) + norm * 0.5f;

					return new RaycastResult { Coordinate = actualCoord, Normal = normal, Position = this.GetAbsolutePosition(actualStart + (ray * Vector3.Dot((planePosition - actualStart), norm) / Vector3.Dot(ray, norm))) };
				}

				if (tx <= ty && tx <= tz)
				{
					if (coord.X == endCoord.X)
						break;
					tx += deltatx;
					coord.X += dx;
					normal = xDirection;
				}
				else if (ty <= tz)
				{
					if (coord.Y == endCoord.Y)
						break;
					ty += deltaty;
					coord.Y += dy;
					normal = yDirection;
				}
				else
				{
					if (coord.Z == endCoord.Z)
						break;
					tz += deltatz;
					coord.Z += dz;
					normal = zDirection;
				}
			}
			return new RaycastResult { Coordinate = null };
		}

		public RaycastResult Raycast(Vector3 rayStart, Vector3 ray, float length)
		{
			return this.Raycast(rayStart, rayStart + (ray * length));
		}

		public CellState this[Coordinate coord]
		{
			get
			{
				return this[coord.X, coord.Y, coord.Z];
			}
		}

		public CellState this[int x, int y, int z]
		{
			get
			{
				if (!this.main.EditorEnabled && !this.EnablePhysics)
					return new CellState();

				Chunk chunk = this.GetChunk(x, y, z, false);
				if (chunk == null)
					return new CellState();
				else if (chunk.Data != null)
				{
					Box box = chunk.Data[x - chunk.X, y - chunk.Y, z - chunk.Z];
					if (box == null)
						return new CellState();
					else
						return box.Type;
				}
				else
					return new CellState();
			}
		}

		public CellState this[Vector3 pos]
		{
			get
			{
				return this[this.GetCoordinate(pos)];
			}
		}

		/// <summary>
		/// Get the coordinates for the specified position in space.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Coordinate GetCoordinate(Vector3 position)
		{
			return this.GetCoordinateFromRelative(this.GetRelativePosition(position));
		}

		/// <summary>
		/// Get the coordinates for the specified position in space.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Coordinate GetCoordinateFromRelative(Vector3 pos)
		{
			return new Coordinate
			{
				X = (int)Math.Floor(pos.X),
				Y = (int)Math.Floor(pos.Y),
				Z = (int)Math.Floor(pos.Z)
			};
		}

		public Coordinate GetCoordinate(int x, int y, int z)
		{
			return new Coordinate
			{
				X = x,
				Y = y,
				Z = z
			};
		}

		/// <summary>
		/// Transforms the given relative position into absolute world space.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Vector3 GetAbsolutePosition(Vector3 position)
		{
			return Vector3.Transform(position - this.Offset, this.Transform);
		}

		public Vector3 GetRelativePosition(Vector3 position)
		{
			return Vector3.Transform(position, Matrix.Invert(this.Transform)) + this.Offset;
		}

		/// <summary>
		/// Gets the absolute position in space of the given location (position is the center of the box).
		/// </summary>
		/// <param name="coord"></param>
		/// <returns></returns>
		public Vector3 GetAbsolutePosition(int x, int y, int z)
		{
			return Vector3.Transform(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) - this.Offset, this.Transform);
		}

		/// <summary>
		/// Gets the relative position in space of the given location (position is the center of the box).
		/// </summary>
		/// <param name="coord"></param>
		/// <returns></returns>
		public Vector3 GetRelativePosition(int x, int y, int z)
		{
			return new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) - this.Offset;
		}

		public Vector3 GetAbsolutePosition(Coordinate coord)
		{
			return this.GetAbsolutePosition(coord.X, coord.Y, coord.Z);
		}

		public Vector3 GetRelativePosition(Coordinate coord)
		{
			return this.GetRelativePosition(coord.X, coord.Y, coord.Z);
		}

		/// <summary>
		/// Gets the box containing the specified position in space.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Box GetBox(Vector3 position)
		{
			return this.GetBox(this.GetCoordinate(position));
		}

		/// <summary>
		/// Get the box containing the specified coordinate.
		/// </summary>
		/// <param name="coord"></param>
		/// <returns></returns>
		public Box GetBox(Coordinate coord)
		{
			return this.GetBox(coord.X, coord.Y, coord.Z);
		}

		/// <summary>
		/// Gets the box containing the specified location, or null if there is none.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Box GetBox(int x, int y, int z)
		{
			Chunk chunk = this.GetChunk(x, y, z, false);
			if (chunk == null || chunk.Data == null)
				return null;
			else
				return chunk.Data[x - chunk.X, y - chunk.Y, z - chunk.Z];
		}
	}

	public class DynamicMap : Map, IUpdateableComponent
	{
		private const float defaultLinearDamping = .03f;
		private const float defaultAngularDamping = .15f;

		private const float floatingLinearDamping = .4f;
		private const float floatingAngularDamping = .5f;

		private bool addedToSpace = false;

		[XmlIgnore]
		public MorphableEntity PhysicsEntity { get; protected set; }

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();

		[XmlIgnore]
		public Property<Vector3> LinearVelocity = new Property<Vector3> { Editable = false };

		public Property<bool> IsAffectedByGravity = new Property<bool> { Editable = true, Value = true };

		private bool firstPhysicsUpdate = true;
		private object physicsLock = new object();
		private bool physicsUpdated;

		[XmlIgnore]
		public Command PhysicsUpdated = new Command();

		public DynamicMap()
			: this(0, 0, 0)
		{

		}

		public DynamicMap(int offsetX, int offsetY, int offsetZ)
			: base(2, 10)
		{
			this.OffsetX = offsetX;
			this.OffsetY = offsetY;
			this.OffsetZ = offsetZ;
		}

		protected override Chunk newChunk()
		{
			Chunk chunk = new Chunk();
			chunk.Map = this;
			return chunk;
		}

		public override void InitializeProperties()
		{
			this.PhysicsEntity = new MorphableEntity(new CompoundShape(new CompoundShapeEntry[] { new CompoundShapeEntry(new BoxShape(1, 1, 1), Vector3.Zero, 1.0f) }));
			this.PhysicsEntity.Tag = this;
			if (this.main.EditorEnabled)
				this.PhysicsEntity.BecomeKinematic();
			base.InitializeProperties();

			this.PhysicsEntity.IsAffectedByGravity = false;
			this.IsAffectedByGravity.Set = delegate(bool value)
			{
				if (value)
				{
					this.PhysicsEntity.LinearDamping = DynamicMap.defaultLinearDamping;
					this.PhysicsEntity.AngularDamping = DynamicMap.defaultAngularDamping;
				}
				else
				{
					this.PhysicsEntity.LinearDamping = DynamicMap.floatingLinearDamping;
					this.PhysicsEntity.AngularDamping = DynamicMap.floatingAngularDamping;
				}
				this.IsAffectedByGravity.InternalValue = value;
				this.PhysicsEntity.IsAffectedByGravity = value;
				this.PhysicsEntity.ActivityInformation.Activate();
			};

			this.Transform.Get = delegate()
			{
				return this.PhysicsEntity.BufferedStates.InterpolatedStates.WorldTransform;
			};
			this.Transform.Set = delegate(Matrix value)
			{
				this.PhysicsEntity.WorldTransform = value;
			};

			this.LinearVelocity.Get = delegate()
			{
				return this.PhysicsEntity.LinearVelocity;
			};
			this.LinearVelocity.Set = delegate(Vector3 value)
			{
				this.PhysicsEntity.LinearVelocity = value;
			};

			this.Add(new CommandBinding(this.OnSuspended, delegate()
			{
				if (this.addedToSpace)
				{
					this.main.Space.SpaceObjectBuffer.Remove(this.PhysicsEntity);
					this.addedToSpace = false;
				}
				foreach (Chunk chunk in this.Chunks)
					chunk.Deactivate();
			}));

			this.Add(new CommandBinding(this.OnResumed, delegate()
			{
				this.PhysicsEntity.LinearVelocity = Vector3.Zero;
				if (!this.addedToSpace && this.PhysicsEntity.Volume > 0.0f && !this.main.EditorEnabled)
				{
					this.main.Space.SpaceObjectBuffer.Add(this.PhysicsEntity);
					this.addedToSpace = true;
				}
				foreach (Chunk chunk in this.Chunks)
					chunk.Activate();
			}));
		}

		void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		public override void updatePhysics()
		{
			foreach (Chunk chunk in this.Chunks)
				chunk.Activate();

			bool hasVolume = false;
			List<CompoundShapeEntry> bodies = new List<CompoundShapeEntry>();
			float mass = 0.0f;
			float volume = 0.0f;
			foreach (Box box in this.Chunks.SelectMany(x => x.Boxes))
			{
				if (!box.Type.Fake)
				{
					bodies.Add(box.GetCompoundShapeEntry());
					float v = box.Width * box.Height * box.Depth;
					volume += v;
					mass += v * box.Type.Density;
				}
			}

			if (bodies.Count > 0)
			{
				Vector3 c;
				CompoundShape shape = new CompoundShape(bodies, out c);
				this.PhysicsEntity.Position += Vector3.TransformNormal(c - this.Offset.Value, this.Transform);
				this.Offset.Value = c;
				if (!this.main.EditorEnabled && this.EnablePhysics)
				{
					hasVolume = true;
					EntityCollidable collisionInfo = shape.GetCollidableInstance();
					collisionInfo.Events.ContactCreated += new BEPUphysics.Collidables.Events.ContactCreatedEventHandler<BEPUphysics.Collidables.MobileCollidables.EntityCollidable>(Events_ContactCreated);
					collisionInfo.Tag = this;
					this.PhysicsEntity.SetCollisionInformation(collisionInfo, mass);
					this.PhysicsEntity.Volume = volume;
					this.PhysicsEntity.ActivityInformation.Activate();
				}
			}

			if (!this.addedToSpace && hasVolume && !this.Suspended && !this.main.EditorEnabled)
			{
				this.main.Space.SpaceObjectBuffer.Add(this.PhysicsEntity);
				this.addedToSpace = true;
			}
			else if (this.addedToSpace && this.PhysicsEntity.Space != null && !hasVolume)
			{
				this.main.Space.SpaceObjectBuffer.Remove(this.PhysicsEntity);
				this.addedToSpace = false;
			}

			if (this.firstPhysicsUpdate)
				this.firstPhysicsUpdate = false;
			else
			{
				lock (this.physicsLock)
					this.physicsUpdated = true;
			}
		}

		void IUpdateableComponent.Update(float dt)
		{
			this.Transform.Changed();
			this.LinearVelocity.Changed();
			if (this.physicsUpdated)
			{
				lock (this.physicsLock)
					this.physicsUpdated = false;
				this.PhysicsUpdated.Execute();
			}
		}

		protected override void delete()
		{
			base.delete();
			if (this.addedToSpace)
			{
				this.main.Space.SpaceObjectBuffer.Remove(this.PhysicsEntity);
				this.addedToSpace = false;
			}
		}
	}
}