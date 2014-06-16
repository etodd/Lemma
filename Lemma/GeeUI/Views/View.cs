using System;
using System.Collections.Generic;
using ComponentBind;
using Lemma;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.ViewLayouts;
using Newtonsoft.Json.Schema;

namespace GeeUI.Views
{
	public class View : Component<Main>
	{
		public delegate void MouseClickEventHandler(object sender, EventArgs e);
		public delegate void MouseOverEventHandler(object sender, EventArgs e);
		public delegate void MouseOffEventHandler(object sender, EventArgs e);

		public event MouseClickEventHandler OnMouseClick;
		public event MouseClickEventHandler OnMouseRightClick;
		public event MouseClickEventHandler OnMouseClickAway;
		public event MouseOverEventHandler OnMouseOver;
		public event MouseOffEventHandler OnMouseOff;

		public GeeUIMain ParentGeeUI;
		public View ParentView;

		public List<ViewLayout> ChildrenLayouts = new List<ViewLayout>();

		public Property<int> ChildrenDepth = new Property<int>() { Value = 0 };
		public Property<int> ThisDepth = new Property<int>() { Value = 0 };

		public Property<bool> IgnoreParentBounds = new Property<bool>() { Value = true };
		public Property<bool> Selected = new Property<bool>() { Value = false };
		public Property<bool> Active = new Property<bool>() { Value = true };
		public Property<bool> EnabledScissor = new Property<bool>() { Value = true };
		public Property<bool> ContentMustBeScissored = new Property<bool>() { Value = false };

		public Property<bool> AllowMouseEvents = new Property<bool>() { Value = true };

		public Property<bool> EnforceRootAttachment = new Property<bool>() { Value = true };

		public Action<float> PostUpdate = null;
		public Action PostDraw = null;

		public Property<float> MyOpacity = new Property<float>() { Value = 1f };
		public float EffectiveOpacity
		{
			get
			{
				if (ParentView == null) return MyOpacity;
				return MyOpacity * ParentView.EffectiveOpacity;
			}
		}

		public Property<int> NumChildrenAllowed = new Property<int>() { Value = -1 };

		public string Name = "";

		protected bool _mouseOver;
		public bool MouseOver
		{
			get
			{
				return _mouseOver;
			}
			set
			{
				_mouseOver = value;
				if (value)
					OnMOver();
				else
					OnMOff();
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
				if (ParentView == null) return BoundBox;
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
				if (ParentView == null) return ContentBoundBox;
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
				if (ParentView == null) return X - (int)AnchorOffset.X;
				return X - (int)ParentView.ContentOffset.Value.X - (int)AnchorOffset.X;
			}
		}

		public int RealY
		{
			get
			{
				if (ParentView == null) return Y - (int)AnchorOffset.Y;
				return Y - (int)ParentView.ContentOffset.Value.Y - (int)AnchorOffset.Y;
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
				if (ParentView == null) return RealPosition;
				return RealPosition + ParentView.AbsolutePosition;
			}
		}

		public Property<int> Width = new Property<int>() { Value = 0 };
		public Property<int> Height = new Property<int>() { Value = 0 };

		protected List<View> _children = new List<View>();

		public int ChildCount
		{
			get
			{
				return _children.Count;
			}
		}

		public View[] Children
		{
			get
			{
				return _children.ToArray();
			}
		}

		internal View(GeeUIMain theGeeUI)
		{
			ParentGeeUI = theGeeUI;
		}

		public View(GeeUIMain theGeeUI, View parentView)
			: this(theGeeUI)
		{
			if (parentView != null)
				parentView.AddChild(this);
		}

		#region Child management

		public virtual void AddChild(View child)
		{
			if (child == null) return;
			if (Children.Length + 1 > NumChildrenAllowed && NumChildrenAllowed != -1)
				throw new Exception("You have attempted to add too many child Views to this View.");
			//Ensure that a child can only belong to one View ever.
			if (child.ParentView != null)
				child.ParentView.RemoveChild(child);
			child.ParentView = this;
			child.ThisDepth.Value = ChildrenDepth.Value++;
			child.ParentGeeUI = ParentGeeUI;
			_children.Add(child);
		}


		public void RemoveAllChildren()
		{
			foreach (var child in Children)
				child.ParentView = null;
			_children.Clear();
		}

		public void RemoveChild(View child)
		{
			_children.Remove(child);
			child.ParentView = null;
			ReOrderChildrenDepth();
		}

		public void OrderChildren()
		{
			foreach(var layout in ChildrenLayouts)
				OrderChildren(layout);
		}
		public virtual void OrderChildren(ViewLayout layout)
		{
			if (layout != null)
				layout.OrderChildren(this);
		}

