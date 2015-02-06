using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Xml.Serialization;
using System.Collections;
using System.Xml;
using Lemma.Util;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class UIComponent : Component<Main>, IGraphicsComponent
	{
		private UIRenderer _renderer;
		protected UIRenderer renderer
		{
			get
			{
				return this._renderer;
			}
			set
			{
				this._renderer = value;
				for (int i = 0; i < this.Children.Count; i++)
					this.Children[i].renderer = value;
			}
		}

		public Property<object> UserData = new Property<object>();
		public Property<Vector2> AnchorPoint = new Property<Vector2>();
		public Property<Vector2> Position = new Property<Vector2>();
		public Property<Vector2> Size = new Property<Vector2>();
		public Property<Vector2> Scale = new Property<Vector2> { Value = Vector2.One };
		public Property<float> Rotation = new Property<float>();
		public Property<bool> Visible = new Property<bool> { Value = true };
		public Property<bool> EnableScissor = new Property<bool>();
		public Property<bool> SwallowMouseEvents = new Property<bool>();
		public Property<string> Name = new Property<string>();
		public Property<int> DrawOrder = new Property<int>();
		public Property<bool> EnableInput = new Property<bool> { Value = true };

		protected bool requiresNewBatch = false;

		[XmlIgnore]
		public Property<Vector2> ScaledSize = new Property<Vector2>();
		[XmlIgnore]
		public Property<Vector2> InverseAnchorPoint = new Property<Vector2> { Value = new Vector2(0.5f, 0.5f) };
		[XmlIgnore]
		public Property<Rectangle> Rectangle = new Property<Rectangle>();
		[XmlIgnore]
		public Property<Rectangle> ScaledRectangle = new Property<Rectangle>();
		[XmlIgnore]
		public Property<bool> Highlighted = new Property<bool>();
		[XmlIgnore]
		public Property<UIComponent> Parent = new Property<UIComponent>();
		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix> { Value = Matrix.Identity };
		[XmlIgnore]
		public Property<bool> MouseLocked = new Property<bool>();

		[XmlIgnore]
		public Command MouseOver = new Command();
		[XmlIgnore]
		public Command MouseOut = new Command();
		[XmlIgnore]
		public Command MouseLeftDown = new Command();
		[XmlIgnore]
		public Command MouseLeftUp = new Command();
		[XmlIgnore]
		public Command MouseMiddleDown = new Command();
		[XmlIgnore]
		public Command MouseMiddleUp = new Command();
		[XmlIgnore]
		public Command MouseRightDown = new Command();
		[XmlIgnore]
		public Command MouseRightUp = new Command();
		[XmlIgnore]
		public Command<int> MouseScrolled = new Command<int>();

		public ListProperty<UIComponent> Children = new ListProperty<UIComponent>();

		protected bool layoutDirty;

		public virtual void LoadContent(bool reload)
		{
			if (reload)
			{
				for (int i = 0; i < this.Children.Count; i++)
					this.Children[i].LoadContent(true);
			}
		}

		public override void delete()
		{
			base.delete();
			if (this.Parent.Value != null)
				this.Parent.Value.Children.Remove(this);
			else
				this.deleteWithoutRemovingFromParent();
		}

		private void deleteWithoutRemovingFromParent()
		{
			this.Parent.Value = null;
			for (int i = 0; i < this.Children.Count; i++)
				this.Children[i].deleteWithoutRemovingFromParent();
			this.Delete.Execute();
		}

		public void CheckLayout()
		{
			for (int i = 0; i < this.Children.Count; i++)
				this.Children[i].CheckLayout();
			if (this.layoutDirty)
			{
				this.updateLayout();
				this.layoutDirty = false;
			}
		}

		public UIComponent GetChildByName(string name)
		{
			for (int i = 0; i < this.Children.Count; i++)
			{
				UIComponent child = this.Children[i];
				if (child.Name.Value == name)
					return child;
				UIComponent result = child.GetChildByName(name);
				if (result != null)
					return result;
			}
			return null;
		}

		protected virtual void updateLayout()
		{

		}

		public void Detach()
		{
			this.Parent.Value.Children.RemoveWithoutNotifying(this);
			this.Parent.Value = null;
		}

		public UIComponent()
		{
			this.Children.ItemAdded += new ListProperty<UIComponent>.ItemAddedEventHandler(delegate(int index, UIComponent child)
			{
				if (child.Parent.Value != null)
					throw new Exception("UIComponent was added as a child, but it already had a parent.");
				child.renderer = this.renderer;
				child.Parent.Value = this;
				if (child.main == null && this.main != null)
					this.main.AddComponent(child);
			});
			this.Children.ItemChanged += new ListProperty<UIComponent>.ItemChangedEventHandler(delegate(int index, UIComponent old, UIComponent newValue)
			{
				old.deleteWithoutRemovingFromParent();
				newValue.renderer = this.renderer;
				if (newValue.Parent.Value != this)
					newValue.Parent.Value = this;
				if (newValue.main == null && this.main != null)
					this.main.AddComponent(newValue);
			});
			this.Children.ItemRemoved += new ListProperty<UIComponent>.ItemRemovedEventHandler(delegate(int index, UIComponent child)
			{
				child.deleteWithoutRemovingFromParent();
			});
			this.Children.Clearing += new ListProperty<UIComponent>.ClearEventHandler(delegate()
			{
				for (int i = 0; i < this.Children.Count; i++)
					this.Children[i].deleteWithoutRemovingFromParent();
			});
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new TwoWayBinding<Vector2, Vector2>(
				this.AnchorPoint,
				x => new Vector2(1.0f - x.X, 1.0f - x.Y),
				this.InverseAnchorPoint,
				x => new Vector2(1.0f - x.X, 1.0f - x.Y)));
			this.Add(new Binding<Vector2>(this.ScaledSize, () => this.Scale * this.Size.Value, this.Scale, this.Size));
			this.Add(new Binding<Rectangle, Vector2>(this.Rectangle, x => new Rectangle(0, 0, (int)Math.Round(x.X), (int)Math.Round(x.Y)), this.Size));
			this.Add(new Binding<Rectangle, Vector2>(this.ScaledRectangle, x => new Rectangle(0, 0, (int)Math.Round(x.X), (int)Math.Round(x.Y)), this.ScaledSize));
			this.Add(new Binding<Matrix>(this.Transform, delegate()
			{
				return Matrix.CreateTranslation(new Vector3(-(this.Size.Value * this.AnchorPoint), 0.0f))
					* Matrix.CreateRotationZ(this.Rotation)
					* Matrix.CreateScale(new Vector3(this.Scale, 1.0f))
					* Matrix.CreateTranslation(new Vector3(this.Position.Value, 0.0f));
			}, this.Position, this.Size, this.Scale, this.Rotation, this.AnchorPoint));

			for (int i = 0; i < this.Children.Count; i++)
			{
				UIComponent child = this.Children[i];
				if (child.main == null)
					this.main.AddComponent(child);
			}
		}

		public Matrix GetAbsoluteTransform()
		{
			UIComponent x = this;
			Matrix result = x.Transform;
			while (x.Parent.Value != null)
			{
				x = x.Parent;
				result *= x.Transform;
			}
			return result;
		}

		public Vector2 GetAbsolutePosition()
		{
			UIComponent x = this;
			Vector2 result = x.Position;
			while (x.Parent.Value != null)
			{
				x = x.Parent;
				result += x.Position;
			}
			return result;
		}

		private bool swallowCurrentMouseEvent;
		public void SwallowCurrentMouseEvent()
		{
			this.swallowCurrentMouseEvent = true;
		}

		public bool HandleMouse(MouseState mouse, MouseState lastMouse, Matrix parent, bool mouseContainedInParent)
		{
			if (!this.Visible || !this.EnableInput || main.GeeUI.LastClickCaptured)
				return false;
			Matrix transform = this.Transform * parent;

			bool newHighlighted = this.MouseLocked;

			if (!newHighlighted && mouseContainedInParent)
			{
				Vector3 pos = new Vector3(mouse.X, mouse.Y, 0.0f);
				pos = Vector3.Transform(pos, Matrix.Invert(transform));
				Point relativePoint = new Point((int)Math.Round(pos.X), (int)Math.Round(pos.Y));
				newHighlighted = this.Rectangle.Value.Contains(relativePoint);
			}

			try
			{
				for (int i = this.Children.Length - 1; i >= 0 && i < this.Children.Length; i--)
				{
					if (this.Children[i].HandleMouse(mouse, lastMouse, transform, newHighlighted))
						return true; // Mouse events have been swallowed
				}
			}
			catch (InvalidOperationException)
			{
				// Children were modified while we were iterating.
			}

			Point absolutePoint = new Point(mouse.X, mouse.Y);

			if (newHighlighted && !this.Highlighted)
			{
				this.MouseOver.Execute();
				this.Highlighted.Value = true;
			}
			else if (!newHighlighted && this.Highlighted)
			{
				this.MouseOut.Execute();
				this.Highlighted.Value = false;
			}

			if (newHighlighted)
			{
				if (mouse.LeftButton == ButtonState.Pressed && lastMouse.LeftButton == ButtonState.Released)
					this.MouseLeftDown.Execute();
				else if (mouse.LeftButton == ButtonState.Released && lastMouse.LeftButton == ButtonState.Pressed)
					this.MouseLeftUp.Execute();

				if (mouse.MiddleButton == ButtonState.Pressed && lastMouse.MiddleButton == ButtonState.Released)
					this.MouseMiddleDown.Execute();
				else if (mouse.MiddleButton == ButtonState.Released && lastMouse.MiddleButton == ButtonState.Pressed)
					this.MouseMiddleUp.Execute();

				if (mouse.RightButton == ButtonState.Pressed && lastMouse.RightButton == ButtonState.Released)
					this.MouseRightDown.Execute();
				else if (mouse.RightButton == ButtonState.Released && lastMouse.RightButton == ButtonState.Pressed)
					this.MouseRightUp.Execute();

				if (mouse.ScrollWheelValue != lastMouse.ScrollWheelValue)
					this.MouseScrolled.Execute(mouse.ScrollWheelValue > lastMouse.ScrollWheelValue ? 1 : -1);
			}
			if (this.swallowCurrentMouseEvent)
			{
				this.swallowCurrentMouseEvent = false;
				return true;
			}
			return newHighlighted && (this.SwallowMouseEvents || this.MouseLocked); // Swallow the mouse events so no one else handles them
		}

		public void Draw(GameTime time, Matrix parent, Rectangle scissor)
		{
			Matrix transform = this.Transform * parent;
			Rectangle newScissor = scissor;
			if (this.EnableScissor)
				newScissor = scissor.Intersect(RectangleExtensions.Create(Vector2.Transform(Vector2.Zero, transform), Vector2.Transform(this.Size, transform)));

			if (newScissor.Width > 0 && newScissor.Height > 0)
			{
				if (this.EnableScissor)
				{
					this.renderer.Batch.End();
					this.renderer.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.renderer.RasterizerState, null, Matrix.Identity);
					this.main.GraphicsDevice.ScissorRectangle = newScissor;
				}

				this.draw(time, parent, transform);
				for (int i = 0; i < this.Children.Count; i++)
				{
					UIComponent child = this.Children[i];
					if (child.Visible)
					{
						if (this.main.GraphicsDevice.ScissorRectangle != newScissor || child.requiresNewBatch)
						{
							this.renderer.Batch.End();
							this.renderer.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.renderer.RasterizerState, null, Matrix.Identity);
							this.main.GraphicsDevice.ScissorRectangle = newScissor;
						}
						child.Draw(time, transform, newScissor);
					}
				}
			}

			if (this.EnableScissor)
			{
				// Restore original scissor
				this.renderer.Batch.End();
				this.renderer.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.renderer.RasterizerState, null, Matrix.Identity);
				this.main.GraphicsDevice.ScissorRectangle = scissor;
			}
		}
		
		protected virtual void draw(GameTime time, Matrix parent, Matrix transform)
		{

		}
	}
}