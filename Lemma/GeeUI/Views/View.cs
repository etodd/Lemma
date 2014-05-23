using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.ViewLayouts;

namespace GeeUI.Views
{
	public class View
	{
		public delegate void MouseClickEventHandler(object sender, EventArgs e);
		public delegate void MouseOverEventHandler(object sender, EventArgs e);
		public delegate void MouseOffEventHandler(object sender, EventArgs e);

		public event MouseClickEventHandler OnMouseClick;
		public event MouseOverEventHandler OnMouseOver;
		public event MouseOffEventHandler OnMouseOff;

		public GeeUIMain ParentGeeUI;
		public View ParentView;

		public ViewLayout ChildrenLayout;

		public int ChildrenDepth;
		public int ThisDepth;

		public bool IgnoreParentBounds = true;
		public bool Selected;
		public bool Active = true;
		public bool EnabledScissor = true;
		public bool ContentMustBeScissored = false;

		public bool EnforceRootAttachment = true;

		public Action<float> PostUpdate = null;
		public Action PostDraw = null;

		public float MasterAlpha = 1;

		public int NumChildrenAllowed = -1;

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
				return (int)Position.X;
			}
			set
			{
				Position = new Vector2(value, Y);
			}
		}
		public int Y
		{
			get
			{
				return (int)Position.Y;
			}
			set
			{
				Position = new Vector2(X, value);
			}
		}

		public int RealX
		{
			get
			{
				if (ParentView == null) return X;
				return X - (int)ParentView.ContentOffset.X;
			}
		}

		public int RealY
		{
			get
			{
				if (ParentView == null) return Y;
				return Y - (int)ParentView.ContentOffset.Y;
			}
		}

		public Vector2 Position = Vector2.Zero;

		public Vector2 RealPosition
		{
			get
			{
				return new Vector2(RealX, RealY);
			}
		}

		public Vector2 ContentOffset = Vector2.Zero;

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

		public int Width, Height;

		protected List<View> _children = new List<View>();

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

			if (Children.Length + 1 > NumChildrenAllowed && NumChildrenAllowed != -1)
				throw new Exception("You have attempted to add too many child Views to this View.");
			//Ensure that a child can only belong to one View ever.
			if (child.ParentView != null)
				child.ParentView.RemoveChild(child);
			child.ParentView = this;
			child.ThisDepth = ChildrenDepth++;
			child.ParentGeeUI = ParentGeeUI;
			_children.Add(child);
		}

		public void RemoveChild(View child)
		{
			_children.Remove(child);
			child.ParentView = null;
			ReOrderChildrenDepth();
		}

		public virtual void OrderChildren(ViewLayout layout)
		{
			if (layout != null)
				layout.OrderChildren(this);
		}

		public View[] FindChildrenByName(string name, int depth = -1)
		{
			bool infinite = depth == -1;
			List<View> ret = new List<View>();
			while (depth-- >= 0 || infinite)
			{
				foreach (var c in Children)
				{
					if (c.Name == name)
						ret.Add(c);
					foreach (var find in c.FindChildrenByName(name, infinite ? -1 : depth - 1))
						ret.Add(find);
				}
			}
			return ret.ToArray();
		}

		public View FindFirstChildByName(string name, int depth = -1)
		{
			bool infinite = depth == -1;

			while (depth-- >= 0 || infinite)
			{
				foreach (var c in Children)
				{
					if (c.Name == name)
						return c;
					foreach (var find in c.FindChildrenByName(name, infinite ? -1 : depth - 1))
						return find;
				}
			}
			return null;
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



		private bool AttachedToRoot(View parent)
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
			ChildrenDepth = 0;
			for (var i = 0; i < _children.Count; i++)
			{
				_children[i].ThisDepth = i;
				ChildrenDepth++;
			}

		}

		public void ReOrderChildrenDepth()
		{
			View[] sortedChildren = Children;
			Array.Sort(sortedChildren, ViewDepthComparer.CompareDepths);
			ChildrenDepth = 0;

			for (int i = 0; i < sortedChildren.Length; i++)
			{
				Children[i].ThisDepth = i;
				ChildrenDepth++;
			}
		}

		#endregion

		#region Virtual methods/events

		public virtual void OnDelete()
		{
			Active = false;
			foreach (var child in Children)
				child.OnDelete();
		}

		public virtual void OnMScroll(Vector2 position, int scrollDelta, bool fromChild = false)
		{
			if (ParentView != null) ParentView.OnMScroll(position, scrollDelta, true);
		}

		public virtual void OnMClick(Vector2 position, bool fromChild = false)
		{
			if (OnMouseClick != null)
				OnMouseClick(this, new EventArgs());
			if (ParentView != null) ParentView.OnMClick(position, true);
		}

		public virtual void OnMClickAway(bool fromChild = false)
		{

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
			if (ChildrenLayout != null)
				OrderChildren(ChildrenLayout);

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
