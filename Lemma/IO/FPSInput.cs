using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Lemma.Util;

namespace Lemma.Components
{
	public class FPSInput : PCInput
	{
		public Property<Vector2> Movement = new Property<Vector2> { };

		public Property<PCInputBinding> ForwardKey = new Property<PCInputBinding> { Value = new PCInputBinding { Key = Keys.W } };
		public Property<PCInputBinding> BackwardKey = new Property<PCInputBinding> { Value = new PCInputBinding { Key = Keys.S } };
		public Property<PCInputBinding> LeftKey = new Property<PCInputBinding> { Value = new PCInputBinding { Key = Keys.A } };
		public Property<PCInputBinding> RightKey = new Property<PCInputBinding> { Value = new PCInputBinding { Key = Keys.D } };
		
		public Property<float> MinX = new Property<float> { };
		public Property<float> MaxX = new Property<float> { };
		public Property<float> MinY = new Property<float> { Value = (float)Math.PI * -0.5f };
		public Property<float> MaxY = new Property<float> { Value = (float)Math.PI * 0.5f };

		const float sensitivityMultiplier = 0.002f;
		public Property<bool> EnableLook = new Property<bool> { Value = true };
		public Property<bool> EnableMovement = new Property<bool> { Value = true };

		public Property<float> MouseSensitivity = new Property<float> { Value = 1.0f };
		public Property<bool> InvertMouseX = new Property<bool> { Value = false };
		public Property<bool> InvertMouseY = new Property<bool> { Value = false };

		const int mouseCenterX = 400;
		const int mouseCenterY = 400;

		public static void RecenterMouse()
		{
			Microsoft.Xna.Framework.Input.Mouse.SetPosition(FPSInput.mouseCenterX, FPSInput.mouseCenterY);
		}

		protected Vector2 lastMouseLook, lastMouseNonLook;
		public override void InitializeProperties()
		{
			base.InitializeProperties();
			this.Add(new CommandBinding(this.OnEnabled, (Action)FPSInput.RecenterMouse));
			this.Add(new CommandBinding(this.OnDisabled, delegate()
			{
				if (!this.Movement.Value.Equals(Vector2.Zero))
					this.Movement.Value = Vector2.Zero;
			}));
			this.EnableLook.Set = delegate(bool value)
			{
				bool oldValue = this.EnableLook.InternalValue;
				this.EnableLook.InternalValue = value;
				if (value && !oldValue)
				{
					this.lastMouseNonLook = this.Mouse;
					this.Mouse.Value = this.lastMouseLook;
					FPSInput.RecenterMouse();
					MouseState oldState = this.main.MouseState;
					this.main.MouseState.Value = new MouseState(FPSInput.mouseCenterX, FPSInput.mouseCenterY, oldState.ScrollWheelValue, oldState.LeftButton, oldState.MiddleButton, oldState.RightButton, oldState.XButton1, oldState.XButton2);
				}
				else if (!value && oldValue)
				{
					this.lastMouseLook = this.Mouse;
					Microsoft.Xna.Framework.Input.Mouse.SetPosition((int)this.lastMouseNonLook.X, (int)this.lastMouseNonLook.Y);
					this.Mouse.Value = this.lastMouseNonLook;
				}
			};
			this.MinX.Set = delegate(float value)
			{
				this.MinX.InternalValue = value.ToAngleRange();
			};
			this.MaxX.Set = delegate(float value)
			{
				this.MaxX.InternalValue = value.ToAngleRange();
			};
			this.MinY.Set = delegate(float value)
			{
				this.MinY.InternalValue = value.ToAngleRange();
			};
			this.MaxY.Set = delegate(float value)
			{
				this.MaxY.InternalValue = value.ToAngleRange();
			};
		}

		public override void Update(float elapsedTime)
		{
			base.Update(elapsedTime); // Calls handleMouse()

			if (!main.IsActive || !this.Enabled)
				return;

			if (this.EnableMovement)
			{
				KeyboardState keyboard = this.main.KeyboardState;
				float x = this.GetInput(this.LeftKey) ? -1.0f : (this.GetInput(this.RightKey) ? 1.0f : 0.0f);
				float y = this.GetInput(this.BackwardKey) ? -1.0f : (this.GetInput(this.ForwardKey) ? 1.0f : 0.0f);
				Vector2 newMovement = new Vector2(x, y);
				if (!this.Movement.Value.Equals(newMovement))
					this.Movement.Value = newMovement;
			}
			else if (!this.Movement.Value.Equals(Vector2.Zero))
				this.Movement.Value = Vector2.Zero;
		}

		protected override void handleMouse()
		{
			if (this.EnableLook)
			{
				MouseState mouse = this.main.MouseState;

				Vector2 mouseMovement = new Vector2(FPSInput.mouseCenterX - mouse.X, mouse.Y - FPSInput.mouseCenterY) * this.MouseSensitivity * FPSInput.sensitivityMultiplier;
				if (this.InvertMouseX)
					mouseMovement.X *= -1;
				if (this.InvertMouseY)
					mouseMovement.Y *= -1;
				if (mouseMovement.LengthSquared() > 0.0f)
				{
					Vector2 newValue = this.Mouse.Value + mouseMovement;

					newValue.X = newValue.X.ToAngleRange();
					newValue.Y = newValue.Y.ToAngleRange();

					float minX = this.MinX, maxX = this.MaxX, minY = this.MinY, maxY = this.MaxY;
					
					const float pi = (float)Math.PI;

					if (!(minX == 0 && maxX == 0))
					{
						float tempX = minX;
						minX = Math.Min(minX, maxX);
						maxX = Math.Max(tempX, maxX);

						if (minX < 0 && maxX > 0 && Math.Abs(minX + pi) + Math.Abs(maxX - pi) < Math.Abs(minX) + Math.Abs(maxX))
						{
							if (newValue.X < 0 && newValue.X > minX)
								newValue.X = minX;
							if (newValue.X > 0 && newValue.X < maxX)
								newValue.X = maxX;
						}
						else
							newValue.X = Math.Min(maxX, Math.Max(minX, newValue.X));
					}

					if (!(minY == 0 && maxY == 0))
					{
						float tempY = minY;
						minY = Math.Min(minY, maxY);
						maxY = Math.Max(tempY, maxY);

						if (minY < 0 && maxY > 0 && Math.Abs(minY + pi) + Math.Abs(maxY - pi) < Math.Abs(minY) + Math.Abs(maxY))
						{
							if (newValue.Y < 0 && newValue.Y > minY)
								newValue.Y = minY;
							if (newValue.Y > 0 && newValue.Y < maxY)
								newValue.Y = maxY;
						}
						else
							newValue.Y = Math.Min(maxY, Math.Max(minY, newValue.Y));
					}

					this.Mouse.Value = newValue;
				}
				FPSInput.RecenterMouse();
			}
			else
				base.handleMouse();
		}
	}
}
