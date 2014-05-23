using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Input;
using BEPUphysics;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class PlayerFactory : Factory<Main>
	{
		public PlayerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
			this.EditorCanSpawn = false;
		}

		private enum VaultType
		{
			None,
			Left,
			Right,
			Straight,
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Player");
		}

		private static Entity instance;
		public static Entity Instance
		{
			get
			{
				if (PlayerFactory.instance != null && !PlayerFactory.instance.Active)
					PlayerFactory.instance = null;
				return PlayerFactory.instance;
			}

			set
			{
				PlayerFactory.instance = value;
			}
		}

		private Random random = new Random();

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			entity.CannotSuspend = true;

			PlayerFactory.Instance = entity;

			this.SetMain(entity, main);

			FPSInput input = new FPSInput();
			input.EnabledWhenPaused = false;
			entity.Add("Input", input);

			AnimationController anim = entity.GetOrCreate<AnimationController>();
			Player player = entity.GetOrCreate<Player>("Player");

			AnimatedModel model = entity.GetOrCreate<AnimatedModel>("Model");
			model.Serialize = false;
			AnimatedModel firstPersonModel = entity.GetOrCreate<AnimatedModel>("FirstPersonModel");
			firstPersonModel.Serialize = false;

			model.Editable = false;
			model.Filename.Value = "Models\\joan";
			model.CullBoundingBox.Value = false;

			firstPersonModel.Editable = false;
			firstPersonModel.Filename.Value = "Models\\joan-firstperson";
			firstPersonModel.CullBoundingBox.Value = false;

			anim.Add(new Binding<bool>(anim.IsSupported, player.Character.IsSupported));
			anim.Add(new Binding<Player.WallRun>(anim.WallRunState, player.WallRunState));
			anim.Add(new Binding<bool>(anim.EnableWalking, player.Character.EnableWalking));
			anim.Add(new Binding<bool>(anim.Crouched, player.Character.Crouched));
			anim.Add(new Binding<Vector3>(anim.LinearVelocity, player.Character.LinearVelocity));
			anim.Add(new Binding<Vector2>(anim.Movement, input.Movement));
			anim.Add(new Binding<Vector2>(anim.Mouse, input.Mouse));
			anim.Add
			(
				new Binding<bool>
				(
					anim.EnableLean,
					() => player.Character.EnableWalking.Value && player.Character.IsSupported.Value && input.Movement.Value.Y > 0.5f,
					player.Character.EnableWalking, player.Character.IsSupported, input.Movement
				)
			);
			anim.Bind(model);

			model.Materials = firstPersonModel.Materials = new Model.Material[3];

			// Hoodie and shoes
			model.Materials[0] = new Model.Material
			{
				SpecularIntensity = 0.0f,
				SpecularPower = 1.0f,
			};

			// Hands
			model.Materials[1] = new Model.Material
			{
				SpecularIntensity = 0.3f,
				SpecularPower = 2.0f,
			};

			// Pants and skin
			model.Materials[2] = new Model.Material
			{
				SpecularIntensity = 0.5f,
				SpecularPower = 20.0f,
			};

			Property<Vector3> floor = new Property<Vector3>();
			transform.Add(new Binding<Vector3>(floor, () => transform.Position + new Vector3(0, player.Character.Height * -0.5f, 0), transform.Position, player.Character.Height));
			AkGameObjectTracker.Attach(entity, floor);

			firstPersonModel.Bind(model);

			// Third person model only gets rendered for shadows. No regular rendering or reflections.
			model.UnsupportedTechniques.Add(Technique.Clip);
			model.UnsupportedTechniques.Add(Technique.Render);
			
			// First-person model only used for regular rendering. No shadows or reflections.
			firstPersonModel.UnsupportedTechniques.Add(Technique.Shadow);
			firstPersonModel.UnsupportedTechniques.Add(Technique.Clip);

			// Build UI
			UIRenderer ui = new UIRenderer();
			ui.DrawOrder.Value = -1;
			ui.EnabledWhenPaused = true;
			ui.EnabledInEditMode = false;
			entity.Add("UI", ui);
			PlayerUI.Attach(main, ui, player.Health);

			GameMain.Config settings = ((GameMain)main).Settings;
			input.Add(new Binding<float>(input.MouseSensitivity, settings.MouseSensitivity));
			input.Add(new Binding<bool>(input.InvertMouseX, settings.InvertMouseX));
			input.Add(new Binding<bool>(input.InvertMouseY, settings.InvertMouseY));
			input.Add(new Binding<PCInput.PCInputBinding>(input.LeftKey, settings.Left));
			input.Add(new Binding<PCInput.PCInputBinding>(input.RightKey, settings.Right));
			input.Add(new Binding<PCInput.PCInputBinding>(input.BackwardKey, settings.Backward));
			input.Add(new Binding<PCInput.PCInputBinding>(input.ForwardKey, settings.Forward));

			model.StartClip("Idle", 0, true, AnimatedModel.DefaultBlendTime);

			Updater update = new Updater();
			update.EnabledInEditMode = false;
			entity.Add(update);

			// Set up AI agent
			Agent agent = entity.GetOrCreate<Agent>();
			agent.Add(new TwoWayBinding<float>(player.Health, agent.Health));
			agent.Add(new Binding<Vector3>(agent.Position, () => transform.Position.Value + new Vector3(0, player.Character.Height * -0.5f, 0), transform.Position, player.Character.Height));
			agent.Add(new CommandBinding(agent.Die, entity.Delete));
			agent.Add(new Binding<bool>(agent.Loud, x => !x, player.Character.Crouched));

			entity.Add(new CommandBinding(player.HealthDepleted, delegate()
			{
				Session.Recorder.Event(main, "DieFromHealth");
				AkSoundEngine.PostEvent("Play_death", entity);
				((GameMain)main).RespawnDistance = GameMain.KilledRespawnDistance;
				((GameMain)main).RespawnInterval = GameMain.KilledRespawnInterval;
			}));

			entity.Add(new CommandBinding(player.HealthDepleted, entity.Delete));

			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				Session.Recorder.Event(main, "Die");
				if (Agent.Query(transform.Position, 0.0f, 10.0f, x => x != agent) != null)
				{
					((GameMain)main).RespawnDistance = GameMain.KilledRespawnDistance;
					((GameMain)main).RespawnInterval = GameMain.KilledRespawnInterval;
				}
			}));

			player.EnabledInEditMode = false;

			input.MaxY.Value = (float)Math.PI * 0.35f;

			Property<float> rotation = entity.GetOrMakeProperty<float>("Rotation");

			Action updateFallSound = delegate()
			{
				float speed = player.Character.LinearVelocity.Value.Length();
				float maxSpeed = player.Character.MaxSpeed * 1.25f;
				float value;
				if (speed > maxSpeed)
					value = (speed - maxSpeed) / (maxSpeed * 2.0f);
				else
					value = 0.0f;
				AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_PLAYER_FALL, value);
			};
			updateFallSound();
			AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_FALL, entity);
			player.Add(new NotifyBinding(updateFallSound, player.Character.LinearVelocity));
			SoundKiller.Add(entity, AK.EVENTS.STOP_PLAYER_FALL);

			Action stopKick = null;

			// Player rotation code
			Property<bool> rotationLocked = new Property<bool>();
			const float rotationLockBlendTime = 0.3f;
			float lockedRotationValue = 0.0f;
			float rotationLockBlending = rotationLockBlendTime;

			input.Mouse.Value = new Vector2(rotation, 0.0f);

			rotation.Set = delegate(float value)
			{
				if (rotationLocked)
					input.Mouse.Value += new Vector2(value - rotation.InternalValue, 0);
				rotation.InternalValue = value;
			};

			rotationLocked.Set = delegate(bool value)
			{
				if (rotationLocked.InternalValue && !value)
					rotationLockBlending = 0.0f;
				else if (!rotationLocked.InternalValue && value)
				{
					lockedRotationValue = rotation.Value.ClosestAngle(input.Mouse.Value.X);
					rotationLockBlending = 0.0f;
				}
				rotationLocked.InternalValue = value;
			};

			// When rotation is locked, we want to make sure the player can't turn their head
			// 180 degrees from the direction they're facing

			input.Add(new Binding<float>(input.MaxY, () => rotationLocked ? (float)Math.PI * 0.3f : (float)Math.PI * 0.4f, rotationLocked));
			input.Add(new Binding<float>(input.MinX, () => rotationLocked ? rotation + ((float)Math.PI * -0.4f) : 0.0f, rotation, rotationLocked));
			input.Add(new Binding<float>(input.MaxX, () => rotationLocked ? rotation + ((float)Math.PI * 0.4f) : 0.0f, rotation, rotationLocked));
			input.Add(new NotifyBinding(delegate() { input.Mouse.Changed(); }, rotationLocked)); // Make sure the rotation locking takes effect even if the player doesn't move the mouse

			update.Add(delegate(float dt)
			{
				if (rotationLockBlending < rotationLockBlendTime)
					rotationLockBlending += dt;

				if (!rotationLocked)
				{
					if (rotationLockBlending < rotationLockBlendTime)
					{
						lockedRotationValue = lockedRotationValue.ClosestAngle(input.Mouse.Value.X);
						rotation.Value = lockedRotationValue + (input.Mouse.Value.X - lockedRotationValue) * (rotationLockBlending / rotationLockBlendTime);
					}
					else
						rotation.Value = input.Mouse.Value.X;
				}
			});

			Map wallRunMap = null, lastWallRunMap = null;
			Direction wallDirection = Direction.None, lastWallDirection = Direction.None;
			Direction wallRunDirection = Direction.None;

			player.Add(new TwoWayBinding<Matrix>(transform.Matrix, player.Character.Transform));

			model.Add(new Binding<Matrix>(model.Transform, delegate()
			{
				const float leanAmount = (float)Math.PI * 0.1f;
				return Matrix.CreateTranslation(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0) * Matrix.CreateRotationZ(anim.Lean * leanAmount) * Matrix.CreateRotationY(rotation) * transform.Matrix;
			}, transform.Matrix, rotation, player.Character.Height, player.Character.SupportHeight, anim.Lean));

			firstPersonModel.Add(new Binding<Matrix>(firstPersonModel.Transform, model.Transform));
			firstPersonModel.Add(new Binding<Vector3>(firstPersonModel.Scale, model.Scale));

			Footsteps footsteps = entity.GetOrCreate<Footsteps>();
			Player.WallRun[] footstepWallrunStates = new[]
			{
				Player.WallRun.Left,
				Player.WallRun.Right,
				Player.WallRun.Straight,
				Player.WallRun.None,
			};
			footsteps.Add(new Binding<bool>(footsteps.SoundEnabled, () => footstepWallrunStates.Contains(player.WallRunState) || (player.Character.IsSupported && player.Character.EnableWalking), player.Character.IsSupported, player.Character.EnableWalking, player.WallRunState));
			footsteps.Add(new Binding<Vector3>(footsteps.Position, transform.Position));
			footsteps.Add(new Binding<float>(footsteps.Rotation, rotation));
			footsteps.Add(new Binding<float>(footsteps.CharacterHeight, player.Character.Height));
			footsteps.Add(new Binding<float>(footsteps.SupportHeight, player.Character.SupportHeight));
			footsteps.Add(new TwoWayBinding<float>(player.Health, footsteps.Health));
			model.Trigger("Run", 0.16f, footsteps.Footstep);
			model.Trigger("Run", 0.58f, footsteps.Footstep);
			model.Trigger("WallRunLeft", 0.16f, footsteps.Footstep);
			model.Trigger("WallRunLeft", 0.58f, footsteps.Footstep);
			model.Trigger("WallRunRight", 0.16f, footsteps.Footstep);
			model.Trigger("WallRunRight", 0.58f, footsteps.Footstep);
			model.Trigger("WallRunStraight", 0.16f, footsteps.Footstep);
			model.Trigger("WallRunStraight", 0.58f, footsteps.Footstep);

			main.IsMouseVisible.Value = false;

			SkinnedModel.Clip sprintAnimation = model["Sprint"], runAnimation = model["Run"];

			// Movement binding
			player.Add(new Binding<Vector2>(player.Character.MovementDirection, delegate()
			{
				Vector2 movement = input.Movement;
				if (movement.LengthSquared() == 0.0f)
					return Vector2.Zero;

				Matrix matrix = Matrix.CreateRotationY(rotation);

				Vector2 forwardDir = new Vector2(matrix.Forward.X, matrix.Forward.Z);
				Vector2 rightDir = new Vector2(matrix.Right.X, matrix.Right.Z);
				return -(forwardDir * movement.Y) - (rightDir * movement.X);
			}, input.Movement, rotation));

			player.Character.Crouched.Value = true;
			player.Character.AllowUncrouch.Value = true;

			bool canKick = false;
			int wallJumpCount = 0;
			Vector3 wallJumpChainStart = Vector3.Zero;
			
			Action resetInAirState = delegate()
			{
				canKick = true;
				wallJumpCount = 0;
			};

			player.Add(new NotifyBinding(delegate()
			{
				if (player.Character.IsSupported)
					resetInAirState();
			}, player.Character.IsSupported));


			Map.CellState temporary = Map.States[Map.t.Temporary];

			BlockPredictor predictor = entity.GetOrCreate<BlockPredictor>("BlockPredictor");
			predictor.Add(new Binding<Vector3>(predictor.FootPosition, floor));
			predictor.Add(new Binding<Vector3>(predictor.LinearVelocity, player.Character.LinearVelocity));
			predictor.Add(new Binding<float>(predictor.Rotation, rotation));
			predictor.Add(new Binding<float>(predictor.MaxSpeed, player.Character.MaxSpeed));
			predictor.Add(new Binding<float>(predictor.JumpSpeed, player.Character.JumpSpeed));

			// Wall run

			const float minWallRunSpeed = 4.0f;

			Action<Map, Direction, Player.WallRun, Vector3, bool> setUpWallRun = delegate(Map map, Direction dir, Player.WallRun state, Vector3 forwardVector, bool addInitialVelocity)
			{
				stopKick();
				player.Character.AllowUncrouch.Value = true;

				wallRunMap = lastWallRunMap = map;
				wallDirection = lastWallDirection = dir;

				if (state == Player.WallRun.Straight)
				{
					// Determine if we're actually going down
					if (!player.Character.IsSupported && player.Character.LinearVelocity.Value.Y < -0.5f)
						state = Player.WallRun.Down;
				}

				player.WallRunState.Value = state;

				string animation;
				switch (state)
				{
					case Player.WallRun.Left:
						animation = "WallRunLeft";
						break;
					case Player.WallRun.Right:
						animation = "WallRunRight";
						break;
					case Player.WallRun.Straight:
						animation = "WallRunStraight";
						break;
					case Player.WallRun.Reverse:
						animation = "WallSlideReverse";
						break;
					default:
						animation = "WallSlideDown";
						break;
				}
				if (!model.IsPlaying(animation))
					model.StartClip(animation, 5, true, 0.1f);

				Session.Recorder.Event(main, "WallRun", animation);

				wallRunDirection = state == Player.WallRun.Straight ? map.GetRelativeDirection(Vector3.Up) : (state == Player.WallRun.Down ? map.GetRelativeDirection(Vector3.Down) : dir.Cross(map.GetRelativeDirection(Vector3.Up)));

				if (state == Player.WallRun.Straight || state == Player.WallRun.Down || state == Player.WallRun.Reverse)
				{
					if (state == Player.WallRun.Straight)
					{
						Vector3 velocity = player.Character.LinearVelocity.Value;
						velocity.X = 0;
						velocity.Z = 0;
						if (addInitialVelocity)
						{
							if (player.Character.IsSupported)
								velocity.Y = player.Character.JumpSpeed * 1.3f;
							else
								velocity.Y = player.Character.LinearVelocity.Value.Y + player.Character.JumpSpeed * 0.75f;
						}
						else
							velocity.Y = player.Character.LinearVelocity.Value.Y;

						player.Character.LinearVelocity.Value = velocity;
						player.Character.IsSupported.Value = false;
						player.Character.HasTraction.Value = false;
					}
					Vector3 wallVector = wallRunMap.GetAbsoluteVector(wallDirection.GetVector());

					if (state == Player.WallRun.Reverse)
						wallVector = -wallVector;

					// Make sure we lock in the correct rotation value
					rotation.Value = (float)Math.Atan2(wallVector.X, wallVector.Z);
					rotationLocked.Value = true;
				}
				else
				{
					player.Character.IsSupported.Value = false;
					player.Character.HasTraction.Value = false;
					Vector3 velocity = map.GetAbsoluteVector(wallRunDirection.GetVector());
					if (Vector3.Dot(velocity, forwardVector) < 0.0f)
					{
						velocity = -velocity;
						wallRunDirection = wallRunDirection.GetReverse();
					}
					rotation.Value = (float)Math.Atan2(velocity.X, velocity.Z);
					rotationLocked.Value = true;

					if (addInitialVelocity)
					{
						velocity.Y = 0.0f;
						float length = velocity.Length();
						if (length > 0)
						{
							velocity /= length;

							Vector3 currentHorizontalVelocity = player.Character.LinearVelocity;
							currentHorizontalVelocity.Y = 0.0f;
							velocity *= Math.Min(player.Character.MaxSpeed * 2.0f, Math.Max(currentHorizontalVelocity.Length() * 1.25f, 6.0f));

							if (state != Player.WallRun.Straight && state != Player.WallRun.Reverse && Vector3.Dot(player.Character.LinearVelocity, forwardVector) < 0.0f)
								velocity = Vector3.Normalize(velocity) * (minWallRunSpeed + 1.0f);

							float currentVerticalSpeed = player.Character.LinearVelocity.Value.Y;
							velocity.Y = (currentVerticalSpeed > -3.0f ? Math.Max(currentVerticalSpeed * 0.7f, 0.0f) : currentVerticalSpeed * 0.5f) + 5.0f;

							player.Character.LinearVelocity.Value = velocity;
						}
					}
				}
			};

			float lastWallRunEnded = -1.0f, lastWallJump = -1.0f;
			const float wallRunDelay = 0.5f;

			// Since block possibilities are instantiated on another thread,
			// we have to give that thread some time to do it before checking if there is actually a wall to run on.
			// Otherwise, we will immediately stop wall-running since the wall hasn't been instantiated yet.
			float wallInstantiationTimer = 0.0f;

			Func<Player.WallRun, bool> activateWallRun = delegate(Player.WallRun state)
			{
				Vector3 playerVelocity = player.Character.LinearVelocity;
				if (playerVelocity.Y < FallDamage.RollingDamageVelocity)
					return false;

				wallInstantiationTimer = 0.0f;

				// Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.
				bool wallRunDelayPassed = main.TotalTime - lastWallRunEnded > wallRunDelay;
				bool wallRunJumpDelayPassed = main.TotalTime - lastWallJump > wallRunDelay;

				Matrix matrix = Matrix.CreateRotationY(rotation);

				Vector3 forwardVector = -matrix.Forward;

				playerVelocity.Normalize();
				playerVelocity.Y = 0.0f;
				if (Vector3.Dot(forwardVector, playerVelocity) < -0.3f)
					return false;

				Vector3 wallVector;
				switch (state)
				{
					case Player.WallRun.Straight:
						wallVector = forwardVector;
						break;
					case Player.WallRun.Left:
						wallVector = -matrix.Left;
						break;
					case Player.WallRun.Right:
						wallVector = -matrix.Right;
						break;
					case Player.WallRun.Reverse:
						wallVector = -forwardVector;
						wallInstantiationTimer = 0.25f;
						break;
					default:
						wallVector = Vector3.Zero;
						break;
				}

				Vector3 pos = transform.Position + new Vector3(0, player.Character.Height * -0.5f, 0);

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
							bool differentWall = map != lastWallRunMap || dir != lastWallDirection;
							activate = differentWall || wallRunJumpDelayPassed;
							addInitialVelocity = differentWall || wallRunDelayPassed;
						}
						else
						{
							// Check block possibilities
							List<BlockPredictor.Possibility> mapBlockPossibilities = predictor.GetPossibilities(map);
							if (mapBlockPossibilities != null)
							{
								foreach (BlockPredictor.Possibility block in mapBlockPossibilities)
								{
									if (wallCoord.Between(block.StartCoord, block.EndCoord))
									{
										predictor.InstantiatePossibility(block);
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
							transform.Position.Value = map.GetAbsolutePosition(coord.Move(dir, i - 2)) + new Vector3(0, player.Character.Height * 0.5f, 0);
							break;
						}
					}

					if (activate)
					{
						setUpWallRun(map, dir, state, forwardVector, addInitialVelocity);
						break;
					}
				}
				return activate;
			};

			Action deactivateWallRun = null;

			Func<Vector3, Vector3, bool, bool> breakWalls = delegate(Vector3 forward, Vector3 right, bool breakFloor)
			{
				BlockFactory blockFactory = Factory.Get<BlockFactory>();
				Vector3 pos = transform.Position + new Vector3(0, 0.1f + (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0) + forward * -1.0f;
				Vector3 basePos = pos;
				bool broke = false;
				foreach (Map map in Map.ActivePhysicsMaps.ToList())
				{
					List<Map.Coordinate> removals = new List<Map.Coordinate>();
					Quaternion mapQuaternion = map.Entity.Get<Transform>().Quaternion;
					pos = basePos;
					for (int i = 0; i < 5; i++)
					{
						Map.Coordinate center = map.GetCoordinate(pos);
						Map.Coordinate top = map.GetCoordinate(basePos + new Vector3(0, player.Character.CrouchedHeight + player.Character.CrouchedSupportHeight + 0.5f, 0));
						Direction upDir = map.GetRelativeDirection(Vector3.Up);
						Direction rightDir = map.GetRelativeDirection(right);
						for (Map.Coordinate y = center.Move(upDir.GetReverse(), breakFloor ? 2 : 0); y.GetComponent(upDir) <= top.GetComponent(upDir); y = y.Move(upDir))
						{
							for (Map.Coordinate z = y.Move(rightDir.GetReverse(), 1); z.GetComponent(rightDir) < center.GetComponent(rightDir) + 2; z = z.Move(rightDir))
							{
								Map.CellState state = map[z];
								if (state.ID != 0 && !state.Permanent && !state.Hard && !removals.Contains(z))
								{
									broke = true;
									removals.Add(z);
									Vector3 cellPos = map.GetAbsolutePosition(z);
									Vector3 toCell = cellPos - pos;
									Entity block = blockFactory.CreateAndBind(main);
									Transform blockTransform = block.Get<Transform>();
									blockTransform.Position.Value = cellPos;
									blockTransform.Quaternion.Value = mapQuaternion;
									state.ApplyToBlock(block);
									toCell += forward * 4.0f;
									toCell.Normalize();
									PhysicsBlock physicsBlock = block.Get<PhysicsBlock>();
									physicsBlock.LinearVelocity.Value = toCell * 15.0f;
									physicsBlock.AngularVelocity.Value = new Vector3(((float)this.random.NextDouble() - 0.5f) * 2.0f, ((float)this.random.NextDouble() - 0.5f) * 2.0f, ((float)this.random.NextDouble() - 0.5f) * 2.0f);
									main.Add(block);
								}
							}
						}
						pos += forward * 0.5f;
					}
					if (removals.Count > 0)
					{
						map.Empty(removals);
						map.Regenerate();
					}
				}
				return broke;
			};

			Func<bool, bool, bool> jump = null;

			// Keep the player glued to the wall while we wall walk
			update.Add(delegate(float dt)
			{
				Player.WallRun wallRunState = player.WallRunState;
				if (wallRunState != Player.WallRun.None)
				{
					if (jump(true, true)) // Try to vault up
						return;

					if (!wallRunMap.Active || player.Character.IsSupported)
					{
						deactivateWallRun();
						return;
					}

					float wallRunSpeed = Vector3.Dot(player.Character.LinearVelocity.Value, wallRunMap.GetAbsoluteVector(wallRunDirection.GetVector()));

					if (wallRunState == Player.WallRun.Straight)
					{
						if (wallRunSpeed < 0.0f)
						{
							// Start sliding down
							player.WallRunState.Value = wallRunState = Player.WallRun.Down;
							model.Stop("WallRunStraight");
							model.StartClip("WallSlideDown", 5, true, AnimatedModel.DefaultBlendTime);
						}
					}
					else if (wallRunState == Player.WallRun.Left || wallRunState == Player.WallRun.Right)
					{
						if (wallRunSpeed < minWallRunSpeed)
						{
							// We landed on the ground or we're going too slow to continue wall-running
							deactivateWallRun();
							return;
						}
					}

					string wallRunAnimation;
					switch (wallRunState)
					{
						case Player.WallRun.Straight:
							wallRunAnimation = "WallRunStraight";
							break;
						case Player.WallRun.Down:
							wallRunAnimation = "WallSlideDown";
							break;
						case Player.WallRun.Left:
							wallRunAnimation = "WallRunLeft";
							break;
						case Player.WallRun.Right:
							wallRunAnimation = "WallRunRight";
							break;
						case Player.WallRun.Reverse:
							wallRunAnimation = "WallSlideReverse";
							break;
						default:
							wallRunAnimation = null;
							break;
					}

					if (wallRunAnimation != null)
					{
						Vector3 wallNormal = wallRunMap.GetAbsoluteVector(wallDirection.GetVector());
						float animationSpeed = (player.Character.LinearVelocity.Value - wallNormal * Vector3.Dot(player.Character.LinearVelocity.Value, wallNormal)).Length();
						model[wallRunAnimation].Speed = Math.Min(1.5f, animationSpeed / 6.0f);
					}

					Vector3 pos = transform.Position + new Vector3(0, player.Character.Height * -0.5f, 0);
					Map.Coordinate coord = wallRunMap.GetCoordinate(pos);
					Map.Coordinate wallCoord = coord.Move(wallDirection, 2);
					Map.CellState wallType = wallRunMap[wallCoord];
					footsteps.WalkedOn.Execute(wallRunMap, wallCoord, wallDirection);

					if (player.EnableEnhancedWallRun && (wallRunState == Player.WallRun.Left || wallRunState == Player.WallRun.Right))
					{
						Direction up = wallRunMap.GetRelativeDirection(Direction.PositiveY);
						Direction right = wallDirection.Cross(up);

						List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();

						const int radius = 5;
						int upwardRadius = wallRunState == Player.WallRun.Down || wallRunState == Player.WallRun.Reverse ? 0 : radius;
						for (Map.Coordinate x = wallCoord.Move(right, -radius); x.GetComponent(right) < wallCoord.GetComponent(right) + radius; x = x.Move(right))
						{
							int dx = x.GetComponent(right) - wallCoord.GetComponent(right);
							for (Map.Coordinate y = x.Move(up, -radius); y.GetComponent(up) < wallCoord.GetComponent(up) + upwardRadius; y = y.Move(up))
							{
								int dy = y.GetComponent(up) - wallCoord.GetComponent(up);
								if ((float)Math.Sqrt(dx * dx + dy * dy) < radius && wallRunMap[y].ID == 0)
								{
									buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
									{
										Map = wallRunMap,
										Coordinate = y,
										State = temporary,
									});
								}
							}
						}
						Factory.Get<EffectBlockFactory>().Build(main, buildCoords, false, transform.Position);
					}
					else if (wallType.ID == 0 && wallInstantiationTimer == 0.0f) // We ran out of wall to walk on
					{
						deactivateWallRun();
						return;
					}

					if (wallRunMap == null || !wallRunMap.Active)
						return;

					wallInstantiationTimer = Math.Max(0.0f, wallInstantiationTimer - dt);

					Vector3 coordPos = wallRunMap.GetAbsolutePosition(coord);

					Vector3 normal = wallRunMap.GetAbsoluteVector(wallDirection.GetVector());
					// Equation of a plane
					// normal (dot) point = d
					float d = Vector3.Dot(normal, coordPos);

					// Distance along the normal to keep the player glued to the wall
					float snapDistance = d - Vector3.Dot(pos, normal);

					transform.Position.Value += normal * snapDistance;

					Vector3 velocity = player.Character.LinearVelocity;

					// Also fix the velocity so we don't jitter away from the wall
					velocity -= Vector3.Dot(velocity, normal) * normal;

					// Slow our descent
					velocity += new Vector3(0, (wallRunState == Player.WallRun.Straight ? 3.0f : 10.0f) * dt, 0);

					player.Character.LinearVelocity.Value = velocity;
				}
			});

			player.Add(new NotifyBinding(delegate()
			{
				if (!player.EnableMoves)
				{
					deactivateWallRun();
					stopKick();
					player.SlowMotion.Value = false;
				}
			}, player.EnableMoves));

			// Fall damage
			FallDamage fallDamage = entity.GetOrCreate<FallDamage>();
			fallDamage.Add(new Binding<bool>(fallDamage.IsSupported, player.Character.IsSupported));
			fallDamage.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, fallDamage.LinearVelocity));
			fallDamage.Add(new TwoWayBinding<float>(player.Health, fallDamage.Health));
			fallDamage.Add(new CommandBinding<BEPUphysics.BroadPhaseEntries.Collidable, ContactCollection>(player.Character.Collided, fallDamage.Collided));
			fallDamage.Model = model;

			Updater vaultMover = null;

			float rollEnded = -1.0f;

			Action<Map, Map.Coordinate> vault = delegate(Map map, Map.Coordinate coord)
			{
				const float vaultVerticalSpeed = 8.0f;
				const float maxVaultTime = 1.0f;

				Vector3 coordPosition = map.GetAbsolutePosition(coord);
				Vector3 forward = Vector3.Normalize(coordPosition - transform.Position);
				forward.Y = 0.0f;

				Vector3 vaultVelocity = new Vector3(0, vaultVerticalSpeed, 0);

				DynamicMap dynamicMap = map as DynamicMap;
				if (dynamicMap != null)
				{
					BEPUphysics.Entities.Entity supportEntity = dynamicMap.PhysicsEntity;
					Vector3 supportLocation = transform.Position.Value + new Vector3(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0);
					vaultVelocity += supportEntity.LinearVelocity + Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
				}

				// If there's nothing on the other side of the wall (it's a one-block-wide wall)
				// then vault over it rather than standing on top of it
				bool vaultOver = map[coordPosition + forward + Vector3.Down].ID == 0;

				player.Character.LinearVelocity.Value = vaultVelocity;
				player.Character.IsSupported.Value = false;
				player.Character.HasTraction.Value = false;

				Matrix rotationMatrix = Matrix.CreateRotationY(rotation);
				Vector3 dir = map.GetAbsoluteVector(map.GetRelativeDirection(new Vector3(-rotationMatrix.Forward.X, 0, -rotationMatrix.Forward.Z)).GetVector());
				rotation.Value = (float)Math.Atan2(dir.X, dir.Z);
				rotationLocked.Value = true;

				player.Character.EnableWalking.Value = false;
				player.Character.Crouched.Value = true;
				player.Character.AllowUncrouch.Value = false;

				float vaultTime = 0.0f;
				if (vaultMover != null)
					vaultMover.Delete.Execute(); // If we're already vaulting, start a new vault
				
				float moveForwardStartTime = 0.0f;
				bool movingForward = false;

				vaultMover = new Updater
				{
					delegate(float dt)
					{
						vaultTime += dt;

						bool delete = false;

						if (movingForward)
						{
							if (vaultTime - moveForwardStartTime > 0.25f)
								delete = true; // Done moving forward
							else
							{
								// Still moving forward
								player.Character.LinearVelocity.Value = forward * player.Character.MaxSpeed;
								player.Character.LastSupportedSpeed.Value = player.Character.MaxSpeed;
							}
						}
						else
						{
							// We're still going up.
							if (player.Character.IsSupported || vaultTime > maxVaultTime || player.Character.LinearVelocity.Value.Y < 0.0f
								|| (transform.Position.Value.Y + (player.Character.Height * -0.5f) - player.Character.SupportHeight > map.GetAbsolutePosition(coord).Y + 0.1f)) // Move forward
							{
								// We've reached the top of the vault. Start moving forward.
								// Max vault time ensures we never get stuck

								if (vaultOver)
								{
									// If we're vaulting over a 1-block-wide wall, we need to keep the vaultMover alive for a while
									// to keep the player moving forward over the wall
									movingForward = true;
									moveForwardStartTime = vaultTime;
								}
								else
								{
									// We're not vaulting over a 1-block-wide wall
									// So just stop
									player.Character.LinearVelocity.Value = forward * player.Character.MaxSpeed;
									player.Character.LastSupportedSpeed.Value = player.Character.MaxSpeed;
									player.Character.Body.ActivityInformation.Activate();
									delete = true;
								}
							}
							else // We're still going up.
								player.Character.LinearVelocity.Value = vaultVelocity;
						}

						if (delete)
						{
							vaultMover.Delete.Execute(); // Make sure we get rid of this vault mover
							vaultMover = null;
							rotationLocked.Value = false;
							player.Character.EnableWalking.Value = true;
							entity.Add(new Animation
							(
								new Animation.Delay(0.1f),
								new Animation.Set<bool>(player.Character.AllowUncrouch, true)
							));
						}
					}
				};
				entity.RemoveComponent("VaultMover");
				entity.Add("VaultMover", vaultMover);
			};

			jump = delegate(bool allowVault, bool onlyVault)
			{
				if (player.Character.Crouched)
					return false;

				bool supported = player.Character.IsSupported;

				Player.WallRun wallRunState = player.WallRunState;

				// Check if we're vaulting
				Matrix rotationMatrix = Matrix.CreateRotationY(rotation);
				VaultType vaultType = VaultType.None;
				if (allowVault)
				{
					foreach (Map map in Map.ActivePhysicsMaps)
					{
						Direction up = map.GetRelativeDirection(Direction.PositiveY);
						Direction right = map.GetRelativeDirection(Vector3.Cross(Vector3.Up, -rotationMatrix.Forward));
						Vector3 pos = transform.Position + rotationMatrix.Forward * -1.75f;
						Map.Coordinate baseCoord = map.GetCoordinate(pos).Move(up, 1);
						int verticalSearchDistance = player.Character.IsSupported ? 2 : 3;
						foreach (int x in new[] { 0, -1, 1 })
						{
							Map.Coordinate coord = baseCoord.Move(right, x);
							for (int i = 0; i < verticalSearchDistance; i++)
							{
								Map.Coordinate downCoord = coord.Move(up.GetReverse());

								if (map[coord].ID != 0)
									break;
								else if (map[downCoord].ID != 0)
								{
									// Vault
									vault(map, coord);
									switch (x)
									{
										case -1:
											vaultType = VaultType.Left;
											break;
										case 1:
											vaultType = VaultType.Right;
											break;
										default:
											vaultType = VaultType.Straight;
											break;
									}
									break;
								}
								coord = coord.Move(up.GetReverse());
							}
							if (vaultType != VaultType.None)
								break;
						}
						if (vaultType != VaultType.None)
							break;
					}
				}

				Vector2 jumpDirection = player.Character.MovementDirection;

				Vector3 baseVelocity = Vector3.Zero;

				bool wallJumping = false;

				const float wallJumpHorizontalVelocityAmount = 0.75f;
				const float wallJumpDistance = 2.0f;

				Action<Map, Direction, Map.Coordinate> wallJump = delegate(Map wallJumpMap, Direction wallNormalDirection, Map.Coordinate wallCoordinate)
				{
					lastWallRunMap = wallJumpMap;
					lastWallDirection = wallNormalDirection.GetReverse();
					lastWallJump = main.TotalTime;

					Map.CellState wallType = wallJumpMap[wallCoordinate];
					if (wallType == Map.EmptyState) // Empty. Must be a block possibility that hasn't been instantiated yet
						wallType = temporary;
					AkSoundEngine.SetSwitch(AK.SWITCHES.FOOTSTEP_MATERIAL.GROUP, wallType.FootstepSwitch, entity);
					AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, entity);

					footsteps.WalkedOn.Execute(wallJumpMap, wallCoordinate, wallNormalDirection.GetReverse());

					wallJumping = true;
					// Set up wall jump velocity
					Vector3 absoluteWallNormal = wallJumpMap.GetAbsoluteVector(wallNormalDirection.GetVector());
					Vector2 wallNormal2 = new Vector2(absoluteWallNormal.X, absoluteWallNormal.Z);
					wallNormal2.Normalize();

					bool wallRunningStraight = wallRunState == Player.WallRun.Straight || wallRunState == Player.WallRun.Down;
					if (wallRunningStraight)
						jumpDirection = new Vector2(main.Camera.Forward.Value.X, main.Camera.Forward.Value.Z);
					else
						jumpDirection = new Vector2(-rotationMatrix.Forward.X, -rotationMatrix.Forward.Z);

					jumpDirection.Normalize();

					float dot = Vector2.Dot(wallNormal2, jumpDirection);
					if (dot < 0)
						jumpDirection = jumpDirection - (2.0f * dot * wallNormal2);
					jumpDirection *= wallJumpHorizontalVelocityAmount;

					if (!wallRunningStraight && Math.Abs(dot) < 0.5f)
					{
						// If we're jumping perpendicular to the wall, add some velocity so we jump away from the wall a bit
						jumpDirection += wallJumpHorizontalVelocityAmount * 0.75f * wallNormal2;
					}

					DynamicMap dynamicMap = wallJumpMap as DynamicMap;
					if (dynamicMap != null)
					{
						BEPUphysics.Entities.Entity supportEntity = dynamicMap.PhysicsEntity;
						Vector3 supportLocation = transform.Position.Value + new Vector3(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0);
						baseVelocity += supportEntity.LinearVelocity + Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
					}
				};

				if (!onlyVault && vaultType == VaultType.None
					&& !supported && wallRunState == Player.WallRun.None
					&& player.Character.LinearVelocity.Value.Y > FallDamage.DamageVelocity * 1.5f)
				{
					// We're not vaulting, not doing our normal jump, and not wall-walking
					// See if we can wall-jump
					Vector3 playerPos = transform.Position;
					Map.GlobalRaycastResult? wallRaycastHit = null;
					Vector3 wallRaycastDirection = Vector3.Zero;

					foreach (Vector3 dir in new[] { rotationMatrix.Left, rotationMatrix.Right, rotationMatrix.Backward, rotationMatrix.Forward })
					{
						Map.GlobalRaycastResult hit = Map.GlobalRaycast(playerPos, dir, wallJumpDistance);
						if (hit.Map != null)
						{
							wallRaycastDirection = dir;
							wallRaycastHit = hit;
							break;
						}
					}

					if (wallRaycastHit != null)
					{
						Map m = wallRaycastHit.Value.Map;
						wallJump(m, wallRaycastHit.Value.Normal, wallRaycastHit.Value.Coordinate.Value);
					}
				}

				// If we're wall-running, we can wall-jump
				// Add some velocity so we jump away from the wall a bit
				if (!onlyVault && wallRunState != Player.WallRun.None)
				{
					Vector3 pos = transform.Position + new Vector3(0, (player.Character.Height * -0.5f) - 0.5f, 0);
					Map.Coordinate wallCoord = wallRunMap.GetCoordinate(pos).Move(wallDirection, 2);
					wallJump(wallRunMap, wallDirection.GetReverse(), wallCoord);
				}

				bool go = vaultType != VaultType.None || (!onlyVault && (supported || wallJumping));

				bool blockPossibilityBeneath = false;

				if (!go && !onlyVault)
				{
					// Check block possibilities beneath us
					Vector3 jumpPos = transform.Position + new Vector3(0, player.Character.Height * -0.5f - player.Character.SupportHeight - 1.0f, 0);
					foreach (BlockPredictor.Possibility possibility in predictor.AllPossibilities)
					{
						if (possibility.Map.GetCoordinate(jumpPos).Between(possibility.StartCoord, possibility.EndCoord)
							&& !possibility.Map.GetCoordinate(jumpPos + new Vector3(2.0f)).Between(possibility.StartCoord, possibility.EndCoord))
						{
							predictor.InstantiatePossibility(possibility);
							go = true;
							blockPossibilityBeneath = true;
							break;
						}
					}
				}

				if (!go && allowVault)
				{
					// Check block possibilities for vaulting
					foreach (BlockPredictor.Possibility possibility in predictor.AllPossibilities)
					{
						Direction up = possibility.Map.GetRelativeDirection(Direction.PositiveY);
						Direction right = possibility.Map.GetRelativeDirection(Vector3.Cross(Vector3.Up, -rotationMatrix.Forward));
						Vector3 pos = transform.Position + rotationMatrix.Forward * -1.75f;
						Map.Coordinate baseCoord = possibility.Map.GetCoordinate(pos).Move(up, 1);
						foreach (int x in new[] { 0, -1, 1 })
						{
							Map.Coordinate coord = baseCoord.Move(right, x);
							for (int i = 0; i < 4; i++)
							{
								Map.Coordinate downCoord = coord.Move(up.GetReverse());
								if (!coord.Between(possibility.StartCoord, possibility.EndCoord) && downCoord.Between(possibility.StartCoord, possibility.EndCoord))
								{
									predictor.InstantiatePossibility(possibility);
									vault(possibility.Map, coord);
									switch (x)
									{
										case -1:
											vaultType = VaultType.Left;
											break;
										case 1:
											vaultType = VaultType.Right;
											break;
										default:
											vaultType = VaultType.Straight;
											break;
									}
									go = true;
									break;
								}
								coord = coord.Move(up.GetReverse());
							}
							if (vaultType != VaultType.None)
								break;
						}
						if (vaultType != VaultType.None)
							break;
					}
				}

				if (!go && !onlyVault)
				{
					// Check block possibilities for wall jumping
					Vector3 playerPos = transform.Position;
					Vector3[] wallJumpDirections = new[] { rotationMatrix.Left, rotationMatrix.Right, rotationMatrix.Backward, rotationMatrix.Forward };
					foreach (BlockPredictor.Possibility possibility in predictor.AllPossibilities)
					{
						foreach (Vector3 dir in wallJumpDirections)
						{
							foreach (Map.Coordinate coord in possibility.Map.Rasterize(playerPos, playerPos + (dir * wallJumpDistance)))
							{
								if (coord.Between(possibility.StartCoord, possibility.EndCoord))
								{
									predictor.InstantiatePossibility(possibility);
									wallJump(possibility.Map, possibility.Map.GetRelativeDirection(dir).GetReverse(), coord);
									wallJumping = true;
									break;
								}
							}
							if (wallJumping)
								break;
						}
						if (wallJumping)
							break;
					}
				}

				if (go)
				{
					float totalMultiplier = 1.0f;

					if (wallJumping)
					{
						if (wallJumpCount == 0)
							wallJumpChainStart = transform.Position;
						else
						{
							Vector3 chainDistance = transform.Position - wallJumpChainStart;
							chainDistance.Y = 0.0f;
							if (chainDistance.Length() > 6.0f)
							{
								wallJumpCount = 0;
								wallJumpChainStart = transform.Position;
							}
						}

						if (wallJumpCount > 3)
							return false;
						totalMultiplier = 1.0f - Math.Min(1.0f, wallJumpCount / 8.0f);
						wallJumpCount++;
					}
					else
					{
						if (supported)
						{
							// Regular jump
							// Take base velocity into account

							BEPUphysics.Entities.Entity supportEntity = player.Character.SupportEntity;
							if (supportEntity != null)
							{
								Vector3 supportLocation = transform.Position.Value + new Vector3(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0);
								baseVelocity += supportEntity.LinearVelocity + Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
							}
						}
						else
						{
							// We haven't hit the ground, so fall damage will not be handled by the physics system.
							// Need to do it manually here.
							fallDamage.Apply.Execute(player.Character.LinearVelocity.Value.Y);

							// Also manually reset in-air state
							resetInAirState();
						}
					}

					if (vaultType == VaultType.None)
					{
						// Just a normal jump.
						Vector3 velocity = player.Character.LinearVelocity;
						float currentVerticalSpeed = velocity.Y;
						velocity.Y = 0.0f;
						float jumpSpeed = jumpDirection.Length();
						if (jumpSpeed > 0)
							jumpDirection *= (wallJumping ? player.Character.MaxSpeed : velocity.Length()) / jumpSpeed;

						float verticalMultiplier = 1.0f;

						if (main.TotalTime - rollEnded < 0.3f)
							totalMultiplier *= 1.5f;

						float verticalJumpSpeed = player.Character.JumpSpeed * verticalMultiplier;

						// If we're not instantiating a block possibility beneath us or we're not currently falling, incorporate some of our existing vertical velocity in our jump
						if (!blockPossibilityBeneath || currentVerticalSpeed > 0.0f)
							verticalJumpSpeed += currentVerticalSpeed * 0.5f;

						player.Character.LinearVelocity.Value = baseVelocity + new Vector3(jumpDirection.X, verticalJumpSpeed, jumpDirection.Y) * totalMultiplier;

						if (supported && player.Character.SupportEntity.Value != null)
						{
							Vector3 impulsePosition = transform.Position + new Vector3(0, player.Character.Height * -0.5f - player.Character.SupportHeight, 0);
							Vector3 impulse = player.Character.LinearVelocity.Value * player.Character.Body.Mass * -1.0f;
							player.Character.SupportEntity.Value.ApplyImpulse(ref impulsePosition, ref impulse);
						}

						Session.Recorder.Event(main, "Jump");

						player.Character.IsSupported.Value = false;
						player.Character.SupportEntity.Value = null;
						player.Character.HasTraction.Value = false;
					}

					AkSoundEngine.PostEvent(vaultType == VaultType.None ? AK.EVENTS.PLAY_PLAYER_JUMP : AK.EVENTS.PLAY_PLAYER_GRUNT, entity);

					model.Stop
					(
						"Vault",
						"VaultLeft",
						"VaultRight",
						"Jump",
						"JumpLeft",
						"JumpRight",
						"JumpBackward"
					);

					if (vaultType != VaultType.None)
					{
						Session.Recorder.Event(main, "Vault");
						string animation;
						switch (vaultType)
						{
							case VaultType.Left:
								animation = "VaultLeft";
								break;
							case VaultType.Right:
								animation = "VaultRight";
								break;
							default:
								animation = "Vault";
								break;
						}
						model.StartClip(animation, 4, false, 0.1f);
					}
					else
					{
						Vector3 velocity = -Vector3.TransformNormal(player.Character.LinearVelocity, Matrix.CreateRotationY(-rotation));
						velocity.Y = 0.0f;
						if (wallRunState == Player.WallRun.Left || wallRunState == Player.WallRun.Right)
							velocity.Z = 0.0f;
						else if (wallJumping)
							velocity.Z *= 0.5f;
						else
							velocity.X = 0.0f;
						Direction direction = DirectionExtensions.GetDirectionFromVector(velocity);
						string animation;
						switch (direction)
						{
							case Direction.NegativeX:
								animation = "JumpLeft";
								break;
							case Direction.PositiveX:
								animation = "JumpRight";
								break;
							case Direction.PositiveZ:
								animation = wallJumping ? "JumpBackward" : "Jump";
								break;
							default:
								animation = "Jump";
								break;
						}
						model.StartClip(animation, 4, false, 0.1f);
					}

					// Deactivate any wall-running we're doing
					deactivateWallRun();

					// Play a footstep sound since we're jumping off the ground
					AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, entity);

					return true;
				}

				return false;
			};

			Action<Vector3> vaultDown = delegate(Vector3 forward)
			{
				const float vaultVerticalSpeed = -8.0f;
				const float maxVaultTime = 0.5f;

				Vector3 velocity = forward * player.Character.MaxSpeed;
				velocity.Y = player.Character.LinearVelocity.Value.Y;
				player.Character.LinearVelocity.Value = velocity;
				rotationLocked.Value = true;
				player.Character.EnableWalking.Value = false;
				player.Character.Crouched.Value = true;
				player.Character.AllowUncrouch.Value = false;

				float vaultTime = 0.0f;
				if (vaultMover != null)
					vaultMover.Delete.Execute(); // If we're already vaulting, start a new vault

				float walkOffEdgeTimer = 0.0f;
				Vector3 originalPosition = transform.Position;

				vaultMover = new Updater
				{
					delegate(float dt)
					{
						vaultTime += dt;

						bool delete = false;

						if (vaultTime > maxVaultTime) // Max vault time ensures we never get stuck
							delete = true;
						else if (walkOffEdgeTimer > 0.2f && player.Character.IsSupported)
							delete = true; // We went over the edge and hit the ground. Stop.
						else if (!player.Character.IsSupported) // We hit the edge, go down it
						{
							walkOffEdgeTimer += dt;

							if (walkOffEdgeTimer > 0.1f)
							{
								player.Character.LinearVelocity.Value = new Vector3(0, vaultVerticalSpeed, 0);

								if (!input.GetInput(settings.Parkour)
									|| transform.Position.Value.Y < originalPosition.Y - 3.0f
									|| activateWallRun(Player.WallRun.Reverse))
								{
									delete = true;
								}
							}
						}

						if (walkOffEdgeTimer < 0.1f)
						{
							velocity = forward * player.Character.MaxSpeed;
							velocity.Y = player.Character.LinearVelocity.Value.Y;
							player.Character.LinearVelocity.Value = velocity;
						}

						if (delete)
						{
							player.Character.AllowUncrouch.Value = true;
							player.Character.EnableWalking.Value = true;
							if (player.WallRunState.Value == Player.WallRun.None)
								rotationLocked.Value = false;
							vaultMover.Delete.Execute(); // Make sure we get rid of this vault mover
							vaultMover = null;
						}
					}
				};
				entity.RemoveComponent("VaultMover");
				entity.Add("VaultMover", vaultMover);
			};

			Func<bool> tryVaultDown = delegate()
			{
				if (player.Character.Crouched || !player.Character.IsSupported)
					return false;

				Matrix rotationMatrix = Matrix.CreateRotationY(rotation);
				bool foundObstacle = false;
				foreach (Map map in Map.ActivePhysicsMaps)
				{
					Direction down = map.GetRelativeDirection(Direction.NegativeY);
					Vector3 pos = transform.Position + rotationMatrix.Forward * -1.75f;
					Map.Coordinate coord = map.GetCoordinate(pos);

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
					vaultDown(-rotationMatrix.Forward);
				}
				return !foundObstacle;
			};

			// Jumping
			input.Bind(settings.Jump, PCInput.InputState.Down, delegate()
			{
				if (!player.EnableMoves)
					return;

				// Don't allow vaulting
				// Also don't try anything if we're crouched or in the middle of vaulting
				if (vaultMover == null && !jump(false, false) && player.EnableSlowMotion && !player.Character.IsSupported)
				{
					player.SlowMotion.Value = true;
					predictor.PredictPlatforms();
				}
			});

			input.Bind(settings.Jump, PCInput.InputState.Up, delegate()
			{
				player.SlowMotion.Value = false;
			});

			// Wall-run, vault, predictive
			input.Bind(settings.Parkour, PCInput.InputState.Down, delegate()
			{
				if (!player.EnableMoves || (player.Character.Crouched && player.Character.IsSupported) || vaultMover != null)
					return;

				bool vaulted = jump(true, true); // Try vaulting first

				bool wallRan = false;
				if (!vaulted && player.EnableWallRun)
				{
					// Try to wall-run
					if (!(wallRan = activateWallRun(Player.WallRun.Straight)) && player.EnableWallRunHorizontal)
						if (!(wallRan = activateWallRun(Player.WallRun.Left)))
							if (!(wallRan = activateWallRun(Player.WallRun.Right)))
								wallRan = activateWallRun(Player.WallRun.Reverse);
				}

				if (!vaulted)
					vaulted = tryVaultDown();

				if (!vaulted && !wallRan && !player.Character.IsSupported && player.EnableSlowMotion)
				{
					player.SlowMotion.Value = true;
					predictor.PredictWalls();
				}
			});

			input.Bind(settings.Parkour, PCInput.InputState.Up, delegate()
			{
				deactivateWallRun();
				player.SlowMotion.Value = false;
			});

			Action<Map, Map.Coordinate, Direction, Direction> buildFloor = delegate(Map floorMap, Map.Coordinate floorCoordinate, Direction forwardDir, Direction rightDir)
			{
				List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();

				Map.Coordinate newFloorCoordinate = floorMap.GetCoordinate(transform.Position);

				floorCoordinate.SetComponent(rightDir, newFloorCoordinate.GetComponent(rightDir));
				floorCoordinate.SetComponent(forwardDir, newFloorCoordinate.GetComponent(forwardDir));

				Map.CellState fillState = temporary;

				const int radius = 3;
				for (Map.Coordinate x = floorCoordinate.Move(rightDir, -radius); x.GetComponent(rightDir) < floorCoordinate.GetComponent(rightDir) + radius; x = x.Move(rightDir))
				{
					int dx = x.GetComponent(rightDir) - floorCoordinate.GetComponent(rightDir);
					for (Map.Coordinate y = x.Move(forwardDir, -radius); y.GetComponent(forwardDir) < floorCoordinate.GetComponent(forwardDir) + radius; y = y.Move(forwardDir))
					{
						int dy = y.GetComponent(forwardDir) - floorCoordinate.GetComponent(forwardDir);
						if ((float)Math.Sqrt(dx * dx + dy * dy) < radius && floorMap[y].ID == 0)
						{
							buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
							{
								Map = floorMap,
								Coordinate = y,
								State = fillState,
							});
						}
					}
				}
				Factory.Get<EffectBlockFactory>().Build(main, buildCoords, false, transform.Position);
			};

			Updater kickUpdate = null;
			stopKick = delegate()
			{
				if (kickUpdate != null)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_SLIDE_LOOP, entity);
					kickUpdate.Delete.Execute();
					kickUpdate = null;
					model.Stop("Kick", "Slide");
					player.Character.EnableWalking.Value = true;
					if (!input.GetInput(settings.RollKick))
						player.Character.AllowUncrouch.Value = true;
					rotationLocked.Value = false;
				}
			};

			Updater rollUpdate = null;

			bool rolling = false;

			Action rollKick = null;
			rollKick = delegate()
			{
				if (!player.EnableMoves || rolling || kickUpdate != null)
					return;

				Matrix rotationMatrix = Matrix.CreateRotationY(rotation);
				Vector3 forward = -rotationMatrix.Forward;
				Vector3 right = rotationMatrix.Right;

				if (player.EnableCrouch && player.EnableRoll && !player.Character.IsSwimming && (!player.EnableKick || !player.Character.IsSupported || player.Character.LinearVelocity.Value.Length() < 2.0f))
				{
					// Try to roll
					Vector3 playerPos = transform.Position + new Vector3(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0);

					Map.GlobalRaycastResult floorRaycast = Map.GlobalRaycast(playerPos, Vector3.Down, player.Character.Height + 1.0f);

					bool nearGround = (player.Character.IsSupported || player.Character.LinearVelocity.Value.Y <= 0.0f) && floorRaycast.Map != null;

					bool instantiatedBlockPossibility = false;

					Map.Coordinate floorCoordinate = new Map.Coordinate();
					Map floorMap = null;

					if (nearGround)
					{
						floorMap = floorRaycast.Map;
						floorCoordinate = floorRaycast.Coordinate.Value;
					}
					else
					{
						// Check for block possibilities
						foreach (BlockPredictor.Possibility block in predictor.AllPossibilities)
						{
							bool first = true;
							foreach (Map.Coordinate coord in block.Map.Rasterize(playerPos + Vector3.Up * 2.0f, playerPos + (Vector3.Down * (player.Character.Height + 3.0f))))
							{
								if (coord.Between(block.StartCoord, block.EndCoord))
								{
									if (first)
										break; // If the top coord is intersecting the possible block, we're too far down into the block. Need to be at the top.
									predictor.InstantiatePossibility(block);
									instantiatedBlockPossibility = true;
									floorMap = block.Map;
									floorCoordinate = coord;
									nearGround = true;
									break;
								}
								first = false;
							}
							if (nearGround)
								break;
						}
					}

					if (nearGround)
					{
						// We're rolling.
						rolling = true;

						Session.Recorder.Event(main, "Roll");

						deactivateWallRun();

						model.Stop
						(
							"CrouchWalkBackward",
							"CrouchWalk",
							"CrouchStrafeRight",
							"CrouchStrafeLeft",
							"Idle",
							"RunBackward",
							"Run",
							"Sprint",
							"RunRight",
							"RunLeft",
							"Jump",
							"JumpLeft",
							"JumpRight",
							"JumpBackward"
						);

						model.StartClip("CrouchIdle", 2, true, AnimatedModel.DefaultBlendTime);

						player.Character.EnableWalking.Value = false;
						rotationLocked.Value = true;

						footsteps.Footstep.Execute(); // We just landed; play a footstep sound
						AkSoundEngine.PostEvent("Skill_Roll_Play", entity);

						model.StartClip("Roll", 5, false, AnimatedModel.DefaultBlendTime);

						Map.CellState floorState = floorRaycast.Map == null ? Map.EmptyState : floorRaycast.Coordinate.Value.Data;
						bool shouldBuildFloor = false;
						if (player.EnableEnhancedWallRun && (instantiatedBlockPossibility || (floorState.ID != 0 && floorState.ID != Map.t.Temporary && floorState.ID != Map.t.Powered)))
							shouldBuildFloor = true;
						
						// If the player is not yet supported, that means they're just about to land.
						// So give them a little speed boost for having such good timing.
						Vector3 velocity = forward * player.Character.MaxSpeed * (player.Character.IsSupported ? 0.75f : 1.25f);
						player.Character.LinearVelocity.Value = new Vector3(velocity.X, instantiatedBlockPossibility ? 0.0f : player.Character.LinearVelocity.Value.Y, velocity.Z);

						// Crouch
						player.Character.Crouched.Value = true;
						player.Character.AllowUncrouch.Value = false;

						Direction rightDir = floorMap.GetRelativeDirection(right);
						Direction forwardDir = floorMap.GetRelativeDirection(forward);

						float rollTime = 0.0f;
						bool firstTimeBreak = false;
						rollUpdate = new Updater
						{
							delegate(float dt)
							{
								rollTime += dt;

								if (rollTime > 0.1f && (rollTime > 1.0f || Vector3.Dot(player.Character.LinearVelocity, forward) < 0.1f))
								{
									rollUpdate.Delete.Execute();
									rollUpdate = null;
									player.Character.EnableWalking.Value = true;
									if (!input.GetInput(settings.RollKick))
										player.Character.AllowUncrouch.Value = true;
									rotationLocked.Value = false;
									rollEnded = main.TotalTime;
									rolling = false;
								}
								else
								{
									player.Character.LinearVelocity.Value = new Vector3(velocity.X, player.Character.LinearVelocity.Value.Y, velocity.Z);
									if (breakWalls(forward, right, false))
									{
										if (firstTimeBreak)
											AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WALL_BREAK_01, entity);
										firstTimeBreak = false;
									}

									if (shouldBuildFloor)
										buildFloor(floorMap, floorCoordinate, forwardDir, rightDir);
								}
							}
						};
						entity.Add(rollUpdate);
					}
				}

				if (!rolling && !model.IsPlaying("Roll") && player.EnableKick && canKick && kickUpdate == null)
				{
					// Kick
					canKick = false;

					Session.Recorder.Event(main, "Kick");

					deactivateWallRun();

					model.Stop
					(
						"CrouchWalkBackward",
						"CrouchWalk",
						"CrouchStrafeRight",
						"CrouchStrafeLeft",
						"Idle",
						"RunBackward",
						"Run",
						"Sprint",
						"RunRight",
						"RunLeft",
						"Jump",
						"JumpLeft",
						"JumpRight",
						"JumpBackward"
					);
					model.StartClip("CrouchIdle", 2, true, AnimatedModel.DefaultBlendTime);

					player.Character.EnableWalking.Value = false;
					rotationLocked.Value = true;

					player.Character.Crouched.Value = true;
					player.Character.AllowUncrouch.Value = false;

					player.Character.LinearVelocity.Value += forward * Math.Max(4.0f, Vector3.Dot(forward, player.Character.LinearVelocity) * 0.5f) + new Vector3(0, player.Character.JumpSpeed * 0.25f, 0);

					Vector3 kickVelocity = player.Character.LinearVelocity;

					entity.Add(new Animation
					(
						new Animation.Delay(0.25f),
						new Animation.Execute(delegate() { AkSoundEngine.PostEvent("Kick_Play", entity); })
					));

					Vector3 playerPos = transform.Position + new Vector3(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0);

					bool shouldBuildFloor = false, shouldBreakFloor = false;

					Map.GlobalRaycastResult floorRaycast = Map.GlobalRaycast(playerPos, Vector3.Down, player.Character.Height);
					if (floorRaycast.Map == null)
						shouldBreakFloor = true;
					else
					{
						if (player.EnableEnhancedWallRun)
						{
							Map.t floorType = floorRaycast.Coordinate.Value.Data.ID;
							if (floorType != Map.t.Temporary && floorType != Map.t.Powered)
								shouldBuildFloor = true;
						}
					}

					model.StartClip(shouldBreakFloor ? "Kick" : "Slide", 5, false, AnimatedModel.DefaultBlendTime);
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE, entity);
					if (!shouldBreakFloor) // We're sliding on the floor
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE_LOOP, entity);

					Direction forwardDir = Direction.None;
					Direction rightDir = Direction.None;

					if (shouldBuildFloor)
					{
						forwardDir = floorRaycast.Map.GetRelativeDirection(forward);
						rightDir = floorRaycast.Map.GetRelativeDirection(right);
					}

					float kickTime = 0.0f;
					bool firstTimeBreak = true;
					kickUpdate = new Updater
					{
						delegate(float dt)
						{
							kickTime += dt;

							if (shouldBreakFloor && !player.Character.IsSupported) // We weren't supported when we started kicking. We're flying.
							{
								// Roll if we hit the ground while kicking mid-air
								playerPos = transform.Position + new Vector3(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0);
								Map.GlobalRaycastResult r = Map.GlobalRaycast(playerPos, Vector3.Down, player.Character.Height);
								if (r.Map != null)
								{
									stopKick();
									rollKick();
									return;
								}
							}

							if (kickTime > 0.75f || player.Character.LinearVelocity.Value.Length() < 0.1f)
							{
								stopKick();
								return;
							}

							player.Character.LinearVelocity.Value = new Vector3(kickVelocity.X, player.Character.LinearVelocity.Value.Y, kickVelocity.Z);
							if (breakWalls(forward, right, shouldBreakFloor))
							{
								if (firstTimeBreak)
									AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WALL_BREAK_01, entity);
								firstTimeBreak = false;
							}
							if (shouldBuildFloor)
								buildFloor(floorRaycast.Map, floorRaycast.Coordinate.Value, forwardDir, rightDir);
						}
					};
					entity.Add(kickUpdate);
				}
			};

			input.Bind(settings.RollKick, PCInput.InputState.Down, rollKick);

			input.Bind(settings.RollKick, PCInput.InputState.Up, delegate()
			{
				if (!rolling && kickUpdate == null)
					player.Character.AllowUncrouch.Value = true;
			});

			deactivateWallRun = delegate()
			{
				if (player.WallRunState.Value != Player.WallRun.None)
					lastWallRunEnded = main.TotalTime; // Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.

				wallRunMap = null;
				wallDirection = Direction.None;
				wallRunDirection = Direction.None;
				player.WallRunState.Value = Player.WallRun.None;
				model.Stop
				(
					"WallRunLeft",
					"WallRunRight",
					"WallRunStraight",
					"WallSlideDown",
					"WallSlideReverse"
				);
				if (vaultMover == null && kickUpdate == null && rollUpdate == null)
					rotationLocked.Value = false;
			};

			// Camera control
			model.UpdateWorldTransforms();

			CameraController cameraControl = entity.GetOrCreate<CameraController>();
			cameraControl.Add(new Binding<Vector2>(cameraControl.Mouse, input.Mouse));
			cameraControl.Add(new Binding<float>(cameraControl.Lean, x => x * (float)Math.PI * 0.05f, anim.Lean));
			cameraControl.Add(new Binding<Vector3>(cameraControl.LinearVelocity, player.Character.LinearVelocity));
			cameraControl.Add(new Binding<float>(cameraControl.MaxSpeed, player.Character.MaxSpeed));
			cameraControl.Add(new Binding<Matrix>(cameraControl.CameraBone, model.GetBoneTransform("Camera")));
			cameraControl.Add(new Binding<Matrix>(cameraControl.HeadBone, model.GetBoneTransform("ORG-head")));
			cameraControl.Add(new Binding<Matrix>(cameraControl.ModelTransform, model.Transform));
			cameraControl.Add(new Binding<float>(cameraControl.BaseCameraShakeAmount, () => MathHelper.Clamp((player.Character.LinearVelocity.Value.Length() - (player.Character.MaxSpeed * 2.5f)) / (player.Character.MaxSpeed * 4.0f), 0, 1), player.Character.LinearVelocity, player.Character.MaxSpeed));
			cameraControl.Offset = model.GetBoneTransform("Camera").Value.Translation - model.GetBoneTransform("ORG-head").Value.Translation;

