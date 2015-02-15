using System;
using System.Diagnostics;
using ComponentBind;
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
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using System.ComponentModel;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using System.Threading;
using System.Collections;
using BEPUphysics.Materials;
using System.Reflection;

namespace Lemma.Components
{
	[XmlInclude(typeof(State))]
	[XmlInclude(typeof(Property<State>))]
	[XmlInclude(typeof(ListProperty<State>))]
	[XmlInclude(typeof(Coord))]
	[XmlInclude(typeof(ListProperty<Coord>))]
	[XmlInclude(typeof(Box))]
	[XmlInclude(typeof(Property<Box>))]
	[XmlInclude(typeof(ListProperty<Box>))]
	[XmlInclude(typeof(Property<Coord>))]
	[XmlInclude(typeof(Direction))]
	[XmlInclude(typeof(Property<Direction>))]
	[XmlInclude(typeof(ListProperty<Direction>))]
	[XmlInclude(typeof(t))]
	[XmlInclude(typeof(Property<t>))]
	[XmlInclude(typeof(ListProperty<t>))]
	public class Voxel : ComponentBind.Component<Main>
	{
		public static Dictionary<Voxel.Coord, bool> CoordDictionaryCache = new Dictionary<Voxel.Coord, bool>();

		private static LargeObjectHeap<Box[, ,][, ,]> subchunkHeap = LargeObjectHeap<Box[, ,][, ,]>.Get(x => new Box[x, x, x][,,], y => y * y * y * IntPtr.Size >= 85000);
		private static LargeObjectHeap<Box[, ,]> boxHeap = LargeObjectHeap<Box[, ,]>.Get(x => new Box[x, x, x], y => y * y * y * IntPtr.Size >= 85000);
		private static LargeObjectHeap<Chunk[, ,]> chunkHeap = LargeObjectHeap<Chunk[, ,]>.Get(x => new Chunk[x, x, x], y => y * y * y * IntPtr.Size >= 85000);
		private static LargeObjectHeap<Vertex[]> vertexHeap = LargeObjectHeap<Vertex[]>.Get(x => new Vertex[x], y => y * Vertex.SizeInBytes >= 85000);
		private static LargeObjectHeap<Vector3[]> physicsVertexHeap = LargeObjectHeap<Vector3[]>.Get(x => new Vector3[x], y => y * sizeof(float) * 3 >= 85000);

		public static Command<Voxel, IEnumerable<Coord>, Voxel> GlobalCellsEmptied = new Command<Voxel, IEnumerable<Coord>, Voxel>();

		public static Command<Voxel, IEnumerable<Coord>, Voxel> GlobalCellsFilled = new Command<Voxel, IEnumerable<Coord>, Voxel>();

		[XmlIgnore]
		public Command<IEnumerable<Coord>, Voxel> CellsEmptied = new Command<IEnumerable<Coord>, Voxel>();

		[XmlIgnore]
		public Command<IEnumerable<Coord>, Voxel> CellsFilled = new Command<IEnumerable<Coord>, Voxel>();

		public enum t // Material type
		{
			Empty = 0,
			Rock = 1,
			Blue = 2,
			AvoidAI = 3,
			Dirt = 4,
			Reset = 5,
			Critical = 6,
			Foliage = 7,
			Hard = 8,
			Floater = 9,
			Glass = 10,
			Wood = 11,
			Snow = 12,
			HardPowered = 13,
			Ice = 14,
			RockGrassy = 15,
			Brick = 16,
			Lattice = 17,
			Neutral = 18,
			Concrete = 19,
			Gravel = 20,
			RockChunky = 23,
			RockRed = 24,
			GlowYellow = 25,
			GlowBlue = 26,
			SocketWhite = 27,
			SocketYellow = 28,
			SocketBlue = 29,
			White = 30,
			Metal = 31,
			MetalSwirl = 32,
			Hex = 33,
			Invisible = 34,
			WhitePermanent = 35,
			Switch = 36,
			PoweredSwitch = 37,
			Powered = 38,
			PermanentPowered = 39,
			HardInfected = 40,
			Infected = 41,
			Black = 42,
			Slider = 43,
			SliderPowered = 44,
		}

		public static class States
		{
			public static void Add(params State[] states)
			{
				foreach (State state in states)
				{
					Voxel.States.All[state.ID] = state;
					Voxel.States.List.Add(state);
				}
			}

			public static void Remove(params State[] states)
			{
				foreach (State state in states)
				{
					Voxel.States.All.Remove(state.ID);
					Voxel.States.List.Remove(state);
				}
			}

			public static Dictionary<t, State> All = new Dictionary<t, State>();
			public static List<State> List = new List<State>();

			public static readonly State Empty = new State
			{
				ID = 0,
				Fake = true,
				Invisible = true,
				Permanent = false,
				Hard = false,
			};

