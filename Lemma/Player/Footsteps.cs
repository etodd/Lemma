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
		public Map.Coordinate Coordinate;
		public float Rotation;
		public Vector3 OriginalPosition;
	}

	public class Footsteps : Component<Main>, IUpdateableComponent
	{
		// Input commands
		[XmlIgnore]
		public Command Footstep = new Command();
		[XmlIgnore]
		public Property<Player.WallRun> WallRunState = new Property<Player.WallRun>();

		// Output commands
		[XmlIgnore]
		public Command<Map, Map.Coordinate, Direction> WalkedOn = new Command<Map, Map.Coordinate, Direction>();

		// Input properties
		public Property<bool> SoundEnabled = new Property<bool>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<float> Rotation = new Property<float>();
		public Property<float> CharacterHeight = new Property<float>();
		public Property<float> SupportHeight = new Property<float>();
		public Property<bool> IsSupported = new Property<bool>();
		public Property<bool> LastSupported = new Property<bool>();

		// Output properties
		public ListProperty<RespawnLocation> RespawnLocations = new ListProperty<RespawnLocation>();

		// Input/output properties
		public Property<float> Health = new Property<float>();

		private Map.GlobalRaycastResult groundRaycast;

		private int walkedOnCount = 0;
		private bool infectedDamage;

		public override void Awake()
		{
			base.Awake();
	
			this.Footstep.Action = delegate()
			{
				if (this.SoundEnabled)
					AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, this.Entity);
			};

			Map.CellState temporary = Map.States[Map.t.Temporary],
				expander = Map.States[Map.t.Expander],
				neutral = Map.States[Map.t.Neutral];

			this.Add(new CommandBinding<Map, Map.Coordinate, Direction>(this.WalkedOn, delegate(Map map, Map.Coordinate coord, Direction dir)
			{
				Map.t id = map[coord].ID;
				if (id == Map.t.Neutral)
				{
					map.Empty(coord);
					map.Fill(coord, temporary);
					map.Regenerate();
				}
				else if (id == Map.t.Reset)
				{
					bool regenerate = false;

					Dictionary<Map.Coordinate, bool> visited = new Dictionary<Map.Coordinate, bool>();
					Queue<Map.Coordinate> queue = new Queue<Map.Coordinate>();
					queue.Enqueue(coord);
					while (queue.Count > 0)
					{
						Map.Coordinate c = queue.Dequeue();
						visited[c] = true;
						foreach (Direction adjacentDirection in DirectionExtensions.Directions)
						{
							Map.Coordinate adjacentCoord = coord.Move(adjacentDirection);
							if (!visited.ContainsKey(adjacentCoord))
							{
								Map.t adjacentID = map[adjacentCoord].ID;
								if (adjacentID == Map.t.Reset)
									queue.Enqueue(adjacentCoord);
								else if (adjacentID == Map.t.Infected || adjacentID == Map.t.Temporary)
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
				bool isInfected = id == Map.t.Infected || id == Map.t.InfectedCritical;
				if (isInfected)
					this.Health.Value -= 0.2f;
				else if (id == Map.t.Floater)
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
							if (map[coord].ID == Map.t.Floater)
							{
								map.Empty(coord);
								map.Regenerate();
							}
						})
					));
				}
				else if (id == Map.t.Expander)
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
							if (map[coord].ID == Map.t.Expander)
							{
								Direction normal = dir.GetReverse();
								Direction right = normal == Direction.PositiveY ? Direction.PositiveX : normal.Cross(Direction.PositiveY);
								Direction ortho = normal.Cross(right);
								pos = map.GetAbsolutePosition(coord);
								// TODO: refactor world so we can get this working
								//sparks(pos, 10.0f);
								List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();
								foreach (Map.Coordinate c in coord.Move(right, -expandWidth).Move(ortho, -expandWidth).CoordinatesBetween(coord.Move(right, expandWidth).Move(ortho, expandWidth).Move(normal, expandLength).Move(1, 1, 1)))
								{
									if (map[c].ID == 0)
									{
										buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
										{
											Map = map,
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
			Map oldMap = this.groundRaycast.Map;
			Map.Coordinate? oldCoord = this.groundRaycast.Coordinate;
			
			Direction direction;

			if (this.WallRunState == Player.WallRun.None)
			{
				groundRaycast = Map.GlobalRaycast(this.Position, Vector3.Down, this.CharacterHeight.Value * 0.5f + this.SupportHeight + 1.1f);
				direction = groundRaycast.Normal.GetReverse();
			}
			else
			{
				// TODO: handle wall-running
				return;
				Vector3 pos = this.Position + new Vector3(0, this.CharacterHeight * -0.5f, 0);
			}

			if (groundRaycast.Map != null &&
				(groundRaycast.Map != oldMap || oldCoord == null || !oldCoord.Value.Equivalent(groundRaycast.Coordinate.Value)))
			{
				AkSoundEngine.SetSwitch(AK.SWITCHES.FOOTSTEP_MATERIAL.GROUP, groundRaycast.Coordinate.Value.Data.FootstepSwitch, this.Entity);
				this.WalkedOn.Execute(groundRaycast.Map, groundRaycast.Coordinate.Value, direction);

				this.walkedOnCount++;
				if (this.walkedOnCount >= 3)
				{
					// Every 3 tiles, save off the location for the auto-respawn system
					this.RespawnLocations.Add(new RespawnLocation
					{
						Coordinate = this.groundRaycast.Coordinate.Value,
						Map = this.groundRaycast.Map.Entity,
						Rotation = this.Rotation,
						OriginalPosition = this.groundRaycast.Map.GetAbsolutePosition(this.groundRaycast.Coordinate.Value),
					});
					while (this.RespawnLocations.Count > GameMain.RespawnMemoryLength)
						this.RespawnLocations.RemoveAt(0);
					this.walkedOnCount = 0;
				}
			}

			if (this.IsSupported && !this.LastSupported)
				this.Footstep.Execute();

			if (this.infectedDamage)
				this.Health.Value -= 0.6f * dt;
		}
	}
}
