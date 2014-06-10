using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;

namespace GeeUI.Views
{
    internal class TabView : View
    {
        public NinePatch NinePatchSelected = new NinePatch();
        public NinePatch NinePatchDefault = new NinePatch();

        public SpriteFont TabFont;

        public string TabText
        {
            get { return TabTextView.Text; }
            set
            {
                TabTextView.Text.Value = value;
				TabTextView.Width.Value = (int)TabFont.MeasureString(value).X;
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
                if (Children.Length == 0)
                {
                    AddChild(value);
                    return;
                }
                _children[0] = value;
                ReOrderChildrenDepth();
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

		public TabView(GeeUIMain GeeUI, View rootView, Vector2 position, SpriteFont font)
            : base(GeeUI, rootView)
        {
			Position.Value = position;
            TabFont = font;
			NumChildrenAllowed.Value = 1;

            NinePatchDefault = GeeUIMain.NinePatchTabDefault;
            NinePatchSelected = GeeUIMain.NinePatchTabSelected;
			this.Height.Value = 25;
            new TextView(GeeUI, this, "", Vector2.Zero, font) {  };
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            var p = (TabContainer)ParentView;
            p.TabClicked(this);

            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
            base.OnMClickAway();
        }

        public override void OnMOver(bool fromChild = false)
        {
            base.OnMOver();
        }
        public override void OnMOff(bool fromChild = false)
        {
            base.OnMOff();
        }

        public override void Update(float dt)
        {
			TabTextView.Width.Value = Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth;
			if (Width >= ParentView.Width) Width.Value = ParentView.Width - 1;
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