		public View[] FindChildrenByName(string name, int depth = -1)
		{
			bool infinite = depth == -1;
			if (!infinite) depth--;
			List<View> ret = new List<View>();
			if (depth >= 0 || infinite)
			{
				foreach (var c in Children)
				{
					if (c.Name == name)
						ret.Add(c);
					foreach (var find in c.FindChildrenByName(name, infinite ? -1 : depth))
						ret.Add(find);
				}
			}
			return ret.ToArray();
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

		#region Parent management

		public void SetParent(View parent)
		{
			if (ParentView != null)
			{
				ParentView.RemoveChild(this);
			}
			parent.AddChild(this);
		}

		private void CheckAttachmentToRoot()
		{
			if (!EnforceRootAttachment) return;

			//We're not attached to root anymore--someone removed us from the hierarchy. We should clean up.
			if (!AttachedToRoot(ParentView))
			{
				OnDelete();
			}
		}



		public bool AttachedToRoot(View parent)
		{
			if (this == ParentGeeUI.RootView) return true;
			else if (parent == ParentGeeUI.RootView) return true;
			else if (parent == null) return false;
			return AttachedToRoot(parent.ParentView);
		}

		#endregion

		#region Child depth ordering

		public virtual void BringChildToFront(View view)
		{
			_children.Remove(view);
			var sortedChildren = _children;
			sortedChildren.Sort(ViewDepthComparer.CompareDepthsInverse);
			sortedChildren.Add(view);
			_children = sortedChildren;
			ChildrenDepth.Value = 0;
			for (var i = 0; i < _children.Count; i++)
			{
				_children[i].ThisDepth.Value = i;
				ChildrenDepth.Value++;
			}

		}

		public void ReOrderChildrenDepth()
		{
			View[] sortedChildren = Children;
			Array.Sort(sortedChildren, ViewDepthComparer.CompareDepths);
			ChildrenDepth.Value = 0;

			for (int i = 0; i < sortedChildren.Length; i++)
			{
				Children[i].ThisDepth.Value = i;
				ChildrenDepth.Value++;
			}
		}

		#endregion

		public void ResetOnMouseClick()
		{
			OnMouseClick = null;
		}

		#region Virtual methods/events

		public virtual void OnDelete()
		{
			Active.Value = false;
			foreach (var child in Children)
				child.OnDelete();
			this.delete();
		}

		public virtual void OnMScroll(Vector2 position, int scrollDelta, bool fromChild = false)
		{
			if (ParentView != null) ParentView.OnMScroll(position, scrollDelta, true);
		}

		public virtual void OnMRightClick(Vector2 position, bool fromChild = false)
		{
			if (OnMouseRightClick != null)
				OnMouseRightClick(this, new EventArgs());
			if (ParentView != null) ParentView.OnMRightClick(position, true);
		}

		public virtual void OnMClick(Vector2 position, bool fromChild = false)
		{
			if (OnMouseClick != null)
				OnMouseClick(this, new EventArgs());
			if (ParentView != null) ParentView.OnMClick(position, true);
		}

		public virtual void OnMClickAway(bool fromChild = false)
		{
			if (OnMouseClickAway != null)
				OnMouseClickAway(this, new EventArgs());
			if (ParentView != null) ParentView.OnMClickAway(true);
		}

		public virtual void OnMOver(bool fromChild = false)
		{
			if (OnMouseOver != null)
				OnMouseOver(this, new EventArgs());
			if (ParentView != null) ParentView.OnMOver(true);
		}

		public virtual void OnMOff(bool fromChild = false)
		{
			if (OnMouseOff != null)
				OnMouseOff(this, new EventArgs());
			if (ParentView != null) ParentView.OnMOff(true);
		}

		public virtual void Update(float dt)
		{
			foreach (var layout in ChildrenLayouts)
			{
				if (layout != null)
					OrderChildren(layout);
			}

			CheckAttachmentToRoot();

			if (ParentView == null || IgnoreParentBounds)
			{
				if (PostUpdate != null)
					PostUpdate(dt);
				return;
			}
			var curBB = AbsoluteBoundBox;
			var parentBB = ParentView.AbsoluteContentBoundBox;
			var xOffset = curBB.Right - parentBB.Right;
			var yOffset = curBB.Bottom - parentBB.Bottom;
			if (xOffset > 0)
				X -= xOffset;
			else
			{
				xOffset = curBB.Left - parentBB.Left;
				if (xOffset < 0)
					X -= xOffset;
			}
			if (yOffset > 0)
				Y -= yOffset;
			else
			{
				yOffset = curBB.Top - parentBB.Top;
				if (yOffset < 0)
					Y -= yOffset;
			}

			if (PostUpdate != null)
				PostUpdate(dt);


		}

		public virtual void Draw(SpriteBatch spriteBatch)
		{
			if (PostDraw != null)
				PostDraw();
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
