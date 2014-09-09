using System;
using System.Linq;
using System.Collections.Generic;
using ComponentBind;
using Lemma;
using Lemma.Components;
using Lemma.GeeUI.Composites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.ViewLayouts;
using System.Collections.ObjectModel;

namespace GeeUI.Views
{
	public class View : Bindable
	{
		public delegate void MouseClickEventHandler(object sender, EventArgs e);
		public delegate void MouseOverEventHandler(object sender, EventArgs e);
		public delegate void MouseOffEventHandler(object sender, EventArgs e);
		public delegate void MouseScrollEventHandler(int delta);

		public event MouseClickEventHandler OnMouseClick;
		public event MouseClickEventHandler OnMouseRightClick;
		public event MouseClickEventHandler OnMouseClickAway;
		public event MouseOverEventHandler OnMouseOver;
		public event MouseOffEventHandler OnMouseOff;
		public event MouseScrollEventHandler OnMouseScroll;

		public GeeUIMain ParentGeeUI;
		public Property<View> ParentView = new Property<View>();

		public List<ViewLayout> ChildrenLayouts = new List<ViewLayout>();

		public Property<bool> IgnoreParentBounds = new Property<bool>() { Value = true };
		public Property<bool> Selected = new Property<bool>() { Value = false };
		public Property<bool> Active = new Property<bool>() { Value = true };
		public Property<bool> EnabledScissor = new Property<bool>() { Value = true };
		public Property<bool> ContentMustBeScissored = new Property<bool>() { Value = false };

		public Property<bool> AllowMouseEvents = new Property<bool>() { Value = true };

		public Property<bool> EnforceRootAttachment = new Property<bool>() { Value = true };

		public bool TemporarilyIgnoreMouseClickAway;

		protected int numChildrenAllowed = -1;

		public ToolTip ToolTipView;
		private Property<string> _toolTipText = new Property<string>();
		private float _toolTipTimer;

		public Property<float> MyOpacity = new Property<float> { Value = 1f };

		public Property<bool> Attached = new Property<bool>();

		public float EffectiveOpacity
		{
			get
			{
				if (ParentView.Value == null) return MyOpacity;
				return MyOpacity * ParentView.Value.EffectiveOpacity;
			}
		}

		public virtual void LoadContent(bool reload)
		{

		}

		public string Name;

		protected bool _mouseOver;
		
		public bool MouseOver
		{
			get
			{
				return _mouseOver;
			}
			set
			{
				bool old = _mouseOver;
				_mouseOver = value;
				if (!old && value)
					OnMOver();
				else if (old && !value)
					OnMOff();

				if (!value)
				{
					for (int i = 0; i < this.Children.Length; i++)
						this.Children[i].MouseOver = false;
				}
			}
		}

		public virtual Rectangle BoundBox
		{
			get
			{
				return new Rectangle(RealX, RealY, Width, Height);
			}
		}

		public virtual Rectangle AbsoluteBoundBox
		{
			get
			{
				if (ParentView.Value == null) return BoundBox;
				Rectangle curBB = BoundBox;
				return new Rectangle(AbsoluteX, AbsoluteY, curBB.Width, curBB.Height);
			}
		}

		public virtual Rectangle ContentBoundBox
		{
			get
			{
				return BoundBox;
			}
		}

		public virtual Rectangle AbsoluteContentBoundBox
		{
			get
			{
				if (ParentView.Value == null) return ContentBoundBox;
				Rectangle curBB = ContentBoundBox;
				curBB.X += AbsoluteX - RealX;
				curBB.Y += AbsoluteY - RealY;
				return curBB;
			}
		}

		public int X
		{
			get
			{
				return (int)Position.Value.X;
			}
			set
			{
				Position.Value = new Vector2(value, Y);
			}
		}
		public int Y
		{
			get
			{
				return (int)Position.Value.Y;
			}
			set
			{
				Position.Value = new Vector2(X, value);
			}
		}

		public int RealX
		{
			get
			{
				if (ParentView.Value == null) return X - (int)AnchorOffset.X;
				return X - (int)ParentView.Value.ContentOffset.Value.X - (int)AnchorOffset.X;
			}
		}

