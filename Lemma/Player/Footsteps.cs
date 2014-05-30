using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;
using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;

namespace Lemma.Components
{
	public class RespawnLocation
	{
		public Entity.Handle Map;
		public Voxel.Coord Coordinate;
		public float Rotation;
		public Vector3 OriginalPosition;
	}

	public class Footsteps : Component<Main>, IUpdateableComponent
	{
		// Input commands
		[XmlIgnore]
		public Command Footstep = new Command();
		[XmlIgnore]
		public Property<WallRun.State> WallRunState = new Property<WallRun.State>();

		// Output commands
		[XmlIgnore]
		public Command<Voxel, Voxel.Coord, Direction> WalkedOn = new Command<Voxel, Voxel.Coord, Direction>();

		// Input properties
		public Property<bool> SoundEnabled = new Property<bool>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<float> Rotation = new Property<float>();
		public Property<float> CharacterHeight = new Property<float>();
		public Property<float> SupportHeight = new Property<float>();
		public Property<bool> IsSupported = new Property<bool>();

		// Output properties
		public ListProperty<RespawnLocation> RespawnLocations = new ListProperty<RespawnLocation>();

		// Input/output properties
		public Property<float> Health = new Property<float>();

		private Voxel.GlobalRaycastResult groundRaycast;
		private bool lastSupported;

		private int walkedOnCount = 0;
		private bool infectedDamage;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Serialize = false;
	
			this.Footstep.Action = delegate()
			{
				if (this.SoundEnabled)
					AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, this.Entity);
			};

			Voxel.State temporary = Voxel.States[Voxel.t.Temporary],
				expander = Voxel.States[Voxel.t.Expander],
				neutral = Voxel.States[Voxel.t.Neutral];