#if DEVELOPMENT
			input.Add(new CommandBinding(input.GetKeyDown(Keys.C), delegate() { cameraControl.ThirdPerson.Value = !cameraControl.ThirdPerson; }));

			firstPersonModel.Add(new Binding<bool>(firstPersonModel.Enabled, x => !x, cameraControl.ThirdPerson));

			model.Add(new NotifyBinding(delegate()
			{
				if (cameraControl.ThirdPerson)
				{
					model.UnsupportedTechniques.Remove(Technique.Clip);
					model.UnsupportedTechniques.Remove(Technique.Render);
				}
				else
				{
					model.UnsupportedTechniques.Add(Technique.Clip);
					model.UnsupportedTechniques.Add(Technique.Render);
				}
			}, cameraControl.ThirdPerson));

			ModelAlpha debugCylinder = new ModelAlpha();
			debugCylinder.Filename.Value = "Models\\alpha-cylinder";
			debugCylinder.Add(new Binding<Matrix>(debugCylinder.Transform, transform.Matrix));
			debugCylinder.Serialize = false;
			debugCylinder.Editable = false;
			debugCylinder.Alpha.Value = 0.25f;
			debugCylinder.Add(new Binding<bool>(debugCylinder.Enabled, cameraControl.ThirdPerson));
			debugCylinder.Add(new Binding<Vector3>(debugCylinder.Scale, delegate()
			{
				return new Vector3(player.Character.Radius * 2.0f, player.Character.Height, player.Character.Radius * 2.0f);
			}, player.Character.Height, player.Character.Radius));
			entity.Add(debugCylinder);

			input.Add(new CommandBinding(input.GetKeyUp(Keys.T), delegate()
			{
				if (main.TimeMultiplier < 1.0f)
					main.TimeMultiplier.Value = 1.0f;
				else
					main.TimeMultiplier.Value = 0.25f;
			}));