		public int RealY
		{
			get
			{
				if (ParentView.Value == null) return Y - (int)AnchorOffset.Y;
				return Y - (int)ParentView.Value.ContentOffset.Value.Y - (int)AnchorOffset.Y;
			}
		}

		public Property<Vector2> Position = new Property<Vector2>() { Value = Vector2.Zero };

		public Vector2 AnchorOffset
		{
			get
			{
				return new Vector2((float)Width * AnchorPoint.Value.X, (float)Height * AnchorPoint.Value.Y);
			}
		}
		public Vector2 RealPosition
		{
			get
			{
				return new Vector2(RealX, RealY);
			}
		}

		public Property<Vector2> ContentOffset = new Property<Vector2>() { Value = Vector2.Zero };

		public Property<Vector2> AnchorPoint = new Property<Vector2>() { Value = Vector2.Zero };

		public int AbsoluteX
		{
			get
			{
				return (int)AbsolutePosition.X;
			}
		}
		public int AbsoluteY
		{
			get
			{
				return (int)AbsolutePosition.Y;
			}
		}
		public Vector2 AbsolutePosition
		{
			get
			{
				if (ParentView.Value == null) return RealPosition;
				return RealPosition + ParentView.Value.AbsolutePosition;
			}
		}

		public Property<int> Width = new Property<int>() { Value = 0 };
		public Property<int> Height = new Property<int>() { Value = 0 };

		public ListProperty<View> Children = new ListProperty<View>();

		internal View(GeeUIMain theGeeUI)
		{
			ParentGeeUI = theGeeUI;

			this.Add(new SetBinding<bool>(this.Attached, delegate(bool value)
			{
				foreach (View v in this.Children)
					v.Attached.Value = value;
			}));

			this.Add(new SetBinding<View>(this.ParentView, delegate(View v)
			{
				if (v == null)
				{
					this.Attached.Value = false;
					this.ParentGeeUI.PotentiallyDetached(this);
				}
				else
					this.Attached.Value = v.Attached;
			}));

			this.Add(new NotifyBinding(delegate()
			{
				View parent = this.ParentView;
				if (parent != null)
					parent.dirty = true;
			}, this.Position, this.Active, this.Width, this.Height));

			this.Children.ItemAdded += delegate(int index, View child)
			{
				if (this.numChildrenAllowed != -1 && this.Children.Length > this.numChildrenAllowed)
					throw new Exception("GeeUI view exceeded max number of allowed children");

				child.RemoveFromParent();
				child.ParentView.Value = this;
				child.ParentGeeUI = ParentGeeUI;
				this.dirty = true;
			};

			this.Children.ItemRemoved += delegate(int index, View child)
			{
				child.ParentView.Value = null;
				this.dirty = true;
			};

			this.Children.Clearing += delegate()
			{
				foreach (var child in this.Children)
					child.ParentView.Value = null;
				this.dirty = true;
			};

			this.Children.ItemChanged += delegate(int index, View old, View newValue)
			{
				old.ParentView.Value = null;
				newValue.ParentView.Value = this;
				this.dirty = true;
			};
		}

		public View(GeeUIMain geeUi, View parent)
			: this(geeUi)
		{
			if (parent != null)
				parent.Children.Add(this);
		}

		#region Child management

		public virtual void OrderChildren()
		{
			foreach (var layout in ChildrenLayouts)
				layout.OrderChildren(this);
			this.dirty = false;
		}
		
		public List<View> FindChildrenByName(string name, int depth = -1, List<View> list = null)
		{
			if (list == null)
				list = new List<View>();
			bool infinite = depth == -1;
			if (!infinite) depth--;
			if (depth >= 0 || infinite)
			{
				foreach (var c in Children)
				{
					if (c.Name == name)
						list.Add(c);
					c.FindChildrenByName(name, infinite ? -1 : depth, list);
				}
			}
			return list;
		}

		public View FindFirstChildByName(string name, int depth = -1)
		{
			bool infinite = depth == -1;
			if (!infinite) depth--;

			if (depth >= 0 || infinite)
			{
				foreach (var c in Children)
				{
					if (c.Name == name)
						return c;
					foreach (var find in c.FindChildrenByName(name, infinite ? -1 : depth))
						return find;
				}
			}
			return null;
		}

