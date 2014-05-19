using GeeUI.Managers;
using GeeUI.Structs;
using Microsoft.Xna.Framework;

namespace GeeUI.Views
{
	public class PanelView : View
	{
		public NinePatch UnselectedNinepatch = new NinePatch();
		public NinePatch SelectedNinepatch = new NinePatch();

		private const int ChildrenPadding = 1;

		/// <summary>
		/// If true, the panel can be dragged by the user.
		/// </summary>
		public bool Draggable = true;

		/// <summary>
		/// If true, the panel can be resized by the user.
		/// </summary>
		public bool Resizeable = true;

		private bool SelectedOffChildren;
		private bool Resizing = false;
		private Vector2 MouseSelectedOffset;

		public override Rectangle BoundBox
		{
			get
			{
				NinePatch curPatch = Selected ? SelectedNinepatch : UnselectedNinepatch;
				return new Rectangle((int)Position.X, (int)Position.Y, Width + ChildrenPadding + curPatch.LeftWidth + curPatch.RightWidth, Height + ChildrenPadding + curPatch.TopHeight + curPatch.BottomHeight);
			}
		}

		public override Rectangle ContentBoundBox
		{
			get
			{
				NinePatch curPatch = Selected ? SelectedNinepatch : UnselectedNinepatch;
				return new Rectangle((int)Position.X + curPatch.LeftWidth + ChildrenPadding, (int)Position.Y + curPatch.TopHeight + ChildrenPadding, Width, Height);
			}
		}

		public PanelView(View rootView, Vector2 position)
			: base(rootView)
		{
			SelectedNinepatch = GeeUI.NinePatchPanelSelected;
			UnselectedNinepatch = GeeUI.NinePatchPanelUnselected;
			Position = position;
		}

		public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
		{
			NinePatch patch = Selected ? SelectedNinepatch : UnselectedNinepatch;
			patch.Draw(spriteBatch, AbsolutePosition, Width, Height);
			base.Draw(spriteBatch);
		}

		protected internal void FollowMouse()
		{
			if (Draggable && !Resizing)
			{
				Vector2 newMousePosition = InputManager.GetMousePosV();
				if (SelectedOffChildren && Selected && InputManager.IsMousePressed(MouseButton.Left))
				{
					Position = (newMousePosition - MouseSelectedOffset);
				}
			}
			else if (Resizeable && Resizing)
			{
				Vector2 newMousePosition = InputManager.GetMousePosV();
				if (SelectedOffChildren && Selected && InputManager.IsMousePressed(MouseButton.Left))
				{
					int newWidth = (int)newMousePosition.X - BoundBox.X;
					int newHeight = (int)newMousePosition.Y - BoundBox.Y;
					if (newWidth >= 10 && newHeight >= 10)
					{
						Width = newWidth;
						Height = newHeight;
					}
				}
			}
		}


		public override void OnMClick(Vector2 position, bool fromChild = false)
		{
			SelectedOffChildren = !fromChild;
			Selected = true;
			MouseSelectedOffset = position - Position;
			if (ParentView != null)
				ParentView.BringChildToFront(this);

			Vector2 corner = new Vector2(BoundBox.Right, BoundBox.Bottom);
			Vector2 click = position;

			Resizing = Vector2.Distance(corner, click) <= 20 && !fromChild && click.X <= corner.X && click.Y <= corner.Y;

			FollowMouse();
			base.OnMClick(position, true);
		}

		public override void OnMClickAway(bool fromChild = false)
		{
			SelectedOffChildren = false;
			Selected = false;
			Resizing = false;
			base.OnMClickAway(true);
		}

		public override void OnMOver(bool fromChild = false)
		{
			FollowMouse();
			base.OnMOver(true);
		}

		public override void OnMOff(bool fromChild = false)
		{
			FollowMouse();
			base.OnMOff(true);
		}
	}
}
