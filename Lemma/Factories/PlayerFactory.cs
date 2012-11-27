using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Input;
using BEPUphysics;
using Lemma.Util;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class PlayerFactory : Factory
	{
		public PlayerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		struct AnimatingBlock
		{
			public Map Map;
			public Map.Coordinate Coord;
			public Map.CellState State;
			private Vector3 pos;
			private bool validPos;
			public Vector3 AbsolutePosition
			{
				get
				{
					if (!this.validPos)
					{
						this.pos = this.Map.GetAbsolutePosition(this.Coord);
						this.validPos = true;
					}
					return this.pos;
				}
			}
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Player");
			result.ID = "player";
			Player player = new Player();
			Transform transform = new Transform();
			AnimatedModel model = new AnimatedModel();
			AnimatedModel firstPersonModel = new AnimatedModel();
			AudioListener audioListener = new AudioListener();
			Sound footsteps = new Sound();
			Sound wind = new Sound();
			wind.Is3D.Value = false;
			wind.Cue.Value = "Speed";
			Timer footstepTimer = new Timer();

			result.Add("Player", player);
			result.Add("Transform", transform);
			result.Add("FirstPersonModel", firstPersonModel);
			result.Add("Model", model);
			result.Add("AudioListener", audioListener);
			result.Add("Footsteps", footsteps);
			result.Add("SpeedSound", wind);
			result.Add("FootstepTimer", footstepTimer);

			model.Editable = false;
			model.Filename.Value = "Models\\player";
			model.CullBoundingBox.Value = false;

			firstPersonModel.Editable = false;
			firstPersonModel.Filename.Value = "Models\\player-firstperson";
			firstPersonModel.CullBoundingBox.Value = false;

			footstepTimer.Repeat.Value = true;
			footstepTimer.Interval.Value = 0.35f;

			result.Add("Submerged", new Property<bool> { Editable = false });

			result.Add("Pistol", new Property<Entity.Handle> { Editable = false });
			result.Add("Phone", new Property<Entity.Handle> { Editable = false });
			result.Add("Rotation", new Property<float> { Editable = false });
			result.Add("Data", new Property<Entity.Handle> { Editable = false });

			/*TextElement debug = new TextElement();
			debug.FontFile.Value = "Font";
			debug.Name.Value = "Debug";
			debug.AnchorPoint.Value = new Vector2(1, 1);
			debug.Add(new Binding<Vector2, Point>(debug.Position, x => new Vector2(x.X, x.Y), main.ScreenSize));
			ui.Root.Children.Add(debug);*/

			return result;
		}

		private enum InputState { Down, Up }

		private class BlockPossibility
		{
			public Map Map;
			public Map.Coordinate StartCoord;
			public Map.Coordinate EndCoord;
			public ModelAlpha Model;
		}

		private class Prediction
		{
			public Vector3 Position;
			public int Level;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;

			this.SetMain(result, main);

			Player player = result.Get<Player>();
			Transform transform = result.Get<Transform>();

			AnimatedModel model = result.Get<AnimatedModel>("Model");
			AnimatedModel firstPersonModel = result.Get<AnimatedModel>("FirstPersonModel");

			firstPersonModel.Bind(model);

			model.UnsupportedTechniques.Add(Technique.Clip);
			model.UnsupportedTechniques.Add(Technique.MotionBlur);
			model.UnsupportedTechniques.Add(Technique.Render);
			firstPersonModel.UnsupportedTechniques.Add(Technique.Shadow);
			firstPersonModel.UnsupportedTechniques.Add(Technique.PointLightShadow);

			FPSInput input = new FPSInput();
			input.Add(new Binding<bool>(input.Enabled, x => !x, main.Paused));
			result.Add("Input", input);

			Model buildReticle = new Model();
			buildReticle.Filename.Value = "Models\\arrow";
			buildReticle.Editable = false;
			buildReticle.Enabled.Value = false;
			buildReticle.Serialize = false;
			result.Add("BuildReticle", buildReticle);

			Model jumpReticle = new Model();
			jumpReticle.Filename.Value = "Models\\crosshair";
			jumpReticle.Editable = false;
			jumpReticle.Enabled.Value = false;
			jumpReticle.Serialize = false;
			result.Add("JumpReticle", jumpReticle);

			AudioListener audioListener = result.Get<AudioListener>();
			Sound footsteps = result.Get<Sound>("Footsteps");
			Timer footstepTimer = result.Get<Timer>("FootstepTimer");

			UIRenderer ui = new UIRenderer();
			ui.DrawOrder.Value = -1;
			ui.Add(new Binding<bool>(ui.Enabled, x => !x, main.Paused));
			result.Add("UI", ui);
			Sprite damageOverlay = new Sprite();
			damageOverlay.Image.Value = "Images\\damage";
			damageOverlay.AnchorPoint.Value = new Vector2(0.5f);
			ui.Root.Children.Add(damageOverlay);

			Sprite crosshair = new Sprite();
			crosshair.Image.Value = "Images\\crosshair";
			crosshair.AnchorPoint.Value = new Vector2(0.5f);
			crosshair.Add(new Binding<Vector2, Point>(crosshair.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
			ui.Root.Children.Add(crosshair);

			const float healthBarWidth = 200.0f;
			const float healthBarHeight = 16.0f;

			Container healthContainer = new Container();
			healthContainer.PaddingBottom.Value = healthContainer.PaddingLeft.Value = healthContainer.PaddingRight.Value = healthContainer.PaddingTop.Value = 1;
			healthContainer.Add(new Binding<Microsoft.Xna.Framework.Color>(healthContainer.Tint, () => player.Sprint || player.SlowMotion ? Microsoft.Xna.Framework.Color.Red : Microsoft.Xna.Framework.Color.White, player.Sprint, player.SlowMotion));
			healthContainer.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
			healthContainer.Add(new Binding<Vector2, Point>(healthContainer.Position, x => new Vector2(x.X * 0.5f, x.Y - healthBarHeight), main.ScreenSize));
			ui.Root.Children.Add(healthContainer);

			Container healthBackground = new Container();
			healthBackground.ResizeHorizontal.Value = false;
			healthBackground.Size.Value = new Vector2(healthBarWidth, healthBarHeight);
			healthBackground.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			healthBackground.PaddingBottom.Value = healthBackground.PaddingLeft.Value = healthBackground.PaddingRight.Value = healthBackground.PaddingTop.Value = 1;
			healthContainer.Children.Add(healthBackground);

			Container healthBar = new Container();
			healthBar.ResizeHorizontal.Value = false;
			healthBar.ResizeVertical.Value = false;
			healthBar.Position.Value = new Vector2(healthBarWidth * 0.5f, 0.0f);
			healthBar.AnchorPoint.Value = new Vector2(0.5f, 0.0f);
			healthBar.Add(new Binding<Microsoft.Xna.Framework.Color, int>(healthBar.Tint, x => x > 10 ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Red, player.Stamina));
			healthBar.Add(new Binding<Vector2, int>(healthBar.Size, x => new Vector2(healthBarWidth * ((float)x / 100.0f), healthBarHeight), player.Stamina));
			healthBackground.Children.Add(healthBar);

			GameMain.Config settings = ((GameMain)main).Settings;
			input.Add(new Binding<float>(input.MouseSensitivity, settings.MouseSensitivity));
			input.Add(new Binding<bool>(input.InvertMouseX, settings.InvertMouseX));
			input.Add(new Binding<bool>(input.InvertMouseY, settings.InvertMouseY));
			input.Add(new Binding<PCInput.PCInputBinding>(input.LeftKey, settings.Left));
			input.Add(new Binding<PCInput.PCInputBinding>(input.RightKey, settings.Right));
			input.Add(new Binding<PCInput.PCInputBinding>(input.BackwardKey, settings.Backward));
			input.Add(new Binding<PCInput.PCInputBinding>(input.ForwardKey, settings.Forward));

			Action<Property<PCInput.PCInputBinding>, InputState, Action> addInput = delegate(Property<PCInput.PCInputBinding> inputBinding, InputState state, Action action)
			{
				CommandBinding commandBinding = null;
				Action rebindCommand = delegate()
				{
					if (commandBinding != null)
						input.Remove(commandBinding);

					PCInput.PCInputBinding ib = inputBinding;
					if (ib.Key == Keys.None && ib.MouseButton == PCInput.MouseButton.None)
						commandBinding = null;
					else
					{
						commandBinding = new CommandBinding(state == InputState.Up ? input.GetInputUp(inputBinding) : input.GetInputDown(inputBinding), action);
						input.Add(commandBinding);
					}
				};
				input.Add(new NotifyBinding(rebindCommand, inputBinding));
				rebindCommand();
			};

			model.StartClip("Idle", 0, true);

			Updater update = new Updater();
			update.EnabledInEditMode.Value = false;
			result.Add(update);

			Property<bool> thirdPerson = new Property<bool> { Value = false };
#if DEBUG
			input.Add(new CommandBinding(input.GetKeyDown(Keys.C), delegate() { thirdPerson.Value = !thirdPerson; }));
#endif

			Property<Entity.Handle> pistol = result.GetProperty<Entity.Handle>("Pistol");
			Property<Entity.Handle> phone = result.GetProperty<Entity.Handle>("Phone");
			Binding<bool> phoneActiveBinding = null;
			Action setupPhone = delegate()
			{
				if (phoneActiveBinding != null)
					result.Remove(phoneActiveBinding);
				Entity p = phone.Value.Target;
				if (p != null)
				{
					phoneActiveBinding = new Binding<bool>(input.Enabled, x => !x, p.GetProperty<bool>("Active"));
					result.Add(phoneActiveBinding);
					p.GetCommand<Entity>("Attach").Execute(result);
				}
			};
			result.Add(new NotifyBinding(setupPhone, phone));
			if (phone.Value.Target != null)
				setupPhone();

			Property<Entity.Handle> data = result.GetProperty<Entity.Handle>("Data");

			result.Add(new PostInitialization
			{
				delegate()
				{
					if (data.Value.Target == null)
						data.Value = Factory.Get<PlayerDataFactory>().Instance(main);
					
					// Bind player data properties
					result.Add(new TwoWayBinding<float>(data.Value.Target.GetProperty<float>("JumpSpeed"), player.JumpSpeed));
					Property<int> stamina = data.Value.Target.GetProperty<int>("Stamina");
					if (creating && stamina < 25)
						stamina.Value = 25;
					result.Add(new TwoWayBinding<int>(stamina, player.Stamina));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableAim"), player.EnableAim));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableRoll"), player.EnableRoll));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableKick"), player.EnableKick));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableWallRun"), player.EnableWallRun));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableBlockBuild"), player.EnableBlockBuild));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableLevitation"), player.EnableLevitation));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableSprint"), player.EnableSprint));
					result.Add(new TwoWayBinding<bool>(data.Value.Target.GetProperty<bool>("EnableSlowMotion"), player.EnableSlowMotion));
					result.Add(new TwoWayBinding<Entity.Handle>(data.Value.Target.GetProperty<Entity.Handle>("Pistol"), pistol));
					result.Add(new TwoWayBinding<Entity.Handle>(data.Value.Target.GetProperty<Entity.Handle>("Phone"), phone));
				}
			});

			// Die if stamina is depleted
			result.Add(new CommandBinding(player.StaminaDepleted, delegate()
			{
				result.Add(new Animation(new Animation.Delay(0.01f), new Animation.Execute(result.Delete)));
			}));

			result.Add(new CommandBinding(result.Delete, delegate()
			{
				Entity p = phone.Value.Target;
				if (p != null)
					p.GetCommand("Detach").Execute();
			}));

			addInput(settings.TogglePhone, InputState.Down, delegate()
			{
				Entity p = phone.Value.Target;
				if (p != null)
				{
					if (!p.GetProperty<bool>("Active"))
						p.GetCommand("Show").Execute();
				}
			});

			if (phone.Value.Target == null)
			{
				result.Add(new PostInitialization
				{
					delegate ()
					{
						phone.Reset();
					}
				});
			}

			input.EnabledInEditMode.Value = false;
			player.EnabledInEditMode.Value = false;
			ui.EnabledInEditMode.Value = false;

			input.MaxY.Value = (float)Math.PI * 0.4f;

			/*TextElement debug = (TextElement)ui.Root.GetChildByName("Debug");
			debug.Add(new Binding<string, float>(debug.Text, x => x.ToString("F"), speedSound.GetProperty("Volume")));*/

			Sound speedSound = result.Get<Sound>("SpeedSound");
			player.Add(new Binding<float, Vector3>(speedSound.GetProperty("Volume"), delegate(Vector3 velocity)
			{
				float speed = velocity.Length();
				float maxSpeed = player.MaxSpeed * 1.25f;
				if (speed > maxSpeed)
					return (speed - maxSpeed) / (maxSpeed * 2.0f);
				else
					return 0.0f;
			}, player.LinearVelocity));
			speedSound.Play.Execute();
			speedSound.GetProperty("Volume").Value = 0.0f;

			player.Add(new TwoWayBinding<bool>(result.GetProperty<bool>("Submerged"), player.IsSwimming));

			// Center the damage overlay and scale it to fit the screen
			damageOverlay.Add(new Binding<Vector2, Point>(damageOverlay.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
			damageOverlay.Add(new Binding<Vector2>(damageOverlay.Scale, () => new Vector2(main.ScreenSize.Value.X / damageOverlay.Size.Value.X, main.ScreenSize.Value.Y / damageOverlay.Size.Value.Y), main.ScreenSize, damageOverlay.Size));
			damageOverlay.Add(new Binding<float, float>(damageOverlay.Opacity, x => 1.0f - x, player.Health));

			// Player rotation code
			Property<float> rotation = result.GetProperty<float>("Rotation");
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
				if (rotationLocked.Value && !value)
				{
					lockedRotationValue = rotation.Value.ClosestAngle(input.Mouse.Value.X);
					rotationLockBlending = 0.0f;
				}
				rotationLocked.InternalValue = value;
			};
			
			// When rotation is locked, we want to make sure the player can't turn their head
			// 180 degrees from the direction they're facing

			input.Add(new Binding<float>(input.MinX, () => rotationLocked ? rotation + ((float)Math.PI * -0.3f) : 0.0f, rotation, rotationLocked));
			input.Add(new Binding<float>(input.MaxX, () => rotationLocked ? rotation + ((float)Math.PI * 0.3f) : 0.0f, rotation, rotationLocked));
			input.Add(new NotifyBinding(delegate() { input.Mouse.Changed(); }, rotationLocked)); // Make sure the rotation locking takes effect even if the player doesn't move the mouse

			update.Add(delegate(float dt)
			{
				if (!rotationLocked)
				{
					if (rotationLockBlending < rotationLockBlendTime)
					{
						lockedRotationValue = lockedRotationValue.ClosestAngle(input.Mouse.Value.X);
						rotation.Value = lockedRotationValue + (input.Mouse.Value.X - lockedRotationValue) * (rotationLockBlending / rotationLockBlendTime);
						rotationLockBlending += dt;
					}
					else
						rotation.Value = input.Mouse.Value.X;
				}
			});

			Map wallRunMap = null;
			Direction wallDirection = Direction.None;

			player.Add(new TwoWayBinding<Matrix>(transform.Matrix, player.Transform));
			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateTranslation(0, (player.Height * -0.5f) - player.SupportHeight, 0) * Matrix.CreateRotationY(rotation) * transform.Matrix, transform.Matrix, rotation, player.Height, player.SupportHeight));
			firstPersonModel.Add(new Binding<Matrix>(firstPersonModel.Transform, model.Transform));
			firstPersonModel.Add(new Binding<Vector3>(firstPersonModel.Scale, model.Scale));
			audioListener.Add(new Binding<Vector3>(audioListener.Position, main.Camera.Position));
			audioListener.Add(new Binding<Vector3>(audioListener.Forward, main.Camera.Forward));
			audioListener.Add(new Binding<Vector3>(audioListener.Velocity, player.LinearVelocity));

			Map.GlobalRaycastResult groundRaycast = new Map.GlobalRaycastResult();

			Command<Map, Map.Coordinate?> walkedOn = new Command<Map,Map.Coordinate?>();
			result.Add("WalkedOn", walkedOn);

			update.Add(delegate(float dt)
			{
				if (player.IsSupported)
				{
					Map oldMap = groundRaycast.Map;
					Map.Coordinate? oldCoord = groundRaycast.Coordinate;
					groundRaycast = Map.GlobalRaycast(transform.Position, Vector3.Down, player.Height.Value * 0.5f + player.SupportHeight + 1.1f);
					if (groundRaycast.Map != oldMap || (oldCoord != null && groundRaycast.Coordinate != null && !oldCoord.Value.Equivalent(groundRaycast.Coordinate.Value)))
						walkedOn.Execute(groundRaycast.Map, groundRaycast.Coordinate);
				}
				else
				{
					if (groundRaycast.Map != null)
						walkedOn.Execute(null, null);
					groundRaycast.Map = null;
					groundRaycast.Coordinate = null;
				}
			});

			footstepTimer.Add(new CommandBinding(footstepTimer.Command, delegate()
			{
				if (player.WallRunState.Value == Player.WallRun.None)
				{
					if (groundRaycast.Map != null)
					{
						footsteps.Cue.Value = groundRaycast.Map[groundRaycast.Coordinate.Value].FootstepCue;
						footsteps.Play.Execute();
					}
				}
				else
				{
					Map.RaycastResult hit = wallRunMap.Raycast(wallRunMap.GetCoordinate(transform.Position - new Vector3(0, player.Height * 0.5f - player.SupportHeight, 0)), wallDirection, 3);
					if (hit.Coordinate != null)
					{
						footsteps.Cue.Value = wallRunMap[hit.Coordinate.Value].FootstepCue;
						footsteps.Play.Execute();
					}
				}
			}));
			footstepTimer.Add(new Binding<bool>(footstepTimer.Enabled, () => player.WallRunState.Value != Player.WallRun.None || (player.MovementDirection.Value.LengthSquared() > 0.0f && player.IsSupported && player.EnableWalking), player.MovementDirection, player.IsSupported, player.EnableWalking, player.WallRunState));
			footsteps.Add(new Binding<Vector3>(footsteps.Position, x => x - new Vector3(0, player.Height * 0.5f, 0), transform.Position));
			footsteps.Add(new Binding<Vector3>(footsteps.Velocity, player.LinearVelocity));

			main.IsMouseVisible.Value = false;

			model.Update(0.0f);
			Property<Matrix> cameraBone = model.GetBoneTransform("Camera");
			Property<Matrix> relativeHeadBone = model.GetRelativeBoneTransform("Head");
			Property<Matrix> relativeSpineBone = model.GetRelativeBoneTransform("Spine3");
			Property<Matrix> clavicleLeft = model.GetBoneTransform("Clavicle_L");
			Property<Matrix> clavicleRight = model.GetBoneTransform("Clavicle_R");
			Property<Matrix> relativeUpperLeftArm = model.GetRelativeBoneTransform("UpArm_L");
			Property<Matrix> relativeUpperRightArm = model.GetRelativeBoneTransform("UpArm_R");
			Property<Matrix> pistolBone = model.GetRelativeBoneTransform("Pistol");
			Property<Matrix> headBone = model.GetBoneTransform("Head");
			Vector3 cameraOffset = cameraBone.Value.Translation - headBone.Value.Translation;

			// Camera updater / fire code
			Property<float> cameraShakeAmount = new Property<float> { Value = 0.0f };
			float cameraShakeTime = 0.0f;
			const float totalCameraShakeTime = 0.5f;
			Animation cameraShakeAnimation = null;

			result.Add("ShakeCamera", new Command<Vector3, float>
			{
				Action = delegate(Vector3 pos, float size)
				{
					if (cameraShakeAnimation != null && cameraShakeAnimation.Active)
						cameraShakeAnimation.Delete.Execute();
					cameraShakeAnimation = new Animation(new Animation.FloatMoveToSpeed(cameraShakeAmount, Math.Max(0.0f, 1.0f - ((pos - main.Camera.Position).Length() / size)), 20.0f));
					cameraShakeTime = totalCameraShakeTime;
					result.Add(cameraShakeAnimation);
				}
			});

			bool fire = false;
			float aimAnimationBlend = 0.0f;
			const float aimAnimationBlendTotal = AnimatedModel.DefaultBlendTime;

			Animation zoomAnimation = null;

			Action scopeOutPistol = delegate()
			{
				if (model.IsPlaying("Aim"))
				{
					model.Stop("Aim");
					Sound.PlayCue(main, "Pistol Scope Out");

					if (zoomAnimation != null)
						zoomAnimation.Delete.Execute();
					zoomAnimation = new Animation(new Animation.FloatMoveToSpeed(main.Camera.FieldOfView, MathHelper.ToRadians(80.0f), 2.0f));
					result.Add(zoomAnimation);
				}
			};

			Property<bool> levitationMode = result.GetOrMakeProperty<bool>("LevitationMode");

			Property<Vector3> handPosition = new Property<Vector3>();

			Property<Matrix> handTransform = model.GetWorldBoneTransform("Palm-2_R");

			update.Add(delegate(float dt)
			{
				relativeHeadBone.Value *= Matrix.CreateRotationX(input.Mouse.Value.Y * 0.5f);
				relativeSpineBone.Value *= Matrix.CreateRotationX(input.Mouse.Value.Y * 0.25f);
				model.UpdateWorldTransforms();

				float targetAngle = input.Mouse.Value.Y * 0.75f;

				float angle = targetAngle;
				if (angle < 0.0f)
					angle *= 0.9f;
				else
					angle *= 1.1f;

				bool pistolDrawn = pistol.Value.Target != null && pistol.Value.Target.GetProperty<bool>("Active");

				bool aimAnimationActive = pistolDrawn || levitationMode;

				aimAnimationBlend = Math.Max(0.0f, aimAnimationBlend - dt);

				Matrix r;
				if (aimAnimationActive)
					r = Matrix.CreateRotationX(angle * (1.0f - (aimAnimationBlend / aimAnimationBlendTotal)));
				else
					r = Matrix.CreateRotationX(angle * aimAnimationBlend / aimAnimationBlendTotal);

				Matrix parent = clavicleLeft;
				parent.Translation = Vector3.Zero;
				relativeUpperLeftArm.Value *= parent * r * Matrix.Invert(parent);

				parent = clavicleRight;
				parent.Translation = Vector3.Zero;
				relativeUpperRightArm.Value *= parent * r * Matrix.Invert(parent);

				if (pistolDrawn)
				{
					angle = ((float)Math.Cos(angle) * -0.15f) + 0.1f + (angle < 0.0f ? angle * 0.25f : angle * 0.15f);
					pistolBone.Value *= Matrix.CreateRotationZ(angle) * Matrix.CreateRotationX(targetAngle < 0.0f ? targetAngle * -0.035f : targetAngle * -0.02f);
				}

				model.UpdateWorldTransforms();

				handPosition.Value = handTransform.Value.Translation;

				float shakeAngle = 0.0f;
				if (cameraShakeTime > 0.0f)
				{
					shakeAngle = (float)Math.Sin(main.TotalTime * ((float)Math.PI / 0.05f)) * 0.4f * cameraShakeAmount * (cameraShakeTime / totalCameraShakeTime);
					cameraShakeTime -= dt;
				}
				else
					cameraShakeAmount.Value = 0.0f;

				if (thirdPerson)
				{
					Vector3 cameraPosition = Vector3.Transform(new Vector3(0.0f, 3.0f, 0.0f), model.Transform);

					main.Camera.Angles.Value = new Vector3(-input.Mouse.Value.Y, input.Mouse.Value.X + (float)Math.PI * 1.0f, shakeAngle);

					Map.GlobalRaycastResult hit = Map.GlobalRaycast(cameraPosition, -main.Camera.Forward.Value, 5.0f);

					float cameraDistance = 4.0f;
					if (hit.Map != null)
						cameraDistance = (hit.Position - cameraPosition).Length() - 1.0f;
					main.Camera.Position.Value = cameraPosition + (main.Camera.Right.Value * cameraDistance * -0.25f) + (main.Camera.Forward.Value * -cameraDistance);
				}
				else
				{
					Vector3 cameraPosition = Vector3.Transform(cameraOffset, headBone.Value * model.Transform);

					main.Camera.Position.Value = cameraPosition;

					Matrix camera = cameraBone.Value * Matrix.CreateRotationY(input.Mouse.Value.X);

					Matrix rot = Matrix.Identity;
					rot.Forward = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0, 1.0f, 0), camera));
					rot.Up = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0.0f, 0, 1.0f), camera));
					rot.Right = Vector3.Normalize(Vector3.Cross(rot.Forward, rot.Up));

					Vector3 right = Vector3.Cross(rot.Forward, Vector3.Up);

					main.Camera.RotationMatrix.Value = rot * Matrix.CreateFromAxisAngle(rot.Forward, shakeAngle) * Matrix.CreateFromAxisAngle(right, -input.Mouse.Value.Y);
				}

				if (fire)
				{
					// Necessary because the fire command must be executed *after* the pistol has been correctly positioned each frame.
					fire = false;
					model.StartClip("PlayerFire", 4, false, 0.0f);
					pistol.Value.Target.GetCommand("Fire").Execute();
					if (pistol.Value.Target.GetProperty<int>("Ammo") == 0)
						scopeOutPistol();
				}
			});

			// Movement binding
			player.Add(new Binding<Vector2>(player.MovementDirection, delegate()
			{
				Vector2 movement = input.Movement;
				if (player.Sprint)
					movement = Vector2.Normalize(new Vector2(movement.X, 1.0f));
				else if (movement.LengthSquared() == 0.0f)
					return Vector2.Zero;

				Matrix matrix = Matrix.CreateRotationY(rotation);

				Vector2 forwardDir = new Vector2(matrix.Forward.X, matrix.Forward.Z);
				Vector2 rightDir = new Vector2(matrix.Right.X, matrix.Right.Z);
				return -Vector2.Normalize((forwardDir * movement.Y) + (rightDir * movement.X));
			}, input.Movement, rotation, player.Sprint));
			
			// Update animation
			bool lastSupported = false;
			bool canKick = false;
			result.Add("AnimationUpdater", new Updater
			{
				delegate(float dt)
				{
					if ((!player.EnableWalking && !player.Sprint) || player.WallRunState.Value != Player.WallRun.None)
						return;

					model.Stop("WallWalkLeft", "WallWalkRight");
					if (player.IsSupported && !lastSupported)
					{
						canKick = true;
						model.Stop("Jump", "Fall", "JumpFall");
						footstepTimer.Command.Execute();
						Sound.PlayCue(main, "Land", transform.Position + new Vector3(0, (player.Height * -0.5f) - player.SupportHeight, 0));
						model.StartClip("Land");
					}
					else if (player.IsSupported)
					{
						canKick = true;

						Vector2 dir = input.Movement;

						string movementAnimation;
						int animationPriority = 0;
						if (!player.Sprint && dir.LengthSquared() == 0.0f)
							movementAnimation = "Idle";
						else
							movementAnimation = dir.Y < 0.0f ? "WalkBackwards" : (dir.X > 0.0f ? "StrafeRight" : (dir.X < 0.0f ? "StrafeLeft" : "Walk"));

						if (player.Crouched)
						{
							movementAnimation = "Crouch" + movementAnimation;
							animationPriority = 2;
						}

						if (!model.IsPlaying(movementAnimation))
						{
							model.Stop
							(
								"CrouchIdle",
								"CrouchWalkBackwards",
								"CrouchWalk",
								"CrouchStrafeRight",
								"CrouchStrafeLeft",
								"Idle",
								"WalkBackwards",
								"Walk",
								"StrafeRight",
								"StrafeLeft",
								"Jump"
							);
							model.StartClip(movementAnimation, animationPriority, true);
						}
					}
					else
					{
						model.Stop
						(
							"CrouchIdle",
							"CrouchWalkBackwards",
							"CrouchWalk",
							"CrouchStrafeRight",
							"CrouchStrafeLeft",
							"Idle",
							"WalkBackwards",
							"Walk",
							"StrafeRight",
							"StrafeLeft"
						);
						if (!model.IsPlaying("Fall") && !model.IsPlaying("JumpFall"))
						{
							model.Stop("JumpFall", "Fall");
							bool jumpFall = model.IsPlaying("Jump");
							model.StartClip(jumpFall ? "JumpFall" : "Fall", 0, true);
						}
					}

					lastSupported = player.IsSupported;
				}
			});

			// Block possibilities
			const int blockInstantiationStaminaCost = 3;

			Dictionary<Map, List<BlockPossibility>> blockPossibilities = new Dictionary<Map, List<BlockPossibility>>();

			Action clearBlockPossibilities = delegate()
			{
				foreach (BlockPossibility block in blockPossibilities.Values.SelectMany(x => x))
					block.Model.Delete.Execute();
				blockPossibilities.Clear();
			};

			Action<BlockPossibility> addBlockPossibility = delegate(BlockPossibility block)
			{
				if (block.Model == null)
				{
					Vector3 start = block.Map.GetRelativePosition(block.StartCoord), end = block.Map.GetRelativePosition(block.EndCoord);

					Matrix matrix = Matrix.CreateScale(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y), Math.Abs(end.Z - start.Z)) * Matrix.CreateTranslation(new Vector3(-0.5f) + (start + end) * 0.5f);

					ModelAlpha box = new ModelAlpha();
					box.Filename.Value = "Models\\alpha-box";
					box.Color.Value = new Vector3(0.5f, 0.7f, 0.9f);
					box.Alpha.Value = 0.125f;
					box.IsInstanced.Value = false;
					box.Editable = false;
					box.Serialize = false;
					box.DrawOrder.Value = 11; // In front of water
					box.CullBoundingBox.Value = false;
					box.DisableCulling.Value = true;
					box.Add(new Binding<Matrix>(box.Transform, () => matrix * Matrix.CreateTranslation(-block.Map.Offset.Value) * block.Map.Transform, block.Map.Transform, block.Map.Offset));
					result.Add(box);
					block.Model = box;
				}

				List<BlockPossibility> mapList;
				if (!blockPossibilities.TryGetValue(block.Map, out mapList))
				{
					mapList = new List<BlockPossibility>();
					blockPossibilities[block.Map] = mapList;
				}
				mapList.Add(block);
			};

			Dictionary<AnimatingBlock, bool> animatingBlocks = new Dictionary<AnimatingBlock, bool>();

			Func<IEnumerable<AnimatingBlock>, bool, bool> buildBlocks = delegate(IEnumerable<AnimatingBlock> blocks, bool fake)
			{
				int index = 0;
				EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
				foreach (AnimatingBlock entry in blocks)
				{
					bool animating = false;
					animatingBlocks.TryGetValue(entry, out animating);
					if (animating)
						continue;

					AnimatingBlock spawn = entry;
					animatingBlocks[spawn] = true;

					Entity block = factory.CreateAndBind(main);
					spawn.State.ApplyToEffectBlock(block.Get<ModelInstance>());
					block.GetProperty<Vector3>("Offset").Value = spawn.Map.GetRelativePosition(spawn.Coord);
					block.GetProperty<Vector3>("StartPosition").Value = spawn.AbsolutePosition + new Vector3(0.05f, 0.1f, 0.05f) * index;
					block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f * index) * Matrix.CreateRotationY(0.15f * index);
					block.GetProperty<float>("TotalLifetime").Value = 0.05f + (index * 0.015f);
					block.GetProperty<Entity.Handle>("TargetMap").Value = spawn.Map.Entity;
					block.GetProperty<int>("TargetCellStateID").Value = fake ? 0 : spawn.State.ID;
					block.GetProperty<Map.Coordinate>("TargetCoord").Value = spawn.Coord;
					block.Add(new CommandBinding(block.Delete, delegate()
					{
						try
						{
							animatingBlocks.Remove(spawn);
						}
						catch (KeyNotFoundException)
						{

						}
					}));

					main.Add(block);
					index++;
				}
				return true;
			};

			Action<BlockPossibility> instantiateBlockPossibility = delegate(BlockPossibility block)
			{
				Map.CellState state = WorldFactory.StatesByName["Temporary"];
				block.Map.Fill(block.StartCoord, block.EndCoord, state);
				block.Map.Regenerate();
				block.Model.Delete.Execute();
				List<BlockPossibility> mapList = blockPossibilities[block.Map];
				mapList.Remove(block);
				if (mapList.Count == 0)
					blockPossibilities.Remove(block.Map);
				player.Stamina.Value -= blockInstantiationStaminaCost;
				main.AddComponent(new Animation
				(
					new Animation.Repeat
					(
						new Animation.Sequence
						(
							new Animation.Execute(delegate()
							{
								Sound.PlayCue(main, "BuildBlock", 1.0f, 0.03f);
							}),
							new Animation.Delay(0.06f)
						),
						3
					)
				));
			};

			// Wall run

			footstepTimer.Add(new Binding<float>(footstepTimer.Interval, () => player.WallRunState == Player.WallRun.None ? (player.Crouched ? 0.5f : 0.35f) : 0.23f, player.WallRunState, player.Crouched));

			Action<Map, Direction, bool, Vector3> setUpWallRun = delegate(Map map, Direction dir, bool right, Vector3 forwardVector)
			{
				wallRunMap = map;
				wallDirection = dir;
				player.WallRunState.Value = right ? Player.WallRun.Right : Player.WallRun.Left;

				string animation = right ? "WallWalkRight" : "WallWalkLeft";
				if (!model.IsPlaying(animation))
					model.StartClip(animation, 5, true);

				Direction walkDirection = dir.Cross(map.GetRelativeDirection(Vector3.Up));
				Vector3 velocity = map.GetAbsoluteVector(walkDirection.GetVector());
				if (Vector3.Dot(velocity, forwardVector) < 0.0f)
					velocity = -velocity;
				rotation.Value = (float)Math.Atan2(velocity.X, velocity.Z);
				rotationLocked.Value = true;
				velocity.Y = 0.0f;
				velocity.Normalize();

				Vector3 currentHorizontalVelocity = player.LinearVelocity;
				currentHorizontalVelocity.Y = 0.0f;
				velocity *= Math.Min(player.MaxSpeed * 2.0f, currentHorizontalVelocity.Length() * 1.25f);
				velocity.Y = player.LinearVelocity.Value.Y + 3.0f;
				player.LinearVelocity.Value = velocity;
			};

			float lastWallRunJump = -1.0f;
			const float wallRunJumpDelay = 0.25f;

			Func<bool, bool> activateWallRun = delegate(bool right)
			{
				if (!player.IsSupported && main.TotalTime - lastWallRunJump > wallRunJumpDelay) // Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.
				{
					Matrix matrix = Matrix.CreateRotationY(rotation);
					Vector3 forwardVector = -matrix.Forward;
					Vector3 strafeVector = right ? matrix.Left : matrix.Right;

					Vector3 pos = transform.Position + new Vector3(0, (player.Height * -0.5f) - 0.5f, 0);

					// Attempt to wall-walk on an existing map
					bool activate = false;
					foreach (Map map in Map.ActiveMaps)
					{
						Map.Coordinate coord = map.GetCoordinate(pos);
						Direction dir = map.GetRelativeDirection(strafeVector);
						for (int i = 1; i < 4; i++)
						{
							Map.Coordinate wallCoord = coord.Move(dir, i);
							if (map[wallCoord].ID != 0)
								activate = true;
							else
							{
								// Check block possibilities
								List<BlockPossibility> mapBlockPossibilities;
								if (blockPossibilities.TryGetValue(map, out mapBlockPossibilities))
								{
									foreach (BlockPossibility block in mapBlockPossibilities)
									{
										if (wallCoord.Between(block.StartCoord, block.EndCoord))
										{
											instantiateBlockPossibility(block);
											activate = true;
											break;
										}
									}
								}
							}

							if (activate)
							{
								// Move so the player is exactly two coordinates away from the wall
								transform.Position.Value = map.GetAbsolutePosition(coord.Move(dir, i - 2)) + new Vector3(0, (player.Height * 0.5f) + 0.5f, 0);
								break;
							}
						}
						
						if (activate)
						{
							setUpWallRun(map, dir, right, forwardVector);
							break;
						}
					}
					return activate;
				}
				return false;
			};

			Action deactivateWallRun = delegate()
			{
				if (player.WallRunState.Value != Player.WallRun.None)
				{
					wallRunMap = null;
					player.WallRunState.Value = Player.WallRun.None;
					rotationLocked.Value = false;
					model.Stop("WallWalkLeft", "WallWalkRight");
				}
			};

			Action<Vector3, Vector3, bool> breakWalls = delegate(Vector3 forward, Vector3 right, bool breakFloor)
			{
				Random random = new Random();
				foreach (Map map in Map.ActiveMaps.ToList())
				{
					List<Map.Coordinate> removals = new List<Map.Coordinate>();
					Vector3 pos = transform.Position + new Vector3(0, 0.1f + player.Height * -0.5f - player.SupportHeight, 0);
					for (int i = 0; i < 5; i++)
					{
						pos += forward * 0.5f;
						Map.Coordinate center = map.GetCoordinate(pos);
						Map.Coordinate top = map.GetCoordinate(transform.Position + new Vector3(0.0f, player.Height * 0.5f + 0.1f, 0.0f));
						Direction upDir = map.GetRelativeDirection(Vector3.Up);
						Direction rightDir = map.GetRelativeDirection(right);
						for (Map.Coordinate y = center.Move(upDir.GetReverse(), breakFloor ? 2 : 0); y.GetComponent(upDir) <= top.GetComponent(upDir); y = y.Move(upDir))
						{
							for (Map.Coordinate z = y.Move(rightDir.GetReverse(), 1); z.GetComponent(rightDir) < center.GetComponent(rightDir) + 2; z = z.Move(rightDir))
							{
								Map.CellState state = map[z];
								if (state.ID != 0 && !state.Permanent && !removals.Contains(z))
								{
									removals.Add(z);
									Vector3 cellPos = map.GetAbsolutePosition(z);
									Vector3 toCell = cellPos - pos;
									Entity block = Factory.CreateAndBind(main, "Block");
									block.Get<Transform>().Position.Value = cellPos;
									block.Get<Transform>().Quaternion.Value = map.Entity.Get<Transform>().Quaternion;
									state.ApplyToBlock(block);
									block.Get<ModelInstance>().GetVector3Parameter("Offset").Value = map.GetRelativePosition(z);
									toCell += forward * 4.0f;
									toCell.Normalize();
									block.Get<PhysicsBlock>().LinearVelocity.Value = toCell * 15.0f;
									block.Get<PhysicsBlock>().AngularVelocity.Value = new Vector3(((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f);
									main.Add(block);
								}
							}
						}
					}
					if (removals.Count > 0)
					{
						map.Empty(removals);
						map.Regenerate();
					}
				}
			};

			// Keep the player glued to the wall while we wall walk
			update.Add(delegate(float dt)
			{
				if (player.WallRunState.Value != Player.WallRun.None)
				{
					if (player.IsSupported || player.LinearVelocity.Value.Length() < 5.0f)
					{
						// We landed on the ground or we're going too slow to continue wall-running
						deactivateWallRun();
						return;
					}

					Vector3 pos = transform.Position + new Vector3(0, (player.Height * -0.5f) - 0.5f, 0);
					Map.Coordinate coord = wallRunMap.GetCoordinate(pos);
					if (wallRunMap[coord.Move(wallDirection, 2)].ID == 0) // We ran out of wall to walk on
					{
						deactivateWallRun();
						return;
					}
					Vector3 coordPos = wallRunMap.GetAbsolutePosition(coord);

					Vector3 normal = wallRunMap.GetAbsoluteVector(wallDirection.GetVector());
					// Equation of a plane
					// normal (dot) point = d
					float d = Vector3.Dot(normal, coordPos);
					
					// Distance along the normal to keep the player glued to the wall
					float snapDistance = d - Vector3.Dot(pos, normal);

					transform.Position.Value += normal * snapDistance;

					player.LinearVelocity.Value += new Vector3(0, 10.0f * dt, 0);

					Matrix rotationMatrix = Matrix.CreateRotationY(rotation);
					Vector3 forward = -rotationMatrix.Forward;
					Vector3 right = rotationMatrix.Right;
					breakWalls(forward, right, true);
				}
			});

			// Aiming / selection

			Action tryLevitate = null, stopLevitate = null, delevitateMap = null;

			Property<bool> aimMode = new Property<bool> { Value = false };

			addInput(settings.Aim, InputState.Down, delegate()
			{
				Entity p = pistol.Value.Target;
				if (p != null && p.GetProperty<bool>("Active"))
				{
					if (!model.IsPlaying("Aim") && !player.Crouched && !model.IsPlaying("PlayerReload") && (player.IsSupported || player.IsSwimming) && p.GetProperty<int>("Ammo") > 0)
					{
						model.StartClip("Aim", 2, true);
						Sound.PlayCue(main, "Pistol Scope In");

						if (zoomAnimation != null)
							zoomAnimation.Delete.Execute();
						zoomAnimation = new Animation(new Animation.FloatMoveToSpeed(main.Camera.FieldOfView, MathHelper.ToRadians(50.0f), 2.0f));
						result.Add(zoomAnimation);
					}
				}
				else if (levitationMode)
				{
					if (player.IsSupported)
						tryLevitate();
				}
				else
					aimMode.Value = true;
			});

			Func<Vector3, Vector3> getPrecisionJumpVelocity = delegate(Vector3 target)
			{
				Vector3 horizontalVelocity = target - transform.Position;
				float verticalDistance = horizontalVelocity.Y + 1.5f;
				horizontalVelocity.Y = 0.0f;

				float horizontalDistance = horizontalVelocity.Length();

				Vector3 currentVelocity = player.LinearVelocity;
				currentVelocity.Y = 0.0f;

				float speed = Vector3.Dot(currentVelocity, horizontalVelocity) > 0.0f ? currentVelocity.Length() * 1.25f : 0.0f;
				if (speed == 0.0f)
					speed = player.MaxSpeed * 0.75f;

				horizontalVelocity *= speed / horizontalDistance;

				float time = horizontalDistance / speed;

				Vector3 velocity = new Vector3(horizontalVelocity.X, (verticalDistance / time) - (0.5f * main.Space.ForceUpdater.Gravity.Y * time), horizontalVelocity.Z);
				return velocity;
			};

			Func<Vector3, bool> canJump = delegate(Vector3 v)
			{
				return v.Y < (player.IsSupported ? player.JumpSpeed * 1.5f : player.LinearVelocity.Value.Y + player.JumpSpeed * 0.25f);
			};

			Func<Vector3, Vector3> normalizeJumpVelocity = delegate(Vector3 v)
			{
				float vertical = Math.Min(v.Y, player.JumpSpeed * 1.75f);
				v.Y = 0.0f;
				float horizontal = v.Length();
				if (horizontal > player.JumpSpeed)
					v *= player.JumpSpeed / horizontal;
				v.Y = vertical;
				return v;
			};

			Map.GlobalRaycastResult aimRaycastResult = new Map.GlobalRaycastResult();
			Property<bool> targetWithinPrecisionJumpRange = new Property<bool>();
			Property<bool> targetWithinBuildJumpRange = new Property<bool>();
			const int buildJumpLength = 6;
			buildReticle.Add(new Binding<Vector3, bool>(buildReticle.Color, x => x ? new Vector3(2.0f) : new Vector3(2.0f, 0.0f, 0.0f), targetWithinBuildJumpRange));
			jumpReticle.Add(new Binding<Vector3, bool>(jumpReticle.Color, x => x ? new Vector3(2.0f) : new Vector3(2.0f, 0.0f, 0.0f), targetWithinPrecisionJumpRange));
			update.Add(delegate(float dt)
			{
				if (aimMode || levitationMode)
					aimRaycastResult = Map.GlobalRaycast(main.Camera.Position.Value, main.Camera.Forward, main.Camera.FarPlaneDistance);

				if (aimMode)
				{
					if (aimRaycastResult.Map != null)
					{
						Vector3 normal = aimRaycastResult.Map.GetAbsoluteVector(aimRaycastResult.Normal.GetVector());

						targetWithinPrecisionJumpRange.Value = canJump(getPrecisionJumpVelocity(aimRaycastResult.Position));
						targetWithinBuildJumpRange.Value = canJump(getPrecisionJumpVelocity(aimRaycastResult.Position + (normal * buildJumpLength)));

						buildReticle.Enabled.Value = !player.IsLevitating && player.EnableBlockBuild;
						jumpReticle.Enabled.Value = !player.IsLevitating && player.EnableAim;

						Matrix matrix = Matrix.Identity;
						matrix.Translation = aimRaycastResult.Position;
						matrix.Forward = -normal;
						if (normal.Equals(Vector3.Up))
							matrix.Right = Vector3.Right;
						else if (normal.Equals(Vector3.Down))
							matrix.Right = Vector3.Left;
						else
							matrix.Right = Vector3.Normalize(Vector3.Cross(normal, Vector3.Up));
						matrix.Up = Vector3.Cross(normal, matrix.Right);
						buildReticle.Transform.Value = matrix;
						jumpReticle.Transform.Value = matrix;
					}
					else
					{
						buildReticle.Enabled.Value = false;
						jumpReticle.Enabled.Value = false;
						targetWithinPrecisionJumpRange.Value = false;
						targetWithinBuildJumpRange.Value = false;
					}
				}
				else
				{
					buildReticle.Enabled.Value = false;
					jumpReticle.Enabled.Value = false;
					targetWithinPrecisionJumpRange.Value = false;
					targetWithinBuildJumpRange.Value = false;
				}
			});

			addInput(settings.TogglePistol, InputState.Down, delegate()
			{
				if (model.IsPlaying("Aim") || model.IsPlaying("PlayerReload"))
					return;

				Entity p = pistol.Value.Target;
				if (p != null)
				{
					Property<bool> pistolActive = p.GetProperty<bool>("Active");
					pistolActive.Value = !pistolActive;
					if (pistolActive)
						aimMode.Value = false;
				}
			});

			addInput(settings.ToggleLevitate, InputState.Down, delegate()
			{
				if (model.IsPlaying("Aim") || model.IsPlaying("PlayerReload") || !player.EnableLevitation)
					return;

				levitationMode.Value = !levitationMode;
			});

			float levitateButtonPressStart = -1.0f;
			DynamicMap levitatingMap = null;

			addInput(settings.Aim, InputState.Up, delegate()
			{
				if (aimMode)
					aimMode.Value = false;
				else if (player.IsLevitating)
				{
					if (main.TotalTime - levitateButtonPressStart < 0.25f)
					{
						// De-levitate the map
						levitateButtonPressStart = -1.0f;
						delevitateMap();
					}

					// Whether the map is still floating or not, we are not controlling it anymore.
					stopLevitate();
				}
				else
					scopeOutPistol();
			});

			result.Add(new CommandBinding(result.Delete, delegate()
			{
				main.Camera.FieldOfView.Value = MathHelper.ToRadians(80.0f);
			}));

			// Fall damage
			Vector3 playerLastVelocity = Vector3.Zero;
			const float damageVelocity = 20.0f; // Vertical velocity above which damage occurs

			Action<float> fallDamage = delegate(float verticalVelocity)
			{
				if (!model.IsPlaying("Roll") && verticalVelocity < -damageVelocity)
					player.Health.Value += (verticalVelocity + damageVelocity) * 0.2f;
			};

			update.Add(delegate(float dt)
			{
				if (player.IsSupported)
				{
					// Damage the player if they fall too hard and they're not smashing or rolling
					fallDamage(playerLastVelocity.Y - player.LinearVelocity.Value.Y);
				}
				playerLastVelocity = player.LinearVelocity;
			});

			// Function for finding a platform to build for the player
			Direction[] buildableDirections = DirectionExtensions.HorizontalDirections.Union(new[] { Direction.NegativeY }).ToArray();
			Func<Vector3, BlockPossibility> findPlatform = delegate(Vector3 position)
			{
				const int searchDistance = 20;
				const int platformSize = 3;

				int shortestDistance = searchDistance;
				Direction relativeShortestDirection = Direction.None, absoluteShortestDirection = Direction.None;
				Map.Coordinate shortestCoordinate = new Map.Coordinate();
				Map shortestMap = null;

				foreach (Map map in Map.ActiveMaps)
				{
					List<Matrix> results = new List<Matrix>();
					Map.CellState fillValue = WorldFactory.StatesByName["Temporary"];
					Map.Coordinate absolutePlayerCoord = map.GetCoordinate(position);
					bool inMap = map.GetChunk(absolutePlayerCoord, false) != null;
					foreach (Direction absoluteDir in buildableDirections)
					{
						Map.Coordinate playerCoord = absoluteDir == Direction.NegativeY ? absolutePlayerCoord : map.GetCoordinate(position + new Vector3(0, platformSize / -2.0f, 0));
						Direction relativeDir = map.GetRelativeDirection(absoluteDir);
						if (!inMap && map.GetChunk(playerCoord.Move(relativeDir, searchDistance), false) == null)
							continue;

						for (int i = 1; i < shortestDistance; i++)
						{
							Map.Coordinate coord = playerCoord.Move(relativeDir, i);
							Map.CellState state = map[coord];
							if (state.ID != 0 || animatingBlocks.ContainsKey(new AnimatingBlock { Map = map, Coord = coord, State = fillValue }))
							{
								shortestDistance = i;
								relativeShortestDirection = relativeDir;
								absoluteShortestDirection = absoluteDir;
								shortestCoordinate = playerCoord;
								shortestMap = map;
								break;
							}
						}
					}
				}

				if (shortestMap != null && shortestDistance > 1)
				{
					Direction yDir = relativeShortestDirection.IsParallel(Direction.PositiveY) ? Direction.PositiveX : Direction.PositiveY;
					Direction zDir = relativeShortestDirection.Cross(yDir);

					int initialOffset = absoluteShortestDirection == Direction.NegativeY ? 0 : -2;
					Map.Coordinate startCoord = shortestCoordinate.Move(relativeShortestDirection, initialOffset).Move(yDir, platformSize / -2).Move(zDir, platformSize / -2);
					Map.Coordinate endCoord = startCoord.Move(relativeShortestDirection, -initialOffset + shortestDistance).Move(yDir, platformSize).Move(zDir, platformSize);

					return new BlockPossibility
					{
						Map = shortestMap,
						StartCoord = new Map.Coordinate { X = Math.Min(startCoord.X, endCoord.X), Y = Math.Min(startCoord.Y, endCoord.Y), Z = Math.Min(startCoord.Z, endCoord.Z) },
						EndCoord = new Map.Coordinate { X = Math.Max(startCoord.X, endCoord.X), Y = Math.Max(startCoord.Y, endCoord.Y), Z = Math.Max(startCoord.Z, endCoord.Z) },
					};
				}
				return null;
			};

			// Wall-run
			addInput(settings.WallRun, InputState.Down, delegate()
			{
				if (model.IsPlaying("Aim") || model.IsPlaying("PlayerReload") || !player.EnableWallRun)
					return;

				// Try to wall-run
				bool wallRan = false;
				if (!(wallRan = activateWallRun(false))) // Left side
					wallRan = activateWallRun(true); // Right side

				if (!wallRan)
				{
					if (player.IsSupported && player.EnableSprint)
						player.Sprint.Value = true; // Start sprinting
					else if (!player.IsSupported)
					{
						// Go into slow-mo and show block possibilities
						player.SlowMotion.Value = true;

						clearBlockPossibilities();

						Vector3 startPosition = transform.Position + new Vector3(0, (player.Height * -0.5f) - player.SupportHeight, 0);

						Vector3 straightAhead = Matrix.CreateRotationY(rotation).Forward * -player.MaxSpeed;

						Vector3 velocity = player.LinearVelocity;
						if (velocity.Length() < player.MaxSpeed * 0.25f)
							velocity += straightAhead * 0.5f;

						Queue<Prediction> predictions = new Queue<Prediction>();

						Action<Vector3, Vector3, int> addJump = delegate(Vector3 start, Vector3 v, int level)
						{
							for (float time = 0.6f; time < (level == 0 ? 1.5f : 1.0f); time += 0.6f)
								predictions.Enqueue(new Prediction { Position = start + (v * time) + (time * time * 0.5f * main.Space.ForceUpdater.Gravity), Level = level });
						};

						Vector3 jumpVelocity = velocity;
						jumpVelocity.Y = player.JumpSpeed;

						addJump(startPosition, velocity, 0);

						while (predictions.Count > 0)
						{
							Prediction prediction = predictions.Dequeue();
							BlockPossibility possibility = findPlatform(prediction.Position);
							if (possibility != null)
							{
								addBlockPossibility(possibility);
								if (prediction.Level == 0)
									addJump(prediction.Position, jumpVelocity, prediction.Level + 1);
							}
						}
					}
				}
			});

			addInput(settings.WallRun, InputState.Up, delegate()
			{
				deactivateWallRun();
				player.SlowMotion.Value = false;
				player.Sprint.Value = false;
			});

			// Hurt the player if they hit something too hard
			result.Add(new CommandBinding<BEPUphysics.Collidables.Collidable, ContactCollection>(player.Collided, delegate(BEPUphysics.Collidables.Collidable other, ContactCollection contacts)
			{
				if (other.Tag is DynamicMap)
				{
					float force = contacts[contacts.Count - 1].NormalImpulse;
					const float threshold = 16.0f;
					if (force > threshold + 4.0f)
						player.Health.Value -= (force - threshold) * 0.04f;
				}
			}));

			Updater vaultMover = null;

			float kickOrRollEnded = -1.0f;

			Action<Map, Map.Coordinate, Vector3> vault = delegate(Map map, Map.Coordinate coord, Vector3 forward)
			{
				rotationLocked.Value = true;
				player.EnableWalking.Value = false;
				player.Crouched.Value = true;
				player.AllowUncrouch.Value = false;
				const float vaultVerticalSpeed = 8.0f;
				const float maxVaultTime = 1.5f;
				float vaultTime = 0.0f;
				if (vaultMover != null)
					vaultMover.Delete.Execute(); // If we're already vaulting, start a new vault
				vaultMover = new Updater
				{
					delegate(float dt)
					{
						vaultTime += dt;

						bool delete = false;

						if (player.IsSupported || vaultTime > maxVaultTime) // Max vault time ensures we never get stuck
							delete = true;
						else if (transform.Position.Value.Y + (player.Height * -0.5f) - player.SupportHeight > map.GetAbsolutePosition(coord).Y + 0.5f) // Move forward
						{
							Vector3 velocity = player.Body.LinearVelocity; // Stop moving upward, start moving forward
							velocity.Y = 0.0f;
							velocity += forward * player.MaxSpeed;
							player.LinearVelocity.Value = velocity;
							player.Body.ActivityInformation.Activate();
							delete = true;
						}
						else
							player.LinearVelocity.Value = new Vector3(0, vaultVerticalSpeed, 0.0f);

						if (delete)
						{
							player.AllowUncrouch.Value = true;
							vaultMover.Delete.Execute(); // Make sure we get rid of this vault mover
							vaultMover = null;
							result.Add(new Animation
							(
								new Animation.Delay(0.25f),
								new Animation.Set<bool>(rotationLocked, false),
								new Animation.Set<bool>(player.EnableWalking, true)
							));
						}
					}
				};
				result.RemoveComponent("VaultMover");
				result.Add("VaultMover", vaultMover);
				player.LinearVelocity.Value = new Vector3(0, vaultVerticalSpeed, 0);
				player.IsSupported.Value = false;
				player.HasTraction.Value = false;
			};

			Func<bool, bool> jump = delegate(bool allowVault)
			{
				if (model.IsPlaying("Aim") || model.IsPlaying("PlayerReload") || player.Crouched)
					return false;

				bool supported = player.IsSupported;

				// Check if we're vaulting
				Matrix matrix = Matrix.CreateRotationY(rotation);
				bool vaulting = false;
				if (allowVault)
				{
					foreach (Map map in Map.ActiveMaps)
					{
						Direction up = map.GetRelativeDirection(Direction.PositiveY);
						Vector3 pos = transform.Position + matrix.Forward * -1.75f;
						Map.Coordinate coord = map.GetCoordinate(pos).Move(up, 1);
						for (int i = 0; i < 4; i++)
						{
							Map.Coordinate downCoord = coord.Move(up.GetReverse());

							if (map[coord].ID == 0 && map[downCoord].ID != 0)
							{
								// Vault
								vault(map, coord, -matrix.Forward);
								vaulting = true;
								break;
							}
							coord = coord.Move(up.GetReverse());
						}
						if (vaulting)
							break;
					}
				}

				Vector2 jumpDirection = player.MovementDirection;

				bool wallJumping = false;

				const float wallJumpHorizontalVelocityAmount = 0.75f;
				const float wallJumpDistance = 2.0f;

				Action<Map, Vector3> wallJump = delegate(Map wallJumpMap, Vector3 absoluteWallNormal)
				{
					wallJumping = true;
					// Set up wall jump velocity
					Vector2 wallNormal2 = new Vector2(absoluteWallNormal.X, absoluteWallNormal.Z);
					wallNormal2.Normalize();

					jumpDirection = new Vector2(-matrix.Forward.X, -matrix.Forward.Z);

					float dot = Vector2.Dot(wallNormal2, jumpDirection);
					if (dot < 0)
						jumpDirection = jumpDirection - (2.0f * Vector2.Dot(jumpDirection, wallNormal2) * wallNormal2);
					jumpDirection *= wallJumpHorizontalVelocityAmount;
					if (Math.Abs(dot) < 0.5f)
					{
						// If we're jumping perpendicular to the wall, add some velocity so we jump away from the wall a bit
						jumpDirection += wallJumpHorizontalVelocityAmount * 0.75f * wallNormal2;
					}
				};

				if (!vaulting && !supported && player.WallRunState.Value == Player.WallRun.None)
				{
					// We're not vaulting, not doing our normal jump, and not wall-walking
					// See if we can wall-jump
					float r = rotation;
					Vector3 playerPos = transform.Position;
					float closestWall = wallJumpDistance;
					Map.GlobalRaycastResult? wallRaycastHit = null;
					Vector3 wallRaycastDirection = Vector3.Zero;
					for (int i = 0; i < 4; i++)
					{
						float r2 = r + (i * (float)Math.PI * 0.5f);
						Vector3 dir = new Vector3((float)Math.Cos(r2), 0, (float)Math.Sin(r2));
						Map.GlobalRaycastResult hit = Map.GlobalRaycast(playerPos, dir, closestWall);
						if (hit.Map != null)
						{
							wallRaycastDirection = dir;
							wallRaycastHit = hit;
						}
					}
					if (wallRaycastHit != null)
					{
						wallJumping = true;
						Map m = wallRaycastHit.Value.Map;
						wallJump(m, m.GetAbsoluteVector(m.GetRelativeDirection(wallRaycastDirection).GetReverse().GetVector()));
					}
				}

				// If we're wall-running, add some velocity so we jump away from the wall a bit
				if (player.WallRunState.Value != Player.WallRun.None)
				{
					lastWallRunJump = main.TotalTime; // Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.
					Matrix r = Matrix.CreateRotationY(rotation);
					jumpDirection += wallJumpHorizontalVelocityAmount * 0.75f * (player.WallRunState.Value == Player.WallRun.Left ? new Vector2(r.Left.X, r.Left.Z) : new Vector2(r.Right.X, r.Right.Z));
				}

				bool go = supported || player.WallRunState.Value != Player.WallRun.None || vaulting || wallJumping;

				if (!go)
				{
					// Check block possibilities beneath us
					Vector3 jumpPos = transform.Position + new Vector3(0, player.Height * -0.5f - player.SupportHeight - 1.0f, 0);
					foreach (BlockPossibility possibility in blockPossibilities.Values.SelectMany(x => x))
					{
						if (possibility.Map.GetCoordinate(jumpPos).Between(possibility.StartCoord, possibility.EndCoord)
							&& !possibility.Map.GetCoordinate(jumpPos + new Vector3(2.0f)).Between(possibility.StartCoord, possibility.EndCoord))
						{
							instantiateBlockPossibility(possibility);
							go = true;
							break;
						}
					}
				}

				if (!go)
				{
					// Check block possibilities for vaulting
					foreach (BlockPossibility possibility in blockPossibilities.Values.SelectMany(x => x))
					{
						Direction up = possibility.Map.GetRelativeDirection(Direction.PositiveY);
						Vector3 pos = transform.Position + matrix.Forward * -1.75f;
						Map.Coordinate coord = possibility.Map.GetCoordinate(pos).Move(up, 1);
						for (int i = 0; i < 4; i++)
						{
							Map.Coordinate downCoord = coord.Move(up.GetReverse());
							if (!coord.Between(possibility.StartCoord, possibility.EndCoord) && downCoord.Between(possibility.StartCoord, possibility.EndCoord))
							{
								instantiateBlockPossibility(possibility);
								vault(possibility.Map, coord, -matrix.Forward);
								vaulting = true;
								go = true;
								break;
							}
							coord = coord.Move(up.GetReverse());
						}
						if (vaulting)
							break;
					}
				}

				if (!go)
				{
					// Check block possibilities for wall jumping
					float r = rotation;
					Vector3 playerPos = transform.Position;
					foreach (BlockPossibility possibility in blockPossibilities.Values.SelectMany(x => x))
					{
						for (int i = 0; i < 4; i++)
						{
							float r2 = r + (i * (float)Math.PI * 0.5f);
							Vector3 dir = new Vector3((float)Math.Cos(r2), 0, (float)Math.Sin(r2));
							foreach (Map.Coordinate coord in possibility.Map.Rasterize(playerPos, playerPos + (dir * wallJumpDistance)))
							{
								if (coord.Between(possibility.StartCoord, possibility.EndCoord))
								{
									instantiateBlockPossibility(possibility);
									wallJump(possibility.Map, possibility.Map.GetAbsoluteVector(possibility.Map.GetRelativeDirection(dir).GetReverse().GetVector()));
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

				bool precisionJumping = aimMode && player.EnableAim && targetWithinPrecisionJumpRange;

				if (go || precisionJumping)
				{
					if (!supported && !precisionJumping)
					{
						// We haven't hit the ground, so fall damage will not be handled by the physics system.
						// Need to do it manually here.
						fallDamage(player.LinearVelocity.Value.Y);
					}

					if (!vaulting)
					{
						if (precisionJumping)
						{
							// Make the player jump exactly to the target.
							Vector3 velocity = normalizeJumpVelocity(getPrecisionJumpVelocity(aimRaycastResult.Position));
							if (velocity.Y < 0.0f && supported)
								return false; // Can't jump down through the floor
							player.LinearVelocity.Value = velocity;
						}
						else
						{
							// Just a normal jump.
							Vector3 velocity = player.LinearVelocity;
							velocity.Y = 0.0f;
							float jumpSpeed = jumpDirection.Length();
							if (jumpSpeed > 0)
								jumpDirection *= (wallJumping ? player.MaxSpeed : velocity.Length()) / jumpSpeed;

							float totalMultiplier = 1.0f;
							float verticalMultiplier = 1.0f;

							if (main.TotalTime - kickOrRollEnded < 0.3f)
								totalMultiplier *= 1.5f;

							if (player.Sprint)
								verticalMultiplier *= 1.2f;

							player.LinearVelocity.Value = new Vector3(jumpDirection.X, player.JumpSpeed * verticalMultiplier, jumpDirection.Y) * totalMultiplier;
						}

						player.IsSupported.Value = false;
						player.HasTraction.Value = false;
					}

					// Deactivate any wall walking we're doing
					deactivateWallRun();

					Sound.PlayCue(main, vaulting ? "Vault" : "Jump", transform.Position);

					model.Stop("Vault", "Jump");
					model.StartClip(vaulting ? "Vault" : "Jump", 4);

					// Play a footstep sound since we're jumping off the ground
					footsteps.Play.Execute();

					return true;
				}

				return false;
			};

			// Jumping / vaulting
			addInput(settings.Jump, InputState.Down, delegate()
			{
				jump(true); // true = allow vaulting
			});

			NotifyBinding pistolActiveBinding = null;

			if (pistol.Value.Target == null)
			{
				result.Add(new PostInitialization
				{
					(Action)pistol.Reset
				});
			}

			// Pistol
			pistol.Set = delegate(Entity.Handle value)
			{
				if (pistolActiveBinding != null)
					result.Remove(pistolActiveBinding);
				Entity pistolEntity = pistol.InternalValue.Target;

				if (pistolEntity != null && pistolEntity != value.Target)
					pistolEntity.GetCommand("Detach").Execute();

				model.Stop("Draw", "Aim");

				pistol.InternalValue = value;
				pistolEntity = value.Target;
				if (pistolEntity != null)
				{
					pistolEntity.GetCommand<Property<Matrix>>("Attach").Execute(model.GetWorldBoneTransform("Pistol"));

					Property<bool> pistolActive = pistolEntity.GetProperty<bool>("Active");

					Action updatePistolAnimation = delegate()
					{
						if (pistolActive)
						{
							model.StartClip("Draw", 1, true);
							levitationMode.Value = false;
						}
						else
							model.Stop("Draw");
						aimAnimationBlend = aimAnimationBlendTotal;
					};
					updatePistolAnimation();
					pistolActiveBinding = new NotifyBinding(updatePistolAnimation, pistolActive);
					result.Add(pistolActiveBinding);
				}
			};

			float lastFire = 0.0f;
			const float fireInterval = 0.15f;
			addInput(settings.FireBuildRoll, InputState.Down, delegate()
			{
				if ((zoomAnimation == null || !zoomAnimation.Active) && main.TotalTime - lastFire > fireInterval && model.IsPlaying("Aim"))
				{
					if (pistol.Value.Target.GetProperty<int>("Ammo") > 0)
					{
						// Fire pistol
						lastFire = main.TotalTime;
						fire = true;
					}
				}
				else if (aimMode && aimRaycastResult.Map != null && targetWithinBuildJumpRange && player.EnableBlockBuild && jump(false))
				{
					// Do a build/jump move

					Direction yDir = aimRaycastResult.Normal.IsParallel(Direction.PositiveY) ? Direction.PositiveX : Direction.PositiveY;
					Direction zDir = aimRaycastResult.Normal.Cross(yDir);

					const int size = 3;

					bool quit = false;

					Map.CellState fillValue = WorldFactory.StatesByName["Temporary"];

					List<AnimatingBlock> blockSpawnList = new List<AnimatingBlock>();

					Map.Coordinate x = aimRaycastResult.Coordinate.Value.Move(aimRaycastResult.Normal);
					int xi = 0;
					while (xi < buildJumpLength && !quit)
					{
						int yi = 0;
						Map.Coordinate y = x.Move(yDir, size / -2);
						while (yi < size && !quit)
						{
							int zi = 0;
							Map.Coordinate z = y.Move(zDir, size / -2);
							while (zi < size)
							{
								Vector3 pos = aimRaycastResult.Map.GetAbsolutePosition(z);

								foreach (Map map in Map.ActiveMaps)
								{
									if (map != aimRaycastResult.Map && map[pos].ID != 0)
									{
										quit = true;
										break;
									}
								}
								if (quit)
									break;
								else
								{
									blockSpawnList.Add(new AnimatingBlock { Map = aimRaycastResult.Map, Coord = z, State = fillValue });
									zi++;
									z = z.Move(zDir);
								}
							}
							y = y.Move(yDir);
							yi++;
						}
						x = x.Move(aimRaycastResult.Normal);
						xi++;
					}

					if (buildBlocks(blockSpawnList, false))
					{
						player.Stamina.Value -= blockInstantiationStaminaCost;
						Map.Coordinate targetCoord = x;
						if (aimRaycastResult.Map.GetAbsoluteDirection(aimRaycastResult.Normal) != Direction.PositiveY)
							targetCoord = targetCoord.Move(aimRaycastResult.Normal.GetReverse(), 2);
						x = x.Move(aimRaycastResult.Map.GetRelativeDirection(Direction.PositiveY));
						player.LinearVelocity.Value = normalizeJumpVelocity(getPrecisionJumpVelocity(aimRaycastResult.Map.GetAbsolutePosition(targetCoord)));
					}
				}
				else if (!aimMode && !input.GetInput(settings.Aim) && !model.IsPlaying("Aim") && !model.IsPlaying("PlayerReload") && !model.IsPlaying("Roll") && player.EnableRoll)
				{
					// Try to roll or kick
					Vector3 playerPos = transform.Position + new Vector3(0, (player.Height * -0.5f) - player.SupportHeight, 0);

					Matrix rotationMatrix = Matrix.CreateRotationY(rotation);
					Vector3 forward = -rotationMatrix.Forward;
					Vector3 right = rotationMatrix.Right;

					bool nearGround = player.IsSupported || Map.GlobalRaycast(playerPos, Vector3.Down, player.Height).Map != null;

					bool instantiatedBlockPossibility = false;

					if (!nearGround)
					{
						// Check for block possibilities
						foreach (BlockPossibility block in blockPossibilities.Values.SelectMany(x => x))
						{
							bool first = true;
							foreach (Map.Coordinate coord in block.Map.Rasterize(playerPos + Vector3.Up * 2.0f, playerPos + (Vector3.Down * (player.Height + 1.0f))))
							{
								if (coord.Between(block.StartCoord, block.EndCoord))
								{
									if (first)
										break; // If the top coord is intersecting the possible block, we're too far down into the block. Need to be at the top.
									instantiateBlockPossibility(block);
									instantiatedBlockPossibility = true;
									nearGround = true;
									break;
								}
								first = false;
							}
							if (nearGround)
								break;
						}
					}

					bool kicking = false;

					if (!instantiatedBlockPossibility && canKick && !player.IsSupported && player.EnableKick && player.LinearVelocity.Value.Y > -5.0f)
					{
						// Try to kick
						Map.GlobalRaycastResult forwardRaycast = Map.GlobalRaycast(transform.Position, forward, 7.0f);
						if (forwardRaycast.Map != null && !forwardRaycast.Map[forwardRaycast.Coordinate.Value].Permanent)
						{
							// We're aiming at a wall we can kick through.
							// Do it!
							kicking = true;
							canKick = false;
							nearGround = false; // We're not rolling anymore

							player.LinearVelocity.Value += forward * player.LinearVelocity.Value.Length() * 0.5f + new Vector3(0, player.JumpSpeed * 0.25f, 0);

							result.Add(new Animation
							(
								new Animation.Delay(0.25f),
								new Animation.Execute(delegate() { Sound.PlayCue(main, "Kick", transform.Position); })
							));
							model.StartClip("Kick", 5, false);

							Updater kickUpdate = null;
							float kickTime = 0.0f;
							kickUpdate = new Updater
							{
								delegate(float dt)
								{
									kickTime += dt;
									if (kickTime > 0.75f || player.LinearVelocity.Value.Length() < 0.1f)
									{
										kickUpdate.Delete.Execute();
										model.Stop("Kick");
										player.EnableWalking.Value = true;
										rotationLocked.Value = false;
										kickOrRollEnded = main.TotalTime;
									}
									else
										breakWalls(forward, right, true);
								}
							};
							result.Add(kickUpdate);
						}
					}

					if (nearGround || kicking)
					{
						// We're rolling or kicking.
						deactivateWallRun();

						model.Stop
						(
							"CrouchWalkBackwards",
							"CrouchWalk",
							"CrouchStrafeRight",
							"CrouchStrafeLeft",
							"Idle",
							"WalkBackwards",
							"Walk",
							"StrafeRight",
							"StrafeLeft",
							"Jump"
						);
						model.StartClip("CrouchIdle", 2, true);

						player.EnableWalking.Value = false;
						rotationLocked.Value = true;
					}

					if (nearGround)
					{
						// We're rolling
						
						footstepTimer.Command.Execute(); // We just landed; play a footstep sound
						Sound.PlayCue(main, "Skill Roll", playerPos);

						model.StartClip("Roll", 5, false);

						// If the player is not yet supported, that means they're just about to land.
						// So give them a little speed boost for having such good timing.
						Vector3 velocity = forward * player.MaxSpeed * (player.IsSupported ? 0.75f : 1.25f);
						player.LinearVelocity.Value = new Vector3(velocity.X, 0.0f, velocity.Z);

						// Crouch
						player.Crouched.Value = true;
						player.AllowUncrouch.Value = false;

						Updater rollUpdate = null;
						float rollTime = 0.0f;
						rollUpdate = new Updater
						{
							delegate(float dt)
							{
								rollTime += dt;

								// Stop if we're about to roll off the edge of an instaniated block possibility.
								bool stop = instantiatedBlockPossibility && rollTime > 0.1f && Map.GlobalRaycast(transform.Position + forward * 0.5f, Vector3.Down, player.Height * 0.5f + player.SupportHeight + 1.1f).Map != null;

								if (stop || rollTime > 1.0f || player.LinearVelocity.Value.Length() < 0.1f)
								{
									rollUpdate.Delete.Execute();
									player.EnableWalking.Value = true;
									player.AllowUncrouch.Value = true;
									rotationLocked.Value = false;
									kickOrRollEnded = main.TotalTime;

									if (stop) // Stop from rolling off the edge
										player.LinearVelocity.Value = new Vector3(0, player.LinearVelocity.Value.Y, 0);
								}
								else
								{
									player.LinearVelocity.Value = new Vector3(velocity.X, player.LinearVelocity.Value.Y, velocity.Z);
									breakWalls(forward, right, false);
								}
							}
						};
						result.Add(rollUpdate);
						return;
					}
				}
			});

			result.Add(new CommandBinding(result.Delete, delegate()
			{
				if (pistol.Value.Target != null)
					pistol.Value.Target.GetCommand("Detach").Execute();
			}));

			// Reload
			addInput(settings.Reload, InputState.Down, delegate()
			{
				Entity p = pistol.Value.Target;
				if (p != null && p.GetProperty<bool>("Active"))
				{
					if (!model.IsPlaying("Aim") && !model.IsPlaying("PlayerReload") && (player.IsSupported || player.IsSwimming) && p.GetProperty<int>("Magazines") > 0)
					{
						p.GetCommand("Reload").Execute();
						model.StartClip("PlayerReload", 6);
					}
				}
			});

			// Levitate
			const float levitationMaxDistance = 25.0f;
			const int levitateStaminaCost = 8;
			const int levitateRipStaminaCost = 6; // In addition to the regular levitate cost
			const int levitateRipRadius = 5;
			Vector3 levitationRelativeGrabPoint = Vector3.Zero;
			float levitatingDistance = 0.0f;
			PointLight levitatingLight = null;

			Action<DynamicMap, Vector3> toggleLevitate = delegate(DynamicMap map, Vector3 grabPoint)
			{
				levitatingMap = map;
				levitatingDistance = (grabPoint - main.Camera.Position).Length();
				levitationRelativeGrabPoint = map.GetRelativePosition(grabPoint);
				player.IsLevitating.Value = true;

				model.StartClip("Levitating", 2, true);

				levitatingLight = new PointLight();
				levitatingLight.Serialize = false;
				levitatingLight.Position.Value = grabPoint;
				levitatingLight.Color.Value = new Vector3(0.0f, 1.0f, 2.0f);
				levitatingLight.Attenuation.Value = 20.0f;
				levitatingLight.Shadowed.Value = false;
				result.Add(levitatingLight);

				if (map.IsAffectedByGravity)
				{
					// This map is just now starting to levitate
					map.IsAffectedByGravity.Value = false;
					Sound.PlayCue(main, "Levitate", grabPoint);
					map.PhysicsEntity.LinearVelocity += new Vector3(0.0f, 1.0f, 0.0f);
					player.Stamina.Value -= levitateStaminaCost;
				}
				else
					levitateButtonPressStart = main.TotalTime;
			};

			ParticleEmitter levitationParticleEmitter = result.GetOrCreate<ParticleEmitter>("Levitation");
			levitationParticleEmitter.ParticlesPerSecond.Value = 100;
			levitationParticleEmitter.ParticleType.Value = "DistortionSmall";
			levitationParticleEmitter.Add(new Binding<Vector3>(levitationParticleEmitter.Position, handPosition));
			levitationParticleEmitter.Add(new Binding<bool>(levitationParticleEmitter.Enabled, levitationMode));

			Action updateLevitateAnimation = delegate()
			{
				if (levitationMode)
				{
					model.StartClip("LevitateMode", 1, true);

					Entity p = pistol.Value.Target;
					if (p != null)
						p.GetProperty<bool>("Active").Value = false; // Put the pistol away
				}
				else
					model.Stop("LevitateMode");
				aimAnimationBlend = aimAnimationBlendTotal;
			};
			updateLevitateAnimation();
			model.Add(new NotifyBinding(updateLevitateAnimation, levitationMode));
			crosshair.Add(new Binding<bool>(crosshair.Visible, () => levitationMode && !player.IsLevitating && player.IsSupported, levitationMode, player.IsLevitating, player.IsSupported));

			result.Add(new NotifyBinding(delegate()
			{
				if (!player.EnableLevitation)
				{
					levitationMode.Value = false;
					stopLevitate();
				}
			}, player.EnableLevitation));

			tryLevitate = delegate()
			{
				if (aimRaycastResult.Map != null && (aimRaycastResult.Position - transform.Position).Length() < levitationMaxDistance)
				{
					Vector3 grabPoint = aimRaycastResult.Position;
					if (aimRaycastResult.Map is DynamicMap)
						toggleLevitate((DynamicMap)aimRaycastResult.Map, grabPoint); // We're already dealing with a DynamicMap
					else if (!aimRaycastResult.Map[aimRaycastResult.Coordinate.Value].Permanent)
					{
						// It's a static map.
						// Break off a chunk of it into a new DynamicMap.
						Map.Coordinate center = aimRaycastResult.Coordinate.Value;

						List<Map.Coordinate> edges = new List<Map.Coordinate>();

						Map.Coordinate ripStart = center.Move(-levitateRipRadius, -levitateRipRadius, -levitateRipRadius);
						Map.Coordinate ripEnd = center.Move(levitateRipRadius, levitateRipRadius, levitateRipRadius);

						List<Map.Box> permanentBoxes = aimRaycastResult.Map.GetContiguousPermanentWithin
						(
							aimRaycastResult.Map.GetBox(center),
							ripStart,
							ripEnd
						);

						foreach (Map.Box b in permanentBoxes)
						{
							// Top and bottom
							for (int x = b.X - 1; x <= b.X + b.Width; x++)
							{
								for (int z = b.Z - 1; z <= b.Z + b.Depth; z++)
								{
									Map.Coordinate coord = new Map.Coordinate { X = x, Y = b.Y + b.Height, Z = z };
									if (coord.Between(ripStart, ripEnd))
										edges.Add(coord);

									coord = new Map.Coordinate { X = x, Y = b.Y - 1, Z = z };
									if (coord.Between(ripStart, ripEnd))
										edges.Add(coord);
								}
							}

							// Outer shell
							for (int y = b.Y; y < b.Y + b.Height; y++)
							{
								// Left and right
								for (int z = b.Z - 1; z <= b.Z + b.Depth; z++)
								{
									Map.Coordinate coord = new Map.Coordinate { X = b.X - 1, Y = y, Z = z };
									if (coord.Between(ripStart, ripEnd))
										edges.Add(coord);

									coord = new Map.Coordinate { X = b.X + b.Width, Y = y, Z = z };
									if (coord.Between(ripStart, ripEnd))
										edges.Add(coord);
								}

								// Backward and forward
								for (int x = b.X; x < b.X + b.Width; x++)
								{
									Map.Coordinate coord = new Map.Coordinate { X = x, Y = y, Z = b.Z - 1 };
									if (coord.Between(ripStart, ripEnd))
										edges.Add(coord);

									coord = new Map.Coordinate { X = x, Y = y, Z = b.Z + b.Depth };
									if (coord.Between(ripStart, ripEnd))
										edges.Add(coord);
								}
							}
						}

						if (edges.Contains(center))
							return;

						// Top and bottom
						for (int x = ripStart.X; x <= ripEnd.X; x++)
						{
							for (int z = ripStart.Z; z <= ripEnd.Z; z++)
							{
								edges.Add(new Map.Coordinate { X = x, Y = ripStart.Y, Z = z });
								edges.Add(new Map.Coordinate { X = x, Y = ripEnd.Y, Z = z });
							}
						}

						// Sides
						for (int y = ripStart.Y + 1; y <= ripEnd.Y - 1; y++)
						{
							// Left and right
							for (int z = ripStart.Z; z <= ripEnd.Z; z++)
							{
								edges.Add(new Map.Coordinate { X = ripStart.X, Y = y, Z = z });
								edges.Add(new Map.Coordinate { X = ripEnd.X, Y = y, Z = z });
							}

							// Backward and forward
							for (int x = ripStart.X; x <= ripEnd.X; x++)
							{
								edges.Add(new Map.Coordinate { X = x, Y = y, Z = ripStart.Z });
								edges.Add(new Map.Coordinate { X = x, Y = y, Z = ripEnd.Z });
							}
						}

						aimRaycastResult.Map.Empty(edges);
						aimRaycastResult.Map.Regenerate(delegate(List<DynamicMap> spawnedMaps)
						{
							foreach (DynamicMap spawnedMap in spawnedMaps)
							{
								if (spawnedMap[center].ID != 0)
								{
									toggleLevitate(spawnedMap, grabPoint);
									player.Stamina.Value -= levitateRipStaminaCost;
									break;
								}
							}
						});
					}
				}
			};

			update.Add(delegate(float dt)
			{
				if (levitatingMap != null)
				{
					if (!levitatingMap.Active)
					{
						stopLevitate();
						return;
					}
					Vector3 target = main.Camera.Position + (main.Camera.Forward.Value * levitatingDistance);
					levitatingLight.Position.Value = target;
					Vector3 grabPoint = levitatingMap.GetAbsolutePosition(levitationRelativeGrabPoint);
					Vector3 diff = (target - grabPoint) * 0.25f * (float)Math.Sqrt(levitatingMap.PhysicsEntity.Mass) * (1.25f - Math.Min(1.0f, ((grabPoint - transform.Position).Length() / levitationMaxDistance)));
					levitatingMap.PhysicsEntity.ApplyImpulse(ref grabPoint, ref diff);
				}
				else if (levitationMode && !player.IsLevitating)
				{
					bool canLevitate = aimRaycastResult.Map != null
						&& (aimRaycastResult.Position - transform.Position).Length() < levitationMaxDistance
						&& (aimRaycastResult.Map is DynamicMap || !aimRaycastResult.Map[aimRaycastResult.Coordinate.Value].Permanent);
					crosshair.Tint.Value = canLevitate ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Red;
				}
			});

			delevitateMap = delegate()
			{
				if (!levitatingMap.IsAffectedByGravity)
				{
					int maxDistance = levitateRipRadius + 7;
					Map closestMap = null;
					Map.Coordinate closestCoord = new Map.Coordinate();
					foreach (Map m in Map.ActiveMaps)
					{
						if (m == levitatingMap)
							continue;

						Map.Coordinate relativeCoord = m.GetCoordinate(levitatingMap.Transform.Value.Translation);
						Map.Coordinate? closestFilled = m.FindClosestFilledCell(relativeCoord, maxDistance);
						if (closestFilled != null)
						{
							maxDistance = Math.Min(Math.Abs(relativeCoord.X - closestFilled.Value.X), Math.Min(Math.Abs(relativeCoord.Y - closestFilled.Value.Y), Math.Abs(relativeCoord.Z - closestFilled.Value.Z)));
							closestMap = m;
							closestCoord = closestFilled.Value;
						}
					}
					if (closestMap != null)
					{
						// Combine this map with the other one

						Direction x = closestMap.GetRelativeDirection(levitatingMap.GetAbsoluteVector(Vector3.Right));
						Direction y = closestMap.GetRelativeDirection(levitatingMap.GetAbsoluteVector(Vector3.Up));
						Direction z = closestMap.GetRelativeDirection(levitatingMap.GetAbsoluteVector(Vector3.Backward));

						if (x.IsParallel(y))
							x = y.Cross(z);
						else if (y.IsParallel(z))
							y = x.Cross(z);

						Map.Coordinate offset = new Map.Coordinate();
						float closestCoordDistance = float.MaxValue;
						Vector3 closestCoordPosition = closestMap.GetAbsolutePosition(closestCoord);
						foreach (Map.Coordinate c in levitatingMap.Chunks.SelectMany(c => c.Boxes).SelectMany(b => b.GetCoords()))
						{
							float distance = (levitatingMap.GetAbsolutePosition(c) - closestCoordPosition).LengthSquared();
							if (distance < closestCoordDistance)
							{
								closestCoordDistance = distance;
								offset = c;
							}
						}
						Vector3 toLevitatingMap = levitatingMap.Transform.Value.Translation - closestMap.GetAbsolutePosition(closestCoord);
						offset = offset.Move(levitatingMap.GetRelativeDirection(-toLevitatingMap));

						Matrix orientation = levitatingMap.Transform.Value;
						orientation.Translation = Vector3.Zero;

						int index = 0;
						foreach (Map.Coordinate c in levitatingMap.Chunks.SelectMany(c => c.Boxes).SelectMany(b => b.GetCoords()))
						{
							Map.Coordinate offsetFromCenter = c.Move(-offset.X, -offset.Y, -offset.Z);
							Map.Coordinate targetCoord = new Map.Coordinate();
							targetCoord.SetComponent(x, offsetFromCenter.GetComponent(Direction.PositiveX));
							targetCoord.SetComponent(y, offsetFromCenter.GetComponent(Direction.PositiveY));
							targetCoord.SetComponent(z, offsetFromCenter.GetComponent(Direction.PositiveZ));
							targetCoord = targetCoord.Move(closestCoord.X, closestCoord.Y, closestCoord.Z);
							if (closestMap[targetCoord].ID == 0)
							{
								Entity block = Factory.Get<EffectBlockFactory>().CreateAndBind(main);
								c.Data.ApplyToEffectBlock(block.Get<ModelInstance>());
								block.GetProperty<Vector3>("Offset").Value = closestMap.GetRelativePosition(targetCoord);
								block.GetProperty<bool>("Scale").Value = false;
								block.GetProperty<Vector3>("StartPosition").Value = levitatingMap.GetAbsolutePosition(c);
								block.GetProperty<Matrix>("StartOrientation").Value = orientation;
								block.GetProperty<float>("TotalLifetime").Value = 0.05f + (index * 0.0075f);
								block.GetProperty<Entity.Handle>("TargetMap").Value = closestMap.Entity;
								block.GetProperty<int>("TargetCellStateID").Value = c.Data.ID;
								block.GetProperty<Map.Coordinate>("TargetCoord").Value = targetCoord;
								main.Add(block);
								index++;
							}
						}

						// Delete the map
						levitatingMap.Entity.Delete.Execute();

						player.Stamina.Value -= levitateRipStaminaCost;
					}
					else
						levitatingMap.IsAffectedByGravity.Value = true;
					Sound.PlayCue(main, "LevitateStop", aimRaycastResult.Position);
				}
			};

			stopLevitate = delegate()
			{
				model.Stop("Levitating");
				if (levitatingMap != null)
				{
					levitatingMap = null;
					player.IsLevitating.Value = false;
					result.Add(new Animation
					(
						new Animation.FloatMoveTo(levitatingLight.Attenuation, 0.0f, 1.0f),
						new Animation.Execute(levitatingLight.Delete)
					));
					levitatingLight = null;
				}
			};
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{

		}
	}
}