#endif


			// Player data bindings

			entity.Add(new PostInitialization
			{
				delegate()
				{
					Entity dataEntity = Factory.Get<PlayerDataFactory>().Instance;

					// HACK. Overwriting the property rather than binding the two together. Oh well.
					footsteps.RespawnLocations = dataEntity.GetOrMakeListProperty<RespawnLocation>("RespawnLocations");
					
					// Bind player data properties
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableRoll"), player.EnableRoll));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableCrouch"), player.EnableCrouch));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableKick"), player.EnableKick));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableWallRun"), player.EnableWallRun));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableWallRunHorizontal"), player.EnableWallRunHorizontal));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableEnhancedWallRun"), player.EnableEnhancedWallRun));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableSlowMotion"), player.EnableSlowMotion));
					entity.Add(new TwoWayBinding<bool>(dataEntity.GetProperty<bool>("EnableMoves"), player.EnableMoves));
					entity.Add(new TwoWayBinding<float>(dataEntity.GetProperty<float>("MaxSpeed"), player.Character.MaxSpeed));

					Phone phone = dataEntity.GetOrCreate<Phone>("Phone");

					phone.Add
					(
						new Binding<bool>
						(
							phone.CanReceiveMessages,
							() => player.Character.IsSupported && !player.Character.IsSwimming && !player.Character.Crouched,
							player.Character.IsSupported,
							player.Character.IsSwimming,
							player.Character.Crouched
						)
					);

					PhoneNote.Attach(main, entity, model, input, phone, player.Character.EnableWalking, player.EnableMoves);
				}
			});
		}
	}
}