			this.Add(new CommandBinding<Voxel, Voxel.Coord, Direction>(this.WalkedOn, delegate(Voxel map, Voxel.Coord coord, Direction dir)
			{
				Voxel.State state = map[coord];

				if (state != Voxel.EmptyState)
				{
					AkSoundEngine.SetSwitch(AK.SWITCHES.FOOTSTEP_MATERIAL.GROUP, state.FootstepSwitch, this.Entity);

					if (this.WallRunState.Value == WallRun.State.None)
					{
						if (map.GetAbsoluteDirection(dir) == Direction.NegativeY)
						{
							this.walkedOnCount++;
							if (this.walkedOnCount >= 2)
							{
								// Every few tiles, save off the location for the auto-respawn system
								this.RespawnLocations.Add(new RespawnLocation
								{
									Coordinate = coord,
									Map = map.Entity,
									Rotation = this.Rotation,
									OriginalPosition = map.GetAbsolutePosition(coord),
								});
								while (this.RespawnLocations.Count > Spawner.RespawnMemoryLength)
									this.RespawnLocations.RemoveAt(0);
								this.walkedOnCount = 0;
							}
						}
					}
				}

				Voxel.t id = state.ID;
				if (id == Voxel.t.Neutral)
				{
					map.Empty(coord);
					map.Fill(coord, temporary);
					map.Regenerate();
					WorldFactory.Instance.Get<Propagator>().Sparks(map.GetAbsolutePosition(coord), Propagator.Spark.Normal);
				}
				else if (id == Voxel.t.Reset)
				{
					bool regenerate = false;

					Dictionary<Voxel.Coord, bool> visited = new Dictionary<Voxel.Coord, bool>();
					Queue<Voxel.Coord> queue = new Queue<Voxel.Coord>();
					queue.Enqueue(coord);
					while (queue.Count > 0)
					{
						Voxel.Coord c = queue.Dequeue();
						visited[c] = true;
						foreach (Direction adjacentDirection in DirectionExtensions.Directions)
						{
							Voxel.Coord adjacentCoord = c.Move(adjacentDirection);
							if (!visited.ContainsKey(adjacentCoord))
							{
								Voxel.t adjacentID = map[adjacentCoord].ID;
								if (adjacentID == Voxel.t.Reset)
									queue.Enqueue(adjacentCoord);
								else if (adjacentID == Voxel.t.Infected || adjacentID == Voxel.t.Temporary)
								{
									map.Empty(adjacentCoord);
									map.Fill(adjacentCoord, neutral);
									regenerate = true;
								}
							}
						}
					}
					if (regenerate)
						map.Regenerate();
				}

				// Lava. Damage the player character if it steps on lava.
				bool isInfected = id == Voxel.t.Infected || id == Voxel.t.InfectedCritical;
				if (isInfected)
					this.Health.Value -= 0.2f;
				else if (id == Voxel.t.Floater)
				{
					// Floater. Delete the block after a delay.
					Vector3 pos = map.GetAbsolutePosition(coord);
					ParticleEmitter.Emit(main, "Smoke", pos, 1.0f, 10);
					AkSoundEngine.PostEvent("Play_FragileDirt_Crumble", pos);
					WorldFactory.Instance.Add(new Animation
					(
						new Animation.Delay(1.0f),
						new Animation.Execute(delegate()
						{
							if (map[coord].ID == Voxel.t.Floater)
							{
								map.Empty(coord);
								map.Regenerate();
							}
						})
					));
				}
				else if (id == Voxel.t.Expander)
				{
					// Expander. Expand the block after a delay.
					const int expandLength = 6;
					const int expandWidth = 1;
					Vector3 pos = map.GetAbsolutePosition(coord);
					ParticleEmitter.Emit(main, "Smoke", pos, 1.0f, 10);
					AkSoundEngine.PostEvent("Play_FragileDirt_Crumble", pos);
					WorldFactory.Instance.Add(new Animation
					(
						new Animation.Delay(1.5f),
						new Animation.Execute(delegate()
						{
							if (map[coord].ID == Voxel.t.Expander)
							{
								Direction normal = dir.GetReverse();
								Direction right = normal == Direction.PositiveY ? Direction.PositiveX : normal.Cross(Direction.PositiveY);
								Direction ortho = normal.Cross(right);
								pos = map.GetAbsolutePosition(coord);
								WorldFactory.Instance.Get<Propagator>().Sparks(map.GetAbsolutePosition(coord), Propagator.Spark.Expander);
								List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();
								foreach (Voxel.Coord c in coord.Move(right, -expandWidth).Move(ortho, -expandWidth).CoordinatesBetween(coord.Move(right, expandWidth).Move(ortho, expandWidth).Move(normal, expandLength).Move(1, 1, 1)))
								{
									if (map[c].ID == 0)
									{
										buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
										{
											Voxel = map,
											Coordinate = c,
											State = expander,
										});
									}
								}
								Factory.Get<EffectBlockFactory>().Build(main, buildCoords, false, pos, 0.15f);
							}
						})
					));
				}

				this.infectedDamage = isInfected;
			}));
		}

		public void Update(float dt)
		{
			Voxel oldMap = this.groundRaycast.Voxel;
			Voxel.Coord? oldCoord = this.groundRaycast.Coordinate;
			
			Direction direction;

			// Wall-run code will call our WalkedOn event for us, so only worry about this stuff if we're walking normally
			if (this.WallRunState == WallRun.State.None)
			{
				this.groundRaycast = Voxel.GlobalRaycast(this.Position, Vector3.Down, this.CharacterHeight.Value * 0.5f + this.SupportHeight + 1.1f);
				direction = this.groundRaycast.Normal.GetReverse();

				if (this.groundRaycast.Voxel != null &&
					(this.groundRaycast.Voxel != oldMap || oldCoord == null || !oldCoord.Value.Equivalent(this.groundRaycast.Coordinate.Value)))
				{
					this.WalkedOn.Execute(this.groundRaycast.Voxel, this.groundRaycast.Coordinate.Value, direction);
				}
			}

			if (this.IsSupported && !this.lastSupported)
				this.Footstep.Execute();

			if (this.infectedDamage && this.IsSupported)
				this.Health.Value -= 0.6f * dt;

			this.lastSupported = this.IsSupported;
		}
	}
}
