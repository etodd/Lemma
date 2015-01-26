using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class VoxelSetter : Component<Main>
	{
		// Input properties
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public Property<Voxel.t> State = new Property<Voxel.t>();
		public Property<bool> Contiguous = new Property<bool>();

		[XmlIgnore]
		public Command Set = new Command();

		public Voxel.State GetState()
		{
			Entity voxelEntity = this.AttachedVoxel.Value.Target;
			if (voxelEntity == null)
				return Voxel.States.Empty;

			Voxel map = voxelEntity.Get<Voxel>();
			if (map == null)
				return Voxel.States.Empty;
			else
				return map[this.Coord];
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new CommandBinding(this.Set, delegate()
			{
				Entity voxelEntity = this.AttachedVoxel.Value.Target;
				if (voxelEntity == null)
					return;

				Voxel map = voxelEntity.Get<Voxel>();
				Voxel.State state = Voxel.States.All[this.State];
				if (this.Contiguous)
				{
					Voxel.Box b = map.GetBox(this.Coord);
					if (b != null && b.Type != state)
					{
						List<Voxel.Coord> coords = map.GetContiguousByType(new[] { b }).SelectMany(x => x.GetCoords()).Select(x => x.WithData(state)).ToList();
						lock (map.MutationLock)
						{
							map.Empty(coords, true, true);
							map.Fill(coords);
						}
						map.Regenerate();
					}
				}
				else
				{
					if (map[this.Coord] != state)
					{
						lock (map.MutationLock)
						{
							map.Empty(this.Coord, true, true);
							map.Fill(this.Coord, state);
						}
						map.Regenerate();
					}
				}
			}));
		}
	}
}
