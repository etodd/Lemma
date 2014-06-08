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
			Main.Config settings = main.Settings;
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			entity.CannotSuspend = true;

			PlayerFactory.Instance = entity;

			this.SetMain(entity, main);

			FPSInput input = new FPSInput();
			input.EnabledWhenPaused = false;
			entity.Add("Input", input);

			AnimatedModel firstPersonModel = entity.GetOrCreate<AnimatedModel>("FirstPersonModel");
			firstPersonModel.Serialize = false;
			AnimatedModel model = entity.GetOrCreate<AnimatedModel>("Model");
			model.Serialize = false;

			model.Editable = false;
			model.Filename.Value = "Models\\joan";
			model.CullBoundingBox.Value = false;

			firstPersonModel.Editable = false;
			firstPersonModel.Filename.Value = "Models\\joan-firstperson";
			firstPersonModel.CullBoundingBox.Value = false;

			AnimationController anim = entity.GetOrCreate<AnimationController>("AnimationController");
			Player player = entity.GetOrCreate<Player>("Player");
			RotationController rotation = entity.GetOrCreate<RotationController>("Rotation");
			BlockPredictor predictor = entity.GetOrCreate<BlockPredictor>("BlockPredictor");
			Jump jump = entity.GetOrCreate<Jump>("Jump");
			RollKickSlide rollKickSlide = entity.GetOrCreate<RollKickSlide>("RollKickSlide");
			Vault vault = entity.GetOrCreate<Vault>("Vault");
			WallRun wallRun = entity.GetOrCreate<WallRun>("WallRun");
			VoxelTools voxelTools = entity.GetOrCreate<VoxelTools>("VoxelTools");
			Footsteps footsteps = entity.GetOrCreate<Footsteps>("Footsteps");
			FallDamage fallDamage = entity.GetOrCreate<FallDamage>("FallDamage");
			CameraController cameraControl = entity.GetOrCreate<CameraController>("CameraControl");
			Rumble rumble = entity.GetOrCreate<Rumble>("Rumble");

			Property<Vector3> floor = new Property<Vector3>();
			transform.Add(new Binding<Vector3>(floor, () => transform.Position + new Vector3(0, player.Character.Height * -0.5f, 0), transform.Position, player.Character.Height));
			AkGameObjectTracker.Attach(entity, floor);

			predictor.Add(new Binding<Vector3>(predictor.FootPosition, floor));
			predictor.Add(new Binding<Vector3>(predictor.LinearVelocity, player.Character.LinearVelocity));
			predictor.Add(new Binding<float>(predictor.Rotation, rotation.Rotation));
			predictor.Add(new Binding<float>(predictor.MaxSpeed, player.Character.MaxSpeed));
			predictor.Add(new Binding<float>(predictor.JumpSpeed, player.Character.JumpSpeed));
			predictor.Add(new Binding<bool>(predictor.IsSupported, player.Character.IsSupported));

			jump.Add(new TwoWayBinding<bool>(player.Character.IsSupported, jump.IsSupported));
			jump.Add(new TwoWayBinding<bool>(player.Character.HasTraction, jump.HasTraction));
			jump.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, jump.LinearVelocity));
			jump.Add(new TwoWayBinding<BEPUphysics.Entities.Entity>(jump.SupportEntity, player.Character.SupportEntity));
			jump.Add(new Binding<Vector2>(jump.AbsoluteMovementDirection, player.Character.MovementDirection));
			jump.Add(new Binding<WallRun.State>(jump.WallRunState, wallRun.CurrentState));
			jump.Add(new Binding<float>(jump.Rotation, rotation.Rotation));
			jump.Add(new Binding<Vector3>(jump.Position, transform.Position));
			jump.Add(new Binding<Vector3>(jump.FloorPosition, floor));
			jump.Add(new Binding<float>(jump.MaxSpeed, player.Character.MaxSpeed));
			jump.Add(new Binding<float>(jump.JumpSpeed, player.Character.JumpSpeed));
			jump.Add(new Binding<float>(jump.Mass, player.Character.Mass));
			jump.Add(new Binding<float>(jump.LastRollEnded, rollKickSlide.LastRollEnded));
			jump.Add(new Binding<Voxel>(jump.WallRunMap, wallRun.WallRunMap));
			jump.Add(new Binding<Direction>(jump.WallDirection, wallRun.WallDirection));
			jump.Add(new CommandBinding<Voxel, Voxel.Coord, Direction>(jump.WalkedOn, footsteps.WalkedOn));
			jump.Add(new CommandBinding(jump.DeactivateWallRun, (Action)wallRun.Deactivate));
			jump.Add(new CommandBinding<float>(jump.FallDamage, fallDamage.Apply));
			jump.Predictor = predictor;
			jump.Model = model;
			jump.Add(new TwoWayBinding<Voxel>(wallRun.LastWallRunMap, jump.LastWallRunMap));
			jump.Add(new TwoWayBinding<Direction>(wallRun.LastWallDirection, jump.LastWallDirection));
			jump.Add(new TwoWayBinding<bool>(rollKickSlide.CanKick, jump.CanKick));
			jump.Add(new TwoWayBinding<float>(player.Character.LastSupportedSpeed, jump.LastSupportedSpeed));

			wallRun.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, wallRun.LinearVelocity));
			wallRun.Add(new TwoWayBinding<Vector3>(transform.Position, wallRun.Position));
			wallRun.Add(new TwoWayBinding<bool>(player.Character.IsSupported, wallRun.IsSupported));
			wallRun.Add(new CommandBinding(wallRun.LockRotation, (Action)rotation.Lock));
			vault.Add(new CommandBinding(wallRun.Vault, delegate() { vault.Go(); }));
			wallRun.Predictor = predictor;
			wallRun.Add(new Binding<float>(wallRun.Height, player.Character.Height));
			wallRun.Add(new Binding<float>(wallRun.JumpSpeed, player.Character.JumpSpeed));
			wallRun.Add(new Binding<float>(wallRun.MaxSpeed, player.Character.MaxSpeed));
			wallRun.Add(new TwoWayBinding<float>(rotation.Rotation, wallRun.Rotation));
			wallRun.Add(new TwoWayBinding<bool>(player.Character.AllowUncrouch, wallRun.AllowUncrouch));
			wallRun.Add(new TwoWayBinding<bool>(player.Character.HasTraction, wallRun.HasTraction));
			wallRun.Add(new Binding<float>(wallRun.LastWallJump, jump.LastWallJump));
			player.Add(new Binding<WallRun.State>(player.Character.WallRunState, wallRun.CurrentState));

			input.Bind(rollKickSlide.RollKickButton, settings.RollKick);
			rollKickSlide.Add(new Binding<bool>(rollKickSlide.EnableCrouch, player.EnableCrouch));
			rollKickSlide.Add(new Binding<float>(rollKickSlide.Rotation, rotation.Rotation));
			rollKickSlide.Add(new Binding<bool>(rollKickSlide.IsSwimming, player.Character.IsSwimming));
			rollKickSlide.Add(new Binding<bool>(rollKickSlide.IsSupported, player.Character.IsSupported));
			rollKickSlide.Add(new Binding<Vector3>(rollKickSlide.FloorPosition, floor));
			rollKickSlide.Add(new Binding<float>(rollKickSlide.Height, player.Character.Height));
			rollKickSlide.Add(new Binding<float>(rollKickSlide.MaxSpeed, player.Character.MaxSpeed));
			rollKickSlide.Add(new Binding<float>(rollKickSlide.JumpSpeed, player.Character.JumpSpeed));
			rollKickSlide.Add(new Binding<Vector3>(rollKickSlide.SupportVelocity, player.Character.SupportVelocity));
			rollKickSlide.Add(new TwoWayBinding<bool>(wallRun.EnableEnhancedWallRun, rollKickSlide.EnableEnhancedRollSlide));
			rollKickSlide.Add(new TwoWayBinding<bool>(player.Character.AllowUncrouch, rollKickSlide.AllowUncrouch));
			rollKickSlide.Add(new TwoWayBinding<bool>(player.Character.Crouched, rollKickSlide.Crouched));
			rollKickSlide.Add(new TwoWayBinding<bool>(player.Character.EnableWalking, rollKickSlide.EnableWalking));
			rollKickSlide.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, rollKickSlide.LinearVelocity));
			rollKickSlide.Add(new TwoWayBinding<Vector3>(transform.Position, rollKickSlide.Position));
			rollKickSlide.Predictor = predictor;
			rollKickSlide.Model = model;
			rollKickSlide.VoxelTools = voxelTools;
			rollKickSlide.Add(new CommandBinding(rollKickSlide.DeactivateWallRun, (Action)wallRun.Deactivate));
			rollKickSlide.Add(new CommandBinding(rollKickSlide.Footstep, footsteps.Footstep));
			rollKickSlide.Add(new CommandBinding(rollKickSlide.LockRotation, (Action)rotation.Lock));
			SoundKiller.Add(entity, AK.EVENTS.STOP_PLAYER_SLIDE_LOOP);

			vault.Add(new Binding<Vector3>(vault.Position, transform.Position));
			vault.Add(new Binding<Vector3>(vault.FloorPosition, floor));
			vault.Add(new Binding<float>(vault.MaxSpeed, player.Character.MaxSpeed));
			vault.Add(new Binding<WallRun.State>(vault.WallRunState, wallRun.CurrentState));
			vault.Add(new CommandBinding(vault.LockRotation, (Action)rotation.Lock));
			vault.Add(new CommandBinding(vault.DeactivateWallRun, (Action)wallRun.Deactivate));
			vault.Add(new CommandBinding<WallRun.State>(vault.ActivateWallRun, delegate(WallRun.State state) { wallRun.Activate(state); }));
			vault.Add(new TwoWayBinding<float>(player.Character.LastSupportedSpeed, vault.LastSupportedSpeed));
			vault.Add(new CommandBinding<float>(vault.FallDamage, fallDamage.Apply));
			vault.Bind(model);
			vault.Predictor = predictor;
			vault.Add(new TwoWayBinding<float>(rotation.Rotation, vault.Rotation));
			vault.Add(new TwoWayBinding<bool>(player.Character.IsSupported, vault.IsSupported));
			vault.Add(new TwoWayBinding<bool>(player.Character.HasTraction, vault.HasTraction));
			vault.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, vault.LinearVelocity));
			vault.Add(new TwoWayBinding<bool>(player.Character.EnableWalking, vault.EnableWalking));
			vault.Add(new TwoWayBinding<bool>(player.Character.AllowUncrouch, vault.AllowUncrouch));
			vault.Add(new TwoWayBinding<bool>(player.Character.Crouched, vault.Crouched));

			rotation.Add(new TwoWayBinding<Vector2>(rotation.Mouse, input.Mouse));
			rotation.Add(new Binding<bool>(rotation.Rolling, rollKickSlide.Rolling));
			rotation.Add(new Binding<bool>(rotation.Kicking, rollKickSlide.Kicking));
			rotation.Add(new Binding<Vault.State>(rotation.VaultState, vault.CurrentState));
			rotation.Add(new Binding<WallRun.State>(rotation.WallRunState, wallRun.CurrentState));

			voxelTools.Add(new Binding<float>(voxelTools.Height, player.Character.Height));
			voxelTools.Add(new Binding<float>(voxelTools.SupportHeight, player.Character.SupportHeight));
			voxelTools.Add(new Binding<Vector3>(voxelTools.Position, transform.Position));

			anim.Add(new Binding<bool>(anim.IsSupported, player.Character.IsSupported));
			anim.Add(new Binding<WallRun.State>(anim.WallRunState, wallRun.CurrentState));
			anim.Add(new Binding<bool>(anim.EnableWalking, player.Character.EnableWalking));
			anim.Add(new Binding<bool>(anim.Crouched, player.Character.Crouched));
			anim.Add(new Binding<Vector3>(anim.LinearVelocity, player.Character.LinearVelocity));
			anim.Add(new Binding<Vector2>(anim.Movement, input.Movement));
			anim.Add(new Binding<Vector2>(anim.Mouse, input.Mouse));
			anim.Add(new Binding<Voxel>(anim.WallRunMap, wallRun.WallRunMap));
			anim.Add(new Binding<Direction>(anim.WallDirection, wallRun.WallDirection));
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

			// Camera control
			model.UpdateWorldTransforms();

			cameraControl.Add(new Binding<Vector2>(cameraControl.Mouse, input.Mouse));
			cameraControl.Add(new Binding<float>(cameraControl.Lean, x => x * (float)Math.PI * 0.05f, anim.Lean));
			cameraControl.Add(new Binding<Vector3>(cameraControl.LinearVelocity, player.Character.LinearVelocity));
			cameraControl.Add(new Binding<float>(cameraControl.MaxSpeed, player.Character.MaxSpeed));
			cameraControl.Add(new Binding<Matrix>(cameraControl.CameraBone, model.GetBoneTransform("Camera")));
			cameraControl.Add(new Binding<Matrix>(cameraControl.HeadBone, model.GetBoneTransform("ORG-head")));
			cameraControl.Add(new Binding<Matrix>(cameraControl.ModelTransform, model.Transform));
			cameraControl.Add(new Binding<float>(cameraControl.BaseCameraShakeAmount, () => MathHelper.Clamp((player.Character.LinearVelocity.Value.Length() - (player.Character.MaxSpeed * 2.5f)) / (player.Character.MaxSpeed * 4.0f), 0, 1), player.Character.LinearVelocity, player.Character.MaxSpeed));
			cameraControl.Offset = model.GetBoneTransform("Camera").Value.Translation - model.GetBoneTransform("ORG-head").Value.Translation;

			rumble.Add(new Binding<float>(rumble.CameraShake, cameraControl.TotalCameraShake));
			rumble.Add(new CommandBinding<float>(fallDamage.Rumble, rumble.Go));
			rumble.Add(new CommandBinding<float>(player.Rumble, rumble.Go));
			rumble.Add(new CommandBinding<float>(rollKickSlide.Rumble, rumble.Go));

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

