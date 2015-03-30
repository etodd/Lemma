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
using Lemma.Console;

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

			Updater parkour = entity.Create<Updater>();
			Updater jumper = entity.Create<Updater>();

			Player player = entity.GetOrCreate<Player>("Player");

			AnimatedModel firstPersonModel = entity.GetOrCreate<AnimatedModel>("FirstPersonModel");
			firstPersonModel.MapContent = false;
			firstPersonModel.Serialize = false;
			firstPersonModel.Filename.Value = "Models\\joan-firstperson";
			firstPersonModel.CullBoundingBox.Value = false;

			AnimatedModel model = entity.GetOrCreate<AnimatedModel>("Model");
			model.MapContent = false;
			model.Serialize = false;
			model.Filename.Value = "Models\\joan";
			model.CullBoundingBox.Value = false;

			AnimationController anim = entity.GetOrCreate<AnimationController>("AnimationController");
			RotationController rotation = entity.GetOrCreate<RotationController>("Rotation");
			BlockPredictor predictor = entity.GetOrCreate<BlockPredictor>("BlockPredictor");
			Jump jump = entity.GetOrCreate<Jump>("Jump");
			RollKickSlide rollKickSlide = entity.GetOrCreate<RollKickSlide>("RollKickSlide");
			Vault vault = entity.GetOrCreate<Vault>("Vault");
			WallRun wallRun = entity.GetOrCreate<WallRun>("WallRun");
			VoxelTools voxelTools = entity.GetOrCreate<VoxelTools>("VoxelTools");
			Footsteps footsteps = entity.GetOrCreate<Footsteps>("Footsteps");
			FallDamage fallDamage = entity.GetOrCreate<FallDamage>("FallDamage");
			FPSCamera fpsCamera = entity.GetOrCreate<FPSCamera>("FPSCamera");
			fpsCamera.Enabled.Value = false;
			Rumble rumble = entity.GetOrCreate<Rumble>("Rumble");
			CameraController cameraControl = entity.GetOrCreate<CameraController>("CameraControl");

			Property<Vector3> floor = new Property<Vector3>();
			transform.Add(new Binding<Vector3>(floor, () => transform.Position + new Vector3(0, player.Character.Height * -0.5f, 0), transform.Position, player.Character.Height));
			Sound.AttachTracker(entity, floor);

			predictor.Add(new Binding<Vector3>(predictor.FootPosition, floor));
			predictor.Add(new Binding<Vector3>(predictor.LinearVelocity, player.Character.LinearVelocity));
			predictor.Add(new Binding<float>(predictor.Rotation, rotation.Rotation));
			predictor.Add(new Binding<float>(predictor.MaxSpeed, player.Character.MaxSpeed));
			predictor.Add(new Binding<float>(predictor.JumpSpeed, player.Character.JumpSpeed));
			predictor.Add(new Binding<bool>(predictor.IsSupported, player.Character.IsSupported));

			jump.Add(new Binding<bool>(jump.Crouched, player.Character.Crouched));
			jump.Add(new TwoWayBinding<bool>(player.Character.IsSupported, jump.IsSupported));
			jump.Add(new TwoWayBinding<bool>(player.Character.HasTraction, jump.HasTraction));
			jump.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, jump.LinearVelocity));
			jump.Add(new TwoWayBinding<BEPUphysics.Entities.Entity>(jump.SupportEntity, player.Character.SupportEntity));
			jump.Add(new TwoWayBinding<Vector3>(jump.SupportVelocity, player.Character.SupportVelocity));
			jump.Add(new Binding<Vector2>(jump.AbsoluteMovementDirection, player.Character.MovementDirection));
			jump.Add(new Binding<WallRun.State>(jump.WallRunState, wallRun.CurrentState));
			jump.Add(new Binding<float>(jump.Rotation, rotation.Rotation));
			jump.Add(new Binding<Vector3>(jump.Position, transform.Position));
			jump.Add(new Binding<Vector3>(jump.FloorPosition, floor));
			jump.Add(new Binding<float>(jump.MaxSpeed, player.Character.MaxSpeed));
			jump.Add(new Binding<float>(jump.JumpSpeed, player.Character.JumpSpeed));
			jump.Add(new Binding<float>(jump.Mass, player.Character.Mass));
			jump.Add(new Binding<float>(jump.LastRollKickEnded, rollKickSlide.LastRollKickEnded));
			jump.Add(new Binding<Voxel>(jump.WallRunMap, wallRun.WallRunVoxel));
			jump.Add(new Binding<Direction>(jump.WallDirection, wallRun.WallDirection));
			jump.Add(new CommandBinding<Voxel, Voxel.Coord, Direction>(jump.WalkedOn, footsteps.WalkedOn));
			jump.Add(new CommandBinding(jump.DeactivateWallRun, (Action)wallRun.Deactivate));
			jump.Add(new CommandBinding<float>(jump.FallDamage, fallDamage.ApplyJump));
			jump.Predictor = predictor;
			jump.Bind(model);
			jump.Add(new TwoWayBinding<Voxel>(wallRun.LastWallRunMap, jump.LastWallRunMap));
			jump.Add(new TwoWayBinding<Direction>(wallRun.LastWallDirection, jump.LastWallDirection));
			jump.Add(new TwoWayBinding<bool>(rollKickSlide.CanKick, jump.CanKick));
			jump.Add(new TwoWayBinding<float>(player.Character.LastSupportedSpeed, jump.LastSupportedSpeed));

			wallRun.Add(new Binding<bool>(wallRun.IsSwimming, player.Character.IsSwimming));
			wallRun.Add(new TwoWayBinding<Vector3>(player.Character.LinearVelocity, wallRun.LinearVelocity));
			wallRun.Add(new TwoWayBinding<Vector3>(transform.Position, wallRun.Position));
			wallRun.Add(new TwoWayBinding<bool>(player.Character.IsSupported, wallRun.IsSupported));
			wallRun.Add(new CommandBinding(wallRun.LockRotation, (Action)rotation.Lock));
			wallRun.Add(new CommandBinding<float>(wallRun.UpdateLockedRotation, rotation.UpdateLockedRotation));
			vault.Add(new CommandBinding(wallRun.Vault, delegate() { vault.Go(true); }));
			wallRun.Predictor = predictor;
			wallRun.Add(new Binding<float>(wallRun.Height, player.Character.Height));
			wallRun.Add(new Binding<float>(wallRun.JumpSpeed, player.Character.JumpSpeed));
			wallRun.Add(new Binding<float>(wallRun.MaxSpeed, player.Character.MaxSpeed));
			wallRun.Add(new TwoWayBinding<float>(rotation.Rotation, wallRun.Rotation));
			wallRun.Add(new TwoWayBinding<bool>(player.Character.AllowUncrouch, wallRun.AllowUncrouch));
			wallRun.Add(new TwoWayBinding<bool>(player.Character.HasTraction, wallRun.HasTraction));
			wallRun.Add(new Binding<float>(wallRun.LastWallJump, jump.LastWallJump));
			wallRun.Add(new Binding<float>(player.Character.LastSupportedSpeed, wallRun.LastSupportedSpeed));
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
			rollKickSlide.Bind(model);
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
			vault.Add(new Binding<float>(vault.Radius, player.Character.Radius));

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
			anim.Add(new Binding<float>(anim.Rotation, rotation.Rotation));
			anim.Add(new Binding<Voxel>(anim.WallRunMap, wallRun.WallRunVoxel));
			anim.Add(new Binding<Direction>(anim.WallDirection, wallRun.WallDirection));
			anim.Add(new Binding<bool>(anim.IsSwimming, player.Character.IsSwimming));
			anim.Add(new Binding<bool>(anim.Kicking, rollKickSlide.Kicking));
			anim.Add(new Binding<Vector3>(anim.SupportVelocity, player.Character.SupportVelocity));
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

			float heightOffset = 0.1f;
