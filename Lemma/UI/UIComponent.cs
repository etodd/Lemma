using System;
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
	public class UIComponent : Component
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
				foreach (UIComponent child in this.Children)
					child.renderer = value;
			}
		}

		public Property<object> UserData = new Property<object> { Editable = false };
		public Property<Vector2> AnchorPoint = new Property<Vector2> { Value = Vector2.Zero, Editable = true };
		public Property<Vector2> Position = new Property<Vector2> { Value = Vector2.Zero, Editable = true };
		public Property<Vector2> Size = new Property<Vector2> { Value = Vector2.Zero, Editable = true };
		public Property<Vector2> Scale = new Property<Vector2> { Value = Vector2.One, Editable = true };
		public Property<float> Rotation = new Property<float> { Value = 0.0f, Editable = true };
		public Property<bool> Visible = new Property<bool> { Value = true, Editable = true };
		public Property<bool> EnableScissor = new Property<bool> { Value = false, Editable = true };
		public Property<bool> SwallowMouseEvents = new Property<bool> { Value = false, Editable = false };
		public Property<string> Name = new Property<string> { Editable = false };
		public Property<int> DrawOrder = new Property<int> { Value = 0, Editable = false };
		public Property<bool> EnableInput = new Property<bool> { Value = true };

		protected bool requiresNewBatch = false;

		[XmlIgnore]
		public Property<Vector2> ScaledSize = new Property<Vector2> { Editable = false };
		[XmlIgnore]
		public Property<Vector2> InverseAnchorPoint = new Property<Vector2> { Value = new Vector2(0.5f, 0.5f), Editable = false };
		[XmlIgnore]
		public Property<Rectangle> Rectangle = new Property<Rectangle> { Editable = false };
		[XmlIgnore]
		public Property<Rectangle> ScaledRectangle = new Property<Rectangle> { Editable = false };
		[XmlIgnore]
		public Property<bool> Highlighted = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<UIComponent> Parent = new Property<UIComponent> { Editable = false };
		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix> { Value = Matrix.Identity, Editable = false };
		[XmlIgnore]
		public Property<bool> MouseLocked = new Property<bool> { Value = false, Editable = false };

		[XmlIgnore]
		public Command<Point> MouseOver = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseOut = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseLeftDown = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseLeftUp = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseMiddleDown = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseMiddleUp = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseRightDown = new Command<Point>();
		[XmlIgnore]
		public Command<Point> MouseRightUp = new Command<Point>();
		[XmlIgnore]
		public Command<Point, int> MouseScrolled = new Command<Point, int>();

		public ListProperty<UIComponent> Children = new ListProperty<UIComponent>();

		protected bool layoutDirty;

		public override void SetMain(Main _main)
		{
			base.SetMain(_main);
			foreach (UIComponent child in this.Children)
			{
				if (child.main == null)
					this.main.AddComponent(child);
			}
		}

		public override void LoadContent(bool reload)
		{
			base.LoadContent(reload);
			if (reload)
			{
				foreach (UIComponent child in this.Children)
					child.LoadContent(true);
			}
		}

		protected override void delete()
		{
			base.delete();
			if (this.Parent.Value != null)
			{
				this.Parent.Value.Children.Remove(this);
				this.Parent.Value = null;
			}
			else
				this.deleteWithoutRemovingFromParent();
		}

		private void deleteWithoutRemovingFromParent()
		{
			foreach (UIComponent child in this.Children)
				child.deleteWithoutRemovingFromParent();
		}

		public void CheckLayout()
		{
			foreach (UIComponent child in this.Children)
				child.CheckLayout();
			if (this.layoutDirty)
			{
				this.updateLayout();
				this.layoutDirty = false;
			}
		}

		public UIComponent GetChildByName(string name)
		{
			foreach (UIComponent child in this.Children)
			{
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
				foreach (UIComponent c in this.Children)
					c.deleteWithoutRemovingFromParent();
			});
		}

		public override void InitializeProperties()
		{
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

		public bool HandleMouse(MouseState mouse, MouseState lastMouse, Matrix parent, bool mouseContainedInParent)
		{
			if (!this.Visible || !this.EnableInput)
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
				foreach (UIComponent child in this.Children)
				{
					if (child.HandleMouse(mouse, lastMouse, transform, newHighlighted))
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
				this.MouseOver.Execute(absolutePoint);
				this.Highlighted.Value = true;
			}
			else if (!newHighlighted && this.Highlighted)
			{
				this.MouseOut.Execute(absolutePoint);
				this.Highlighted.Value = false;
			}

			if (newHighlighted)
			{
				if (mouse.LeftButton == ButtonState.Pressed && lastMouse.LeftButton == ButtonState.Released)
					this.MouseLeftDown.Execute(absolutePoint);
				else if (mouse.LeftButton == ButtonState.Released && lastMouse.LeftButton == ButtonState.Pressed)
					this.MouseLeftUp.Execute(absolutePoint);

				if (mouse.MiddleButton == ButtonState.Pressed && lastMouse.MiddleButton == ButtonState.Released)
					this.MouseMiddleDown.Execute(absolutePoint);
				else if (mouse.MiddleButton == ButtonState.Released && lastMouse.MiddleButton == ButtonState.Pressed)
					this.MouseMiddleUp.Execute(absolutePoint);

				if (mouse.RightButton == ButtonState.Pressed && lastMouse.RightButton == ButtonState.Released)
					this.MouseRightDown.Execute(absolutePoint);
				else if (mouse.RightButton == ButtonState.Released && lastMouse.RightButton == ButtonState.Pressed)
					this.MouseRightUp.Execute(absolutePoint);

				if (mouse.ScrollWheelValue != lastMouse.ScrollWheelValue)
					this.MouseScrolled.Execute(absolutePoint, mouse.ScrollWheelValue > lastMouse.ScrollWheelValue ? 1 : -1);
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
				foreach (UIComponent child in this.Children)
				{
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
