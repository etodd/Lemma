using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace Lemma.Components
{
	public class PCInput : ComponentBind.Component<Main>, IUpdateableComponent
	{
		public enum MouseButton { None, LeftMouseButton, MiddleMouseButton, RightMouseButton }

		public enum InputState { Down, Up }

		public class PCInputBinding
		{
			public Keys Key;
			public MouseButton MouseButton;
			[DefaultValue(Buttons.BigButton)]
			public Buttons GamePadButton = Buttons.BigButton;

			public override string ToString()
			{
				string value = null;
				if (this.Key != Keys.None)
					value = "\\" + this.Key.ToString();
				else if (this.MouseButton != PCInput.MouseButton.None)
					value = "\\" + this.MouseButton.ToString();

				if (GamePad.GetState(PlayerIndex.One).IsConnected && this.GamePadButton != Buttons.BigButton)
					return "\\" + this.GamePadButton.ToString();
				else if (value != null)
					return value;
				else
					return "[?]";
			}
		}

		public Property<Vector2> Mouse = new Property<Vector2> { };
		public Property<int> ScrollWheel = new Property<int> { };
		public Property<bool> LeftMouseButton = new Property<bool> { };
		public Property<bool> MiddleMouseButton = new Property<bool> { };
		public Property<bool> RightMouseButton = new Property<bool> { };

		public Command LeftMouseButtonDown = new Command();
		public Command MiddleMouseButtonDown = new Command();
		public Command RightMouseButtonDown = new Command();
		public Command LeftMouseButtonUp = new Command();
		public Command MiddleMouseButtonUp = new Command();
		public Command RightMouseButtonUp = new Command();
		public Command<int> MouseScrolled = new Command<int>();

		public Property<bool> EnableMouse = new Property<bool> { Value = true };

		public void Bind(Property<PCInput.PCInputBinding> inputBinding, InputState state, Action action)
		{
			CommandBinding commandBinding = null;
			CommandBinding buttonBinding = null;
			Action rebindCommand = delegate()
			{
				if (commandBinding != null)
					this.Remove(commandBinding);

				if (buttonBinding != null)
					this.Remove(buttonBinding);

				PCInput.PCInputBinding ib = inputBinding;
				if (ib.Key == Keys.None && ib.MouseButton == PCInput.MouseButton.None)
					commandBinding = null;
				else
				{
					commandBinding = new CommandBinding(state == InputState.Up ? this.GetInputUp(ib) : this.GetInputDown(ib), action);
					this.Add(commandBinding);
				}

				if (ib.GamePadButton == Buttons.BigButton)
					buttonBinding = null;
				else
				{
					buttonBinding = new CommandBinding(state == InputState.Up ? this.GetButtonUp(ib.GamePadButton) : this.GetButtonDown(ib.GamePadButton), action);
					this.Add(buttonBinding);
				}
			};
			this.Add(new NotifyBinding(rebindCommand, inputBinding));
			rebindCommand();
		}

		protected Dictionary<Keys, Property<bool>> keyProperties = new Dictionary<Keys, Property<bool>>();

		protected Dictionary<Keys, Command> keyUpCommands = new Dictionary<Keys, Command>();
		protected Dictionary<Keys, Command> keyDownCommands = new Dictionary<Keys, Command>();

		protected Dictionary<Buttons, Property<bool>> buttonProperties = new Dictionary<Buttons, Property<bool>>();

		protected Dictionary<Buttons, Command> buttonUpCommands = new Dictionary<Buttons, Command>();
		protected Dictionary<Buttons, Command> buttonDownCommands = new Dictionary<Buttons, Command>();

		protected List<Action<PCInput.PCInputBinding>> nextInputListeners = new List<Action<PCInputBinding>>();

		public struct Chord
		{
			public Keys Modifier, Key;
		}

		protected Dictionary<Chord, Command> chords = new Dictionary<Chord, Command>();

		protected bool chordActivated = false;

		public PCInput()
		{
			this.Editable = false;
			this.Serialize = false;
		}

		public override Entity Entity
		{
			get
			{
				return base.Entity;
			}
			set
			{
				base.Entity = value;
				this.EnabledWhenPaused = false;
			}
		}

		private bool preventKeyDownEvents = false;

		public override void Awake()
		{
			base.Awake();
			this.Add(new CommandBinding(this.OnDisabled, delegate()
			{
				// Release all the keys
				foreach (KeyValuePair<Keys, Property<bool>> pair in this.keyProperties)
				{
					if (pair.Value.Value)
					{
						Command command;
						if (keyUpCommands.TryGetValue(pair.Key, out command))
							command.Execute();
						pair.Value.Value = false;
					}
				}

				this.chordActivated = false;

				// Release mouse buttons
				if (this.LeftMouseButton)
				{
					this.LeftMouseButton.Value = false;
					this.LeftMouseButtonUp.Execute();
				}

				if (this.RightMouseButton)
				{
					this.RightMouseButton.Value = false;
					this.RightMouseButtonUp.Execute();
				}

				if (this.MiddleMouseButton)
				{
					this.MiddleMouseButton.Value = false;
					this.MiddleMouseButtonUp.Execute();
				}
			}));

			this.Add(new CommandBinding(this.OnEnabled, delegate()
			{
				// Don't send key-down events for the first frame after we're enabled.
				this.preventKeyDownEvents = true;
			}));
		}

		public Property<bool> GetKey(Keys key)
		{
			if (this.keyProperties.ContainsKey(key))
				return this.keyProperties[key];
			else
			{
				Property<bool> newProperty = new Property<bool> { };
				this.keyProperties.Add(key, newProperty);
				return newProperty;
			}
		}

		public Property<bool> GetButton(Buttons button)
		{
			if (this.buttonProperties.ContainsKey(button))
				return this.buttonProperties[button];
			else
			{
				Property<bool> newProperty = new Property<bool> { };
				this.buttonProperties.Add(button, newProperty);
				return newProperty;
			}
		}

		public Command GetKeyDown(Keys key)
		{
			if (this.keyDownCommands.ContainsKey(key))
				return this.keyDownCommands[key];
			else
			{
				this.GetKey(key);
				Command command = new Command();
				this.keyDownCommands.Add(key, command);
				return command;
			}
		}

		public Command GetKeyUp(Keys key)
		{
			if (this.keyUpCommands.ContainsKey(key))
				return this.keyUpCommands[key];
			else
			{
				this.GetKey(key);
				Command command = new Command();
				this.keyUpCommands.Add(key, command);
				return command;
			}
		}

		public Command GetButtonDown(Buttons button)
		{
			if (this.buttonDownCommands.ContainsKey(button))
				return this.buttonDownCommands[button];
			else
			{
				this.GetButton(button);
				Command command = new Command();
				this.buttonDownCommands.Add(button, command);
				return command;
			}
		}

		public Command GetButtonUp(Buttons button)
		{
			if (this.buttonUpCommands.ContainsKey(button))
				return this.buttonUpCommands[button];
			else
			{
				this.GetButton(button);
				Command command = new Command();
				this.buttonUpCommands.Add(button, command);
				return command;
			}
		}

		public void GetNextInput(Action<PCInput.PCInputBinding> listener)
		{
			this.nextInputListeners.Add(listener);
		}

		public bool GetInput(PCInputBinding binding)
		{
			bool result = false;

			if (binding.Key != Keys.None)
				result |= this.GetKey(binding.Key);

			switch (binding.MouseButton)
			{
				case MouseButton.LeftMouseButton:
					result |= this.LeftMouseButton;
					break;
				case MouseButton.MiddleMouseButton:
					result |= this.MiddleMouseButton;
					break;
				case MouseButton.RightMouseButton:
					result |= this.RightMouseButton;
					break;
				default:
					break;
			}

			result |= this.GetButton(binding.GamePadButton);

			return result;
		}

		public Command GetInputUp(PCInputBinding binding)
		{
			if (binding.Key != Keys.None)
				return this.GetKeyUp(binding.Key);
			else if (binding.MouseButton != MouseButton.None)
			{
				switch (binding.MouseButton)
				{
					case MouseButton.LeftMouseButton:
						return this.LeftMouseButtonUp;
					case MouseButton.MiddleMouseButton:
						return this.MiddleMouseButtonUp;
					case MouseButton.RightMouseButton:
						return this.RightMouseButtonUp;
					default:
						return null;
				}
			}
			else
				return this.GetButtonUp(binding.GamePadButton);
		}

		public Command GetInputDown(PCInputBinding binding)
		{
			if (binding.Key != Keys.None)
				return this.GetKeyDown(binding.Key);
			else if (binding.MouseButton != MouseButton.None)
			{
				switch (binding.MouseButton)
				{
					case MouseButton.LeftMouseButton:
						return this.LeftMouseButtonDown;
					case MouseButton.MiddleMouseButton:
						return this.MiddleMouseButtonDown;
					case MouseButton.RightMouseButton:
						return this.RightMouseButtonDown;
					default:
						return null;
				}
			}
			else
				return this.GetButtonDown(binding.GamePadButton);
		}

		public Command GetChord(Chord chord)
		{
			if (this.chords.ContainsKey(chord))
				return this.chords[chord];
			else
			{
				Command cmd = new Command();
				this.chords.Add(chord, cmd);
				return cmd;
			}
		}

		private void notifyNextInputListeners(PCInput.PCInputBinding input)
		{
			foreach (Action<PCInput.PCInputBinding> listener in this.nextInputListeners)
				listener(input);
			this.nextInputListeners.Clear();
			this.preventKeyDownEvents = true;
		}

		public virtual void Update(float elapsedTime)
		{
			if (!main.IsActive)
				return;

			KeyboardState keyboard = this.main.KeyboardState;

			Keys[] keys = keyboard.GetPressedKeys();
			if (keys.Length > 0 && this.nextInputListeners.Count > 0)
				this.notifyNextInputListeners(new PCInputBinding { Key = keys[0] });

			foreach (KeyValuePair<Keys, Property<bool>> pair in this.keyProperties)
			{
				bool newValue = keyboard.IsKeyDown(pair.Key);
				if (newValue != pair.Value.Value)
				{
					pair.Value.Value = newValue;
					if (!this.preventKeyDownEvents)
					{
						if (newValue)
						{
							Command command;
							if (keyDownCommands.TryGetValue(pair.Key, out command))
								command.Execute();
						}
						else
						{
							Command command;
							if (keyUpCommands.TryGetValue(pair.Key, out command))
								command.Execute();
						}
					}
				}
			}

			GamePadState gamePad = this.main.GamePadState;
			if (gamePad.IsConnected)
			{
				if (this.nextInputListeners.Count > 0)
				{
					List<Buttons> buttons = new List<Buttons>();
					if (gamePad.IsButtonDown(Buttons.A))
						buttons.Add(Buttons.A);
					if (gamePad.IsButtonDown(Buttons.B))
						buttons.Add(Buttons.B);
					if (gamePad.IsButtonDown(Buttons.Back))
						buttons.Add(Buttons.Back);
					if (gamePad.IsButtonDown(Buttons.DPadDown))
						buttons.Add(Buttons.DPadDown);
					if (gamePad.IsButtonDown(Buttons.DPadLeft))
						buttons.Add(Buttons.DPadLeft);
					if (gamePad.IsButtonDown(Buttons.DPadRight))
						buttons.Add(Buttons.DPadRight);
					if (gamePad.IsButtonDown(Buttons.DPadUp))
						buttons.Add(Buttons.DPadUp);
					if (gamePad.IsButtonDown(Buttons.LeftShoulder))
						buttons.Add(Buttons.LeftShoulder);
					if (gamePad.IsButtonDown(Buttons.RightShoulder))
						buttons.Add(Buttons.RightShoulder);
					if (gamePad.IsButtonDown(Buttons.LeftStick))
						buttons.Add(Buttons.LeftStick);
					if (gamePad.IsButtonDown(Buttons.RightStick))
						buttons.Add(Buttons.RightStick);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickDown))
						buttons.Add(Buttons.LeftThumbstickDown);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickRight))
						buttons.Add(Buttons.LeftThumbstickRight);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickLeft))
						buttons.Add(Buttons.LeftThumbstickLeft);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickUp))
						buttons.Add(Buttons.LeftThumbstickUp);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickDown))
						buttons.Add(Buttons.RightThumbstickDown);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickRight))
						buttons.Add(Buttons.RightThumbstickRight);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickLeft))
						buttons.Add(Buttons.RightThumbstickLeft);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickUp))
						buttons.Add(Buttons.RightThumbstickUp);
					if (gamePad.IsButtonDown(Buttons.LeftTrigger))
						buttons.Add(Buttons.LeftTrigger);
					if (gamePad.IsButtonDown(Buttons.RightTrigger))
						buttons.Add(Buttons.RightTrigger);
					if (gamePad.IsButtonDown(Buttons.X))
						buttons.Add(Buttons.X);
					if (gamePad.IsButtonDown(Buttons.Y))
						buttons.Add(Buttons.Y);
					if (gamePad.IsButtonDown(Buttons.Start))
						buttons.Add(Buttons.Start);

					if (buttons.Count > 0)
						this.notifyNextInputListeners(new PCInputBinding { GamePadButton = buttons[0] });
				}

				foreach (KeyValuePair<Buttons, Property<bool>> pair in this.buttonProperties)
				{
					bool newValue = gamePad.IsButtonDown(pair.Key);
					if (newValue != pair.Value.Value)
					{
						pair.Value.Value = newValue;
						if (!this.preventKeyDownEvents)
						{
							if (newValue)
							{
								Command command;
								if (buttonDownCommands.TryGetValue(pair.Key, out command))
									command.Execute();
							}
							else
							{
								Command command;
								if (buttonUpCommands.TryGetValue(pair.Key, out command))
									command.Execute();
							}
						}
					}
				}
			}

			foreach (KeyValuePair<Keys, Property<bool>> pair in this.keyProperties)
			{
				bool newValue = keyboard.IsKeyDown(pair.Key);
				if (newValue != pair.Value.Value)
				{
					pair.Value.Value = newValue;
					if (!this.preventKeyDownEvents)
					{
						if (newValue)
						{
							Command command;
							if (keyDownCommands.TryGetValue(pair.Key, out command))
								command.Execute();
						}
						else
						{
							Command command;
							if (keyUpCommands.TryGetValue(pair.Key, out command))
								command.Execute();
						}
					}
				}
			}

			if (!this.chordActivated && !this.preventKeyDownEvents)
			{
				if (keys.Length == 2)
				{
					Chord chord = new Chord();
					if (keys[1] == Keys.LeftAlt || keys[1] == Keys.LeftControl || keys[1] == Keys.LeftShift || keys[1] == Keys.LeftWindows
						|| keys[1] == Keys.RightAlt || keys[1] == Keys.RightControl || keys[1] == Keys.RightShift || keys[1] == Keys.RightWindows)
					{
						chord.Modifier = keys[1];
						chord.Key = keys[0];
					}
					else
					{
						chord.Modifier = keys[0];
						chord.Key = keys[1];
					}
					if (this.chords.ContainsKey(chord))
					{
						this.chords[chord].Execute();
						this.chordActivated = true;
					}
				}
			}
			else if (keyboard.GetPressedKeys().Length == 0)
				this.chordActivated = false;

			if (this.EnableMouse)
			{
				MouseState mouse = this.main.MouseState;
				this.handleMouse();

				bool newLeftMouseButton = mouse.LeftButton == ButtonState.Pressed;
				if (newLeftMouseButton != this.LeftMouseButton)
				{
					this.LeftMouseButton.Value = newLeftMouseButton;
					if (!this.preventKeyDownEvents)
					{
						if (newLeftMouseButton)
						{
							if (this.nextInputListeners.Count > 0)
								this.notifyNextInputListeners(new PCInputBinding { MouseButton = MouseButton.LeftMouseButton });
							this.LeftMouseButtonDown.Execute();
						}
						else
							this.LeftMouseButtonUp.Execute();
					}
				}

				bool newMiddleMouseButton = mouse.MiddleButton == ButtonState.Pressed;
				if (newMiddleMouseButton != this.MiddleMouseButton)
				{
					this.MiddleMouseButton.Value = newMiddleMouseButton;
					if (!this.preventKeyDownEvents)
					{
						if (newMiddleMouseButton)
						{
							if (this.nextInputListeners.Count > 0)
								this.notifyNextInputListeners(new PCInputBinding { MouseButton = MouseButton.MiddleMouseButton });
							this.MiddleMouseButtonDown.Execute();
						}
						else
							this.MiddleMouseButtonUp.Execute();
					}
				}

				bool newRightMouseButton = mouse.RightButton == ButtonState.Pressed;
				if (newRightMouseButton != this.RightMouseButton)
				{
					this.RightMouseButton.Value = newRightMouseButton;
					if (!this.preventKeyDownEvents)
					{
						if (newRightMouseButton)
						{
							if (this.nextInputListeners.Count > 0)
								this.notifyNextInputListeners(new PCInputBinding { MouseButton = MouseButton.RightMouseButton });
							this.RightMouseButtonDown.Execute();
						}
						else
							this.RightMouseButtonUp.Execute();
					}
				}

				int newScrollWheel = mouse.ScrollWheelValue;
				int oldScrollWheel = this.ScrollWheel;
				if (newScrollWheel != oldScrollWheel)
				{
					this.ScrollWheel.Value = newScrollWheel;
					if (!this.preventKeyDownEvents)
						this.MouseScrolled.Execute(newScrollWheel > oldScrollWheel ? 1 : -1);
				}
			}
			this.preventKeyDownEvents = false;
		}

		public void SwallowEvents()
		{
			this.preventKeyDownEvents = true;
		}

		protected virtual void handleMouse()
		{
			MouseState state = this.main.MouseState, lastState = this.main.LastMouseState;
			if (state.X != lastState.X || state.Y != lastState.Y)
				this.Mouse.Value = new Vector2(state.X, state.Y);
		}
	}
}