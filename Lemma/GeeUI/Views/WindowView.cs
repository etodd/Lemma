using GeeUI.Structs;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GeeUI.Managers;

namespace GeeUI.Views
{
	public class WindowView : View
	{
		public NinePatch NinePatchSelected = new NinePatch();
		public NinePatch NinePatchNormal = new NinePatch();

		public string WindowText = "Hello this is a VIEW!";
		public SpriteFont WindowTextFont;

		protected internal bool SelectedOffChildren;
		protected internal Vector2 LastMousePosition = Vector2.Zero;
		protected internal Vector2 MouseSelectedOffset = Vector2.Zero;

		/// <summary>
		/// If true, the window can be dragged by the user.
		/// </summary>
		public bool Draggable = true;

		public View WindowContentView
		{
			get
			{
				return Children.Count == 0 ? null : Children[0];
			}
			set
			{
				if (Children.Count == 0)
				{
					AddChild(value);
					return;
				}
				Children[0] = value;
			}
		}

		public override Rectangle ContentBoundBox
		{
			get
			{
				NinePatch patch = Selected ? NinePatchSelected : NinePatchNormal;
				Vector2 windowTextSize = WindowTextFont.MeasureString(WindowText);
				var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);
				return new Rectangle(RealX, RealY + (int)barHeight, Width, Height - (int)barHeight);
			}
		}


		public WindowView(GeeUIMain GeeUI, View rootView, Vector2 position, SpriteFont windowTextFont)
			: base(GeeUI, rootView)
		{
			Position.Value = position;
			WindowTextFont = windowTextFont;
			NinePatchNormal = GeeUIMain.NinePatchWindowUnselected;
			NinePatchSelected = GeeUIMain.NinePatchWindowSelected;
		}

		protected internal void FollowMouse()
		{
			if (!Draggable) return;
			Vector2 newMousePosition = InputManager.GetMousePosV();
			if (SelectedOffChildren && Selected && InputManager.IsMousePressed(MouseButton.Left))
			{
				Position.Value = (newMousePosition - MouseSelectedOffset);
			}
			LastMousePosition = newMousePosition;
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			NinePatch patch = Selected ? NinePatchSelected : NinePatchNormal;

			patch.Draw(spriteBatch, AbsolutePosition, Width - patch.LeftWidth - patch.RightWidth, (int)WindowTextFont.MeasureString(WindowText).Y, 0f, EffectiveOpacity);

			string text = TextView.TruncateString(WindowText, WindowTextFont, WindowContentView.ContentBoundBox.Width);
			spriteBatch.DrawString(WindowTextFont, text, AbsolutePosition + new Vector2(patch.LeftWidth, patch.TopHeight), Color.Black * EffectiveOpacity);

			if(WindowContentView != null)
			{
				WindowContentView.Width = Width;
				Vector2 windowTextSize = WindowTextFont.MeasureString(WindowText);
				var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);
				WindowContentView.Height.Value = Height.Value - (int)barHeight;
			}

			base.Draw(spriteBatch);
		}

		public override void Update(float dt)
		{
			FollowMouse();
			if (WindowContentView != null)
			{
				WindowContentView.IgnoreParentBounds.Value = true;

				NinePatch patch = Selected ? NinePatchSelected : NinePatchNormal;
				Vector2 windowTextSize = WindowTextFont.MeasureString(WindowText);
				var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);

				this.Width = WindowContentView.Width;
				this.Height.Value = WindowContentView.Height.Value + (int)barHeight;
			}

			base.Update(dt);
		}

		public override void OnMClick(Vector2 position, bool fromChild = false)
		{
			SelectedOffChildren = !fromChild;
			Selected.Value = true;
			WindowContentView.Selected.Value = true;
			LastMousePosition = position;
			MouseSelectedOffset = position - Position;

			if (ParentView.Value != null)
				ParentView.Value.BringChildToFront(this);
			FollowMouse();
			base.OnMClick(position, true);
		}

		public override void OnMClickAway(bool fromChild = false)
		{
			SelectedOffChildren = false;
			Selected.Value = false;
			WindowContentView.Selected.Value = false;
			base.OnMClickAway(true);
		}
		public override void OnMOff(bool fromChild = false)
		{
			FollowMouse();
			base.OnMOff(true);
		}

		public override void OnMOver(bool fromChild = false)
		{
			FollowMouse();
			base.OnMOver(fromChild);
		}
	}
}