#if VR
			if (main.VR)
				heightOffset = 0.4f;
#endif
			cameraControl.Offset += new Vector3(0, heightOffset, 0);

			rumble.Add(new Binding<float>(rumble.CameraShake, cameraControl.TotalCameraShake));
			rumble.Add(new CommandBinding<float>(fallDamage.Rumble, rumble.Go));
			rumble.Add(new CommandBinding<float>(player.Rumble, rumble.Go));
			rumble.Add(new CommandBinding<float>(rollKickSlide.Rumble, rumble.Go));

			firstPersonModel.Add(new Binding<bool>(firstPersonModel.Enabled, x => !x, cameraControl.ThirdPerson));

			model.Add(new ChangeBinding<bool>(cameraControl.ThirdPerson, delegate(bool old, bool value)
			{
				if (value && !old)
				{
					model.UnsupportedTechniques.Remove(Technique.Clip);
					model.UnsupportedTechniques.Remove(Technique.Render);
				}
				else if (old && !value)
				{
					model.UnsupportedTechniques.Add(Technique.Clip);
					model.UnsupportedTechniques.Add(Technique.Render);
				}
			}));

			Lemma.Console.Console.AddConCommand(new Console.ConCommand("third_person", "Toggle third-person view (WARNING: EXPERIMENTAL)", delegate(Console.ConCommand.ArgCollection args)
			{
				cameraControl.ThirdPerson.Value = !cameraControl.ThirdPerson;
			}));
			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				Lemma.Console.Console.RemoveConCommand("third_person");
			}));

			// When rotation is locked, we want to make sure the player can't turn their head
			// 180 degrees from the direction they're facing

