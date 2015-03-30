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
		public Property<bool> IsSwimming = new Property<bool>();

		// Output properties
		public ListProperty<RespawnLocation> RespawnLocations = new ListProperty<RespawnLocation>();

		// Input/output properties
		public Command<float> Damage = new Command<float>();

		private Voxel.GlobalRaycastResult groundRaycast;
		private bool lastSupported;

		private int walkedOnCount;
		private float lastFootstepSound = -1.0f;
		private const float footstepSoundInterval = 0.1f;
		private bool infectedDamage;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Serialize = false;
	
			this.Footstep.Action = delegate()
			{
				if (this.SoundEnabled && this.main.TotalTime - this.lastFootstepSound > footstepSoundInterval)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, this.Entity);
					this.lastFootstepSound = this.main.TotalTime;
				}
			};

			this.Add(new CommandBinding<Voxel, Voxel.Coord, Direction>(this.WalkedOn, delegate(Voxel map, Voxel.Coord coord, Direction dir)
			{
				Voxel.State state = map[coord];

				if (state != Voxel.States.Empty)
				{
					AkSoundEngine.SetSwitch(AK.SWITCHES.FOOTSTEP_MATERIAL.GROUP, state.FootstepSwitch, this.Entity);

					if (this.WallRunState.Value == WallRun.State.None)
					{
						if (map.GetAbsoluteDirection(dir) == Direction.NegativeY && !this.IsSwimming)
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
								while (this.RespawnLocations.Length > Spawner.RespawnMemoryLength)
									this.RespawnLocations.RemoveAt(0);
								this.walkedOnCount = 0;
							}
						}
					}
				}

				Voxel.t id = state.ID;
				if (id == Voxel.t.Neutral)
				{
					map.Empty(coord, false, true, map);
					bool isPowered = false;
					for (int i = 0; i < 6; i++)
					{
						Voxel.Coord adjacentCoord = coord.Move(DirectionExtensions.Directions[i]);
						Voxel.t adjacentId = map[coord].ID;
						if (adjacentId == Voxel.t.Powered || adjacentId == Voxel.t.PermanentPowered || adjacentId == Voxel.t.PoweredSwitch || adjacentId == Voxel.t.HardPowered)
						{
							isPowered = true;
							break;
						}
					}
					map.Fill(coord, isPowered ? Voxel.States.Powered : Voxel.States.Blue);
					map.Regenerate();
					WorldFactory.Instance.Get<Propagator>().Sparks(map.GetAbsolutePosition(coord), Propagator.Spark.Normal);
				}
				else if (id == Voxel.t.Reset)
				{
					bool regenerate = false;

					Queue<Voxel.Coord> queue = new Queue<Voxel.Coord>();
					queue.Enqueue(coord);
					Voxel.CoordSetCache.Add(coord);
					while (queue.Count > 0)
					{
						Voxel.Coord c = queue.Dequeue();
						for (int i = 0; i < 6; i++)
						{
							Voxel.Coord adjacentCoord = c.Move(DirectionExtensions.Directions[i]);
							if (!Voxel.CoordSetCache.Contains(adjacentCoord))
							{
								Voxel.CoordSetCache.Add(adjacentCoord);
								Voxel.t adjacentID = map[adjacentCoord].ID;
								if (adjacentID == Voxel.t.Reset || adjacentID == Voxel.t.Hard)
									queue.Enqueue(adjacentCoord);
								else if (adjacentID == Voxel.t.Infected || adjacentID == Voxel.t.Blue || adjacentID == Voxel.t.Powered)
								{
									map.Empty(adjacentCoord, false, true, map);
									map.Fill(adjacentCoord, Voxel.States.Neutral);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.HardPowered || adjacentID == Voxel.t.HardInfected)
								{
									map.Empty(adjacentCoord, false, true, map);
									map.Fill(adjacentCoord, Voxel.States.Hard);
									regenerate = true;
								}
							}
						}
					}
					Voxel.CoordSetCache.Clear();
					if (regenerate)
						map.Regenerate();
				}

				// Lava. Damage the player character if it steps on lava.
				bool isInfected = id == Voxel.t.Infected || id == Voxel.t.HardInfected;
				if (isInfected)
					this.Damage.Execute(0.2f);
				else if (id == Voxel.t.Floater)
				{
					// Floater. Delete the block after a delay.
					Vector3 pos = map.GetAbsolutePosition(coord);
					ParticleEmitter.Emit(main, "Smoke", pos, 1.0f, 10);
					Sound.PostEvent(AK.EVENTS.PLAY_CRUMBLE, pos);
					WorldFactory.Instance.Add(new Animation
					(
						new Animation.Delay(0.5f),
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

			if (this.infectedDamage && (this.IsSupported || this.WallRunState != WallRun.State.None))
				this.Damage.Execute(0.6f * dt);

			this.lastSupported = this.IsSupported;
		}
	}
}