			public static readonly State Rock = new State
			{
				ID = t.Rock,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\rock",
				NormalMap = "Textures\\rock-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.2f,
					},
					new Model.Material
					{
						SpecularPower = 150.0f,
						SpecularIntensity = 0.2f,
					},
				},
				Tint = new Vector3(0.88f, 0.89f, 0.9f),
			};
			public static readonly State Blue = new State
			{
				ID = t.Blue,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 0.25f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					}
				},
				Tint = new Vector3(0.3f, 0.5f, 0.7f),
			};
			public static readonly State AvoidAI = new State
			{
				ID = t.AvoidAI,
				Permanent = true,
				Hard = true,
				Supported = true,
				Density = 2,
				DiffuseMap = "Textures\\dirty",
				NormalMap = "Textures\\dirty-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
				Tint = new Vector3(0.15f),
			};
			public static readonly State Dirt = new State
			{
				ID = t.Dirt,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 0.5f,
				DiffuseMap = "Textures\\dirt",
				NormalMap = "Textures\\dirt-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.SAND,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
			};
			public static readonly State Reset = new State
			{
				ID = t.Reset,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				Tint = new Vector3(0.7f, 0.3f, 2.0f),
				DiffuseMap = "Textures\\powered-permanent",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
			};
			public static readonly State Critical = new State
			{
				ID = t.Critical,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\danger",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
			};
			public static readonly State Foliage = new State
			{
				ID = t.Foliage,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\foliage",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.SAND,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
				AllowAlpha = true,
				Tiling = 3.0f,
			};
			public static readonly State Hard = new State
			{
				ID = t.Hard,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 0.5f,
				DiffuseMap = "Textures\\dirty",
				NormalMap = "Textures\\metal-channels-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.WOOD,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
				Tint = new Vector3(0.55f, 0.47f, 0.4f),
			};
			public static readonly State Floater = new State
			{
				ID = t.Floater,
				Permanent = false,
				Supported = true,
				Hard = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					}
				},
				Tint = new Vector3(1.0f, 0.8f, 0.0f),
			};
			public static readonly State Glass = new State
			{
				ID = t.Glass,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\glass",
				NormalMap = "Textures\\glass-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				ShadowCast = true,
				AlphaShadowMask = true,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.2f,
					},
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 1.5f,
					},
				},
				Tiling = 4.0f,
			};
			public static readonly State Wood = new State
			{
				ID = t.Wood,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 0.25f,
				DiffuseMap = "Textures\\wood",
				NormalMap = "Textures\\wood-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.WOOD,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
				Tiling = 1.5f,
			};
			public static readonly State HardPowered = new State
			{
				ID = t.HardPowered,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\powered-hard",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.WOOD,
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
			};
			public static readonly State Neutral = new State
			{
				ID = t.Neutral,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 1,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
				Tint = new Vector3(0.7f),
			};
			public static readonly State Concrete = new State
			{
				ID = t.Concrete,
				Permanent = false,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\concrete",
				NormalMap = "Textures\\concrete-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
			};
			public static readonly State Gravel = new State
			{
				ID = t.Gravel,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 1,
				DiffuseMap = "Textures\\gravel",
				NormalMap = "Textures\\gravel-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.GRAVEL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
			};
			public static readonly State RockGrassy = new State
			{
				ID = t.RockGrassy,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\rock-grassy",
				NormalMap = "Textures\\rock-grassy-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
				Tiling = 0.7f,
			};
			public static readonly State Brick = new State
			{
				ID = t.Brick,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\bricks",
				NormalMap = "Textures\\bricks-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
				Tiling = 1.25f,
			};
			public static readonly State Lattice = new State
			{
				ID = t.Lattice,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\lattice",
				NormalMap = "Textures\\lattice-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 100.0f,
						SpecularIntensity = 0.3f,
					}
				},
			};
			public static readonly State RockChunky = new State
			{
				ID = t.RockChunky,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\rock-chunky",
				NormalMap = "Textures\\rock-chunky-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
				Tiling = 0.5f,
				Tint = new Vector3(0.88f, 0.89f, 0.9f),
			};
			public static readonly State RockRed = new State
			{
				ID = t.RockRed,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\red-rock",
				NormalMap = "Textures\\red-rock-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Tiling = 0.5f,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					}
				},
			};
			public static readonly State GlowYellow = new State
			{
				ID = t.GlowYellow,
				Permanent = true,
				Supported = true,
				Hard = true,
				ShadowCast = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					Model.Material.Unlit,
				},
				Tint = new Vector3(1.4f, 1.4f, 0.7f),
				AllowOverlay = false,
			};
			public static readonly State GlowBlue = new State
			{
				ID = t.GlowBlue,
				Permanent = true,
				Supported = true,
				Hard = true,
				ShadowCast = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					Model.Material.Unlit,
				},
				Tint = new Vector3(0.7f, 0.8f, 1.4f),
				AllowOverlay = false,
			};
			public static readonly State SocketWhite = new State
			{
				ID = t.SocketWhite,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 0.5f,
				DiffuseMap = "Textures\\rectangles",
				NormalMap = "Textures\\rectangles-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
				Tint = new Vector3(0.4f, 0.4f, 0.4f),
			};
			public static readonly State SocketYellow = new State
			{
				ID = t.SocketYellow,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 0.5f,
				DiffuseMap = "Textures\\rectangles",
				NormalMap = "Textures\\rectangles-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
				Tint = new Vector3(0.5f, 0.5f, 0.1f),
			};
			public static readonly State SocketBlue = new State
			{
				ID = t.SocketBlue,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 0.5f,
				DiffuseMap = "Textures\\rectangles",
				NormalMap = "Textures\\rectangles-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
				Tint = new Vector3(0.1f, 0.3f, 0.5f),
			};
			public static readonly State White = new State
			{
				ID = t.White,
				Permanent = false,
				Supported = false,
				Hard = false,
				ShadowCast = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					Model.Material.Unlit,
				},
				AllowOverlay = false,
			};
			public static readonly State Metal = new State
			{
				ID = t.Metal,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				DiffuseMap = "Textures\\metal-channels2",
				NormalMap = "Textures\\metal-channels2-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.2f,
					},
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.15f,
					},
				},
			};
			public static readonly State MetalSwirl = new State
			{
				ID = t.MetalSwirl,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				DiffuseMap = "Textures\\metal-swirl",
				NormalMap = "Textures\\metal-swirl-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.2f,
					},
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.15f,
					},
				},
			};
			public static readonly State Hex = new State
			{
				ID = t.Hex,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				DiffuseMap = "Textures\\hex",
				NormalMap = "Textures\\hex-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
			};
			public static readonly State Invisible = new State
			{
				ID = t.Invisible,
				Permanent = true,
				Supported = true,
				Hard = true,
				Invisible = true,
				AllowAlpha = true,
				Density = 1,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.WOOD,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\plain-normal",
				Tint = new Vector3(0.5f),
				AllowOverlay = false,
			};
			public static readonly State WhitePermanent = new State
			{
				ID = t.WhitePermanent,
				Permanent = true,
				Supported = true,
				Hard = true,
				ShadowCast = false,
				Density = 0.5f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				Materials = new[]
				{
					Model.Material.Unlit,
				},
				AllowOverlay = false,
			};
			public static readonly State Switch = new State
			{
				ID = t.Switch,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				DiffuseMap = "Textures\\switch",
				NormalMap = "Textures\\switch-normal",
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					}
				},
				Tiling = 3.0f,
				Tint = new Vector3(0.3f, 0.6f, 0.8f),
			};
			public static readonly State PoweredSwitch = new State
			{
				ID = t.PoweredSwitch,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				DiffuseMap = "Textures\\powered-switch",
				NormalMap = "Textures\\switch-normal",
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
				Tiling = 3.0f,
			};
			public static readonly State Powered = new State
			{
				ID = t.Powered,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 2,
				DiffuseMap = "Textures\\powered",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
			};
			public static readonly State PermanentPowered = new State
			{
				ID = t.PermanentPowered,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 2,
				DiffuseMap = "Textures\\powered-permanent",
				NormalMap = "Textures\\temporary-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
			};
			public static readonly State HardInfected = new State
			{
				ID = t.HardInfected,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 0.5f,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				DiffuseMap = "Textures\\infected-hard",
				NormalMap = "Textures\\temporary-normal",
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
			};
			public static readonly State Infected = new State
			{
				ID = t.Infected,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 3,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				DiffuseMap = "Textures\\infected",
				NormalMap = "Textures\\temporary-normal",
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
			};
			public static readonly State Black = new State
			{
				ID = t.Black,
				Permanent = true,
				Supported = true,
				Hard = true,
				ShadowCast = true,
				Density = 0.1f,
				DiffuseMap = "Textures\\white",
				NormalMap = "Textures\\plain-normal",
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.STONE,
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
				Tint = Vector3.Zero,
				AllowOverlay = false,
			};
			public static readonly State Slider = new State
			{
				ID = t.Slider,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				DiffuseMap = "Textures\\slider",
				NormalMap = "Textures\\slider-normal",
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					}
				},
				KineticFriction = 0,
				StaticFriction = 0,
				Tiling = 2.0f,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
			};
			public static readonly State SliderPowered = new State
			{
				ID = t.SliderPowered,
				Permanent = true,
				Supported = true,
				Hard = true,
				Density = 1,
				DiffuseMap = "Textures\\slider",
				NormalMap = "Textures\\slider-normal",
				Materials = new[]
				{
					Model.Material.Unlit,
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.4f,
					},
				},
				KineticFriction = 0,
				StaticFriction = 0,
				Tiling = 2.0f,
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
			};
			public static readonly State Snow = new State
			{
				ID = t.Snow,
				Permanent = false,
				Supported = false,
				Hard = false,
				Density = 0.5f,
				Tiling = 0.5f,
				DiffuseMap = "Textures\\snow",
				NormalMap = "Textures\\snow-normal",
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
					new Model.Material
					{
						SpecularPower = 50.0f,
						SpecularIntensity = 0.4f,
					},
				},
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.GRAVEL,
				AllowOverlay = false,
			};
			public static readonly State Ice = new State
			{
				ID = t.Ice,
				Permanent = false,
				Supported = false,
				Hard = true,
				Density = 0.5f,
				Tiling = 0.5f,
				DiffuseMap = "Textures\\ice",
				NormalMap = "Textures\\ice-normal",
				Materials = new[]
				{
					new Model.Material
					{
						SpecularPower = 200.0f,
						SpecularIntensity = 0.8f,
					},
					new Model.Material
					{
						SpecularPower = 1.0f,
						SpecularIntensity = 0.0f,
					},
				},
				Tint = new Vector3(0.5f, 0.6f, 0.8f),
				FootstepSwitch = AK.SWITCHES.FOOTSTEP_MATERIAL.SWITCH.METAL,
				AllowOverlay = false,
			};

			public static void Init()
			{
				foreach (FieldInfo field in typeof(States).GetFields(BindingFlags.Static | BindingFlags.Public))
				{
					if (field.FieldType == typeof(State))
						States.Add((State)field.GetValue(null));
				}
			}
		}

		public struct Vertex
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

		public class Snapshot
		{
			private Dictionary<Coord, ChunkData> chunks = new Dictionary<Coord, ChunkData>();
			private Voxel map;

			public Snapshot(Voxel m, Coord start, Coord end)
			{
				this.map = m;
				this.Add(start, end);
			}

			public void Add(Coord start, Coord end)
			{
				foreach (Chunk chunk in this.map.GetChunksBetween(start, end))
				{
					Coord chunkKey = new Coord { X = chunk.IndexX, Y = chunk.IndexY, Z = chunk.IndexZ };
					ChunkData data;
					if (!this.chunks.TryGetValue(chunkKey, out data))
					{
						this.chunks[chunkKey] = data = new ChunkData(this.map.chunkSize);
						data.X = chunk.X;
						data.Y = chunk.Y;
						data.Z = chunk.Z;
						data.IndexX = chunk.IndexX;
						data.IndexY = chunk.IndexY;
						data.IndexZ = chunk.IndexZ;
						for (int u = 0; u < this.map.chunkSize; u++)
						{
							for (int v = 0; v < this.map.chunkSize; v++)
							{
								for (int w = 0; w < this.map.chunkSize; w++)
									data[u, v, w] = chunk[u, v, w];
							}
						}
					}
				}
			}

			public void Free()
			{
				this.chunks.Clear();
			}

			public State this[Coord coord]
			{
				get
				{
					int indexX = (coord.X - this.map.minX) / this.map.chunkSize;
					int indexY = (coord.Y - this.map.minY) / this.map.chunkSize;
					int indexZ = (coord.Z - this.map.minZ) / this.map.chunkSize;
					ChunkData data;
					if (this.chunks.TryGetValue(new Coord { X = indexX, Y = indexY, Z = indexZ }, out data))
					{
						Box box = data[coord.X - data.X, coord.Y - data.Y, coord.Z - data.Z];
						if (box != null)
							return box.Type;
					}
					return Voxel.States.Empty;
				}
			}
		}

		public class State
		{
			public t ID;
			public bool Permanent;
			public bool Supported;
			public bool Hard;
			[DefaultValue(true)]
			public bool AllowOverlay = true;
			public string DiffuseMap;
			public string NormalMap;
			public uint FootstepSwitch;
			public uint RubbleEvent;
			public float KineticFriction = MaterialManager.DefaultKineticFriction;
			public float StaticFriction = MaterialManager.DefaultStaticFriction;
			public float Density;
			[DefaultValue(false)]
			public bool AllowAlpha;
			[DefaultValue(false)]
			public bool AlphaShadowMask;
			[DefaultValue(true)]
			public bool ShadowCast = true;
			[DefaultValue(false)]
			public bool Fake;
			[DefaultValue(false)]
			public bool Invisible;
			public Model.Material[] Materials = new[]
			{
				new Model.Material(),
				new Model.Material()
			};
			[DefaultValue(1.0f)]
			public float Tiling = 1.0f;
			public Vector3 Tint = Vector3.One;

			public void ApplyTo(Model model)
			{
				model.DiffuseTexture.Value = this.DiffuseMap;
				model.NormalMap.Value = this.NormalMap;
				model.Materials = this.Materials;
				model.DisableCulling.Value = this.AllowAlpha;
				model.Color.Value = this.Tint;
				if (this.AllowOverlay)
				{
					World world = WorldFactory.Instance.Get<World>();
					model.Add(new Binding<string>(model.TechniquePostfix, delegate(string overlay)
					{
						if (string.IsNullOrEmpty(overlay))
							return this.AllowAlpha ? "Alpha" : (this.AlphaShadowMask ? "ShadowMask" : "");
						else
							return this.AllowAlpha ? "OverlayAlpha" : (this.AlphaShadowMask ? "OverlayShadowMask" : "Overlay");
					}, world.OverlayTexture));
					model.Add(new Binding<Texture2D>(model.GetTexture2DParameter("Overlay" + Model.SamplerPostfix), world.OverlayTextureHandle));
					model.Add(new Binding<float>(model.GetFloatParameter("OverlayTiling"), x => 0.075f * x, world.OverlayTiling));
				}
			}

			public void ApplyToBlock(ComponentBind.Entity block)
			{
				block.Get<PhysicsBlock>().Box.Mass = this.Density * 0.5f * 0.5f * 0.5f;
				this.ApplyToEffectBlock(block.Get<ModelInstance>());
			}

			public void ApplyToEffectBlock(ModelInstance modelInstance)
			{
				modelInstance.Setup("InstancedModels\\block", (int)this.ID);
				if (modelInstance.IsFirstInstance)
				{
					Model model = modelInstance.Model;
					this.ApplyTo(model);
					model.GetMatrixParameter("UVScaleRotation").Value = Matrix.CreateScale(0.075f * this.Tiling);
				}
			}

			public override string ToString()
			{
				return this.ID.ToString();
			}
		}

		public class ChunkData
		{
			public int IndexX, IndexY, IndexZ;
			public int X, Y, Z;
			private Box[, ,][, ,] data;
			private int size;
			private int subchunkSize;
			private const int subchunks = 10;

			public ChunkData(int size)
			{
				if (size % subchunks != 0)
					throw new Exception(string.Format("Chunk size must be a multiple of {0}.", subchunks));
				this.size = size;
				this.subchunkSize = size / subchunks;
				this.data = Voxel.subchunkHeap.Get(subchunks);
			}

			public Box this[int x, int y, int z]
			{
				get
				{
					int ix = x / this.subchunkSize, iy = y / this.subchunkSize, iz = z / this.subchunkSize;
					Box[, ,] subchunk = this.data[ix, iy, iz];
					if (subchunk == null)
						return null;
					return subchunk[x - (ix * this.subchunkSize), y - (iy * this.subchunkSize), z - (iz * this.subchunkSize)];
				}
				set
				{
					int ix = x / this.subchunkSize, iy = y / this.subchunkSize, iz = z / this.subchunkSize;
					Box[, ,] subchunk = this.data[ix, iy, iz];
					if (subchunk == null)
						this.data[ix, iy, iz] = subchunk = Voxel.boxHeap.Get(this.subchunkSize);
					subchunk[x - (ix * this.subchunkSize), y - (iy * this.subchunkSize), z - (iz * this.subchunkSize)] = value;
				}
			}

			public void Free()
			{
				for (int x = 0; x < subchunks; x++)
				{
					for (int y = 0; y < subchunks; y++)
					{
						for (int z = 0; z < subchunks; z++)
						{
							this.data[x, y, z] = null;
							Box[,,] subchunk = this.data[x, y, z];
							if (subchunk != null)
								Voxel.boxHeap.Free(this.subchunkSize, subchunk);
						}
					}
				}
				Voxel.subchunkHeap.Free(subchunks, this.data);
			}
		}

		public class Chunk : ChunkData
		{
			protected class MeshEntry
			{
				public StaticMesh Mesh;
				public DynamicModel<Vertex> Model;
				public bool Dirty;
				public bool Added;
			}

			public bool Active = false;
			public bool EnablePhysics;
			public Voxel Voxel;
			public ListProperty<Box> Boxes = new ListProperty<Box>();
			public BoundingBox RelativeBoundingBox;

			public List<Box> DataBoxes;

			protected Dictionary<t, MeshEntry> meshes = new Dictionary<t, MeshEntry>();

			public void UpdateTransform()
			{
				Matrix transform = this.Voxel.Transform;
				lock (this.meshes)
				{
					foreach (KeyValuePair<t, MeshEntry> pair in this.meshes)
					{
						if (pair.Value.Mesh != null)
							pair.Value.Mesh.WorldTransform = new BEPUutilities.AffineTransform(BEPUutilities.Matrix3x3.CreateFromMatrix(transform), transform.Translation);
					}
				}
			}

			public void MarkDirty(Box box)
			{
				MeshEntry entry = null;
				if (!this.meshes.TryGetValue(box.Type.ID, out entry))
				{
					lock (this.meshes)
					{
						entry = new MeshEntry();
						if (this.Voxel.CreateModel != null)
						{
							Vector3 min = new Vector3(this.X, this.Y, this.Z);
							Vector3 max = min + new Vector3(this.Voxel.chunkSize);
							entry.Model = this.Voxel.CreateModel(min, max, box.Type);
						}
						this.meshes[box.Type.ID] = entry;
					}
				}
				entry.Dirty = true;
			}

			public Chunk(int size)
				: base(size)
			{
				this.Boxes.ItemAdded += delegate(int index, Box t)
				{
					int chunkHalfSize = this.Voxel.chunkHalfSize;
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
					for (int i = index; i < this.Boxes.Length; i++)
						this.Boxes[i].ChunkIndex = i;
				};
			}

			private static Vertex negativeX = new Vertex { Normal = Vector3.Left, Binormal = Vector3.Up, Tangent = Vector3.Backward };
			private static Vertex positiveX = new Vertex { Normal = Vector3.Right, Binormal = Vector3.Up, Tangent = Vector3.Forward };
			private static Vertex negativeY = new Vertex { Normal = Vector3.Down, Binormal = Vector3.Right, Tangent = Vector3.Backward };
			private static Vertex positiveY = new Vertex { Normal = Vector3.Up, Binormal = Vector3.Right, Tangent = Vector3.Forward };
			private static Vertex negativeZ = new Vertex { Normal = Vector3.Forward, Binormal = Vector3.Up, Tangent = Vector3.Left };
			private static Vertex positiveZ = new Vertex { Normal = Vector3.Backward, Binormal = Vector3.Up, Tangent = Vector3.Right };

			public void RefreshImmediately()
			{
				lock (this.meshes)
				{
					foreach (KeyValuePair<t, MeshEntry> pair in this.meshes)
					{
						if (!pair.Value.Dirty)
							continue;

						MeshEntry entry = pair.Value;
						entry.Dirty = false;

						int surfaces = 0;
						for (int i = 0; i < this.Boxes.Count; i++)
						{
							Box b = this.Boxes[i];
							if (b.Type.ID == pair.Key)
							{
								// Count number of set bits
								surfaces +=
									((b.Surfaces & (1 << 0)) != 0 ? 1 : 0)
									+ ((b.Surfaces & (1 << 1)) != 0 ? 1 : 0)
									+ ((b.Surfaces & (1 << 2)) != 0 ? 1 : 0)
									+ ((b.Surfaces & (1 << 3)) != 0 ? 1 : 0)
									+ ((b.Surfaces & (1 << 4)) != 0 ? 1 : 0)
									+ ((b.Surfaces & (1 << 5)) != 0 ? 1 : 0);
							}
						}

						State type = Voxel.States.All[pair.Key];

						Vertex[] vertices = null;
						Vector3[] physicsVertices = null;

						DynamicModel<Vertex> model = pair.Value.Model;

						if (surfaces > 0)
						{
							if (model != null)
								vertices = Voxel.vertexHeap.Get((int)Math.Pow(LargeObjectHeap<Vertex[]>.GrowthFactor, Math.Ceiling(Math.Log(surfaces * 4, LargeObjectHeap<Vertex[]>.GrowthFactor))));

							if (this.EnablePhysics && !type.Fake)
								physicsVertices = Voxel.physicsVertexHeap.Get((int)Math.Pow(LargeObjectHeap<Vector3[]>.GrowthFactor, Math.Ceiling(Math.Log(surfaces * 4, LargeObjectHeap<Vector3[]>.GrowthFactor))));

							uint vertexIndex = 0;
							for (int i = 0; i < this.Boxes.Count; i++)
							{
								Box box = this.Boxes[i];
								if (box.Type.ID == pair.Key)
								{
									Vector3 a = new Vector3(box.X, box.Y, box.Z);
									Vector3 b = new Vector3(box.X, box.Y, box.Z + box.Depth);
									Vector3 c = new Vector3(box.X, box.Y + box.Height, box.Z);
									Vector3 d = new Vector3(box.X, box.Y + box.Height, box.Z + box.Depth);
									Vector3 e = new Vector3(box.X + box.Width, box.Y, box.Z);
									Vector3 f = new Vector3(box.X + box.Width, box.Y, box.Z + box.Depth);
									Vector3 g = new Vector3(box.X + box.Width, box.Y + box.Height, box.Z);
									Vector3 h = new Vector3(box.X + box.Width, box.Y + box.Height, box.Z + box.Depth);

									if ((box.Surfaces & (1 << (int)Direction.NegativeX)) != 0)
									{
										if (vertices != null)
										{
											Chunk.negativeX.Position = a; vertices[vertexIndex + 0] = Chunk.negativeX;
											Chunk.negativeX.Position = b; vertices[vertexIndex + 1] = Chunk.negativeX;
											Chunk.negativeX.Position = c; vertices[vertexIndex + 2] = Chunk.negativeX;
											Chunk.negativeX.Position = d; vertices[vertexIndex + 3] = Chunk.negativeX;
										}
										if (physicsVertices != null)
										{
											physicsVertices[vertexIndex + 0] = a;
											physicsVertices[vertexIndex + 1] = b;
											physicsVertices[vertexIndex + 2] = c;
											physicsVertices[vertexIndex + 3] = d;
										}
										vertexIndex += 4;
									}
									if ((box.Surfaces & (1 << (int)Direction.PositiveX)) != 0)
									{
										if (vertices != null)
										{
											Chunk.positiveX.Position = e; vertices[vertexIndex + 0] = Chunk.positiveX;
											Chunk.positiveX.Position = g; vertices[vertexIndex + 1] = Chunk.positiveX;
											Chunk.positiveX.Position = f; vertices[vertexIndex + 2] = Chunk.positiveX;
											Chunk.positiveX.Position = h; vertices[vertexIndex + 3] = Chunk.positiveX;
										}
										if (physicsVertices != null)
										{
											physicsVertices[vertexIndex + 0] = e;
											physicsVertices[vertexIndex + 1] = g;
											physicsVertices[vertexIndex + 2] = f;
											physicsVertices[vertexIndex + 3] = h;
										}
										vertexIndex += 4;
									}
									if ((box.Surfaces & (1 << (int)Direction.NegativeY)) != 0)
									{
										if (vertices != null)
										{
											Chunk.negativeY.Position = a; vertices[vertexIndex + 0] = Chunk.negativeY;
											Chunk.negativeY.Position = e; vertices[vertexIndex + 1] = Chunk.negativeY;
											Chunk.negativeY.Position = b; vertices[vertexIndex + 2] = Chunk.negativeY;
											Chunk.negativeY.Position = f; vertices[vertexIndex + 3] = Chunk.negativeY;
										}
										if (physicsVertices != null)
										{
											physicsVertices[vertexIndex + 0] = a;
											physicsVertices[vertexIndex + 1] = e;
											physicsVertices[vertexIndex + 2] = b;
											physicsVertices[vertexIndex + 3] = f;
										}
										vertexIndex += 4;
									}
									if ((box.Surfaces & (1 << (int)Direction.PositiveY)) != 0)
									{
										if (vertices != null)
										{
											Chunk.positiveY.Position = c; vertices[vertexIndex + 0] = Chunk.positiveY;
											Chunk.positiveY.Position = d; vertices[vertexIndex + 1] = Chunk.positiveY;
											Chunk.positiveY.Position = g; vertices[vertexIndex + 2] = Chunk.positiveY;
											Chunk.positiveY.Position = h; vertices[vertexIndex + 3] = Chunk.positiveY;
										}
										if (physicsVertices != null)
										{
											physicsVertices[vertexIndex + 0] = c;
											physicsVertices[vertexIndex + 1] = d;
											physicsVertices[vertexIndex + 2] = g;
											physicsVertices[vertexIndex + 3] = h;
										}
										vertexIndex += 4;
									}
									if ((box.Surfaces & (1 << (int)Direction.NegativeZ)) != 0)
									{
										if (vertices != null)
										{
											Chunk.negativeZ.Position = a; vertices[vertexIndex + 0] = Chunk.negativeZ;
											Chunk.negativeZ.Position = c; vertices[vertexIndex + 1] = Chunk.negativeZ;
											Chunk.negativeZ.Position = e; vertices[vertexIndex + 2] = Chunk.negativeZ;
											Chunk.negativeZ.Position = g; vertices[vertexIndex + 3] = Chunk.negativeZ;
										}
										if (physicsVertices != null)
										{
											physicsVertices[vertexIndex + 0] = a;
											physicsVertices[vertexIndex + 1] = c;
											physicsVertices[vertexIndex + 2] = e;
											physicsVertices[vertexIndex + 3] = g;
										}
										vertexIndex += 4;
									}
									if ((box.Surfaces & (1 << (int)Direction.PositiveZ)) != 0)
									{
										if (vertices != null)
										{
											Chunk.positiveZ.Position = b; vertices[vertexIndex + 0] = Chunk.positiveZ;
											Chunk.positiveZ.Position = f; vertices[vertexIndex + 1] = Chunk.positiveZ;
											Chunk.positiveZ.Position = d; vertices[vertexIndex + 2] = Chunk.positiveZ;
											Chunk.positiveZ.Position = h; vertices[vertexIndex + 3] = Chunk.positiveZ;
										}
										if (physicsVertices != null)
										{
											physicsVertices[vertexIndex + 0] = b;
											physicsVertices[vertexIndex + 1] = f;
											physicsVertices[vertexIndex + 2] = d;
											physicsVertices[vertexIndex + 3] = h;
										}
										vertexIndex += 4;
									}
								}
							}
						}

						Vertex[] verticesCopy = null;
						if (vertices != null)
						{
							verticesCopy = Voxel.vertexHeap.Get(vertices.Length);
							Array.Copy(vertices, verticesCopy, surfaces * 4);
						}

						if (model != null)
						{
							lock (model.Lock)
								model.UpdateVertices(verticesCopy, surfaces);
						}

						if (vertices != null)
							Voxel.vertexHeap.Free(vertices.Length, vertices);

						StaticMesh oldMesh = null;
						if (entry.Mesh != null && entry.Added)
							oldMesh = entry.Mesh;

						if (physicsVertices != null)
						{
							Matrix transform = this.Voxel.Transform;
							Vector3[] physicsVerticesCopy = Voxel.physicsVertexHeap.Get(physicsVertices.Length);
							Array.Copy(physicsVertices, physicsVerticesCopy, surfaces * 4);
							StaticMesh mesh = new StaticMesh(physicsVerticesCopy, DynamicModel<Vertex>.GetIndices(surfaces * 6), surfaces * 6, new BEPUutilities.AffineTransform(BEPUutilities.Matrix3x3.CreateFromMatrix(transform), transform.Translation));
							mesh.Material.KineticFriction = type.KineticFriction;
							mesh.Material.StaticFriction = type.StaticFriction;
							mesh.Tag = this.Voxel;
							mesh.Sidedness = BEPUutilities.TriangleSidedness.Counterclockwise;
							entry.Mesh = mesh;
							if (this.Active)
							{
								entry.Added = true;
								this.Voxel.main.Space.SpaceObjectBuffer.Add(mesh);
							}
							Voxel.physicsVertexHeap.Free(physicsVertices.Length, physicsVertices);
						}
						else
							entry.Mesh = null;

						if (oldMesh != null)
							this.Voxel.main.Space.SpaceObjectBuffer.Remove(oldMesh);
					}
				}
			}

			public void Instantiate()
			{
				for (int i = 0; i < this.DataBoxes.Count; i++)
					this.Voxel.addBoxWithoutAdjacency(this.DataBoxes[i]);

				this.Boxes.AddAll(this.DataBoxes);
				this.DataBoxes.Clear();
				this.DataBoxes = null;

				if (!this.Voxel.main.EditorEnabled && !this.Voxel.Mutable)
				{
					this.Free();
					for (int i = 0; i < this.Boxes.Count; i++)
					{
						Box box = this.Boxes[i];
						box.Adjacent.Clear();
						box.Adjacent = null;
					}
				}

				this.RefreshImmediately();
			}

			public void RebuildAdjacency()
			{
				this.Voxel.calculateAdjacency(this.Boxes);
				for (int i = 0; i < this.Boxes.Count; i++)
					this.Voxel.regenerateSurfaces(this.Boxes[i]);
				this.RefreshImmediately();
			}

			public virtual void Activate()
			{
				if (!this.Active && this.EnablePhysics)
				{
					foreach (MeshEntry entry in this.meshes.Values)
					{
						if (!entry.Added)
						{
							entry.Added = true;
							if (entry.Mesh != null)
								this.Voxel.main.Space.SpaceObjectBuffer.Add(entry.Mesh);
						}
					}
				}
				this.Active = true;
			}

			public virtual void Deactivate()
			{
				if (this.Active && this.EnablePhysics)
				{
					foreach (MeshEntry entry in this.meshes.Values)
					{
						if (entry.Added)
						{
							entry.Added = false;
							if (entry.Mesh != null)
								this.Voxel.main.Space.SpaceObjectBuffer.Remove(entry.Mesh);
						}
					}
				}
				this.Active = false;
			}

			public virtual void Delete()
			{
				if (this.Active && this.EnablePhysics)
				{
					foreach (MeshEntry entry in this.meshes.Values)
					{
						if (entry.Added)
						{
							entry.Added = false;
							if (entry.Mesh != null)
								this.Voxel.main.Space.SpaceObjectBuffer.Remove(entry.Mesh);
						}
					}
				}

				this.Active = false;
				this.Free();
				this.meshes.Clear();
				for (int i = 0; i < this.Boxes.Count; i++)
				{
					Box box = this.Boxes[i];
					if (box.Adjacent != null)
					{
						box.Adjacent.Clear();
						box.Adjacent = null;
					}
				}
				this.Boxes.Clear();
				this.Boxes = null;
			}
		}

		public struct Coord
		{
			public int X;
			public int Y;
			public int Z;
			public State Data;

			public Coord WithData(State state)
			{
				return new Coord
				{
					X = this.X,
					Y = this.Y,
					Z = this.Z,
					Data = state,
				};
			}

			public static readonly int SizeInBytes = 3 * sizeof(int) + IntPtr.Size;

			public Coord Move(Direction dir, int amount)
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
				return new Coord { X = x, Y = y, Z = z, Data = this.Data };
			}

			public static Coord Max(Coord a, Coord b)
			{
				return new Coord
				{
					X = Math.Max(a.X, b.X),
					Y = Math.Max(a.Y, b.Y),
					Z = Math.Max(a.Z, b.Z),
				};
			}

			public static Coord Min(Coord a, Coord b)
			{
				return new Coord
				{
					X = Math.Min(a.X, b.X),
					Y = Math.Min(a.Y, b.Y),
					Z = Math.Min(a.Z, b.Z),
				};
			}

			public Coord Plus(Coord other)
			{
				return new Coord { X = this.X + other.X, Y = this.Y + other.Y, Z = this.Z + other.Z };
			}

			public Coord Minus(Coord other)
			{
				return new Coord { X = this.X - other.X, Y = this.Y - other.Y, Z = this.Z - other.Z };
			}

			// Expects every dimension of A to be smaller than every dimension of B.
			public bool Between(Coord a, Coord b)
			{
				return this.X >= a.X && this.X < b.X
					&& this.Y >= a.Y && this.Y < b.Y
					&& this.Z >= a.Z && this.Z < b.Z;
			}

			public IEnumerable<Coord> CoordinatesBetween(Coord b)
			{
				for (int x = this.X; x < b.X; x++)
				{
					for (int y = this.Y; y < b.Y; y++)
					{
						for (int z = this.Z; z < b.Z; z++)
						{
							yield return new Coord { X = x, Y = y, Z = z };
						}
					}
				}
			}

			public bool Equivalent(Coord coord)
			{
				return coord.X == this.X && coord.Y == this.Y && coord.Z == this.Z;
			}

			public override int GetHashCode()
			{
				int hash = 23;
				hash = hash * 31 + this.X;
				hash = hash * 31 + this.Y;
				hash = hash * 31 + this.Z;
				return hash;
			}

			public Coord Reorient(Direction x, Direction y, Direction z)
			{
				Coord c = new Coord();
				c.SetComponent(x, this.X);
				c.SetComponent(y, this.Y);
				c.SetComponent(z, this.Z);
				return c;
			}

			public override bool Equals(object obj)
			{
				if (obj.GetType() == typeof(Voxel.Coord))
				{
					Voxel.Coord coord = (Voxel.Coord)obj;
					return coord.X == this.X && coord.Y == this.Y && coord.Z == this.Z;
				}
				else
					return false;
			}

			public Coord Move(int x, int y, int z)
			{
				return new Coord { X = this.X + x, Y = this.Y + y, Z = this.Z + z, Data = this.Data };
			}

			public Coord Move(Direction dir)
			{
				return this.Move(dir, 1);
			}

			public Coord Clone()
			{
				return new Coord { X = this.X, Y = this.Y, Z = this.Z, Data = this.Data };
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

			public override string ToString()
			{
				return string.Format("[{0},{1},{2}]", this.X, this.Y, this.Z);
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
			public State Type;

			[XmlIgnore]
			public bool Active = true;
			[XmlIgnore]
			public bool Added;
			[XmlIgnore]
			public int ChunkIndex;
			[XmlIgnore]
			public Chunk Chunk;
			[XmlIgnore]
			public List<Box> Adjacent = new List<Box>();
			[XmlIgnore]
			public int Surfaces;

			[XmlIgnore]
			public int Volume
			{
				get
				{
					return this.Width * this.Height * this.Depth;
				}
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

			public IEnumerable<Voxel.Coord> GetCoords()
			{
				for (int x = this.X; x < this.X + this.Width; x++)
				{
					for (int y = this.Y; y < this.Y + this.Height; y++)
					{
						for (int z = this.Z; z < this.Z + this.Depth; z++)
						{
							yield return new Voxel.Coord { X = x, Y = y, Z = z, Data = this.Type };
						}
					}
				}
			}

			public Vector3 GetCenter()
			{
				return new Vector3(this.X + (this.Width * 0.5f), this.Y + (this.Height * 0.5f), this.Z + (this.Depth * 0.5f));
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

			public bool Contains(Coord coord)
			{
				return coord.X >= this.X && coord.X < this.X + this.Width
					&& coord.Y >= this.Y && coord.Y < this.Y + this.Height
					&& coord.Z >= this.Z && coord.Z < this.Z + this.Depth;
			}

			public bool Between(Coord a, Coord b)
			{
				return (a.X < this.X + this.Width || b.X >= this.X)
					&& (a.Y < this.Y + this.Height || b.Y >= this.Y)
					&& (a.Z < this.Z + this.Depth || b.Z >= this.Z);
			}

			public CompoundShapeEntry GetCompoundShapeEntry()
			{
				return new CompoundShapeEntry(new BoxShape(this.Width, this.Height, this.Depth), new Vector3(this.X + (this.Width * 0.5f), this.Y + (this.Height * 0.5f), this.Z + (this.Depth * 0.5f)), this.Type.Density * this.Width * this.Height * this.Depth);
			}
		}

		public static readonly List<Voxel> Voxels = new List<Voxel>();

		public static IEnumerable<Voxel> ActivePhysicsVoxels
		{
			get
			{
				return Voxel.Voxels.Where(x => x.Active && !x.Suspended && x.EnablePhysics && x.Scale.Value == 1.0f);
			}
		}

		public static IEnumerable<Voxel> ActiveVoxels
		{
			get
			{
				return Voxel.Voxels.Where(x => x.Active && !x.Suspended);
			}
		}

		public struct GlobalRaycastResult
		{
			public Voxel Voxel;
			public Voxel.Coord? Coordinate;
			public Vector3 Position;
			public Direction Normal;
			public float Distance;
		}

		public struct RaycastResult
		{
			public Voxel.Coord? Coordinate;
			public Vector3 Position;
			public Direction Normal;
			public float Distance;
		}

		public static GlobalRaycastResult GlobalRaycast(Vector3 start, Vector3 ray, float length, Func<int, t, bool> filter = null, bool includeScenery = false, bool includeInactive = false)
		{
			// Voxel raycasting
			GlobalRaycastResult result = new GlobalRaycastResult();
			result.Distance = length;
			result.Position = start + ray * length;

			IEnumerable<Voxel> maps = Voxel.Voxels.Where(x => x.Active);

			if (!includeInactive)
				maps = maps.Where(x => !x.Suspended);

			if (!includeScenery)
				maps = maps.Where(x => x.EnablePhysics);

			foreach (Voxel map in maps)
			{
				RaycastResult hit = map.Raycast(start, ray, result.Distance, filter);
				if (hit.Coordinate != null && hit.Distance < result.Distance)
				{
					result.Voxel = map;
					result.Coordinate = hit.Coordinate;
					result.Normal = hit.Normal;
					result.Position = hit.Position;
					result.Distance = hit.Distance;
				}
			}
			return result;
		}

		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix> { Value = Matrix.Identity };

		[XmlIgnore]
		public Property<Vector3> LinearVelocity = new Property<Vector3>();

		[XmlIgnore]
		public Property<Vector3> AngularVelocity = new Property<Vector3>();

		public class SerializedVoxelData
		{
			public string Value;
		}

		private static List<Box> boxCache = new List<Box>();

		private int[] serializedVoxelData;
		public SerializedVoxelData Data
		{
			get
			{
				List<int> result = new List<int>();
				lock (this.MutationLock)
				{
					Voxel.boxCache.AddRange(this.Chunks.SelectMany(x => x.Boxes));
					bool[] modifications = this.simplify(Voxel.boxCache);
					this.simplify(Voxel.boxCache, modifications);
					this.applyChanges(Voxel.boxCache, modifications);
					this.updateGraphics(this.Chunks);

					Voxel.boxCache.Clear();
					Voxel.boxCache.AddRange(this.Chunks.SelectMany(x => x.Boxes));

					result.Add(Voxel.boxCache.Count);

					Dictionary<Box, int> indexLookup = new Dictionary<Box, int>();

					for (int i = 0; i < Voxel.boxCache.Count; i++)
					{
						Box box = Voxel.boxCache[i];
						result.Add(box.X);
						result.Add(box.Y);
						result.Add(box.Z);
						int packedData = 0;
						int ID = (int)box.Type.ID;
						/*Store the data in a packed integer*/
						packedData = packedData.StoreByte((byte)box.Width);
						packedData = packedData.StoreByte((byte)box.Height, 8);
						packedData = packedData.StoreByte((byte)box.Depth, 16);
						packedData = packedData.StoreByte((byte)ID, 24);
						result.Add(packedData);

						//We need to use 11 bits to store each value.
						//So pack it all up nice and tidy. This will store them in 9 ints, as opposed to 24. Nice.
						result.Add(box.Surfaces);
						indexLookup.Add(box, i);
					}

					List<int> indexData = new List<int>();
					for (int i = 0; i < Voxel.boxCache.Count; i++)
					{
						Box box = Voxel.boxCache[i];
						if (box.Adjacent != null)
						{
							lock (box.Adjacent)
							{
								foreach (Box adjacent in box.Adjacent)
								{
									BoxRelationship relationship1 = new BoxRelationship { A = box, B = adjacent };
									BoxRelationship relationship2 = new BoxRelationship { A = adjacent, B = box };
									if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
									{
										this.relationshipCache[relationship1] = true;
										indexData.Add(i);
										indexData.Add(indexLookup[adjacent]);
									}
								}
							}
						}
					}
					this.relationshipCache.Clear();

					BitWorker.PackInts(result, 17, indexData);
					Voxel.boxCache.Clear();
				}
				return Voxel.serializeData(result);
			}

			set
			{
				if (this.awoken)
					this.processSerializedData(Voxel.deserializeData(value));
				else
					this.serializedVoxelData = Voxel.deserializeData(value);
			}
		}

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
				return this.maxX;
			}
		}

		[XmlIgnore]
		public int MaxY
		{
			get
			{
				return this.maxY;
			}
		}

		[XmlIgnore]
		public int MaxZ
		{
			get
			{
				return this.maxZ;
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
		public ListProperty<Chunk> Chunks = new ListProperty<Chunk>();

		private Chunk[, ,] chunks;

		protected List<Box> additions = new List<Box>();
		protected List<Box> removals = new List<Box>();
		protected List<Coord> removalCoords = new List<Coord>();

		[XmlIgnore]
		public Property<Vector3> Offset = new Property<Vector3>();

		public Property<bool> EnablePhysics = new Property<bool> { Value = true };
		public Property<bool> Mutable = new Property<bool> { Value = true };

		[DefaultValueAttribute(0)]
		public int OffsetX { get; set; }
		[DefaultValueAttribute(0)]
		public int OffsetY { get; set; }
		[DefaultValueAttribute(0)]
		public int OffsetZ { get; set; }

		public Property<float> Scale = new Property<float> { Value = 1.0f };

		public Property<float> UVRotation = new Property<float>();
		public Property<Vector2> UVOffset = new Property<Vector2>();

		[XmlIgnore]
		public Func<Vector3, Vector3, State, DynamicModel<Voxel.Vertex>> CreateModel;

		public Voxel()
			: this(0, 0, 0)
		{

		}

		public Voxel(int offsetX, int offsetY, int offsetZ)
			: this(20, 40)
		{
			this.OffsetX = offsetX;
			this.OffsetY = offsetY;
			this.OffsetZ = offsetZ;
		}

		protected Voxel(int maxChunks, int chunkHalfSize)
		{
			this.chunkHalfSize = chunkHalfSize;
			this.chunkSize = chunkHalfSize * 2;
			this.maxChunks = maxChunks;
			this.chunks = Voxel.chunkHeap.Get(maxChunks);
		}

		public virtual void updatePhysics()
		{
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
			public Voxel Source;
			public Action<List<DynamicVoxel>> Callback;
		}
		private static List<SpawnGroup> spawns = new List<SpawnGroup>();

		protected virtual void transformBinding()
		{
			if (this.EnablePhysics && !this.main.EditorEnabled)
			{
				this.Add(new ChangeBinding<Matrix>(this.Transform, delegate(Matrix old, Matrix value)
				{
					for (int i = 0; i < this.Chunks.Count; i++)
						this.Chunks[i].UpdateTransform();
				}));
			}
		}

		private bool awoken = false;
		public override void Awake()
		{
			base.Awake();
			this.transformBinding();
			this.updateBounds();

			if (Voxel.workThread == null)
			{
				Voxel.workThread = new Thread(new ThreadStart(Voxel.worker));
				Voxel.workThread.Start();
				this.main.Exiting += delegate(object a, EventArgs b)
				{
					Voxel.workThread.Abort();
				};
				Main m = this.main;
				Voxel.spawner = new Updater
				(
					delegate(float dt)
					{
						DynamicVoxelFactory factory = Factory.Get<DynamicVoxelFactory>();
						BlockFactory blockFactory = Factory.Get<BlockFactory>();
						List<SpawnGroup> spawns = new List<SpawnGroup>();
						lock (Voxel.spawns)
						{
							spawns.AddRange(Voxel.spawns);
							Voxel.spawns.Clear();
						}
						for (int i = 0; i < spawns.Count; i++)
						{
							SpawnGroup spawn = spawns[i];
							List<DynamicVoxel> spawnedMaps = new List<DynamicVoxel>();
							for (int j = 0; j < spawn.Islands.Count; j++)
							{
								List<Box> island = spawn.Islands[j];
								Box firstBox = island.First();
								if (island.Count == 1 && firstBox.Width * firstBox.Height * firstBox.Depth == 1)
								{
									// Just create a temporary physics block instead of a full-blown map
									Coord coord = new Coord { X = firstBox.X, Y = firstBox.Y, Z = firstBox.Z };
									ComponentBind.Entity block = blockFactory.CreateAndBind(main);
									block.Get<Transform>().Matrix.Value = this.Transform;
									block.Get<Transform>().Position.Value = this.GetAbsolutePosition(coord);
									firstBox.Type.ApplyToBlock(block);
									block.Get<ModelInstance>().GetVector3Parameter("Offset").Value = this.GetRelativePosition(coord);
									main.Add(block);
								}
								else
								{
									ComponentBind.Entity newMap = factory.CreateAndBind(spawn.Source.main, firstBox.X, firstBox.Y, firstBox.Z);
									newMap.Get<Transform>().Matrix.Value = spawn.Source.Transform;
									DynamicVoxel newMapComponent = newMap.Get<DynamicVoxel>();
									newMapComponent.Offset.Value = spawn.Source.Offset;
									newMapComponent.BuildFromBoxes(island);
									newMapComponent.UpdatePhysicsImmediately();
									newMapComponent.Transform.Value = newMapComponent.PhysicsEntity.WorldTransform;
									if (spawn.Source is DynamicVoxel)
										newMapComponent.IsAffectedByGravity.Value = ((DynamicVoxel)spawn.Source).IsAffectedByGravity;
									spawn.Source.notifyEmptied(island.SelectMany(x => x.GetCoords()), newMapComponent);
									spawn.Source.main.Add(newMap);
									spawnedMaps.Add(newMapComponent);
								}
							}
							if (spawn.Callback != null)
								spawn.Callback(spawnedMaps);
						}
						spawns.Clear();
					}
				);
				Voxel.spawner.EnabledInEditMode = true;
				Voxel.spawner.EnabledWhenPaused = true;
				this.main.AddComponent(Voxel.spawner);
			}

			Voxel.Voxels.Add(this);

			if (this.serializedVoxelData != null)
			{
				int[] data = this.serializedVoxelData;
				this.serializedVoxelData = null;
				this.processSerializedData(data);
			}

			this.awoken = true;
		}

		private void processSerializedData(int[] data)
		{
			int boxCount = data[0];

			bool rebuildAdjacency = false;
			const int boxDataSize = 5;

			for (int i = 0; i < boxCount; i++)
			{
				// Format:
				// x
				// y
				// z
				// width-height-depth-type, packed in one int
				// one bool for each of six surfaces, packed in one int
				int index = 1 + (i * boxDataSize);
				int packedData = data[index + 3];
				int v = packedData.ExtractBits(24, 8);
				if (v != 0)
				{
					int x = data[index], y = data[index + 1], z = data[index + 2];
					int w = packedData.ExtractBits(0, 8), h = packedData.ExtractBits(8, 8), d = packedData.ExtractBits(16, 8);
					int surfaces = data[index + 4];
					State state = Voxel.States.All[(t)v];
					int chunkX = this.minX + ((x - this.minX) / this.chunkSize) * this.chunkSize, chunkY = this.minY + ((y - this.minY) / this.chunkSize) * this.chunkSize, chunkZ = this.minZ + ((z - this.minZ) / this.chunkSize) * this.chunkSize;
					int nextChunkX = this.minX + ((x + w - this.minX - 1) / this.chunkSize) * this.chunkSize, nextChunkY = this.minY + ((y + h - this.minY - 1) / this.chunkSize) * this.chunkSize, nextChunkZ = this.minZ + ((z + d - this.minZ - 1) / this.chunkSize) * this.chunkSize;
					rebuildAdjacency |= chunkX != nextChunkX || chunkY != nextChunkY || chunkZ != nextChunkZ;
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
									Width = Math.Min(bx + w, ix + this.chunkSize) - bx,
									Height = Math.Min(by + h, iy + this.chunkSize) - by,
									Depth = Math.Min(bz + d, iz + this.chunkSize) - bz,
									Type = state,
									Active = true,
								};

								if (box.Width > 0 && box.Height > 0 && box.Depth > 0)
								{
									Chunk chunk = this.GetChunk(bx, by, bz);
									Voxel.boxCache.Add(box);
									if (chunk.DataBoxes == null)
										chunk.DataBoxes = new List<Box>();
									chunk.DataBoxes.Add(box);
									box.Chunk = chunk;
									box.Surfaces = surfaces;
								}
							}
						}
					}
				}
			}

			int packedBoxesStart = 1 + boxCount * boxDataSize;
			int[] packedBoxes = new int[data.Length - packedBoxesStart];
			for (int i = packedBoxesStart; i < data.Length; i++)
				packedBoxes[i - packedBoxesStart] = data[i];

			int[] unPackedBoxes = BitWorker.UnPackInts(17, -1, packedBoxes);

			try
			{
				if (rebuildAdjacency)
					throw new Exception();
				else
				{
					for (int i = 0; i < unPackedBoxes.Length- 1; i += 2)
					{
						Box box1 = Voxel.boxCache[unPackedBoxes[i]], box2 = Voxel.boxCache[unPackedBoxes[i + 1]];
						if (box1 != null && box2 != null)
						{
							box1.Adjacent.Add(box2);
							box2.Adjacent.Add(box1);
						}
					}
				}
				Voxel.boxCache.Clear();
			}
			catch (Exception)
			{
				Log.d("Error reading adjacency data. Rebuilding adjacency...");
				Voxel.boxCache.Clear();
				this.RebuildAdjacency();
			}


			this.postDeserialization();
		}

		public IEnumerable<Chunk> GetChunksBetween(Voxel.Coord a, Voxel.Coord b)
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

		protected virtual void postDeserialization()
		{
			for (int i = 0; i < this.Chunks.Count; i++)
				this.Chunks[i].Instantiate();
			this.updatePhysics();
		}

		protected static SerializedVoxelData serializeData(List<int> data)
		{
			byte[] result = new byte[data.Count * 4];
			for (int i = 0; i < data.Count; i++)
			{
				int value = data[i];
				int j = i * 4;
				result[j] = (byte)(value >> 24);
				result[j + 1] = (byte)(value >> 16);
				result[j + 2] = (byte)(value >> 8);
				result[j + 3] = (byte)value;
			}
			return new SerializedVoxelData { Value = System.Convert.ToBase64String(result) };
		}

		protected static int[] deserializeData(SerializedVoxelData data)
		{
			byte[] temp = System.Convert.FromBase64String(data.Value);
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

		public override void delete()
		{
			base.delete();
			lock (this.MutationLock)
			{
				for (int i = 0; i < this.Chunks.Count; i++)
					this.Chunks[i].Delete();
				this.Chunks.Clear();

				for (int i = 0; i < this.maxChunks; i++)
				{
					for (int j = 0; j < this.maxChunks; j++)
					{
						for (int k = 0; k < this.maxChunks; k++)
							this.chunks[i, j, k] = null;
					}
				}
				this.chunks = null;
				this.CreateModel = null;
			}
			Voxel.Voxels.Remove(this);
		}

		public Chunk GetChunk(Coord coord, bool createIfNonExistent = true)
		{
			return this.GetChunk(coord.X, coord.Y, coord.Z, createIfNonExistent);
		}

		public Chunk GetChunk(int x, int y, int z, bool createIfNonExistent = true)
		{
			if (this.chunks == null)
				return null;
			while (x < this.minX || x >= this.maxX || y < this.minY || y >= this.maxY || z < this.minZ || z >= this.maxZ)
			{
				if (createIfNonExistent)
				{
					int originalChunkArraySize = this.maxChunks;
					int oldMin = this.maxChunks / -2, oldMax = this.maxChunks / 2;
					this.maxChunks *= 2;
					int newMin = this.maxChunks / -2;

					Chunk[, ,] newChunks = Voxel.chunkHeap.Get(this.maxChunks);

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

					Voxel.chunkHeap.Free(originalChunkArraySize, this.chunks);

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
				chunk.Voxel = this;
				chunk.X = this.minX + (ix * this.chunkSize);
				chunk.Y = this.minY + (iy * this.chunkSize);
				chunk.Z = this.minZ + (iz * this.chunkSize);
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
			Chunk chunk = new Chunk(this.chunkSize) { EnablePhysics = !this.main.EditorEnabled && this.EnablePhysics };
			chunk.Voxel = this;
			return chunk;
		}

		public bool Contains(Coord coord)
		{
			return coord.X >= this.minX && coord.X < this.maxX
				&& coord.Y >= this.minY && coord.Y < this.maxY
				&& coord.Z >= this.minZ && coord.Z < this.maxZ;
		}

		public bool Fill(Vector3 pos, State state, bool notify = true)
		{
			return this.Fill(this.GetCoordinate(pos), state, notify);
		}

		public bool Fill(Coord start, Coord end, State state, bool notify = true)
		{
			bool changed = false;
			for (int x = start.X; x < end.X; x++)
			{
				for (int y = start.Y; y < end.Y; y++)
				{
					for (int z = start.Z; z < end.Z; z++)
					{
						changed |= this.Fill(x, y, z, state, notify);
					}
				}
			}
			return changed;
		}

		public bool Fill(Coord coord, State state, bool notify = true, Voxel transferredFromMap = null)
		{
			return this.Fill(coord.X, coord.Y, coord.Z, state, notify, transferredFromMap);
		}

		public bool Empty(Vector3 pos, bool force = false, bool forceHard = true, Voxel transferringToNewMap = null, bool notify = true)
		{
			return this.Empty(this.GetCoordinate(pos), force, forceHard, transferringToNewMap, notify);
		}

		public bool Empty(Coord coord, bool force = false, bool forceHard = true, Voxel transferringToNewMap = null, bool notify = true)
		{
			return this.Empty(coord.X, coord.Y, coord.Z, force, forceHard, transferringToNewMap, notify);
		}

		public bool Empty(Coord a, Coord b, bool force = false, bool forceHard = true, Voxel transferringToNewMap = null, bool notify = true)
		{
			int minY = Math.Min(a.Y, b.Y);
			int minZ = Math.Min(a.Z, b.Z);
			int maxX = Math.Max(a.X, b.X);
			int maxY = Math.Max(a.Y, b.Y);
			int maxZ = Math.Max(a.Z, b.Z);
			List<Voxel.Coord> coords = new List<Coord>();
			for (int x = Math.Min(a.X, b.X); x < maxX; x++)
			{
				for (int y = minY; y < maxY; y++)
				{
					for (int z = minZ; z < maxZ; z++)
					{
						coords.Add(new Voxel.Coord { X = x, Y = y, Z = z });
					}
				}
			}
			return this.Empty(coords, force, forceHard, transferringToNewMap, notify);
		}

		/// <summary>
		/// Fills the specified location. This change will not take effect until Generate() or Regenerate() is called.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public bool Fill(int x, int y, int z, State state, bool notify = true, Voxel transferredFromMap = null)
		{
			if (state == Voxel.States.Empty || (!this.main.EditorEnabled && !this.Mutable))
				return false;

			bool filled = false;
			lock (this.MutationLock)
			{
				Chunk chunk = this.GetChunk(x, y, z);
				if (chunk != null)
				{
					if (chunk[x - chunk.X, y - chunk.Y, z - chunk.Z] == null)
					{
						this.addBox(new Box { Type = state, X = x, Y = y, Z = z, Depth = 1, Height = 1, Width = 1 });
						filled = true;
					}
				}
			}
			if (filled && notify)
				this.notifyFilled(new Coord[] { new Coord { X = x, Y = y, Z = z, Data = state } }, transferredFromMap);
			return filled;
		}

		public void Fill(IEnumerable<Coord> coords, bool notify = true, Voxel transferredFromMap = null)
		{
			if (!this.main.EditorEnabled && !this.Mutable)
				return;

			List<Coord> notifyList = null;
			if (notify)
				notifyList = new List<Coord>();
			lock (this.MutationLock)
			{
				foreach (Voxel.Coord c in coords)
				{
					int x = c.X, y = c.Y, z = c.Z;
					Chunk chunk = this.GetChunk(x, y, z);
					if (chunk != null)
					{
						if (chunk[x - chunk.X, y - chunk.Y, z - chunk.Z] == null)
						{
							this.addBox(new Box { Type = c.Data, X = x, Y = y, Z = z, Depth = 1, Height = 1, Width = 1 });
							notifyList.Add(c);
						}
					}
				}
			}
			if (notify)
				this.notifyFilled(notifyList, transferredFromMap);
			return;
		}

		private void notifyFilled(IEnumerable<Coord> coords, Voxel transferredFromMap)
		{
			this.CellsFilled.Execute(coords, transferredFromMap);
			Voxel.GlobalCellsFilled.Execute(this, coords, transferredFromMap);
		}

		private void notifyEmptied(IEnumerable<Coord> coords, Voxel transferringToNewMap)
		{
			this.CellsEmptied.Execute(coords, transferringToNewMap);
			Voxel.GlobalCellsEmptied.Execute(this, coords, transferringToNewMap);

			bool completelyEmptied = true;
			lock (this.MutationLock)
			{
				if (this.additions.FirstOrDefault(x => x.Active) != null)
					completelyEmptied = false;
				else
				{
					for (int i = 0; i < this.Chunks.Count; i++)
					{
						Chunk chunk = this.Chunks[i];
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
			}

			if (completelyEmptied)
				this.CompletelyEmptied.Execute();
		}

		public bool Empty(IEnumerable<Coord> coords, bool force = false, bool forceHard = true, Voxel transferringToNewMap = null, bool notify = true)
		{
			if (!this.main.EditorEnabled && !this.Mutable)
				return false;

			bool modified = false;
			List<Box> boxAdditions = new List<Box>();
			List<Coord> removed = new List<Coord>();
			lock (this.MutationLock)
			{
				foreach (Voxel.Coord coord in coords)
				{
					Chunk chunk = this.GetChunk(coord.X, coord.Y, coord.Z, false);

					if (chunk == null)
						continue;

					Box box = chunk[coord.X - chunk.X, coord.Y - chunk.Y, coord.Z - chunk.Z];
					if (box != null && (force || !box.Type.Permanent) && (forceHard || !box.Type.Hard))
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

							removed.Add(new Voxel.Coord { X = coord.X, Y = coord.Y, Z = coord.Z, Data = box.Type });
							modified = true;
						}
					}
				}
				this.calculateAdjacency(boxAdditions.Where(x => x.Active));
			}

			if (modified && notify)
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
		public bool Empty(int x, int y, int z, bool force = false, bool forceHard = true, Voxel transferringToNewMap = null, bool notify = true)
		{
			if (!this.main.EditorEnabled && !this.Mutable)
				return false;

			bool modified = false;
			Voxel.Coord coord = new Coord { X = x, Y = y, Z = z, };
			lock (this.MutationLock)
			{
				Chunk chunk = this.GetChunk(x, y, z, false);

				if (chunk == null)
					return false;

				Box box = chunk[x - chunk.X, y - chunk.Y, z - chunk.Z];
				if (box != null && (force || !box.Type.Permanent) && (forceHard || !box.Type.Hard))
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

			if (modified && notify)
				this.notifyEmptied(new Coord[] { coord }, transferringToNewMap);

			return modified;
		}

		protected void addBoxWithoutAdjacency(Box box)
		{
			Chunk chunk = this.GetChunk(box.X, box.Y, box.Z);
			if (chunk != null)
			{
				chunk.MarkDirty(box);

				box.Chunk = chunk;

				for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
				{
					for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
					{
						for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
						{
							chunk[x, y, z] = box;
						}
					}
				}
			}
		}

		private Dictionary<Box, bool> adjacentCache = new Dictionary<Box, bool>();
		protected void addBox(Box box)
		{
			this.addBoxWithoutAdjacency(box);

			this.additions.Add(box);

			// Front face
			for (int x = box.X; x < box.X + box.Width; x++)
			{
				for (int y = box.Y; y < box.Y + box.Height; )
				{
					Box adjacent = this.GetBox(x, y, box.Z + box.Depth);
					if (adjacent != null)
					{
						if (!this.adjacentCache.ContainsKey(adjacent))
						{
							this.adjacentCache[adjacent] = true;
							lock (box.Adjacent)
								box.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
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
						if (!this.adjacentCache.ContainsKey(adjacent))
						{
							this.adjacentCache[adjacent] = true;
							lock (box.Adjacent)
								box.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
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
						if (!this.adjacentCache.ContainsKey(adjacent))
						{
							this.adjacentCache[adjacent] = true;
							lock (box.Adjacent)
								box.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
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
						if (!this.adjacentCache.ContainsKey(adjacent))
						{
							this.adjacentCache[adjacent] = true;
							lock (box.Adjacent)
								box.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
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
						if (!this.adjacentCache.ContainsKey(adjacent))
						{
							this.adjacentCache[adjacent] = true;
							lock (box.Adjacent)
								box.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
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
						if (!this.adjacentCache.ContainsKey(adjacent))
						{
							this.adjacentCache[adjacent] = true;
							lock (box.Adjacent)
								box.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
								adjacent.Adjacent.Add(box);
						}
						z = adjacent.Z + adjacent.Depth;
					}
					else
						z++;
				}
			}

			this.adjacentCache.Clear();
		}

		public void RebuildAdjacency()
		{
			for (int i = 0; i < this.Chunks.Count; i++)
				this.Chunks[i].RebuildAdjacency();
		}

		private Dictionary<BoxRelationship, bool> relationshipCache = new Dictionary<BoxRelationship, bool>();
		protected void calculateAdjacency(IEnumerable<Box> boxes)
		{
			foreach (Box box in boxes)
			{
				if (box.Adjacent == null)
					box.Adjacent = new List<Box>();
				else
					box.Adjacent.Clear();
			}

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
							if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
							{
								this.relationshipCache[relationship1] = true;
								lock (box.Adjacent)
									box.Adjacent.Add(adjacent);
								lock (adjacent.Adjacent)
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
							if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
							{
								this.relationshipCache[relationship1] = true;
								lock (box.Adjacent)
									box.Adjacent.Add(adjacent);
								lock (adjacent.Adjacent)
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
							if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
							{
								this.relationshipCache[relationship1] = true;
								lock (box.Adjacent)
									box.Adjacent.Add(adjacent);
								lock (adjacent.Adjacent)
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
							if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
							{
								this.relationshipCache[relationship1] = true;
								lock (box.Adjacent)
									box.Adjacent.Add(adjacent);
								lock (adjacent.Adjacent)
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
							if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
							{
								this.relationshipCache[relationship1] = true;
								lock (box.Adjacent)
									box.Adjacent.Add(adjacent);
								lock (adjacent.Adjacent)
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
							if (!this.relationshipCache.ContainsKey(relationship1) && !this.relationshipCache.ContainsKey(relationship2))
							{
								this.relationshipCache[relationship1] = true;
								lock (box.Adjacent)
									box.Adjacent.Add(adjacent);
								lock (adjacent.Adjacent)
									adjacent.Adjacent.Add(box);
							}
							z = adjacent.Z + adjacent.Depth;
						}
						else
							z++;
					}
				}
			}

			this.relationshipCache.Clear();
		}

		protected bool regenerateSurfaces(Box box)
		{
			int x, y, z;
			State type = box.Type;
			bool stop;
			Box adjacent;

			box.Surfaces = 0;
			x = box.X + box.Width;
			stop = false;

			for (y = box.Y; y < box.Y + box.Height; y++)
			{
				for (z = box.Z; z < box.Z + box.Depth; z++)
				{
					adjacent = this.GetBox(x, y, z);
					if (adjacent == null || adjacent.Type != type)
					{
						box.Surfaces |= 1 << (int)Direction.PositiveX;
						stop = true;
						break;
					}
				}
				if (stop)
					break;
			}

			x = box.X - 1;
			stop = false;

			for (y = box.Y; y < box.Y + box.Height; y++)
			{
				for (z = box.Z; z < box.Z + box.Depth; z++)
				{
					adjacent = this.GetBox(x, y, z);
					if (adjacent == null || adjacent.Type != type)
					{
						box.Surfaces |= 1 << (int)Direction.NegativeX;
						stop = true;
						break;
					}
				}
				if (stop)
					break;
			}

			y = box.Y + box.Height;
			stop = false;

			for (x = box.X; x < box.X + box.Width; x++)
			{
				for (z = box.Z; z < box.Z + box.Depth; z++)
				{
					adjacent = this.GetBox(x, y, z);
					if (adjacent == null || adjacent.Type != type)
					{
						box.Surfaces |= 1 << (int)Direction.PositiveY;
						stop = true;
						break;
					}
				}
				if (stop)
					break;
			}

			y = box.Y - 1;
			stop = false;

			for (x = box.X; x < box.X + box.Width; x++)
			{
				for (z = box.Z; z < box.Z + box.Depth; z++)
				{
					adjacent = this.GetBox(x, y, z);
					if (adjacent == null || adjacent.Type != type)
					{
						box.Surfaces |= 1 << (int)Direction.NegativeY;
						stop = true;
						break;
					}
				}
				if (stop)
					break;
			}

			z = box.Z + box.Depth;
			stop = false;

			for (y = box.Y; y < box.Y + box.Height; y++)
			{
				for (x = box.X; x < box.X + box.Width; x++)
				{
					adjacent = this.GetBox(x, y, z);
					if (adjacent == null || adjacent.Type != type)
					{
						box.Surfaces |= 1 << (int)Direction.PositiveZ;
						stop = true;
						break;
					}
				}
			}

			z = box.Z - 1;
			stop = false;

			for (y = box.Y; y < box.Y + box.Height; y++)
			{
				for (x = box.X; x < box.X + box.Width; x++)
				{
					adjacent = this.GetBox(x, y, z);
					if (adjacent == null || adjacent.Type != type)
					{
						box.Surfaces |= 1 << (int)Direction.NegativeZ;
						stop = true;
						break;
					}
				}
				if (stop)
					break;
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
						chunk[x, y, z] = null;
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
			lock (box.Adjacent)
			{
				for (int i = 0; i < box.Adjacent.Count; i++)
				{
					Box box2 = box.Adjacent[i];
					lock (box2.Adjacent)
						box2.Adjacent.Remove(box);
				}
			}
		}

		public void Regenerate(Action<List<DynamicVoxel>> callback = null)
		{
			workQueue.Enqueue(new WorkItem { Voxel = this, Callback = callback });
		}

		private struct WorkItem
		{
			public Voxel Voxel;
			public Action<List<DynamicVoxel>> Callback;
		}

		private static BlockingQueue<WorkItem> workQueue = new BlockingQueue<WorkItem>(32);

		private static void worker()
		{
			while (true)
			{
				WorkItem item = Voxel.workQueue.Dequeue();
				item.Voxel.RegenerateImmediately(item.Callback);
			}
		}

		private static Thread workThread;

		public object MutationLock = new object();

		private Dictionary<Box, bool> regeneratedCache = new Dictionary<Box, bool>();

		/// <summary>
		/// Applies any changes made to the map.
		/// </summary>
		public void RegenerateImmediately(Action<List<DynamicVoxel>> callback = null)
		{
			List<DynamicVoxel> spawnedMaps = new List<DynamicVoxel>();

			if (!this.main.EditorEnabled && !this.Mutable)
				return;
			List<Chunk> chunks;
			lock (this.MutationLock)
			{
				if (!this.Active)
					return;

				if (!this.main.EditorEnabled)
				{
					// Spawn new maps for portions that have been cut off

					IEnumerable<IEnumerable<Box>> islands;
					if (this.getAdjacentIslands(this.removalCoords, out islands))
					{
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
							lock (Voxel.spawns)
							{
								Voxel.spawns.Add(new SpawnGroup
								{
									Source = this,
									Callback = callback,
									Islands = finalIslands
								});
							}
						}
					}
				}
				this.removalCoords.Clear();

				// Figure out which blocks need updating

				// Update graphics

				for (int i = 0; i < this.removals.Count; i++)
				{
					Box box = this.removals[i];
					for (int j = 0; j < box.Adjacent.Count; j++)
					{
						Box adjacent = box.Adjacent[j];
						if (adjacent.Active && adjacent.Type == box.Type && !this.regeneratedCache.ContainsKey(adjacent))
							this.regeneratedCache[adjacent] = this.regenerateSurfaces(adjacent);
					}
				}

				for (int i = 0; i < this.additions.Count; i++)
				{
					Box box = this.additions[i];
					if (box.Active)
					{
						if (!this.regeneratedCache.ContainsKey(box))
							this.regeneratedCache[box] = this.regenerateSurfaces(box);

						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent.Active && adjacent.Type == box.Type && !this.regeneratedCache.ContainsKey(adjacent))
								this.regeneratedCache[adjacent] = this.regenerateSurfaces(adjacent);
						}
					}
				}

				List<Box> boxes = this.regeneratedCache.Keys.ToList();

				bool[] modifications = this.regeneratedCache.Values.ToArray();
				this.regeneratedCache.Clear();
				this.simplify(boxes, modifications);
				this.simplify(boxes, modifications);

				this.applyChanges(boxes, modifications);
				chunks = this.Chunks.ToList();
			}
			this.updateGraphics(chunks);
		}

		private void updateGraphics(IEnumerable<Chunk> chunks)
		{
			foreach (Chunk chunk in chunks)
				chunk.RefreshImmediately();
		}

		private void applyChanges(List<Box> boxes, bool[] modifications)
		{
			for (int i = 0; i < this.removals.Count; i++)
			{
				Box box = this.removals[i];
				if (box.Added)
					box.Chunk.Boxes.RemoveAt(box.ChunkIndex);
			}

			for (int i = 0; i < boxes.Count; i++)
			{
				Box box = boxes[i];
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
			}

			for (int i = 0; i < this.additions.Count; i++)
			{
				Box box = this.additions[i];
				if (box.Active && !box.Added)
					box.Chunk.Boxes.Add(box);
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
			State state = input.First().Type;
			return this.GetContiguous(input, x => x.Type == state);
		}

		public List<Box> GetContiguous(IEnumerable<Box> input, Func<Box, bool> filter)
		{
			Queue<Box> boxes = new Queue<Box>();

			foreach (Box box in input)
				boxes.Enqueue(box);

			List<Box> result = new List<Box>();
			Dictionary<Box, bool> alreadyVisited = new Dictionary<Box, bool>();

			while (boxes.Count > 0)
			{
				Box b = boxes.Dequeue();

				if (filter(b))
				{
					result.Add(b);
					if (b.Adjacent != null)
					{
						lock (b.Adjacent)
						{
							for (int i = 0; i < b.Adjacent.Count; i++)
							{
								Box adjacent = b.Adjacent[i];
								if (!alreadyVisited.ContainsKey(adjacent))
								{
									boxes.Enqueue(adjacent);
									alreadyVisited.Add(adjacent, true);
								}
							}
						}
					}
				}
			}

			return result;
		}

		private bool getAdjacentIslands(IEnumerable<Coord> removals, out IEnumerable<IEnumerable<Box>> islands)
		{
			List<Dictionary<Box, bool>> lists = new List<Dictionary<Box, bool>>();

			int allLists = 0;
			// Build adjacency lists
			foreach (Coord removal in removals)
			{
				if (this[removal] != Voxel.States.Empty) // A new block was subsequently filled in after removal. Forget about it.
					continue;

				for (int i = 0; i < 6; i++)
				{
					Coord adjacentCoord = removal.Move(DirectionExtensions.Directions[i]);
					Box box = this.GetBox(adjacentCoord);
					if (box == null)
						continue;
					bool alreadyFound = false;
					for (int j = 0; j < lists.Count; j++)
					{
						Dictionary<Box, bool> list = lists[j];
						if (list.ContainsKey(box))
						{
							alreadyFound = true;
							break;
						}
					}
					if (alreadyFound)
						continue;
					Dictionary<Box, bool> newList = new Dictionary<Box, bool>();
					bool supported = this.buildAdjacency(box, this.internalBoxAdjacencyCache, newList);
					if (newList.Count > 0)
					{
						allLists++;
						if (!supported)
							lists.Add(newList);
					}
				}
			}

			// Spawn the dynamic maps
			islands = lists.Select(x => x.Keys);
			return allLists > 1;
		}

		public IEnumerable<IEnumerable<Box>> GetAdjacentIslands(IEnumerable<Coord> removals, Func<State, bool> filter, Func<State, bool> search)
		{
			List<Dictionary<Box, bool>> lists = new List<Dictionary<Box, bool>>();

			// Build adjacency lists
			foreach (Coord removal in removals)
			{
				for (int i = 0; i < 6; i++)
				{
					Coord adjacentCoord = removal.Move(DirectionExtensions.Directions[i]);
					Box box = this.GetBox(adjacentCoord);
					if (box == null || (!filter(box.Type) && !search(box.Type)))
						continue;
					bool alreadyFound = false;
					for (int j = 0; j < lists.Count; j++)
					{
						Dictionary<Box, bool> list = lists[j];
						if (list.ContainsKey(box))
						{
							alreadyFound = true;
							break;
						}
					}
					if (alreadyFound)
						continue;
					Dictionary<Box, bool> newList = new Dictionary<Box, bool>();
					bool found = this.buildAdjacency(box, this.externalBoxAdjacencyCache, newList, filter, search);
					if (!found && newList.Count > 0)
						lists.Add(newList);
				}
			}

			return lists.Select(x => x.Keys);
		}

		public Coord? FindClosestFilledCell(Coord coord, int maxDistance = 20)
		{
			if (this[coord].ID != 0)
				return coord;

			Vector3 pos = this.GetRelativePosition(coord);

			Coord? closestCoord = null;

			for (int radius = 1; radius < maxDistance; radius++)
			{
				float closestDistance = float.MaxValue;

				// Left
				for (int y = -radius; y <= radius; y++)
				{
					for (int z = -radius; z <= radius; z++)
					{
						Coord c = coord.Move(-radius, y, z);
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
						Coord c = coord.Move(radius, y, z);
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
						Coord c = coord.Move(x, -radius, z);
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
						Coord c = coord.Move(x, radius, z);
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
						Coord c = coord.Move(x, y, -radius);
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
						Coord c = coord.Move(x, y, radius);
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

		private Queue<Box> externalBoxAdjacencyCache = new Queue<Box>();
		private Queue<Box> internalBoxAdjacencyCache = new Queue<Box>();

		private bool buildAdjacency(Box box, Queue<Box> boxAdjacencyCache, Dictionary<Box, bool> list, Func<State, bool> filter, Func<State, bool> search)
		{
			if (search(box.Type))
			{
				list.Add(box, true);
				return true;
			}

			if (filter(box.Type) && !list.ContainsKey(box))
			{
				boxAdjacencyCache.Enqueue(box);
				list.Add(box, true);
			}

			while (boxAdjacencyCache.Count > 0)
			{
				Box b = boxAdjacencyCache.Dequeue();

				if (b.Adjacent != null)
				{
					lock (b.Adjacent)
					{
						for (int i = 0; i < b.Adjacent.Count; i++)
						{
							Box adjacent = b.Adjacent[i];
							if (!list.ContainsKey(adjacent))
							{
								if (search(adjacent.Type))
								{
									boxAdjacencyCache.Clear();
									return true;
								}
								else if (filter(adjacent.Type))
								{
									boxAdjacencyCache.Enqueue(adjacent);
									list.Add(adjacent, true);
								}
							}
						}
					}
				}
			}
			boxAdjacencyCache.Clear();
			return false;
		}

		private bool buildAdjacency(Box box, Queue<Box> boxAdjacencyCache, Dictionary<Box, bool> list)
		{
			if (!list.ContainsKey(box))
			{
				boxAdjacencyCache.Enqueue(box);
				list.Add(box, true);
			}

			while (boxAdjacencyCache.Count > 0)
			{
				Box b = boxAdjacencyCache.Dequeue();

				if (b.Type.Supported)
				{
					boxAdjacencyCache.Clear();
					return true;
				}

				if (b.Adjacent != null)
				{
					lock (b.Adjacent)
					{
						for (int i = 0; i < b.Adjacent.Count; i++)
						{
							Box adjacent = b.Adjacent[i];
							if (!list.ContainsKey(adjacent))
							{
								boxAdjacencyCache.Enqueue(adjacent);
								list.Add(adjacent, true);
							}
						}
					}
				}
			}
			boxAdjacencyCache.Clear();
			return false;
		}

		private bool[] simplify(List<Box> list, bool[] modified = null)
		{
			if (modified == null)
				modified = new bool[list.Count];

			// Z
			for (int i = 0; i < list.Count; i++)
			{
				Box baseBox = list[i];
				if (!baseBox.Active)
					continue;

				Chunk chunk = baseBox.Chunk;
				for (int z2 = baseBox.Z + baseBox.Depth - chunk.Z; z2 < this.chunkSize; )
				{
					Box box = chunk[baseBox.X - chunk.X, baseBox.Y - chunk.Y, z2];
					if (box != null && box.X == baseBox.X && box.Y == baseBox.Y && box.Z == z2 + chunk.Z && box.Type == baseBox.Type && box.Width == baseBox.Width && box.Height == baseBox.Height)
					{
						box.Active = false;
						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent == baseBox)
								continue;
							lock (baseBox.Adjacent)
								baseBox.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
								adjacent.Adjacent.Add(baseBox);
						}
						this.removeBoxAdjacency(box);
						this.removals.Add(box);
						baseBox.Depth += box.Depth;
						box.Chunk.MarkDirty(box);

						bool negativeZ = (baseBox.Surfaces & (1 << (int)Direction.NegativeZ)) != 0;
						baseBox.Surfaces |= box.Surfaces;
						baseBox.Surfaces = baseBox.Surfaces.SetBit((int)Direction.NegativeZ, negativeZ);

						for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
						{
							for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
							{
								for (z2 = box.Z - chunk.Z; z2 < box.Z + box.Depth - chunk.Z; z2++)
									chunk[x, y, z2] = baseBox;
							}
						}
						modified[i] = true;
					}
					else
						break;
				}
			}

			// X
			for (int i = 0; i < list.Count; i++)
			{
				Box baseBox = list[i];
				if (!baseBox.Active)
					continue;

				Chunk chunk = baseBox.Chunk;
				for (int x2 = baseBox.X + baseBox.Width - chunk.X; x2 < this.chunkSize; )
				{
					Box box = chunk[x2, baseBox.Y - chunk.Y, baseBox.Z - chunk.Z];
					if (box != null && box.X == x2 + chunk.X && box.Y == baseBox.Y && box.Z == baseBox.Z && box.Type == baseBox.Type && box.Depth == baseBox.Depth && box.Height == baseBox.Height)
					{
						box.Active = false;
						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent == baseBox)
								continue;
							lock (baseBox.Adjacent)
								baseBox.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
								adjacent.Adjacent.Add(baseBox);
						}
						this.removeBoxAdjacency(box);
						this.removals.Add(box);
						baseBox.Width += box.Width;
						box.Chunk.MarkDirty(box);

						bool negativeX = (baseBox.Surfaces & (1 << (int)Direction.NegativeX)) != 0;
						baseBox.Surfaces |= box.Surfaces;
						baseBox.Surfaces = baseBox.Surfaces.SetBit((int)Direction.NegativeX, negativeX);

						for (x2 = box.X - chunk.X; x2 < box.X + box.Width - chunk.X; x2++)
						{
							for (int y = box.Y - chunk.Y; y < box.Y + box.Height - chunk.Y; y++)
							{
								for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
									chunk[x2, y, z] = baseBox;
							}
						}
						modified[i] = true;
					}
					else
						break;
				}
			}

			// Y
			for (int i = 0; i < list.Count; i++)
			{
				Box baseBox = list[i];
				if (!baseBox.Active)
					continue;

				Chunk chunk = baseBox.Chunk;
				for (int y2 = baseBox.Y + baseBox.Height - chunk.Y; y2 < this.chunkSize; )
				{
					Box box = chunk[baseBox.X - chunk.X, y2, baseBox.Z - chunk.Z];
					if (box != null && box.X == baseBox.X && box.Y == y2 + chunk.Y && box.Z == baseBox.Z && box.Type == baseBox.Type && box.Depth == baseBox.Depth && box.Width == baseBox.Width)
					{
						box.Active = false;
						foreach (Box adjacent in box.Adjacent)
						{
							if (adjacent == baseBox)
								continue;
							lock (baseBox.Adjacent)
								baseBox.Adjacent.Add(adjacent);
							lock (adjacent.Adjacent)
								adjacent.Adjacent.Add(baseBox);
						}
						this.removeBoxAdjacency(box);
						this.removals.Add(box);
						baseBox.Height += box.Height;
						box.Chunk.MarkDirty(box);

						bool negativeY = (baseBox.Surfaces & (1 << (int)Direction.NegativeY)) != 0;
						baseBox.Surfaces |= box.Surfaces;
						baseBox.Surfaces = baseBox.Surfaces.SetBit((int)Direction.NegativeY, negativeY);

						for (int x = box.X - chunk.X; x < box.X + box.Width - chunk.X; x++)
						{
							for (y2 = box.Y - chunk.Y; y2 < box.Y + box.Height - chunk.Y; y2++)
							{
								for (int z = box.Z - chunk.Z; z < box.Z + box.Depth - chunk.Z; z++)
									chunk[x, y2, z] = baseBox;
							}
						}
						modified[i] = true;
					}
					else
						break;
				}
			}

			return modified;
		}

		public RaycastResult Raycast(Coord start, Direction dir, int length, Func<int, t, bool> filter = null)
		{
			return this.Raycast(start, start.Move(dir, length), filter);
		}

		public RaycastResult Raycast(Coord start, Coord end, Func<int, t, bool> filter = null)
		{
			return this.Raycast(this.GetRelativePosition(start), this.GetRelativePosition(end), filter);
		}

		private Coord getChunkCoordinateFromCoordinate(Coord coord)
		{
			return new Coord { X = (coord.X - this.minX) / this.chunkSize, Y = (coord.Y - this.minY) / this.chunkSize, Z = (coord.Z - this.minZ) / this.chunkSize };
		}

		private IEnumerable<Chunk> rasterizeChunks(Vector3 startRelative, Vector3 endRelative)
		{
			// Adapted from PolyVox
			// http://www.volumesoffun.com/polyvox/documentation/library/doc/html/_raycast_8inl_source.html

			startRelative = (startRelative - new Vector3(this.minX, this.minY, this.minZ)) / this.chunkSize;
			endRelative = (endRelative - new Vector3(this.minX, this.minY, this.minZ)) / this.chunkSize;

			Coord startCoord = new Coord { X = (int)startRelative.X, Y = (int)startRelative.Y, Z = (int)startRelative.Z };
			Coord endCoord = new Coord { X = (int)endRelative.X, Y = (int)endRelative.Y, Z = (int)endRelative.Z };

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

			Coord coord = startCoord.Clone();

			Direction xDirection = dx > 0 ? Direction.NegativeX : (dx < 0 ? Direction.PositiveX : Direction.None);
			Direction yDirection = dy > 0 ? Direction.NegativeY : (dy < 0 ? Direction.PositiveY : Direction.None);
			Direction zDirection = dz > 0 ? Direction.NegativeZ : (dz < 0 ? Direction.PositiveZ : Direction.None);

			for (; ; )
			{
				if (coord.X >= 0 && coord.X < this.maxChunks
					&& coord.Y >= 0 && coord.Y < this.maxChunks
					&& coord.Z >= 0 && coord.Z < this.maxChunks)
				{
					if (this.chunks == null)
						break;
					else
						yield return this.chunks[coord.X, coord.Y, coord.Z];
				}

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

		public IEnumerable<Coord> Rasterize(Vector3 start, Vector3 end)
		{
			start = this.GetRelativePosition(start);
			end = this.GetRelativePosition(end);

			Coord startCoord = this.GetCoordinateFromRelative(start);
			Coord endCoord = this.GetCoordinateFromRelative(end);

			foreach (Coord coord in this.rasterize(start, end, startCoord, endCoord))
				yield return coord;
		}

		public IEnumerable<Coord> Rasterize(Coord startCoord, Coord endCoord)
		{
			Vector3 start = this.GetRelativePosition(startCoord);
			Vector3 end = this.GetRelativePosition(endCoord);

			foreach (Coord coord in this.rasterize(start, end, startCoord, endCoord))
				yield return coord;
		}

		private IEnumerable<Coord> rasterize(Vector3 start, Vector3 end, Coord startCoord, Coord endCoord)
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

			Coord coord = startCoord.Clone();

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

		public RaycastResult Raycast(Vector3 start, Vector3 end, Func<int, t, bool> filter = null)
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

					RaycastResult result = this.raycastChunk(intersections[0], intersections[1], c, filter);
					if (result.Coordinate != null)
					{
						result.Distance = (result.Position - absoluteStart).Length();
						return result;
					}
				}
			}

			return new RaycastResult { Coordinate = null };
		}

		private RaycastResult raycastChunk(Vector3 start, Vector3 end, Chunk c, Func<int, t, bool> filter)
		{
			if (filter == null)
				filter = (x, y) => true;

			Vector3 actualStart = start, actualEnd = end;

			start -= new Vector3(c.X, c.Y, c.Z);
			end -= new Vector3(c.X, c.Y, c.Z);

			Coord startCoord = new Coord { X = (int)start.X, Y = (int)start.Y, Z = (int)start.Z };
			Coord endCoord = new Coord { X = (int)end.X, Y = (int)end.Y, Z = (int)end.Z };

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

			Coord coord = startCoord.Clone();

			Direction normal = Direction.None;

			Direction xDirection = dx > 0 ? Direction.NegativeX : (dx < 0 ? Direction.PositiveX : Direction.None);
			Direction yDirection = dy > 0 ? Direction.NegativeY : (dy < 0 ? Direction.PositiveY : Direction.None);
			Direction zDirection = dz > 0 ? Direction.NegativeZ : (dz < 0 ? Direction.PositiveZ : Direction.None);

			int i = 0;

			for (; ; )
			{
				if (coord.X >= 0 && coord.X < this.chunkSize
					&& coord.Y >= 0 && coord.Y < this.chunkSize
					&& coord.Z >= 0 && coord.Z < this.chunkSize)
				{
					Box box = c[coord.X, coord.Y, coord.Z];
					if (box != null && filter(i, box.Type.ID))
					{
						Coord actualCoord = coord.Move(c.X, c.Y, c.Z);
						actualCoord.Data = box.Type;

						// Found intersection

						Vector3 ray = actualEnd - actualStart;

						if (normal == Direction.None)
							normal = DirectionExtensions.GetDirectionFromVector(-Vector3.Normalize(ray));

						Vector3 norm = normal.GetVector();

						Vector3 planePosition = new Vector3(actualCoord.X + 0.5f, actualCoord.Y + 0.5f, actualCoord.Z + 0.5f) + norm * 0.5f;

						return new RaycastResult { Coordinate = actualCoord, Normal = normal, Position = this.GetAbsolutePosition(actualStart + (ray * Vector3.Dot((planePosition - actualStart), norm) / Vector3.Dot(ray, norm))) };
					}
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
				i++;
			}
			return new RaycastResult { Coordinate = null };
		}

		public RaycastResult Raycast(Vector3 rayStart, Vector3 ray, float length, Func<int, t, bool> filter = null)
		{
			return this.Raycast(rayStart, rayStart + (ray * length), filter);
		}

		public State this[Coord coord]
		{
			get
			{
				return this[coord.X, coord.Y, coord.Z];
			}
		}

		public State this[int x, int y, int z]
		{
			get
			{
				Chunk chunk = this.GetChunk(x, y, z, false);
				if (chunk == null)
					return Voxel.States.Empty;
				else
				{
					Box box = chunk[x - chunk.X, y - chunk.Y, z - chunk.Z];
					if (box == null)
						return Voxel.States.Empty;
					else
						return box.Type;
				}
			}
		}

		public State this[Vector3 pos]
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
		public Coord GetCoordinate(Vector3 position)
		{
			return this.GetCoordinateFromRelative(this.GetRelativePosition(position));
		}

		/// <summary>
		/// Get the coordinates for the specified position in space.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Coord GetCoordinateFromRelative(Vector3 pos)
		{
			return new Coord
			{
				X = (int)Math.Floor(pos.X),
				Y = (int)Math.Floor(pos.Y),
				Z = (int)Math.Floor(pos.Z)
			};
		}

		public Coord GetCoordinate(int x, int y, int z)
		{
			return new Coord
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

		public Vector3 GetAbsolutePosition(Coord coord)
		{
			return this.GetAbsolutePosition(coord.X, coord.Y, coord.Z);
		}

		public Vector3 GetRelativePosition(Coord coord)
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
		public Box GetBox(Coord coord)
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
			if (chunk == null)
				return null;
			else
				return chunk[x - chunk.X, y - chunk.Y, z - chunk.Z];
		}
	}

	public class DynamicVoxel : Voxel, IUpdateableComponent
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

		public Property<bool> IsAffectedByGravity = new Property<bool> { Value = true };

		public Property<bool> IsAlwaysActive = new Property<bool> { Value = false };

		public Property<float> KineticFriction = new Property<float> { Value = MaterialManager.DefaultKineticFriction };

		public Property<float> StaticFriction = new Property<float> { Value = MaterialManager.DefaultStaticFriction };

		public Property<bool> CannotSuspendByDistance = new Property<bool>();

		private bool firstPhysicsUpdate = true;
		private bool physicsDirty;

		[XmlIgnore]
		public Command PhysicsUpdated = new Command();

		public DynamicVoxel()
			: this(0, 0, 0)
		{

		}

		public DynamicVoxel(int offsetX, int offsetY, int offsetZ)
			: base(4, 20)
		{
			this.OffsetX = offsetX;
			this.OffsetY = offsetY;
			this.OffsetZ = offsetZ;
		}

		protected override Chunk newChunk()
		{
			Chunk chunk = new Chunk(this.chunkSize);
			chunk.Voxel = this;
			return chunk;
		}

		public override void Awake()
		{
			this.PhysicsEntity = new MorphableEntity(new CompoundShape(new CompoundShapeEntry[] { new CompoundShapeEntry(new BoxShape(1, 1, 1), Vector3.Zero, 1.0f) }));
			this.PhysicsEntity.Tag = this;
			if (this.main.EditorEnabled)
				this.PhysicsEntity.BecomeKinematic();
			base.Awake();

			this.PhysicsEntity.IsAffectedByGravity = false;
			this.Add(new SetBinding<bool>(this.IsAffectedByGravity, delegate(bool value)
			{
				if (value)
				{
					this.PhysicsEntity.LinearDamping = DynamicVoxel.defaultLinearDamping;
					this.PhysicsEntity.AngularDamping = DynamicVoxel.defaultAngularDamping;
				}
				else
				{
					this.PhysicsEntity.LinearDamping = DynamicVoxel.floatingLinearDamping;
					this.PhysicsEntity.AngularDamping = DynamicVoxel.floatingAngularDamping;
				}
				this.PhysicsEntity.IsAffectedByGravity = value;
				this.PhysicsEntity.ActivityInformation.Activate();
			}));

			this.Add(new SetBinding<float>(this.KineticFriction, delegate(float value)
			{
				this.PhysicsEntity.Material.KineticFriction = value;
			}));

			this.Add(new SetBinding<float>(this.StaticFriction, delegate(float value)
			{
				this.PhysicsEntity.Material.StaticFriction = value;
			}));

			this.Add(new SetBinding<bool>(this.IsAlwaysActive, delegate(bool value)
			{
				this.PhysicsEntity.ActivityInformation.IsAlwaysActive = value;
				this.PhysicsEntity.ActivityInformation.Activate();
			}));

			this.Add(new SetBinding<Matrix>(this.Transform, delegate(Matrix value)
			{
				this.PhysicsEntity.WorldTransform = value;
			}));

			this.Add(new SetBinding<Vector3>(this.LinearVelocity, delegate(Vector3 value)
			{
				this.PhysicsEntity.LinearVelocity = value;
			}));

			this.Add(new SetBinding<Vector3>(this.AngularVelocity, delegate(Vector3 value)
			{
				this.PhysicsEntity.AngularVelocity = value;
			}));

			this.Add(new CommandBinding(this.OnSuspended, delegate()
			{
				if (this.addedToSpace)
				{
					this.main.Space.SpaceObjectBuffer.Remove(this.PhysicsEntity);
					this.addedToSpace = false;
				}
				for (int i = 0; i < this.Chunks.Count; i++)
					this.Chunks[i].Deactivate();
			}));

			this.Add(new CommandBinding(this.OnResumed, delegate()
			{
				this.PhysicsEntity.LinearVelocity = Vector3.Zero;
				if (!this.addedToSpace && this.PhysicsEntity.Volume > 0.0f && !this.main.EditorEnabled)
				{
					this.main.Space.SpaceObjectBuffer.Add(this.PhysicsEntity);
					this.addedToSpace = true;
				}
				for (int i = 0; i < this.Chunks.Count; i++)
					this.Chunks[i].Activate();
				this.PhysicsEntity.ActivityInformation.Activate();
			}));

			this.Add(new SetBinding<bool>(this.CannotSuspendByDistance, delegate(bool value)
			{
				this.Entity.CannotSuspendByDistance = value;
			}));
		}

		void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		protected override void postDeserialization()
		{
			base.postDeserialization();
			this.UpdatePhysicsImmediately();
		}

		public override void updatePhysics()
		{
			this.physicsDirty = true;
		}

		protected override void transformBinding()
		{
			// Disable chunk transform binding, we already handle it
		}

		private void updatePhysicsImmediately()
		{
			for (int i = 0; i < this.Chunks.Count; i++)
				this.Chunks[i].Activate();

			bool hasVolume = false;
			List<CompoundShapeEntry> bodies = new List<CompoundShapeEntry>();
			float mass = 0.0f;
			lock (this.MutationLock)
			{
				for (int i = 0; i < this.Chunks.Count; i++)
				{
					Chunk c = this.Chunks[i];
					for (int j = 0; j < c.Boxes.Count; j++)
					{
						Box box = c.Boxes[j];
						if (!box.Type.Fake)
						{
							bodies.Add(box.GetCompoundShapeEntry());
							mass += box.Width * box.Height * box.Depth * box.Type.Density;
						}
					}
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
					collisionInfo.Events.ContactCreated += new BEPUphysics.BroadPhaseEntries.Events.ContactCreatedEventHandler<BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable>(Events_ContactCreated);
					collisionInfo.Tag = this;
					this.PhysicsEntity.SetCollisionInformation(collisionInfo, mass);
					this.PhysicsEntity.ActivityInformation.Activate();
				}
			}

			if (!this.addedToSpace && hasVolume && !this.Suspended && !this.main.EditorEnabled)
			{
				this.main.Space.SpaceObjectBuffer.Add(this.PhysicsEntity);
				this.addedToSpace = true;
			}
			else if (this.addedToSpace && !hasVolume)
			{
				this.main.Space.SpaceObjectBuffer.Remove(this.PhysicsEntity);
				this.addedToSpace = false;
			}

			this.firstPhysicsUpdate = false;
		}

		public void UpdatePhysicsImmediately()
		{
			this.physicsDirty = false;
			this.updatePhysicsImmediately();
		}

		void IUpdateableComponent.Update(float dt)
		{
			bool dirty = this.physicsDirty;
			if (dirty)
			{
				this.physicsDirty = false;
				this.updatePhysicsImmediately();
			}

			if (dirty && !this.firstPhysicsUpdate)
			{
				this.PhysicsUpdated.Execute();
				this.firstPhysicsUpdate = false;
			}

			this.Transform.Value = this.PhysicsEntity.WorldTransform;
			this.LinearVelocity.Value = this.PhysicsEntity.LinearVelocity;
			this.AngularVelocity.Value = this.PhysicsEntity.AngularVelocity;
		}

		public override void delete()
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