		public void RemoveToolTip()
		{
			_toolTipTimer = 0f;
			if (this.ToolTipView != null)
			{
				this.ToolTipView.RemoveFromParent();
				this.ToolTipView = null;
			}
		}

		public void SetToolTipText(string text)
		{
			if (text == null) return;
			this._toolTipText.Value = text;
		}

		private void ShowToolTip()
		{
			RemoveToolTip();
			this.ToolTipView = new ToolTip(ParentGeeUI, ParentGeeUI.RootView, this, this._toolTipText);
		}

		#endregion

		#region Setters

		public View SetWidth(int width)
		{
			this.Width.Value = width;
			return this;
		}

		public View SetHeight(int height)
		{
			this.Height.Value = height;
			return this;
		}

		public View SetPosition(Vector2 position)
		{
			this.Position.Value = position;
			return this;
		}

		public View SetOpacity(float opacity)
		{
			this.MyOpacity.Value = opacity;
			return this;
		}

		public View SetContentOffset(Vector2 offset)
		{
			this.ContentOffset.Value = offset;
			return this;
		}

		#endregion

		public void BringToFront()
		{
			View p = this.ParentView;
			if (p != null)
				p.BringChildToFront(this);
		}

		public void BringChildToFront(View view)
		{
			if (this.Children[this.Children.Length - 1] != view)
			{
				this.Children.Remove(view);
				this.Children.Add(view);
				this.dirty = true;
			}
		}

		public void RemoveFromParent()
		{
			View p = this.ParentView;
			if (p != null)
				p.Children.Remove(this);
		}

		public void ResetOnMouseClick()
		{
			OnMouseClick = null;
		}

		public void ResetOnMouseScroll()
		{
			OnMouseScroll = null;
		}

		#region Virtual methods/events

		public virtual void OnDelete()
		{
			Active.Value = false;
			foreach (var child in Children)
				child.OnDelete();
			this.delete();
		}

		public virtual void OnMScroll(Vector2 position, int scrollDelta, bool fromChild)
		{
			if (ParentView.Value != null) ParentView.Value.OnMScroll(position, scrollDelta, true);
			if (OnMouseScroll != null)
				OnMouseScroll(scrollDelta);
		}

		public virtual void OnMRightClick(Vector2 position, bool fromChild)
		{
			if (OnMouseRightClick != null)
				OnMouseRightClick(this, new EventArgs());
			if (ParentView.Value != null) ParentView.Value.OnMRightClick(position, true);
		}

		public virtual void OnMClick(Vector2 position, bool fromChild)
		{
			if (OnMouseClick != null)
				OnMouseClick(this, new EventArgs());
			if (ParentView.Value != null) ParentView.Value.OnMClick(position, true);
		}

		public virtual void OnMClickAway()
		{
			if (this.TemporarilyIgnoreMouseClickAway)
				this.TemporarilyIgnoreMouseClickAway = false;
			else if (OnMouseClickAway != null)
				OnMouseClickAway(this, new EventArgs());
		}

		public virtual void OnMOver()
		{
			RemoveToolTip();
			if (OnMouseOver != null)
				OnMouseOver(this, new EventArgs());
			if (ParentView.Value != null) ParentView.Value.OnMOver();
		}

		public virtual void OnMOff()
		{
			RemoveToolTip();
			if (OnMouseOff != null)
				OnMouseOff(this, new EventArgs());
			if (ParentView.Value != null) ParentView.Value.OnMOff();
		}

		protected bool dirty;

		public virtual void Update(float dt)
		{
		}

		public void PostUpdate(float dt)
		{
			if (MouseOver && !string.IsNullOrEmpty(_toolTipText.Value))
			{
				_toolTipTimer += dt;
				if (_toolTipTimer >= 1f && ToolTipView == null)
					ShowToolTip();
			}

			if (this.dirty)
				this.OrderChildren();
		}

		public virtual void Draw(SpriteBatch spriteBatch)
		{
		}

		/// <summary>
		/// This will essentially cause the view to draw the things that should be scissored to its own bounds.
		/// </summary>
		/// <param name="spriteBatch"></param>
		public virtual void DrawContent(SpriteBatch spriteBatch)
		{

		}

		#endregion
	}
}
