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
				return Children.Length == 0 ? null : Children[0];
			}
			set
			{
				if (this.Children.Length == 0)
					this.Children.Add(value);
				else
					this.Children[0] = value;
			}
		}

		public override Rectangle ContentBoundBox
		{
			get
			{
				NinePatch patch = Selected ? NinePatchSelected : NinePatchNormal;
				Vector2 windowTextSize = GeeUIMain.Font.MeasureString(WindowText);
				var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);
				return new Rectangle(RealX, RealY + (int)barHeight, Width, Height - (int)barHeight);
			}
		}


		public WindowView(GeeUIMain GeeUI, View rootView, Vector2 position)
			: base(GeeUI, rootView)
		{
			Position.Value = position;
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

			patch.Draw(spriteBatch, AbsolutePosition, Width - patch.LeftWidth - patch.RightWidth, (int)GeeUIMain.Font.MeasureString(WindowText).Y, 0f, EffectiveOpacity);

			string text = TextView.TruncateString(WindowText, WindowContentView.ContentBoundBox.Width);
			spriteBatch.DrawString(GeeUIMain.Font, text, AbsolutePosition + new Vector2(patch.LeftWidth, patch.TopHeight), Color.Black * EffectiveOpacity);

			if(WindowContentView != null)
			{
				WindowContentView.Width = Width;
				Vector2 windowTextSize = GeeUIMain.Font.MeasureString(WindowText);
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
				Vector2 windowTextSize = GeeUIMain.Font.MeasureString(WindowText);
				var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);

				this.Width = WindowContentView.Width;
				this.Height.Value = WindowContentView.Height.Value + (int)barHeight;
			}

			base.Update(dt);
		}

		public override void OnMClick(Vector2 position, bool fromChild)
		{
			SelectedOffChildren = !fromChild;
			Selected.Value = true;
			WindowContentView.Selected.Value = true;
			LastMousePosition = position;
			MouseSelectedOffset = position - Position;

			this.BringToFront();
			FollowMouse();
			base.OnMClick(position, fromChild);
		}

		public override void OnMClickAway()
		{
			SelectedOffChildren = false;
			Selected.Value = false;
			WindowContentView.Selected.Value = false;
			base.OnMClickAway();
		}
		public override void OnMOff()
		{
			FollowMouse();
			base.OnMOff();
		}

		public override void OnMOver()
		{
			FollowMouse();
			base.OnMOver();
		}
	}
}
