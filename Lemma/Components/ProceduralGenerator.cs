using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class ProceduralGenerator : Component<Main>
	{
		// =========================
		// Classic 2D and 3D perlin noise implementation
		// Adapted from http://webstaff.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf
		// =========================

		// Array of possible gradients to choose from for each cell.
		// The permutation array indexes into this array.
		private static Vector3[] gradients = new[]
		{
			new Vector3(1, 1, 0),
			new Vector3(-1, 1, 0),
			new Vector3(1, -1, 0),
			new Vector3(-1, -1, 0),
			new Vector3(1, 0, 1),
			new Vector3(-1, 0, 1),
			new Vector3(1, 0, -1),
			new Vector3(-1, 0, -1),
			new Vector3(0, 1, 1),
			new Vector3(0, -1, 1),
			new Vector3(0, 1, -1),
			new Vector3(0, -1, -1),
		};

		// Pseudo-random permutation index array.
		// This is actually constant in the reference implementation.
		// But we want a different map each time.
		// This way I could set a seed if I wanted to re-generate the same map later.
		private int[] permutations = new int[512];

		public ProceduralGenerator()
		{
			this.reseed();
		}

		private void reseed()
		{
			Random random = new Random();
			for (int i = 0; i < 512; i++)
				this.permutations[i] = random.Next(256);
		}

		public Command Reseed = new Command { ShowInEditor = true };

		// Get a pseudo-random gradient for the given 2D cell
		private Vector2 gradientAtCell2d(int x, int y)
		{
			Vector3 g = gradients[this.permutations[x + this.permutations[y]] % gradients.Length];
			return new Vector2(g.X, g.Y);
		}

		// Get a psuedo-random gradient for the given 3D cell
		private Vector3 gradientAtCell3d(Map.Coordinate coord)
		{
			return gradients[this.permutations[coord.X + this.permutations[coord.Y + this.permutations[coord.Z]]] % gradients.Length];
		}

		// f(x) = 6x^5 - 15x^4 + 10x^3
		private static float blendCurve(float t)
		{
			return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
		}

		// Interpolate between two float values
		private static float lerp(float a, float b, float blend)
		{
			return a + (blend * (b - a));
		}

		// Classic 2D perlin noise
		float noise2d(Vector2 pos)
		{
			int x = (int)Math.Floor(pos.X) & 255;
			int y = (int)Math.Floor(pos.Y) & 255;
			
			Vector2 withinCell = pos - new Vector2(x, y);
			
			// Calculate contribution of gradients from each cell
			float contribution00 = Vector2.Dot(this.gradientAtCell2d(x, y), withinCell);
			float contribution01 = Vector2.Dot(this.gradientAtCell2d(x, y + 1), withinCell - new Vector2(0, 1));
			float contribution10 = Vector2.Dot(this.gradientAtCell2d(x + 1, y), withinCell - new Vector2(1, 0));
			float contribution11 = Vector2.Dot(this.gradientAtCell2d(x + 1, y + 1), withinCell - new Vector2(1, 1));
			
			Vector2 blend = new Vector2(blendCurve(withinCell.X), blendCurve(withinCell.Y));
			
			// Interpolate along X
			float contribution0 = lerp(contribution00, contribution10, blend.X);
			float contribution1 = lerp(contribution01, contribution11, blend.X);
			
			// Interpolate along Y
			return lerp(contribution0, contribution1, blend.Y);
		}

		// Classic 3D perlin noise
		float noise3d(Vector3 pos)
		{
			Map.Coordinate cell = new Map.Coordinate { X = (int)Math.Floor(pos.X) & 255, Y = (int)Math.Floor(pos.Y) & 255, Z = (int)Math.Floor(pos.Z) & 255 };
			
			pos.X = pos.X % 256;
			pos.Y = pos.Y % 256;
			pos.Z = pos.Z % 256;
			Vector3 withinCell = pos - new Vector3(cell.X, cell.Y, cell.Z);
			
			// Calculate contribution of gradients from each cell
			float contribution000 = Vector3.Dot(this.gradientAtCell3d(cell), withinCell);
			float contribution001 = Vector3.Dot(this.gradientAtCell3d(cell.Move(0, 0, 1)), withinCell - new Vector3(0, 0, 1));
			float contribution010 = Vector3.Dot(this.gradientAtCell3d(cell.Move(0, 1, 0)), withinCell - new Vector3(0, 1, 0));
			float contribution011 = Vector3.Dot(this.gradientAtCell3d(cell.Move(0, 1, 1)), withinCell - new Vector3(0, 1, 1));
			float contribution100 = Vector3.Dot(this.gradientAtCell3d(cell.Move(1, 0, 0)), withinCell - new Vector3(1, 0, 0));
			float contribution101 = Vector3.Dot(this.gradientAtCell3d(cell.Move(1, 0, 1)), withinCell - new Vector3(1, 0, 1));
			float contribution110 = Vector3.Dot(this.gradientAtCell3d(cell.Move(1, 1, 0)), withinCell - new Vector3(1, 1, 0));
			float contribution111 = Vector3.Dot(this.gradientAtCell3d(cell.Move(1, 1, 1)), withinCell - new Vector3(1, 1, 1));
			
			Vector3 blend = new Vector3(blendCurve(withinCell.X), blendCurve(withinCell.Y), blendCurve(withinCell.Z));
			
			// Interpolate along X
			float contribution00 = lerp(contribution000, contribution100, blend.X);
			float contribution01 = lerp(contribution001, contribution101, blend.X);
			float contribution10 = lerp(contribution010, contribution110, blend.X);
			float contribution11 = lerp(contribution011, contribution111, blend.X);
			
			// Interpolate along Y
			float contribution0 = lerp(contribution00, contribution10, blend.Y);
			float contribution1 = lerp(contribution01, contribution11, blend.Y);
			
			// Interpolate along Z
			return lerp(contribution0, contribution1, blend.Z);
		}

		// =========================
		// Procedural generation of a voxel environment based on the above noise functions.
		// =========================

		// This is the density function that builds the environment.
		// We sample the noise function at different octaves and combine them together.
		// If its value sampled at a certain voxel cell is above a certain threshold,
		// we fill in that voxel cell.
		private float density(Map.Coordinate sample)
		{
			Vector3 sampleVector = new Vector3(sample.X, sample.Y, sample.Z);

			// First octave
			float value = this.noise3d(sampleVector / this.PrimaryOctave1);
			
			// Second octave
			value += this.noise3d(sampleVector / this.PrimaryOctave2) * 0.8f;
			
			// Third octave
			value += this.noise3d(sampleVector / this.PrimaryOctave3) * 0.4f;
			
			return value;
		}

		public Property<int> Ceiling = new Property<int> { Value = 80 };

		public Property<int> Floor = new Property<int> { Value = 0 };

		public Property<int> FloorDepth = new Property<int> { Value = 10 };

		public Property<float> PrimaryOctave1 = new Property<float> { Value = 50.0f };

		public Property<float> PrimaryOctave2 = new Property<float> { Value = 20.0f };

		public Property<float> PrimaryOctave3 = new Property<float> { Value = 10.0f };

		public Property<float> SecondaryOctave = new Property<float> { Value = 30.0f };

		public Property<float> HeightOctave = new Property<float> { Value = 20.0f };

		private Map.CellState primaryFillValue;
		public Property<string> PrimaryFillValue = new Property<string> { Value = "Rock" };

		private Map.CellState secondaryFillValue;
		public Property<string> SecondaryFillValue = new Property<string> { Value = "Rock" };

		public Property<float> PrimaryFillThreshold = new Property<float> { Value = 0.0f };

		public Property<float> SecondaryFillThreshold = new Property<float> { Value = 0.2f };

		public float Sample(Vector3 vector)
		{
			return this.noise3d(vector);
		}

		public float Sample(Map map, Map.Coordinate coord, float octave)
		{
			coord.X -= map.MinX;
			coord.Y -= map.MinY;
			coord.Z -= map.MinZ;
			return this.noise3d(new Vector3(coord.X / octave, coord.Y / octave, coord.Z / octave));
		}

		public Map.CellState GetValue(Map map, Map.Coordinate coord)
		{
			coord.X -= map.MinX;
			coord.Y -= map.MinY;
			coord.Z -= map.MinZ;
			float value = this.density(coord);

			if (value > this.PrimaryFillThreshold)
			{
				// We are filling in material in this cell

				// Determine whether to fill in with primary or secondary material
				Map.CellState state;
				if (this.density(coord.Move(0, 2, 0)) < this.PrimaryFillThreshold // We're on the top of a ground formation
					&& this.noise3d(new Vector3(coord.X, coord.Y, coord.Z) / this.SecondaryOctave) > this.SecondaryFillThreshold) // Modulate by another noise function
					state = this.secondaryFillValue;
				else
					state = this.primaryFillValue;

				return state;
			}
			return new Map.CellState();
		}

		public override void InitializeProperties()
		{
			this.EnabledInEditMode.Value = true;

			this.Reseed.Action = this.reseed;

			this.PrimaryFillValue.Set = delegate(string value)
			{
				Map.CellState state;
				if (!WorldFactory.StatesByName.TryGetValue(value, out state))
					state = WorldFactory.States[0];
				this.primaryFillValue = state;
				this.PrimaryFillValue.InternalValue = value;
			};

			this.SecondaryFillValue.Set = delegate(string value)
			{
				Map.CellState state;
				if (!WorldFactory.StatesByName.TryGetValue(value, out state))
					state = WorldFactory.States[0];
				this.secondaryFillValue = state;
				this.SecondaryFillValue.InternalValue = value;
			};
		}
	}
}
