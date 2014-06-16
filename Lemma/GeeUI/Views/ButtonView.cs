using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.Managers;

namespace GeeUI.Views
{

    public class ButtonView : View
    {
        public NinePatch NinePatchNormal;
        public NinePatch NinePatchHover;
        public NinePatch NinePatchClicked;

        public View ButtonContentview
        {
            get {
                return Children.Length == 0 ? null : Children[0];
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

        public string Text
        {
            get
            {
                if (!(Children[0] is TextView))
                {
                    return "";
                }
                var c = (TextView) Children[0];
                return c.Text;
            }
            set
            {
                if (!(Children[0] is TextView)) return;
                var c = (TextView)Children[0];
                c.Text.Value = value;
				Width.Value = (int)c.Font.MeasureString(c.Text.Value).X + NinePatchNormal.LeftWidth + NinePatchNormal.RightWidth;
				Height.Value = (int)c.Font.MeasureString(c.Text.Value).Y + NinePatchNormal.TopHeight + NinePatchNormal.BottomHeight;
            }
        }

        public NinePatch CurrentNinepatch
        {
            get
            {
                if (MouseOver || Selected)
                {
                    return InputManager.IsMousePressed(MouseButton.Left) ? NinePatchClicked : NinePatchHover;
                }
                return NinePatchNormal;
            }
        }

		public ButtonView(GeeUIMain GeeUI, View rootView, string text, Vector2 position, SpriteFont font)
            : base(GeeUI, rootView)
        {
            NinePatchNormal = GeeUIMain.NinePatchBtnDefault;
            NinePatchHover = GeeUIMain.NinePatchBtnHover;
            NinePatchClicked = GeeUIMain.NinePatchBtnClicked;
			Position.Value = position;

            //Make the TextView for the text
            new TextView(this.ParentGeeUI, this, text, new Vector2(0, 0), font) {TextJustification = TextJustification.Left};
			Width.Value = (int)font.MeasureString(text).X + NinePatchNormal.LeftWidth + NinePatchNormal.RightWidth;
			Height.Value = (int)font.MeasureString(text).Y + NinePatchNormal.TopHeight + NinePatchNormal.BottomHeight;
        }

		public ButtonView(GeeUIMain GeeUI, View rootview, View contentView, Vector2 position)
			: base(GeeUI, rootview)
        {
            NinePatchNormal = GeeUIMain.NinePatchBtnDefault;
            NinePatchHover = GeeUIMain.NinePatchBtnHover;
            NinePatchClicked = GeeUIMain.NinePatchBtnClicked;
			Position.Value = position;
            ButtonContentview = contentView;
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
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

        public override void Draw(SpriteBatch spriteBatch)
        {
            NinePatch patch = CurrentNinepatch;
            int width = Width - patch.LeftWidth - patch.RightWidth;
            int height = Height - patch.TopHeight - patch.BottomHeight;

            patch.Draw(spriteBatch, AbsolutePosition, width, height, 0f, EffectiveOpacity);

            View childView = ButtonContentview;
            if (childView != null)
            {
				childView.Width.Value = width;
				childView.Height.Value = height;
                childView.X = patch.LeftWidth;
                childView.Y = patch.TopHeight;
            }

            base.Draw(spriteBatch);
        }
    }
}
