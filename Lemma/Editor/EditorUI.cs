using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeeUI.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Collections;
using Microsoft.Xna.Framework.Input;
using System.Xml.Serialization;
using Lemma.Util;
using ComponentBind;

namespace Lemma.Components
{
	public class EditorUI : Component<Main>, IUpdateableComponent
	{
		private const float precisionDelta = 0.025f;
		private const float normalDelta = 1.0f;
		private const float stringNavigateInterval = 0.08f;

		private static Keys[] ignoredKeys = new Keys[]
		{ 
			Keys.Escape, Keys.LeftShift, Keys.RightShift, Keys.LeftControl, Keys.RightControl, Keys.LeftAlt, Keys.RightAlt, Keys.Tab, Keys.Back, Keys.Left, Keys.Right, Keys.Up, Keys.Down,
			Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
			Keys.Delete, Keys.CapsLock,
		};

		private struct Chord
		{
			public Keys Keys;
			public bool Shift;

			public override bool Equals(object obj)
			{
				if (obj is Chord)
				{
					Chord c = (Chord)obj;
					return c.Keys == this.Keys && c.Shift == this.Shift;
				}
				else
					return false;
			}

			public override int GetHashCode()
			{
				return (int)this.Keys | (this.Shift ? 1 << 32 : 0);
			}
		}

		public struct PopupCommand
		{
			public string Description;
			public PCInput.Chord Chord;
			public Command Action;
			public Func<bool> Enabled;
		}