#if VR
			if (main.VR)
				input.MaxY.Value = input.MinY.Value = 0;
			else
#endif
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
			agent.Add(new Binding<bool>(agent.Loud, () => player.Character.MovementDirection.Value.LengthSquared() > 0 && !player.Character.Crouched, player.Character.Crouched));

			// Blocks
			BlockCloud blockCloud = entity.GetOrCreate<BlockCloud>("BlockCloud");
			blockCloud.Scale.Value = 0.5f;
			blockCloud.Add(new Binding<Vector3>(blockCloud.Position, () => transform.Position.Value + new Vector3(0, player.Character.Height * 0.5f + player.Character.LinearVelocity.Value.Y, 0), transform.Position, player.Character.Height, player.Character.LinearVelocity));
			blockCloud.Blocks.ItemAdded += delegate(int index, Entity.Handle block)
			{
				Entity e = block.Target;
				if (e != null)
				{
					e.Serialize = false;
					PhysicsBlock.CancelPlayerCollisions(e.Get<PhysicsBlock>());
				}
			};
			predictor.Add(new Binding<Voxel.t>(predictor.BlockType, blockCloud.Type));

			PointLight blockLight = entity.Create<PointLight>();
			blockLight.Add(new Binding<Vector3>(blockLight.Position, blockCloud.AveragePosition));
			blockLight.Add(new Binding<bool, int>(blockLight.Enabled, x => x > 0, blockCloud.Blocks.Length));
			blockLight.Attenuation.Value = 30.0f;
			blockLight.Add(new Binding<Vector3, Voxel.t>(blockLight.Color, delegate(Voxel.t t)
			{
				switch (t)
				{
					case Voxel.t.GlowBlue:
						return new Vector3(0.7f, 0.7f, 0.9f);
					case Voxel.t.GlowYellow:
						return new Vector3(0.9f, 0.9f, 0.7f);
					default:
						return new Vector3(0.8f, 0.8f, 0.8f);
				}
			}, blockCloud.Type));

			blockLight.Add(new ChangeBinding<bool>(blockLight.Enabled, delegate(bool old, bool value)
			{
				if (!old && value)
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_MAGIC_CUBE_LOOP, entity);
				else if (old && !value)
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_MAGIC_CUBE_LOOP, entity);
			}));
			if (blockLight.Enabled)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_MAGIC_CUBE_LOOP, entity);

			// Death
			entity.Add(new CommandBinding(player.Die, blockCloud.Clear));
			entity.Add(new CommandBinding(player.Die, delegate()
			{
				Session.Recorder.Event(main, "Die");
				if (agent.Killed || Agent.Query(transform.Position, 0.0f, 10.0f, x => x != agent) != null)
				{
					Session.Recorder.Event(main, "Killed");
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_DEATH, entity);
					main.Spawner.RespawnDistance = Spawner.KilledRespawnDistance;
					main.Spawner.RespawnInterval = Spawner.KilledRespawnInterval;
				}
				entity.Add(new Animation(new Animation.Execute(entity.Delete)));
			}));

			player.EnabledInEditMode = false;

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
			footsteps.Add(new Binding<bool>(footsteps.SoundEnabled, () => !player.Character.Crouched && footstepWallrunStates.Contains(wallRun.CurrentState) || (player.Character.IsSupported && player.Character.EnableWalking), player.Character.IsSupported, player.Character.EnableWalking, wallRun.CurrentState, player.Character.Crouched));
			footsteps.Add(new Binding<Vector3>(footsteps.Position, transform.Position));
			footsteps.Add(new Binding<float>(footsteps.Rotation, rotation.Rotation));
			footsteps.Add(new Binding<float>(footsteps.CharacterHeight, player.Character.Height));
			footsteps.Add(new Binding<float>(footsteps.SupportHeight, player.Character.SupportHeight));
			footsteps.Add(new Binding<bool>(footsteps.IsSupported, player.Character.IsSupported));
			footsteps.Add(new Binding<bool>(footsteps.IsSwimming, player.Character.IsSwimming));
			footsteps.Add(new CommandBinding<float>(footsteps.Damage, agent.Damage));
			footsteps.Add(new CommandBinding<Voxel, Voxel.Coord, Direction>(wallRun.WalkedOn, footsteps.WalkedOn));
			model.Trigger("Run", 0.16f, footsteps.Footstep);
			model.Trigger("Run", 0.58f, footsteps.Footstep);
			model.Trigger("WallRunLeft", 0.16f, footsteps.Footstep);
			model.Trigger("WallRunLeft", 0.58f, footsteps.Footstep);
			model.Trigger("WallRunRight", 0.16f, footsteps.Footstep);
			model.Trigger("WallRunRight", 0.58f, footsteps.Footstep);
			model.Trigger("WallRunStraight", 0.16f, footsteps.Footstep);
			model.Trigger("WallRunStraight", 0.58f, footsteps.Footstep);
			model.Trigger("TurnLeft", 0.15f, footsteps.Footstep);
			model.Trigger("TurnRight", 0.15f, footsteps.Footstep);
			model.Trigger("TopOut", 1.0f, new Command
			{
				Action = delegate()
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_GRUNT, entity);
				}
			});

			main.UI.IsMouseVisible.Value = false;

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
			fallDamage.Add(new TwoWayBinding<bool>(player.EnableMoves, fallDamage.EnableMoves));
			fallDamage.Add(new TwoWayBinding<bool>(fallDamage.Landing, rotation.Landing));
			fallDamage.Add(new CommandBinding(fallDamage.LockRotation, (Action)rotation.Lock));
			fallDamage.Add(new CommandBinding<float>(fallDamage.PhysicsDamage, agent.Damage));
			fallDamage.Bind(model);

			// Swim up
			input.Bind(player.Character.SwimUp, settings.Jump);

			float parkourTime = 0;
			float jumpTime = 0;
			jumper.Action = delegate(float dt)
			{
				if (player.EnableMoves && player.Character.EnableWalking
					&& vault.CurrentState.Value == Vault.State.None
					&& !rollKickSlide.Rolling && !rollKickSlide.Kicking
					&& jumpTime < Player.SlowmoTime)
				{
					if (jump.Go())
					{
						parkour.Enabled.Value = false;
						jumper.Enabled.Value = false;
					}
					jumpTime += dt;
				}
				else
					jumper.Enabled.Value = false;
			};
			jumper.Add(new CommandBinding(jumper.Enable, delegate() { jumpTime = 0; }));
			jumper.Enabled.Value = false;
			entity.Add(jumper);

			// Jumping
			input.Bind(settings.Jump, PCInput.InputState.Down, delegate()
			{
				jumper.Enabled.Value = true;
			});

			input.Bind(settings.Jump, PCInput.InputState.Up, delegate()
			{
				jumper.Enabled.Value = false;
			});

			// Wall-run, vault, predictive
			parkour.Action = delegate(float dt)
			{
				if (player.EnableMoves
					&& player.Character.EnableWalking
					&& !(player.Character.Crouched && player.Character.IsSupported)
					&& vault.CurrentState.Value == Vault.State.None
					&& !rollKickSlide.Kicking
					&& !rollKickSlide.Rolling
					&& wallRun.CurrentState.Value == WallRun.State.None
					&& parkourTime < Player.SlowmoTime)
				{
					bool didSomething = false;

					bool parkourBeganThisFrame = parkourTime == 0;
					if (predictor.PossibilityCount > 0)
					{
						// In slow motion, prefer left and right wall-running
						if (!(didSomething = wallRun.Activate(WallRun.State.Left, parkourBeganThisFrame)))
							if (!(didSomething = wallRun.Activate(WallRun.State.Right, parkourBeganThisFrame)))
								didSomething = vault.Go(parkourBeganThisFrame);
					}
					else
					{
						// In normal mode, prefer straight wall-running
						if (!(didSomething = vault.Go(parkourBeganThisFrame)))
							if (!(didSomething = wallRun.Activate(WallRun.State.Straight, parkourBeganThisFrame)))
								if (!(didSomething = wallRun.Activate(WallRun.State.Left, parkourBeganThisFrame)))
									didSomething = wallRun.Activate(WallRun.State.Right, parkourBeganThisFrame);
					}

					if (didSomething)
					{
						jumper.Enabled.Value = false;
						player.SlowMotion.Value = false;
						parkour.Enabled.Value = false;
					}
					else if (parkourBeganThisFrame && player.Character.LinearVelocity.Value.Y > FallDamage.RollingDeathVelocity)
					{
						if (blockCloud.Blocks.Length > 0)
						{
							player.SlowMotion.Value = true;
							predictor.ClearPossibilities();
							predictor.PredictPlatforms();
							predictor.PredictWalls();
						}
						else if (player.EnableSlowMotion)
							player.SlowMotion.Value = true;
					}

					parkourTime += dt;
				}
				else
					parkour.Enabled.Value = false;
			};
			parkour.Add(new CommandBinding(parkour.Enable, delegate()
			{
				parkourTime = 0;
			}));
			entity.Add(parkour);
			parkour.Enabled.Value = false;

			input.Bind(settings.Parkour, PCInput.InputState.Down, delegate()
			{
				parkour.Enabled.Value = true;
			});

			input.Bind(settings.Parkour, PCInput.InputState.Up, delegate()
			{
				parkour.Enabled.Value = false;
				wallRun.Deactivate();
				if (player.SlowMotion)
					player.SlowMotion.Value = false;
			});

			input.Bind(settings.RollKick, PCInput.InputState.Down, delegate()
			{
				if (player.EnableMoves && player.Character.EnableWalking)
				{
					rollKickSlide.Go();
					parkour.Enabled.Value = false;
					jumper.Enabled.Value = false;
				}
			});

			input.Bind(settings.RollKick, PCInput.InputState.Up, delegate()
			{
				if (!rollKickSlide.Rolling && !rollKickSlide.Kicking)
					player.Character.AllowUncrouch.Value = true;
			});

			// Special ability
			/*
			input.Bind(settings.SpecialAbility, PCInput.InputState.Down, delegate()
			{
				Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(main.Camera.Position, main.Camera.Forward, main.Camera.FarPlaneDistance, null);
				if (hit.Voxel != null && hit.Voxel.GetType() != typeof(DynamicVoxel))
				{
					VoxelRip.Go(hit.Voxel, hit.Coordinate.Value, 7, delegate(List<DynamicVoxel> results)
					{
						foreach (DynamicVoxel v in results)
						{
							v.IsAffectedByGravity.Value = false;
							v.LinearVelocity.Value = hit.Voxel.GetAbsoluteVector(hit.Normal.GetVector()) * 7.0f
								+ new Vector3((float)this.random.NextDouble() * 2.0f - 1.0f, (float)this.random.NextDouble() * 2.0f - 1.0f, (float)this.random.NextDouble() * 2.0f - 1.0f);
						}
					});
				}
			});
			*/

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
					entity.Add(new TwoWayBinding<float>(WorldFactory.Instance.Get<World>().CameraShakeAmount, cameraControl.CameraShakeAmount));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableRoll, rollKickSlide.EnableRoll));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableCrouch, player.EnableCrouch));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableKick, rollKickSlide.EnableKick));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableWallRun, wallRun.EnableWallRun));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableWallRunHorizontal, wallRun.EnableWallRunHorizontal));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableEnhancedWallRun, wallRun.EnableEnhancedWallRun));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableMoves, player.EnableMoves));
					entity.Add(new TwoWayBinding<float>(playerData.MaxSpeed, player.Character.MaxSpeed));

					if (playerData.CloudType.Value == Voxel.t.Empty) // This makes everything work if we spawn next to a power block socket
						entity.Add(new TwoWayBinding<Voxel.t>(blockCloud.Type, playerData.CloudType));
					else
						entity.Add(new TwoWayBinding<Voxel.t>(playerData.CloudType, blockCloud.Type));

					entity.Add(new TwoWayBinding<bool>(playerData.ThirdPerson, cameraControl.ThirdPerson));
					entity.Add(new TwoWayBinding<bool>(playerData.EnableSlowMotion, player.EnableSlowMotion));

					Phone phone = dataEntity.GetOrCreate<Phone>("Phone");

					entity.Add
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

					PhoneNote.Attach(main, entity, player, model, input, phone, player.Character.EnableWalking, playerData.PhoneActive, playerData.NoteActive);

					PlayerUI.Attach(main, entity, ui, player.Health, rotation.Rotation, playerData.NoteActive, playerData.PhoneActive);
				}
			});

			fpsCamera.Add(new Binding<Vector2>(fpsCamera.Mouse, input.Mouse));
			fpsCamera.Add(new Binding<Vector2>(fpsCamera.Movement, input.Movement));
			input.Bind(fpsCamera.SpeedMode, settings.Parkour);
			input.Bind(fpsCamera.Up, settings.Jump);
			fpsCamera.Add(new Binding<bool>(fpsCamera.Down, input.GetKey(Keys.LeftControl)));
			Lemma.Console.Console.AddConCommand(new ConCommand("noclip", "Toggle free camera mode", delegate(ConCommand.ArgCollection args)
			{
				bool freeCameraMode = !fpsCamera.Enabled;
				fpsCamera.Enabled.Value = freeCameraMode;
				cameraControl.Enabled.Value = !freeCameraMode;
				firstPersonModel.Enabled.Value = !freeCameraMode;
				model.Enabled.Value = !freeCameraMode;
				ui.Enabled.Value = !freeCameraMode;
				player.Character.EnableWalking.Value = !freeCameraMode;
				player.EnableMoves.Value = !freeCameraMode;
				player.Character.Body.IsAffectedByGravity = !freeCameraMode;
				if (freeCameraMode)
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_BREATHING_SOFT, entity);
				else
					transform.Position.Value = main.Camera.Position;
			}));

			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				Lemma.Console.Console.RemoveConCommand("noclip");
				if (fpsCamera.Enabled) // Movement is disabled. Re-enable it.
				{
					player.Character.EnableWalking.Value = true;
					player.EnableMoves.Value = true;
				}
				PlayerFactory.Instance = null;
			}));
		}
	}
}