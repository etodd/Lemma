using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;

namespace Lemma.Components
{
	public class Snake : Component<Main>
	{
		public Property<float> OperationalRadius = new Property<float> { Value = 100.0f };

		public ListProperty<Voxel.Coord> Path = new ListProperty<Voxel.Coord>();

		public Property<Entity.Handle> TargetAgent = new Property<Entity.Handle>();

		public Property<Voxel.Coord> CrushCoordinate = new Property<Voxel.Coord>();

		public Property<bool> EnableHearing = new Property<bool> { Value = true };
	}
}
