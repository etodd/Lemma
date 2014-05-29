using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class WallRun : Component<Main>, IUpdateableComponent
	{
		public enum State { None, Left, Right, Straight, Down, Reverse }

		// Input/output properties
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<float> Rotation = new Property<float>();
		public Property<bool> IsSupported = new Property<bool>();
		public BlockPredictor Predictor;

		// Input properties
		public Property<float> Height = new Property<float>();
		public Property<float> JumpSpeed = new Property<float>();
		public Property<float> MaxSpeed = new Property<float>();
		public Property<bool> EnableWallRun = new Property<bool>();
		public Property<bool> EnableWallRunHorizontal = new Property<bool>();
		public Property<bool> EnableEnhancedWallRun = new Property<bool>();
		public Property<float> LastWallJump = new Property<float>();

		// Output properties
		public Property<bool> AllowUncrouch = new Property<bool>();
		public Property<bool> HasTraction = new Property<bool>();
		public Command StopKick = new Command();
		public Command LockRotation = new Command();
		public Command Vault = new Command();
		public Command<Map, Map.Coordinate, Direction> WalkedOn = new Command<Map, Map.Coordinate, Direction>();
		public Property<State> CurrentState = new Property<State>();
		public Property<Map> WallRunMap = new Property<Map>();
		public Property<Direction> WallDirection = new Property<Direction>();
		public Property<Direction> WallRunDirection = new Property<Direction>();

		private Map.CellState temporary;

		private const float minWallRunSpeed = 4.0f;

		private float lastWallRunEnded = -1.0f;
		private const float wallRunDelay = 0.5f;

		// Since block possibilities are instantiated on another thread,
		// we have to give that thread some time to do it before checking if there is actually a wall to run on.
		// Otherwise, we will immediately stop wall-running since the wall hasn't been instantiated yet.
		private float wallInstantiationTimer = 0.0f;

		public Property<Map> LastWallRunMap = new Property<Map>();
		public Property<Direction> LastWallDirection = new Property<Direction>();

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.temporary = Map.States[Map.t.Temporary];
		}

		public bool Activate(State state)
		{
			if (!this.EnableWallRun)
				return false;

			Vector3 playerVelocity = this.LinearVelocity;
			if (playerVelocity.Y < FallDamage.RollingDamageVelocity)
				return false;

			wallInstantiationTimer = 0.0f;

			// Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.
			bool wallRunDelayPassed = main.TotalTime - this.lastWallRunEnded > wallRunDelay;
			bool wallRunJumpDelayPassed = main.TotalTime - this.LastWallJump > wallRunDelay;

			Matrix matrix = Matrix.CreateRotationY(this.Rotation);

			Vector3 forwardVector = -matrix.Forward;

			playerVelocity.Normalize();
			playerVelocity.Y = 0.0f;
			if (Vector3.Dot(forwardVector, playerVelocity) < -0.3f)
				return false;

			Vector3 wallVector;
			switch (state)
			{
				case State.Straight:
					wallVector = forwardVector;
					break;
				case State.Left:
					if (!this.EnableWallRunHorizontal)
						return false;
					wallVector = -matrix.Left;
					break;
				case State.Right:
					if (!this.EnableWallRunHorizontal)
						return false;
					wallVector = -matrix.Right;
					break;
				case State.Reverse:
					wallVector = -forwardVector;
					wallInstantiationTimer = 0.25f;
					break;
				default:
					wallVector = Vector3.Zero;
					break;
			}

			Vector3 pos = this.Position + new Vector3(0, this.Height * -0.5f, 0);

			// Attempt to wall-run on an existing map
			bool activate = false, addInitialVelocity = false;
			foreach (Map map in Map.ActivePhysicsMaps)
			{
				Map.Coordinate coord = map.GetCoordinate(pos);
				Direction dir = map.GetRelativeDirection(wallVector);
				Direction up = map.GetRelativeDirection(Direction.PositiveY);
				for (int i = 1; i < 4; i++)
				{
					Map.Coordinate wallCoord = coord.Move(dir, i);
					if (map[coord.Move(dir, i - 1)].ID != 0
						|| map[coord.Move(dir, i - 1).Move(up, 1)].ID != 0
						|| map[coord.Move(dir, i - 1).Move(up, 2)].ID != 0)
					{
						// Blocked
						break;
					}

					// Need at least two blocks to consider it a wall
					if (map[wallCoord].ID != 0 && map[wallCoord.Move(up)].ID != 0)
					{
						bool differentWall = map != this.LastWallRunMap.Value || dir != this.LastWallDirection.Value;
						activate = differentWall || wallRunJumpDelayPassed;
						addInitialVelocity = differentWall || wallRunDelayPassed;
					}
					else
					{
						// Check block possibilities
						List<BlockPredictor.Possibility> mapBlockPossibilities = this.Predictor.GetPossibilities(map);
						if (mapBlockPossibilities != null)
						{
							foreach (BlockPredictor.Possibility block in mapBlockPossibilities)
							{
								if (wallCoord.Between(block.StartCoord, block.EndCoord))
								{
									this.Predictor.InstantiatePossibility(block);
									this.Predictor.ClearPossibilities();
									activate = true;
									addInitialVelocity = true;
									wallInstantiationTimer = 0.25f;
									break;
								}
							}
						}
					}

					if (activate)
					{
						// Move so the player is exactly two coordinates away from the wall
						this.Position.Value = map.GetAbsolutePosition(coord.Move(dir, i - 2)) + new Vector3(0, this.Height * 0.5f, 0);
						break;
					}
				}

				if (activate)
				{
					this.setup(map, dir, state, forwardVector, addInitialVelocity);
					break;
				}
			}
			return activate;
		}

		private void setup(Map map, Direction dir, State state, Vector3 forwardVector, bool addInitialVelocity)
		{
			this.StopKick.Execute();
			this.AllowUncrouch.Value = true;

			this.WallRunMap.Value = this.LastWallRunMap.Value = map;
			this.WallDirection.Value = this.LastWallDirection.Value = dir;

			if (state == State.Straight)
			{
				// Determine if we're actually going down
				if (!this.IsSupported && this.LinearVelocity.Value.Y < -0.5f)
					state = State.Down;
			}

			this.CurrentState.Value = state;

			Session.Recorder.Event(main, "WallRun", this.CurrentState.Value.ToString());

			this.WallRunDirection.Value = state == State.Straight ? map.GetRelativeDirection(Vector3.Up) : (state == State.Down ? map.GetRelativeDirection(Vector3.Down) : dir.Cross(map.GetRelativeDirection(Vector3.Up)));

			if (state == State.Straight || state == State.Down || state == State.Reverse)
			{
				if (state == State.Straight)
				{
					Vector3 velocity = this.LinearVelocity.Value;
					velocity.X = 0;
					velocity.Z = 0;
					if (addInitialVelocity)
					{
						if (this.IsSupported)
							velocity.Y = this.JumpSpeed * 1.3f;
						else
							velocity.Y = this.LinearVelocity.Value.Y + this.JumpSpeed * 0.75f;
					}
					else
						velocity.Y = this.LinearVelocity.Value.Y;

					this.LinearVelocity.Value = velocity;
					this.IsSupported.Value = false;
					this.HasTraction.Value = false;
				}
				Vector3 wallVector = this.WallRunMap.Value.GetAbsoluteVector(this.WallDirection.Value.GetVector());

				if (state == State.Reverse)
					wallVector = -wallVector;

				// Make sure we lock in the correct rotation value
				this.Rotation.Value = (float)Math.Atan2(wallVector.X, wallVector.Z);
				this.LockRotation.Execute();
			}
			else
			{
				this.IsSupported.Value = false;
				this.HasTraction.Value = false;
				Vector3 velocity = map.GetAbsoluteVector(this.WallRunDirection.Value.GetVector());
				if (Vector3.Dot(velocity, forwardVector) < 0.0f)
				{
					velocity = -velocity;
					this.WallRunDirection.Value = this.WallRunDirection.Value.GetReverse();
				}
				this.Rotation.Value = (float)Math.Atan2(velocity.X, velocity.Z);
				this.LockRotation.Execute();

				if (addInitialVelocity)
				{
					velocity.Y = 0.0f;
					float length = velocity.Length();
					if (length > 0)
					{
						velocity /= length;

						Vector3 currentHorizontalVelocity = this.LinearVelocity;
						currentHorizontalVelocity.Y = 0.0f;
						velocity *= Math.Min(this.MaxSpeed * 2.0f, Math.Max(currentHorizontalVelocity.Length() * 1.25f, 6.0f));

						if (state != State.Straight && state != State.Reverse && Vector3.Dot(this.LinearVelocity, forwardVector) < 0.0f)
							velocity = Vector3.Normalize(velocity) * (minWallRunSpeed + 1.0f);

						float currentVerticalSpeed = this.LinearVelocity.Value.Y;
						velocity.Y = (currentVerticalSpeed > -3.0f ? Math.Max(currentVerticalSpeed * 0.7f, 0.0f) : currentVerticalSpeed * 0.5f) + 5.0f;

						this.LinearVelocity.Value = velocity;
					}
				}
			}
		}

		public void Deactivate()
		{
			if (this.CurrentState.Value != State.None)
				this.lastWallRunEnded = main.TotalTime; // Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.

			this.WallRunMap.Value = null;
			this.WallDirection.Value = Direction.None;
			this.WallRunDirection.Value = Direction.None;
			this.CurrentState.Value = State.None;
		}

		public void Update(float dt)
		{
			State wallRunState = this.CurrentState;
			if (wallRunState != State.None)
			{
				this.Vault.Execute(); // Try to vault up
				if (this.CurrentState.Value == State.None) // We vaulted
					return;

				if (!this.WallRunMap.Value.Active || this.IsSupported)
				{
					this.Deactivate();
					return;
				}

				float wallRunSpeed = Vector3.Dot(this.LinearVelocity.Value, this.WallRunMap.Value.GetAbsoluteVector(this.WallRunDirection.Value.GetVector()));

				if (wallRunState == State.Straight)
				{
					if (wallRunSpeed < 0.0f)
					{
						// Start sliding down
						this.CurrentState.Value = wallRunState = State.Down;
					}
				}
				else if (wallRunState == State.Left || wallRunState == State.Right)
				{
					if (this.IsSupported || wallRunSpeed < minWallRunSpeed)
					{
						// We landed on the ground or we're going too slow to continue wall-running
						this.Deactivate();
						return;
					}
				}

				Vector3 pos = this.Position + new Vector3(0, this.Height * -0.5f, 0);
				Map.Coordinate coord = this.WallRunMap.Value.GetCoordinate(pos);
				Map.Coordinate wallCoord = coord.Move(this.WallDirection, 2);
				Map.CellState wallType = this.WallRunMap.Value[wallCoord];
				this.WalkedOn.Execute(this.WallRunMap, wallCoord, this.WallDirection);

				if (this.EnableEnhancedWallRun && (wallRunState == State.Left || wallRunState == State.Right))
				{
					Direction up = this.WallRunMap.Value.GetRelativeDirection(Direction.PositiveY);
					Direction right = this.WallDirection.Value.Cross(up);

					List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();

					const int radius = 5;
					int upwardRadius = wallRunState == State.Down || wallRunState == State.Reverse ? 0 : radius;
					for (Map.Coordinate x = wallCoord.Move(right, -radius); x.GetComponent(right) < wallCoord.GetComponent(right) + radius; x = x.Move(right))
					{
						int dx = x.GetComponent(right) - wallCoord.GetComponent(right);
						for (Map.Coordinate y = x.Move(up, -radius); y.GetComponent(up) < wallCoord.GetComponent(up) + upwardRadius; y = y.Move(up))
						{
							int dy = y.GetComponent(up) - wallCoord.GetComponent(up);
							if ((float)Math.Sqrt(dx * dx + dy * dy) < radius && this.WallRunMap.Value[y].ID == 0)
							{
								buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
								{
									Map = this.WallRunMap,
									Coordinate = y,
									State = temporary,
								});
							}
						}
					}
					Factory.Get<EffectBlockFactory>().Build(main, buildCoords, false, this.Position);
				}
				else if (wallType.ID == 0 && wallInstantiationTimer == 0.0f) // We ran out of wall to walk on
				{
					this.Deactivate();
					return;
				}

				if (this.WallRunMap.Value == null || !this.WallRunMap.Value.Active)
					return;

				wallInstantiationTimer = Math.Max(0.0f, wallInstantiationTimer - dt);

				Vector3 coordPos = this.WallRunMap.Value.GetAbsolutePosition(coord);

				Vector3 normal = this.WallRunMap.Value.GetAbsoluteVector(this.WallDirection.Value.GetVector());
				// Equation of a plane
				// normal (dot) point = d
				float d = Vector3.Dot(normal, coordPos);

				// Distance along the normal to keep the player glued to the wall
				float snapDistance = d - Vector3.Dot(pos, normal);

				this.Position.Value += normal * snapDistance;

				Vector3 velocity = this.LinearVelocity;

				// Also fix the velocity so we don't jitter away from the wall
				velocity -= Vector3.Dot(velocity, normal) * normal;

				// Slow our descent
				velocity += new Vector3(0, (wallRunState == State.Straight ? 3.0f : 10.0f) * dt, 0);

				this.LinearVelocity.Value = velocity;
			}
		}
	}
}