#if DEVELOPMENT
			input.Add(new CommandBinding(input.GetKeyDown(Keys.C), delegate() { cameraControl.ThirdPerson.Value = !cameraControl.ThirdPerson; }));

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
#else
			// Disable the player model in third person in Release mode
			model.Add(new Binding<bool>(model.Enabled, x => !x, cameraControl.ThirdPerson));
#endif

			// When rotation is locked, we want to make sure the player can't turn their head
			// 180 degrees from the direction they're facing

			input.Add(new Binding<float>(input.MaxY, () => rotation.Locked ? (float)Math.PI * 0.3f : (float)Math.PI * 0.4f, rotation.Locked));
			input.Add(new Binding<float>(input.MinX, () => rotation.Locked ? rotation.Rotation + ((float)Math.PI * -0.4f) : 0.0f, rotation.Rotation, rotation.Locked));
			input.Add(new Binding<float>(input.MaxX, () => rotation.Locked ? rotation.Rotation + ((float)Math.PI * 0.4f) : 0.0f, rotation.Rotation, rotation.Locked));
			input.Add(new NotifyBinding(delegate() { input.Mouse.Changed(); }, rotation.Locked)); // Make sure the rotation locking takes effect even if the player doesn't move the mouse

			// Setup rendering properties

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

			input.Add(new Binding<float>(input.MouseSensitivity, settings.MouseSensitivity));
			input.Add(new Binding<bool>(input.InvertMouseX, settings.InvertMouseX));
			input.Add(new Binding<bool>(input.InvertMouseY, settings.InvertMouseY));
			input.Add(new Binding<PCInput.PCInputBinding>(input.LeftKey, settings.Left));
			input.Add(new Binding<PCInput.PCInputBinding>(input.RightKey, settings.Right));
			input.Add(new Binding<PCInput.PCInputBinding>(input.BackwardKey, settings.Backward));
			input.Add(new Binding<PCInput.PCInputBinding>(input.ForwardKey, settings.Forward));

			model.StartClip("Idle", 0, true, AnimatedModel.DefaultBlendTime);

			// Set up AI agent
			Agent agent = entity.GetOrCreate<Agent>();
			agent.Add(new TwoWayBinding<float>(player.Health, agent.Health));
			agent.Add(new Binding<Vector3>(agent.Position, () => transform.Position.Value + new Vector3(0, player.Character.Height * -0.5f, 0), transform.Position, player.Character.Height));
			agent.Add(new CommandBinding(agent.Die, entity.Delete));
			agent.Add(new Binding<bool>(agent.Loud, x => !x, player.Character.Crouched));

			entity.Add(new CommandBinding(player.HealthDepleted, delegate()
			{
				Session.Recorder.Event(main, "DieFromHealth");
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_DEATH, entity);
				main.Spawner.RespawnDistance = Spawner.KilledRespawnDistance;
				main.Spawner.RespawnInterval = Spawner.KilledRespawnInterval;
			}));

			entity.Add(new CommandBinding(player.HealthDepleted, entity.Delete));

			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				Session.Recorder.Event(main, "Die");
				if (Agent.Query(transform.Position, 0.0f, 10.0f, x => x != agent) != null)
				{
					main.Spawner.RespawnDistance = Spawner.KilledRespawnDistance;
					main.Spawner.RespawnInterval = Spawner.KilledRespawnInterval;
				}
			}));

			player.EnabledInEditMode = false;

			input.MaxY.Value = (float)Math.PI * 0.35f;

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

			player.Add(new TwoWayBinding<Matrix>(transform.Matrix, player.Character.Transform));

			model.Add(new Binding<Matrix>(model.Transform, delegate()
			{
				const float leanAmount = (float)Math.PI * 0.1f;
				return Matrix.CreateTranslation(0, (player.Character.Height * -0.5f) - player.Character.SupportHeight, 0) * Matrix.CreateRotationZ(anim.Lean * leanAmount) * Matrix.CreateRotationY(rotation.Rotation) * transform.Matrix;
			}, transform.Matrix, rotation.Rotation, player.Character.Height, player.Character.SupportHeight, anim.Lean));

			firstPersonModel.Add(new Binding<Matrix>(firstPersonModel.Transform, model.Transform));
			firstPersonModel.Add(new Binding<Vector3>(firstPersonModel.Scale, model.Scale));

			WallRun.State[] footstepWallrunStates = new[]
			{
				WallRun.State.Left,
				WallRun.State.Right,
				WallRun.State.Straight,
				WallRun.State.None,
			};
			footsteps.Add(new Binding<bool>(footsteps.SoundEnabled, () => footstepWallrunStates.Contains(wallRun.CurrentState) || (player.Character.IsSupported && player.Character.EnableWalking), player.Character.IsSupported, player.Character.EnableWalking, wallRun.CurrentState));
			footsteps.Add(new Binding<Vector3>(footsteps.Position, transform.Position));
			footsteps.Add(new Binding<float>(footsteps.Rotation, rotation.Rotation));
			footsteps.Add(new Binding<float>(footsteps.CharacterHeight, player.Character.Height));
			footsteps.Add(new Binding<float>(footsteps.SupportHeight, player.Character.SupportHeight));
			footsteps.Add(new TwoWayBinding<float>(player.Health, footsteps.Health));
			footsteps.Add(new CommandBinding<Voxel, Voxel.Coord, Direction>(wallRun.WalkedOn, footsteps.WalkedOn));
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

				Matrix matrix = Matrix.CreateRotationY(rotation.Rotation);

				Vector2 forwardDir = new Vector2(matrix.Forward.X, matrix.Forward.Z);
				Vector2 rightDir = new Vector2(matrix.Right.X, matrix.Right.Z);
				return -(forwardDir * movement.Y) - (rightDir * movement.X);
			}, input.Movement, rotation.Rotation));

			player.Character.Crouched.Value = true;
			player.Character.AllowUncrouch.Value = true;

			// Fall damage
			fallDamage.Add(new Binding<bool>(fallDamage.IsSupported, player.Character.IsSupported));
			fallDamage.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, fallDamage.LinearVelocity));
			fallDamage.Add(new TwoWayBinding<float>(player.Health, fallDamage.Health));
			fallDamage.Add(new CommandBinding<BEPUphysics.BroadPhaseEntries.Collidable, ContactCollection>(player.Character.Collided, fallDamage.Collided));
			fallDamage.Add(new TwoWayBinding<bool>(player.Character.EnableWalking, fallDamage.EnableWalking));
			fallDamage.Bind(model);

			// Jumping
			input.Bind(settings.Jump, PCInput.InputState.Down, delegate()
			{
				if (!player.EnableMoves)
					return;

				if (vault.CurrentState.Value == Vault.State.None)
					jump.Go();
			});

			// Wall-run, vault, predictive
			input.Bind(settings.Parkour, PCInput.InputState.Down, delegate()
			{
				if (!player.EnableMoves || !player.Character.EnableWalking || (player.Character.Crouched && player.Character.IsSupported) || vault.CurrentState.Value != Vault.State.None)
					return;

				bool didSomething = false;

				if (predictor.PossibilityCount > 0)
				{
					// In slow motion, prefer left and right wall-running
					if (!(didSomething = wallRun.Activate(WallRun.State.Left)))
						if (!(didSomething = wallRun.Activate(WallRun.State.Right)))
							if (!(didSomething = vault.Go()))
								if (!(didSomething = wallRun.Activate(WallRun.State.Straight)))
									didSomething = wallRun.Activate(WallRun.State.Reverse);
				}
				else
				{
					// In normal mode, prefer straight wall-running
					if (!(didSomething = vault.Go()))
						if (!(didSomething = wallRun.Activate(WallRun.State.Straight)))
							if (!(didSomething = wallRun.Activate(WallRun.State.Left)))
								if (!(didSomething = wallRun.Activate(WallRun.State.Right)))
									didSomething = wallRun.Activate(WallRun.State.Reverse);
				}

				if (!didSomething)
					didSomething = vault.TryVaultDown();

				if (!didSomething && player.EnableSlowMotion)
				{
					player.SlowMotion.Value = true;
					predictor.ClearPossibilities();
					predictor.PredictPlatforms();
					predictor.PredictWalls();
				}
			});

			input.Bind(settings.Parkour, PCInput.InputState.Up, delegate()
			{
				wallRun.Deactivate();
				if (player.EnableSlowMotion)
					player.SlowMotion.Value = false;
			});

			input.Bind(settings.RollKick, PCInput.InputState.Down, rollKickSlide.Go);

			input.Bind(settings.RollKick, PCInput.InputState.Up, delegate()
			{
				if (!rollKickSlide.Rolling && !rollKickSlide.Kicking)
					player.Character.AllowUncrouch.Value = true;
			});

			// Player data bindings

			entity.Add(new PostInitialization
			{
				delegate()
				{
					Entity dataEntity = PlayerDataFactory.Instance;
					PlayerData playerData = dataEntity.Get<PlayerData>();

					// HACK. Overwriting the property rather than binding the two together. Oh well.
					// This is because I haven't written a two-way list binding.
					footsteps.RespawnLocations = playerData.RespawnLocations;
					
					// Bind player data properties
					entity.Add(new TwoWayBinding<float>(playerData.CameraShakeAmount, cameraControl.CameraShakeAmount));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableRoll, rollKickSlide.EnableRoll));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableCrouch, player.EnableCrouch));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableKick, rollKickSlide.EnableKick));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableWallRun, wallRun.EnableWallRun));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableWallRunHorizontal, wallRun.EnableWallRunHorizontal));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableEnhancedWallRun, wallRun.EnableEnhancedWallRun));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableSlowMotion, player.EnableSlowMotion));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableMoves, player.EnableMoves));
					entity.Add(new TwoWayBinding<float>(playerData.MaxSpeed, player.Character.MaxSpeed));

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

					Property<bool> phoneActive = dataEntity.GetOrMakeProperty<bool>("PhoneActive");
					Property<bool> noteActive = dataEntity.GetOrMakeProperty<bool>("NoteActive");
					PhoneNote.Attach(main, entity, model, input, phone, player.Character.EnableWalking, player.EnableMoves, phoneActive, noteActive);
				}
			});
		}
	}
}
