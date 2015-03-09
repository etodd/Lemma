using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;

namespace GeeUI.Views
{
	internal class TabView : View
	{
		public NinePatch NinePatchSelected = new NinePatch();
		public NinePatch NinePatchDefault = new NinePatch();

		public string TabText
		{
			get { return TabTextView.Text; }
			set
			{
				TabTextView.Text.Value = value;
				TabTextView.Width.Value = (int)GeeUIMain.Font.MeasureString(value).X;
				TabTextView.Height.Value = Height - CurNinepatch.TopHeight - CurNinepatch.BottomHeight;
				this.Width.Value = TabTextView.Width + CurNinepatch.LeftWidth + CurNinepatch.RightWidth;
			}
		}

		private TextView TabTextView
		{
			get
			{
				return Children.Length == 0 ? null : (TextView)Children[0];
			}
			set
			{
				if (this.Children.Length == 0)
					this.Children.Add(value);
				else
					this.Children[0] = value;
			}
		}

		public NinePatch CurNinepatch
		{
			get { return Selected ? NinePatchSelected : NinePatchDefault; }
		}


		public override Rectangle ContentBoundBox
		{
			get
			{
				return new Rectangle(RealX + CurNinepatch.LeftWidth, RealY + CurNinepatch.TopHeight,
						Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth,
						Height - CurNinepatch.TopHeight - CurNinepatch.BottomHeight);
			}
		}

		public TabView(GeeUIMain GeeUI, View rootView, Vector2 position)
			: base(GeeUI, rootView)
		{
			Position.Value = position;
			this.numChildrenAllowed = 1;

			NinePatchDefault = GeeUIMain.NinePatchTabDefault;
			NinePatchSelected = GeeUIMain.NinePatchTabSelected;

			// HACK
			this.Height.Value = (int)(25.0f * GeeUI.Main.FontMultiplier);

			new TextView(GeeUI, this, "", Vector2.Zero) {  };
		}

		public override void OnMClick(Vector2 position, bool fromChild)
		{
			var p = (TabContainer)ParentView;
			p.TabClicked(this);

			base.OnMClick(position, fromChild);
		}

		public override void Update(float dt)
		{
			TabTextView.Width.Value = Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth;
			if (Width >= ParentView.Value.Width) Width.Value = ParentView.Value.Width - 1;
			TabTextView.X = CurNinepatch.LeftWidth;
			TabTextView.Y = CurNinepatch.TopHeight;
			base.Update(dt);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			var width = Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth;
			var height = Height - CurNinepatch.TopHeight - CurNinepatch.BottomHeight;

			CurNinepatch.Draw(spriteBatch, AbsolutePosition, width, height, 0f, EffectiveOpacity);
			base.Draw(spriteBatch);
		}
	}
}
