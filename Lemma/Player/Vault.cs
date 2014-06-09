using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Vault : Component<Main>, IUpdateableComponent
	{
		public enum State
		{
			None,
			Straight,
			Down,
		}

		private Random random = new Random();

		// Input
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Vector3> FloorPosition = new Property<Vector3>();
		public Property<float> MaxSpeed = new Property<float>();
		public Property<WallRun.State> WallRunState = new Property<WallRun.State>();

		// Output
		public Property<State> CurrentState = new Property<State>();
		public Command LockRotation = new Command();
		public Property<float> LastSupportedSpeed = new Property<float>();
		public Command DeactivateWallRun = new Command();
		public Command<WallRun.State> ActivateWallRun = new Command<WallRun.State>();
		public Command<float> FallDamage = new Command<float>();
		private AnimatedModel model;

		// Input/output
		public BlockPredictor Predictor;
		public Property<float> Rotation = new Property<float>();
		public Property<bool> IsSupported = new Property<bool>();
		public Property<bool> HasTraction = new Property<bool>();
		public Property<bool> EnableWalking = new Property<bool>();
		public Property<bool> AllowUncrouch = new Property<bool>();
		public Property<bool> Crouched = new Property<bool>();
		public Property<Vector3> LinearVelocity = new Property<Vector3>();

		private float vaultTime;

		private bool vaultOver;
		private bool isTopOut;
		
		private float moveForwardStartTime;
		private bool movingForward;

		private float walkOffEdgeTimer;
		private Vector3 originalPosition;
		private Vector3 vaultVelocity;
		private Vector3 forward;
		private Voxel map;
		private Voxel.Coord coord;

		const float topOutVerticalSpeed = 4.0f;
		const float mantleVaultVerticalSpeed = 8.0f;
		const float maxVaultTime = 1.0f;
		const float maxTopoutTime = 2.0f;
		const int searchUpDistance = 2;
		const int searchDownDistance = 4;

		public void Bind(AnimatedModel model)
		{
			this.model = model;

			// Filters are in Blender's Z-up coordinate system

			this.model["Mantle"].Speed = 1.3f;
			this.model["Mantle"].GetChannel(this.model.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix m)
			{
				m.Translation = new Vector3(0.0f, 0.0f, 2.0f);
				return m;
			};
			this.model["TopOut"].Speed = 2.0f;
			this.model["TopOut"].GetChannel(this.model.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix m)
			{
				Vector3 diff = this.originalPosition - this.Position;
				m.Translation += Vector3.Transform(diff, Matrix.CreateRotationY(-this.Rotation) * Matrix.CreateRotationX((float)Math.PI * 0.5f) * Matrix.CreateTranslation(0, -0.5f, 0.5f));
				return m;
			};
			this.model["Vault"].GetChannel(this.model.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix m)
			{
				m.Translation = new Vector3(0.0f, 0.0f, 1.0f);
				return m;
			};
		}

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledWhenPaused = false;
		}

		public bool Go()
		{
			Matrix rotationMatrix = Matrix.CreateRotationY(this.Rotation);
			foreach (Voxel map in Voxel.ActivePhysicsVoxels)
			{
				Direction up = map.GetRelativeDirection(Direction.PositiveY);
				Direction right = map.GetRelativeDirection(Vector3.Cross(Vector3.Up, -rotationMatrix.Forward));
				Vector3 pos = this.Position + rotationMatrix.Forward * -1.75f;
				Voxel.Coord baseCoord = map.GetCoordinate(pos).Move(up, searchUpDistance);
				foreach (int x in new[] { 0, -1, 1 })
				{
					Voxel.Coord coord = baseCoord.Move(right, x);
					for (int i = 0; i < searchDownDistance; i++)
					{
						Voxel.Coord downCoord = coord.Move(up.GetReverse());

						if (map[coord].ID != 0)
							break;
						else if (map[downCoord].ID != 0)
						{
							// Vault
							this.vault(map, coord);
							return true;
						}
						coord = coord.Move(up.GetReverse());
					}
				}
			}

			// Check block possibilities for vaulting
			foreach (BlockPredictor.Possibility possibility in this.Predictor.AllPossibilities)
			{
				Direction up = possibility.Map.GetRelativeDirection(Direction.PositiveY);
				Direction right = possibility.Map.GetRelativeDirection(Vector3.Cross(Vector3.Up, -rotationMatrix.Forward));
				Vector3 pos = this.Position + rotationMatrix.Forward * -1.75f;
				Voxel.Coord baseCoord = possibility.Map.GetCoordinate(pos).Move(up, searchUpDistance);
				foreach (int x in new[] { 0, -1, 1 })
				{
					Voxel.Coord coord = baseCoord.Move(right, x);
					for (int i = 0; i < searchDownDistance; i++)
					{
						Voxel.Coord downCoord = coord.Move(up.GetReverse());
						if (!coord.Between(possibility.StartCoord, possibility.EndCoord) && downCoord.Between(possibility.StartCoord, possibility.EndCoord))
						{
							this.Predictor.InstantiatePossibility(possibility);
							this.vault(possibility.Map, coord);
							return true;
						}
						coord = coord.Move(up.GetReverse());
					}
				}
			}

			return false;
		}

		private void vault(Voxel map, Voxel.Coord coord)
		{
			DynamicVoxel dynamicMap = map as DynamicVoxel;
			Vector3 supportVelocity = Vector3.Zero;

			if (dynamicMap != null)
			{
				BEPUphysics.Entities.Entity supportEntity = dynamicMap.PhysicsEntity;
				Vector3 supportLocation = this.FloorPosition;
				supportVelocity = supportEntity.LinearVelocity + Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
			}

			this.FallDamage.Execute(this.LinearVelocity.Value.Y - supportVelocity.Y);
			if (!this.Active) // We died from fall damage
				return;

			this.DeactivateWallRun.Execute();
			this.CurrentState.Value = State.Straight;

			this.coord = coord;

			Vector3 coordPosition = map.GetAbsolutePosition(coord);
			this.forward = coordPosition - this.Position;

			this.isTopOut = forward.Y > 2.0f;

			this.forward.Y = 0.0f;
			this.forward.Normalize();

			// If there's nothing on the other side of the wall (it's a one-block-wide wall)
			// then vault over it rather than standing on top of it
			this.vaultOver = map[coordPosition + this.forward + Vector3.Down].ID == 0;
			if (this.vaultOver)
				this.isTopOut = false; // Don't do a top out animation if we're going to vault over it

			this.vaultVelocity = supportVelocity + new Vector3(0, this.isTopOut ? topOutVerticalSpeed : mantleVaultVerticalSpeed, 0);

			this.map = map;

			this.LinearVelocity.Value = this.vaultVelocity;
			this.IsSupported.Value = false;
			this.HasTraction.Value = false;

			Vector3 dir = map.GetAbsoluteVector(map.GetRelativeDirection(this.forward).GetVector());
			this.Rotation.Value = (float)Math.Atan2(dir.X, dir.Z);
			this.LockRotation.Execute();

			this.EnableWalking.Value = false;
			this.Crouched.Value = true;
			this.AllowUncrouch.Value = false;

			Session.Recorder.Event(main, "Vault");
			this.model.Stop
			(
				"Vault",
				"Mantle",
				"TopOut",
				"Jump",
				"JumpLeft",
				"JumpRight",
				"JumpBackward"
			);
			this.model.StartClip(this.vaultOver ? "Vault" : (this.isTopOut ? "TopOut" : "Mantle"), 4, false, AnimatedModel.DefaultBlendTime);

			if (this.random.NextDouble() > 0.5)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_GRUNT, this.Entity);
			if (this.random.NextDouble() > 0.5)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_VAULT, this.Entity);

			this.vaultTime = 0.0f;
			this.moveForwardStartTime = 0.0f;
			this.movingForward = false;
			this.originalPosition = this.Position;
		}

		private void vaultDown(Vector3 forward)
		{
			this.forward = forward;
			this.vaultVelocity = this.forward * this.MaxSpeed;
			this.vaultVelocity.Y = this.LinearVelocity.Value.Y;
			this.LinearVelocity.Value = this.vaultVelocity;
			this.LockRotation.Execute();
			this.EnableWalking.Value = false;
			this.Crouched.Value = true;
			this.AllowUncrouch.Value = false;
			this.walkOffEdgeTimer = 0.0f;

			this.vaultTime = 0.0f;
			this.CurrentState.Value = State.Down;

			this.originalPosition = this.Position;
		}

		public bool TryVaultDown()
		{
			if (this.Crouched || !this.IsSupported)
				return false;

			Matrix rotationMatrix = Matrix.CreateRotationY(this.Rotation);
			bool foundObstacle = false;
			foreach (Voxel map in Voxel.ActivePhysicsVoxels)
			{
				Direction down = map.GetRelativeDirection(Direction.NegativeY);
				Vector3 pos = this.Position + rotationMatrix.Forward * -1.75f;
				Voxel.Coord coord = map.GetCoordinate(pos);

				for (int i = 0; i < 5; i++)
				{
					if (map[coord].ID != 0)
					{
						foundObstacle = true;
						break;
					}
					coord = coord.Move(down);
				}

				if (foundObstacle)
					break;
			}

			if (!foundObstacle)
			{
				// Vault
				this.vaultDown(-rotationMatrix.Forward);
			}
			return !foundObstacle;
		}

		public void Update(float dt)
		{
			if (this.CurrentState == State.Down)
			{
				this.vaultTime += dt;

				bool delete = false;

				if (this.vaultTime > (this.isTopOut ? maxTopoutTime : maxVaultTime)) // Max vault time ensures we never get stuck
					delete = true;
				else if (this.walkOffEdgeTimer > 0.2f && this.IsSupported)
					delete = true; // We went over the edge and hit the ground. Stop.
				else if (!this.IsSupported) // We hit the edge, go down it
				{
					this.walkOffEdgeTimer += dt;

					if (this.walkOffEdgeTimer > 0.1f)
					{
						this.LinearVelocity.Value = new Vector3(0, -mantleVaultVerticalSpeed, 0);

						if (this.Position.Value.Y < this.originalPosition.Y - 3.0f)
							delete = true;
						else
						{
							this.ActivateWallRun.Execute(WallRun.State.Reverse);
							if (this.WallRunState.Value == WallRun.State.Reverse)
								delete = true;
						}
					}
				}

				if (this.walkOffEdgeTimer < 0.1f)
				{
					Vector3 velocity = this.forward * this.MaxSpeed;
					velocity.Y = this.LinearVelocity.Value.Y;
					this.LinearVelocity.Value = velocity;
				}

				if (delete)
				{
					this.AllowUncrouch.Value = true;
					this.EnableWalking.Value = true;
					this.CurrentState.Value = State.None;
				}
			}
			else if (this.CurrentState != State.None)
			{
				this.vaultTime += dt;

				bool delete = false;

				if (this.movingForward)
				{
					if (this.vaultOver && this.vaultTime - this.moveForwardStartTime > 0.25f)
						delete = true; // Done moving forward
					else if (!this.vaultOver && !this.model.IsPlaying("TopOut", "Mantle"))
						delete = true;
					else
					{
						// Still moving forward
						this.LinearVelocity.Value = this.forward * this.MaxSpeed;
						this.LastSupportedSpeed.Value = this.MaxSpeed;
					}
				}
				else
				{
					// We're still going up.
					if (this.IsSupported || this.vaultTime > (this.isTopOut ? maxTopoutTime : maxVaultTime) || this.LinearVelocity.Value.Y < 0.0f
						|| (this.FloorPosition.Value.Y > this.map.GetAbsolutePosition(this.coord).Y + (this.vaultOver ? 0.2f : 0.1f))) // Move forward
					{
						// We've reached the top of the vault. Start moving forward.
						// Max vault time ensures we never get stuck

						// If we're vaulting over a 1-block-wide wall, we need to keep the vaultMover alive for a while
						// to keep the player moving forward over the wall
						this.movingForward = true;
						this.moveForwardStartTime = this.vaultTime;
					}
					else // We're still going up.
						this.LinearVelocity.Value = vaultVelocity;
				}

				if (delete)
				{
					this.map = null;
					this.CurrentState.Value = State.None;
					this.EnableWalking.Value = true;
					this.Entity.Add(new Animation
					(
						new Animation.Delay(0.1f),
						new Animation.Set<bool>(this.AllowUncrouch, true)
					));
				}
			}
		}
	}
}
