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
                TabTextView.Text = value;
                TabTextView.Width = (int)TabFont.MeasureString(value).X;
                TabTextView.Height = Height - CurNinepatch.TopHeight - CurNinepatch.BottomHeight;
                this.Width = TabTextView.Width + CurNinepatch.LeftWidth + CurNinepatch.RightWidth;
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

        public override Rectangle BoundBox
        {
            get
            {
                return new Rectangle(X, Y,
                        Width,
                        Height);
            }
        }

        public override Rectangle ContentBoundBox
        {
            get
            {
                return new Rectangle(X + CurNinepatch.LeftWidth, Y + CurNinepatch.TopHeight,
                        Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth,
                        Height - CurNinepatch.TopHeight - CurNinepatch.BottomHeight);
            }
        }

        public TabView(View rootView, Vector2 position, SpriteFont font)
            : base(rootView)
        {
            Position = position;
            TabFont = font;
            NumChildrenAllowed = 1;

            NinePatchDefault = GeeUI.NinePatchTabDefault;
            NinePatchSelected = GeeUI.NinePatchTabSelected;
            this.Height = 25;
            new TextView(this, "", Vector2.Zero, font) { TextJustification = TextJustification.Center };
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            var p = (TabContainer)ParentView;
            p.TabClicked(this);

            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
            //base.OnMClickAway();
        }

        public override void OnMOver(bool fromChild = false)
        {
            base.OnMOver();
        }
        public override void OnMOff(bool fromChild = false)
        {
            base.OnMOff();
        }

        public override void Update(GameTime theTime)
        {
            TabTextView.Width = Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth;
            if (Width >= ParentView.Width) Width = ParentView.Width - 1;
            base.Update(theTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var width = Width - CurNinepatch.LeftWidth - CurNinepatch.RightWidth;
            var height = Height - CurNinepatch.TopHeight - CurNinepatch.BottomHeight;

            CurNinepatch.Draw(spriteBatch, AbsolutePosition, width, height);
            base.Draw(spriteBatch);
        }
    }
}