		private static Dictionary<Chord, string> inputKeyMappings = new Dictionary<Chord, string>();
		static EditorUI()
		{
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemBackslash }, "\\");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemBackslash, Shift = true }, "|");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemPipe }, "\\");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemPipe, Shift = true }, "|");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemPeriod }, ".");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemPeriod, Shift = true }, ">");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemComma }, ",");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemComma, Shift = true }, "<");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemQuestion }, "/");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemQuestion, Shift = true }, "?");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemQuotes }, "'");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemQuotes, Shift = true }, "\"");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemSemicolon }, ";");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemSemicolon, Shift = true }, ":");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemOpenBrackets }, "[");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemOpenBrackets, Shift = true }, "{");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemCloseBrackets }, "]");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemCloseBrackets, Shift = true }, "}");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemPlus }, "=");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemPlus, Shift = true }, "+");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemMinus }, "-");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemMinus, Shift = true }, "_");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemTilde }, "`");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.OemTilde, Shift = true }, "~");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D0 }, "0");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D1 }, "1");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D2 }, "2");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D3 }, "3");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D4 }, "4");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D5 }, "5");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D6 }, "6");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D7 }, "7");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D8 }, "8");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D9 }, "9");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad0 }, "0");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad1 }, "1");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad2 }, "2");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad3 }, "3");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad4 }, "4");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad5 }, "5");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad6 }, "6");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad7 }, "7");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad8 }, "8");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.NumPad9 }, "9");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D0, Shift = true }, ")");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D1, Shift = true }, "!");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D2, Shift = true }, "@");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D3, Shift = true }, "#");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D4, Shift = true }, "$");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D5, Shift = true }, "%");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D6, Shift = true }, "^");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D7, Shift = true }, "&");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D8, Shift = true }, "*");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.D9, Shift = true }, "(");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.Space }, " ");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.Space, Shift = true }, " ");
			EditorUI.inputKeyMappings.Add(new Chord { Keys = Keys.Enter }, "\n");
		}

		[XmlIgnore]
		public ListProperty<UIComponent> UIElements = new ListProperty<UIComponent>();
		[XmlIgnore]
		public ListProperty<Entity> SelectedEntities = new ListProperty<Entity>();
		[XmlIgnore]
		public Property<bool> MapEditMode = new Property<bool>();
		[XmlIgnore]
		public Property<bool> EnablePrecision = new Property<bool>();

		[XmlIgnore]
		public Property<bool> PopupVisible = new Property<bool>();
		[XmlIgnore]
		public Property<string> PopupSearchText = new Property<string> { Value = "" };
		[XmlIgnore]
		public ListProperty<PopupCommand> PopupCommands = new ListProperty<PopupCommand>();
		[XmlIgnore]
		public ListProperty<UIComponent> PopupElements = new ListProperty<UIComponent>();

		[XmlIgnore]
		public Property<bool> NeedsSave = new Property<bool>();

		[XmlIgnore]
		public Property<bool> StringPropertyLocked = new Property<bool>();

		protected Keys[] lastPressedKeys = new Keys[] { };
		private IProperty selectedStringProperty;
		private Binding<string> selectedStringBinding;
		private Property<string> selectedStringDisplayProperty;
		private string selectedStringValue;
		private bool selectedStringAllowMultiline;
		private bool selectedStringAllowEscape;
		private int selectedStringIndex;
		private float selectedStringNavigateInterval;

		public void ClearSelectedStringProperty()
		{
			this.selectedStringIndex = 0;
			this.selectedStringValue = "";
			this.selectedStringDisplayProperty.Value = "_";
		}

		public override void Awake()
		{
			base.Awake();
			this.SelectedEntities.ItemAdded += new ListProperty<Entity>.ItemAddedEventHandler(delegate(int index, Entity item)
			{
				this.refresh();
			});
			this.SelectedEntities.ItemRemoved += new ListProperty<Entity>.ItemRemovedEventHandler(delegate(int index, Entity item)
			{
				this.refresh();
			});
			this.SelectedEntities.ItemChanged += new ListProperty<ComponentBind.Entity>.ItemChangedEventHandler(delegate(int index, Entity old, Entity newValue)
			{
				this.refresh();
			});
			this.SelectedEntities.Cleared += new ListProperty<ComponentBind.Entity>.ClearEventHandler(this.refresh);
			this.Add(new NotifyBinding(this.refresh, this.MapEditMode));

			this.PopupVisible.Set = delegate(bool value)
			{
				if (this.PopupVisible.InternalValue && !value)
					this.revertStringProperty();
				else if (!this.PopupVisible.InternalValue && value)
					this.lockStringProperty(this.PopupSearchText, null, this.PopupSearchText, null, false, true);

				this.PopupVisible.InternalValue = value;
			};

			this.Add(new NotifyBinding(delegate()
			{
				string search = this.PopupSearchText.Value.ToLower().TrimEnd('_');
				bool first = true;
				foreach (Container element in this.PopupElements)
				{
					PopupCommand command = (PopupCommand)element.UserData.Value;

					bool visible = command.Enabled() && command.Description.ToLower().Contains(search);
					element.Visible.Value = visible;
					if (first && visible)
					{
						first = false;
						element.Opacity.Value = 1.0f;
					}
					else
						element.Opacity.Value = 0.0f;
				}
			}, () => this.PopupVisible, this.PopupVisible, this.PopupSearchText));

			this.UIElements.ItemRemoved += delegate(int index, UIComponent removed)
			{
				removed.Delete.Execute();
			};

			this.Add(new ListBinding<UIComponent, PopupCommand>(this.PopupElements, this.PopupCommands, delegate(PopupCommand command)
			{
				Container container = new Container();
				container.UserData.Value = command;
				container.Tint.Value = Color.Black;
				container.Add(new Binding<float, bool>(container.Opacity, x => x ? 1.0f : 0.0f, container.Highlighted));
				container.Add(new CommandBinding(container.MouseLeftUp, delegate()
				{
					this.PopupVisible.Value = false;
					command.Action.Execute();
				}));

				ListContainer layout = new ListContainer();
				layout.Orientation.Value = ListContainer.ListOrientation.Horizontal;
				container.Children.Add(layout);

				Container descriptionContainer = new Container();
				descriptionContainer.Opacity.Value = 0.0f;
				descriptionContainer.ResizeHorizontal.Value = false;
				descriptionContainer.Size.Value = new Vector2(110.0f, 0.0f);
				descriptionContainer.EnableScissor.Value = true;
				layout.Children.Add(descriptionContainer);
				TextElement description = new TextElement();
				description.Name.Value = "Description";
				description.FontFile.Value = "Font";
				description.Interpolation.Value = false;
				description.Text.Value = command.Description;
				descriptionContainer.Children.Add(description);

				if (command.Chord.Key != Keys.None)
				{
					Container chordContainer = new Container();
					chordContainer.Opacity.Value = 0.0f;
					layout.Children.Add(chordContainer);
					TextElement chord = new TextElement();
					chord.Interpolation.Value = false;
					chord.FontFile.Value = "Font";
					if (command.Chord.Modifier != Keys.None)
						chord.Text.Value = command.Chord.Modifier.ToString() + " " + command.Chord.Key.ToString();
					else
						chord.Text.Value = command.Chord.Key.ToString();
					chordContainer.Children.Add(chord);
				}

				return container;
			}));
		}

		private Container addText(string text)
		{
			Container container = new Container();
			container.Tint.Value = Color.Black;
			container.Opacity.Value = 0.2f;
			TextElement display = new TextElement();
			display.Interpolation.Value = true;
			display.FontFile.Value = "Font";
			display.Text.Value = text;
			container.Children.Add(display);
			this.UIElements.Add(container);
			return container;
		}

		private void show(Entity entity)
		{
			foreach (DictionaryEntry entry in new DictionaryEntry[] { new DictionaryEntry("[" + entity.Type.ToString() + " entity]", entity.Properties.Concat(entity.Commands)) }
				.Union(entity.Components.Where(x => ((IComponent)x.Value).Editable)))
			{
				IEnumerable<DictionaryEntry> properties = null;
				if (typeof(IComponent).IsAssignableFrom(entry.Value.GetType()))
					properties = entry.Value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
						.Select(x => new DictionaryEntry(x.Name, x.GetValue(entry.Value)))
						.Concat(entry.Value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
						.Where(y => y.GetIndexParameters().Length == 0)
						.Select(z => new DictionaryEntry(z.Name, z.GetValue(entry.Value, null))));
				else
					properties = (IEnumerable<DictionaryEntry>)entry.Value;
				properties = properties.Where(x => x.Value != null
					&& ((x.Value.GetType() == typeof(Command) && ((Command)x.Value).ShowInEditor)
					|| (typeof(IProperty).IsAssignableFrom(x.Value.GetType()) && !typeof(IListProperty).IsAssignableFrom(x.Value.GetType()) && (bool)x.Value.GetType().GetProperty("Editable").GetValue(x.Value, null))));

				if (properties.FirstOrDefault().Value == null)
					continue;

				Container label = this.addText(entry.Key.ToString());

				Container propertyListContainer = new Container();
				propertyListContainer.PaddingLeft.Value = 10.0f;
				propertyListContainer.PaddingRight.Value = 0.0f;
				propertyListContainer.PaddingBottom.Value = 0.0f;
				propertyListContainer.PaddingTop.Value = 0.0f;
				propertyListContainer.Opacity.Value = 0.0f;
				this.UIElements.Add(propertyListContainer);

				ListContainer propertyList = new ListContainer();
				propertyListContainer.Children.Add(propertyList);

				label.Add(new Binding<float, bool>(label.Opacity, x => x ? 1.0f : 0.5f, label.Highlighted));

				label.Add(new CommandBinding(label.MouseLeftUp, delegate()
				{
					propertyListContainer.Visible.Value = !propertyListContainer.Visible;
				}));

				foreach (DictionaryEntry propEntry in properties)
				{
					DictionaryEntry property = propEntry;
					ListContainer row = new ListContainer();
					row.Orientation.Value = ListContainer.ListOrientation.Horizontal;

					Container keyContainer = new Container();
					keyContainer.Tint.Value = Color.Black;
					keyContainer.Opacity.Value = 0.5f;
					keyContainer.ResizeHorizontal.Value = false;
					keyContainer.Size.Value = new Vector2(128.0f, 0.0f);
					TextElement keyText = new TextElement();
					keyText.Interpolation.Value = false;
					keyText.FontFile.Value = "Font";
					keyText.Text.Value = property.Key.ToString();
					keyContainer.Children.Add(keyText);
					row.Children.Add(keyContainer);

					if (property.Value.GetType() == typeof(Command))
					{
						// It's a command
						row.Children.Add(this.BuildButton((Command)property.Value, "[Execute]"));
					}
					else
					{
						// It's a property
						row.Children.Add(this.BuildValueField((IProperty)property.Value));
					}
					
					propertyList.Children.Add(row);
				}

				if (typeof(IEditorUIComponent).IsAssignableFrom(entry.Value.GetType()))
					((IEditorUIComponent)entry.Value).AddEditorElements(propertyList, this);
			}
		}

		public UIComponent BuildButton(Command command, string label, Color color = default(Color))
		{
			Container field = (Container)this.BuildLabel(label, color);

			field.Add(new Binding<float, bool>(field.Opacity, x => x ? 1.0f : 0.5f, field.Highlighted));

			field.Add(new CommandBinding(field.MouseLeftUp, command));
			field.SwallowMouseEvents.Value = true;

			return field;
		}

		public UIComponent BuildLabel(string label, Color color = default(Color))
		{
			if (color.A == 0)
				color = Color.White;

			Container field = new Container();
			field.Tint.Value = Color.Black;
			field.Opacity.Value = 0.5f;

			TextElement textField = new TextElement();
			textField.FontFile.Value = "Font";
			textField.Interpolation.Value = false;
			textField.Text.Value = label;
			textField.Tint.Value = color;
			field.Children.Add(textField);

			return field;
		}

		private void refresh()
		{
			this.UIElements.Clear();

			if (this.SelectedEntities.Count == 0 || this.MapEditMode)
				this.show(this.Entity);
			else if (this.SelectedEntities.Count == 1)
				this.show(this.SelectedEntities.First());
			else
				this.addText("[" + this.SelectedEntities.Count.ToString() + " entities]");
		}

		void IUpdateableComponent.Update(float dt)
		{
			KeyboardState keyboard = this.main.KeyboardState;
			Keys[] unfilteredKeys = keyboard.GetPressedKeys();
			Keys[] keys = unfilteredKeys.Except(this.lastPressedKeys).ToArray();
			if (this.StringPropertyLocked && unfilteredKeys.Length > 0)
			{
				if (this.selectedStringNavigateInterval > EditorUI.stringNavigateInterval)
				{
					if (unfilteredKeys.Contains(Keys.Back))
					{
						this.selectedStringNavigateInterval = 0.0f;
						if (this.selectedStringValue.Length > 0 && this.selectedStringIndex > 0)
						{
							this.selectedStringValue = this.selectedStringValue.Remove(this.selectedStringIndex - 1, 1);
							this.selectedStringIndex--;
						}
					}
					else if (unfilteredKeys.Contains(Keys.Delete))
					{
						this.selectedStringNavigateInterval = 0.0f;
						if (this.selectedStringIndex < this.selectedStringValue.Length)
							this.selectedStringValue = this.selectedStringValue.Remove(this.selectedStringIndex, 1);
					}
					else if (unfilteredKeys.Contains(Keys.Down))
					{
						this.selectedStringNavigateInterval = 0.0f;
						if (this.selectedStringValue.Length > 0 && this.selectedStringAllowMultiline)
						{
							int index = this.selectedStringValue.IndexOf('\n', this.selectedStringIndex + 1);
							if (index == -1)
								this.selectedStringIndex = this.selectedStringValue.Length - 1;
							else
								this.selectedStringIndex = index;
						}
					}
					else if (unfilteredKeys.Contains(Keys.Up))
					{
						this.selectedStringNavigateInterval = 0.0f;
						if (this.selectedStringValue.Length > 0 && this.selectedStringAllowMultiline)
						{
							int index = this.selectedStringValue.Substring(0, Math.Max(0, this.selectedStringIndex - 1)).LastIndexOf('\n');
							if (index == -1)
								this.selectedStringIndex = 0;
							else
								this.selectedStringIndex = index;
						}
					}
					else if (unfilteredKeys.Contains(Keys.Left) || unfilteredKeys.Contains(Keys.Right))
					{
						this.selectedStringNavigateInterval = 0.0f;
						if (this.selectedStringValue.Length > 0)
						{
							this.selectedStringIndex += unfilteredKeys.Contains(Keys.Right) ? 1 : -1;
							if (this.selectedStringIndex < 0)
								this.selectedStringIndex = Math.Max(0, this.selectedStringValue.Length);
							else if (this.selectedStringIndex > this.selectedStringValue.Length)
								this.selectedStringIndex = 0;
						}
					}
				}

				if (unfilteredKeys.Contains(Keys.Tab) || (unfilteredKeys.Contains(Keys.Enter) && !this.selectedStringAllowMultiline))
				{
					this.commitStringProperty();
					return;
				}
				else if (unfilteredKeys.Contains(Keys.Escape) && !this.selectedStringAllowEscape)
				{
					this.revertStringProperty();
					return;
				}
				
				bool caps = unfilteredKeys.Contains(Keys.LeftShift) || unfilteredKeys.Contains(Keys.RightShift);
				foreach (Keys key in keys)
				{
					if (!EditorUI.ignoredKeys.Contains(key))
					{
						this.selectedStringIndex++;
						Chord chord = new Chord { Keys = key, Shift = caps };
						if (EditorUI.inputKeyMappings.ContainsKey(chord))
							this.selectedStringValue = this.selectedStringValue.Insert(this.selectedStringIndex - 1, EditorUI.inputKeyMappings[chord]);
						else
							this.selectedStringValue = this.selectedStringValue.Insert(this.selectedStringIndex - 1, caps ? key.ToString().ToUpper() : key.ToString().ToLower());
					}
				}

				this.selectedStringDisplayProperty.Value = this.selectedStringValue.Insert(this.selectedStringIndex, "_");
			}
			this.lastPressedKeys = unfilteredKeys;
			this.selectedStringNavigateInterval += dt;
		}

		public UIComponent BuildValueMemberField(Type type, IProperty property, VectorElement element)
		{
			Container field = new Container();
			field.Tint.Value = Color.Black;

			field.Add(new Binding<float, bool>(field.Opacity, x => x ? 1.0f : 0.5f, field.Highlighted));

			TextElement textField = new TextElement();
			textField.FontFile.Value = "Font";
			textField.Interpolation.Value = false;
			field.Children.Add(textField);
			
			field.Add(new CommandBinding(field.MouseLeftDown, delegate()
			{
				field.SwallowMouseEvents.Value = true;
				field.MouseLocked.Value = true;
			}));
			field.Add(new CommandBinding(field.MouseLeftUp, delegate()
			{
				field.SwallowMouseEvents.Value = false;
				field.MouseLocked.Value = false;
			}));

			if (type.Equals(typeof(Vector2)))
			{
				Property<Vector2> socket = (Property<Vector2>)property;
				field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
				{
					this.NeedsSave.Value = true;
					float delta = scroll * (this.EnablePrecision ? EditorUI.precisionDelta : EditorUI.normalDelta);
					socket.Value = socket.Value.SetElement(element, socket.Value.GetElement(element) + delta);
				}));
				textField.Add(new Binding<string, Vector2>(textField.Text, x => x.GetElement(element).ToString("F"), socket));
			}
			else if (type.Equals(typeof(Vector3)))
			{
				Property<Vector3> socket = (Property<Vector3>)property;
				field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
				{
					this.NeedsSave.Value = true;
					float delta = scroll * (this.EnablePrecision ? EditorUI.precisionDelta : EditorUI.normalDelta);
					socket.Value = socket.Value.SetElement(element, socket.Value.GetElement(element) + delta);
				}));
				textField.Add(new Binding<string, Vector3>(textField.Text, x => x.GetElement(element).ToString("F"), socket));
			}
			else if (type.Equals(typeof(Voxel.Coord)))
			{
				Property<Voxel.Coord> socket = (Property<Voxel.Coord>)property;
				Direction dir;
				switch (element)
				{
					case VectorElement.X:
						dir = Direction.PositiveX;
						break;
					case VectorElement.Y:
						dir = Direction.PositiveY;
						break;
					default:
						dir = Direction.PositiveZ;
						break;
				}

				field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
				{
					this.NeedsSave.Value = true;
					int delta = scroll * (this.EnablePrecision ? 1 : 10);
					Voxel.Coord c = socket.Value;
					c.SetComponent(dir, c.GetComponent(dir) + delta);
					socket.Value = c;
				}));
				textField.Add(new Binding<string, Voxel.Coord>(textField.Text, x => x.GetComponent(dir).ToString(), socket));
			}
			else if (type.Equals(typeof(Vector4)))
			{
				Property<Vector4> socket = (Property<Vector4>)property;
				field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
				{
					this.NeedsSave.Value = true;
					float delta = scroll * (this.EnablePrecision ? EditorUI.precisionDelta : EditorUI.normalDelta);
					socket.Value = socket.Value.SetElement(element, socket.Value.GetElement(element) + delta);
				}));
				textField.Add(new Binding<string, Vector4>(textField.Text, x => x.GetElement(element).ToString("F"), socket));
			}
			else if (type.Equals(typeof(Quaternion)))
			{
				Property<Quaternion> socket = (Property<Quaternion>)property;
				field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
				{
					this.NeedsSave.Value = true;
					float delta = scroll * (this.EnablePrecision ? EditorUI.precisionDelta : EditorUI.normalDelta);
					socket.Value = socket.Value.SetElement(element, socket.Value.GetElement(element) + delta);
				}));
				textField.Add(new Binding<string, Quaternion>(textField.Text, x => x.GetElement(element).ToString("F"), socket));
			}
			else if (type.Equals(typeof(Color)))
			{
				Property<Color> socket = (Property<Color>)property;
				field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
				{
					this.NeedsSave.Value = true;
					socket.Value = socket.Value.SetElement(element, (byte)Math.Max(0, Math.Min(255, socket.Value.GetElement(element) + scroll * (this.EnablePrecision ? 1 : 10))));
				}));
				textField.Add(new Binding<string, Color>(textField.Text, x => x.GetElement(element).ToString(), socket));
			}

			return field;
		}

		public UIComponent BuildValueField(IProperty property)
		{
			PropertyInfo propertyInfo = property.GetType().GetProperty("Value");
			if (propertyInfo.PropertyType.Equals(typeof(Vector2)))
			{
				ListContainer elementList = new ListContainer();
				elementList.Orientation.Value = ListContainer.ListOrientation.Horizontal;
				foreach (VectorElement field in new[] { VectorElement.X, VectorElement.Y })
					elementList.Children.Add(this.BuildValueMemberField(propertyInfo.PropertyType, property, field));
				return elementList;
			}
			else if (propertyInfo.PropertyType.Equals(typeof(Vector3)) || propertyInfo.PropertyType.Equals(typeof(Voxel.Coord)))
			{
				ListContainer elementList = new ListContainer();
				elementList.Orientation.Value = ListContainer.ListOrientation.Horizontal;
				foreach (VectorElement field in new[] { VectorElement.X, VectorElement.Y, VectorElement.Z })
					elementList.Children.Add(this.BuildValueMemberField(propertyInfo.PropertyType, property, field));
				return elementList;
			}
			else if (propertyInfo.PropertyType.Equals(typeof(Vector4)) || propertyInfo.PropertyType.Equals(typeof(Quaternion)) || propertyInfo.PropertyType.Equals(typeof(Color)))
			{
				ListContainer elementList = new ListContainer();
				elementList.Orientation.Value = ListContainer.ListOrientation.Horizontal;
				foreach (VectorElement field in new[] { VectorElement.X, VectorElement.Y, VectorElement.Z, VectorElement.W })
					elementList.Children.Add(this.BuildValueMemberField(propertyInfo.PropertyType, property, field));
				return elementList;
			}
			else
			{
				Container field = new Container();
				field.Tint.Value = Color.Black;

				field.Add(new Binding<float, bool>(field.Opacity, x => x ? 1.0f : 0.5f, field.Highlighted));

				TextElement textField = new TextElement();
				textField.FontFile.Value = "Font";
				textField.Interpolation.Value = false;
				field.Children.Add(textField);
			
				if (!propertyInfo.PropertyType.Equals(typeof(string)))
				{
					// Some kind of float, int, or bool
					field.Add(new CommandBinding(field.MouseLeftDown, delegate()
					{
						field.SwallowMouseEvents.Value = true;
						field.MouseLocked.Value = true;
					}));
					field.Add(new CommandBinding(field.MouseLeftUp, delegate()
					{
						field.SwallowMouseEvents.Value = false;
						field.MouseLocked.Value = false;
					}));
				}

				if (propertyInfo.PropertyType.Equals(typeof(int)))
				{
					Property<int> socket = (Property<int>)property;
					field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
					{
						this.NeedsSave.Value = true;
						socket.Value += scroll * (this.EnablePrecision ? 1 : 10);
					}));
					textField.Add(new Binding<string, int>(textField.Text, x => x.ToString(), socket));
				}
				else if (propertyInfo.PropertyType.Equals(typeof(float)))
				{
					Property<float> socket = (Property<float>)property;
					field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
					{
						this.NeedsSave.Value = true;
						socket.Value += scroll * (this.EnablePrecision ? EditorUI.precisionDelta : EditorUI.normalDelta);;
					}));
					textField.Add(new Binding<string, float>(textField.Text, x => x.ToString("F"), socket));
				}
				else if (propertyInfo.PropertyType.Equals(typeof(bool)))
				{
					Property<bool> socket = (Property<bool>)property;
					field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
					{
						this.NeedsSave.Value = true;
						socket.Value = !socket;
					}));
					textField.Add(new Binding<string, bool>(textField.Text, x => x.ToString(), socket));
				}
				else if (typeof(Enum).IsAssignableFrom(propertyInfo.PropertyType))
				{
					int numFields = propertyInfo.PropertyType.GetFields(BindingFlags.Static | BindingFlags.Public).Length;
					field.Add(new CommandBinding<int>(field.MouseScrolled, () => this.selectedStringProperty == null && field.MouseLocked, delegate(int scroll)
					{
						this.NeedsSave.Value = true;
						int i = (int)propertyInfo.GetValue(property, null);
						i += scroll;
						if (i < 0)
							i = numFields - 1;
						else if (i >= numFields)
							i = 0;
						propertyInfo.SetValue(property, Enum.ToObject(propertyInfo.PropertyType, i), null);
					}));
					textField.Add(new Binding<string>(textField.Text, () => propertyInfo.GetValue(property, null).ToString(), (IProperty)property));
				}
				else if (propertyInfo.PropertyType.Equals(typeof(string)))
				{
					Property<string> socket = (Property<string>)property;
					textField.Add(new Binding<float, Point>(textField.WrapWidth, x => x.X * 0.5f, this.main.ScreenSize));
					Binding<string> binding = new Binding<string>(textField.Text, socket);
					textField.Add(binding);
					field.Add(new CommandBinding(field.MouseLeftUp, delegate()
					{
						if (this.selectedStringProperty != socket)
						{
							if (this.selectedStringProperty != null)
								this.revertStringProperty();
							this.selectedStringProperty = socket;
							this.selectedStringDisplayProperty = textField.Text;
							binding.Enabled = false;
							this.selectedStringBinding = binding;
							this.selectedStringValue = socket.Value ?? "";
							this.selectedStringIndex = this.selectedStringValue.Length;
							this.selectedStringAllowMultiline = true;
							this.selectedStringDisplayProperty.Value = this.selectedStringValue.Insert(this.selectedStringIndex, "_");
							this.StringPropertyLocked.Value = true;
						}
					}));
				}
				else if (propertyInfo.PropertyType.Equals(typeof(Entity.Handle)))
				{
					Property<Entity.Handle> socket = (Property<Entity.Handle>)property;
					Binding<string> binding = new Binding<string>(textField.Text, () => socket.Value.ID, socket);
					textField.Add(binding);
					field.Add(new CommandBinding(field.MouseLeftUp, delegate()
					{
						this.lockStringProperty(textField.Text, socket, socket.Value.ID ?? "", binding, true);
					}));
				}
				else if (propertyInfo.PropertyType.Equals(typeof(Matrix)))
					textField.Text.Value = "[matrix]";
				else if (propertyInfo.PropertyType.Equals(typeof(Voxel.Coord)))
					textField.Add(new Binding<string, Voxel.Coord>(textField.Text, x => "X:" + x.X.ToString() + " Y:" + x.Y.ToString() + " Z:" + x.Z.ToString(), (Property<Voxel.Coord>)property));
			
				return field;
			}
		}

		private void lockStringProperty(Property<string> displayProperty, IProperty targetProperty, string initialValue, Binding<string> binding, bool allowMultiline = true, bool allowEscape = false)
		{
			if (targetProperty == null || this.selectedStringProperty != targetProperty)
			{
				if (this.selectedStringProperty != null)
					this.revertStringProperty();
				this.selectedStringProperty = targetProperty;
				this.selectedStringDisplayProperty = displayProperty;
				if (binding != null)
					binding.Enabled = false;
				this.selectedStringBinding = binding;
				this.selectedStringValue = initialValue;
				this.selectedStringIndex = this.selectedStringValue.Length;
				this.selectedStringDisplayProperty.Value = this.selectedStringValue.Insert(this.selectedStringIndex, "_");
				this.selectedStringAllowMultiline = allowMultiline;
				this.selectedStringAllowEscape = allowEscape;
				this.StringPropertyLocked.Value = true;
			}
		}

		private void commitStringProperty()
		{
			if (this.selectedStringDisplayProperty == this.PopupSearchText)
			{
				this.PopupSearchText.Value = this.PopupSearchText.Value.TrimEnd('_');
				this.PopupVisible.Value = false;
				UIComponent popupElement = this.PopupElements.FirstOrDefault(x => x.Visible);
				if (popupElement != null)
				{
					PopupCommand command = (PopupCommand)popupElement.UserData.Value;
					command.Action.Execute();
				}
			}
			else
			{
				this.NeedsSave.Value = true;
				if (typeof(Property<string>).IsAssignableFrom(selectedStringProperty.GetType()))
					((Property<string>)this.selectedStringProperty).Value = this.selectedStringValue;
				else if (typeof(Property<Entity.Handle>).IsAssignableFrom(selectedStringProperty.GetType()))
					((Property<Entity.Handle>)this.selectedStringProperty).Value = new Entity.Handle { ID = this.selectedStringValue };
			}

			if (this.selectedStringBinding != null)
				this.selectedStringBinding.Enabled = true;
			this.selectedStringBinding = null;
			this.selectedStringValue = null;
			this.selectedStringProperty = null;
			this.selectedStringDisplayProperty = null;
			this.StringPropertyLocked.Value = false;
		}

		private void revertStringProperty()
		{
			if (this.selectedStringDisplayProperty == this.PopupSearchText)
				this.PopupSearchText.Value = this.PopupSearchText.Value.TrimEnd('_');

			if (this.selectedStringBinding != null)
				this.selectedStringBinding.Enabled = true;
			this.selectedStringBinding = null;
			this.selectedStringValue = null;
			this.selectedStringProperty = null;
			this.selectedStringDisplayProperty = null;
			this.StringPropertyLocked.Value = false;
		}
	}
}
