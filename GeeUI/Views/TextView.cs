using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GeeUI.Managers;

namespace GeeUI.Views
{
    public class TextView : View
    {
        public SpriteFont Font;

        public string Text;

        public Color TextColor = Color.Black;

        public TextJustification TextJustification = TextJustification.Left;

        public override Rectangle BoundBox
        {
            get
            {
                return new Rectangle(X, Y, Width, Height);
            }
        }

        private Vector2 TextOrigin
        {
            get
            {
                var width = (int)Font.MeasureString(shortenedText).X;
                var height = (int) Font.MeasureString(shortenedText).Y;
                switch (TextJustification)
                {
                    default:
                        return new Vector2(0, 0);
                    case TextJustification.Center:
                        return new Vector2(-((Width / 2) - (width / 2)), -((Height / 2) - (height / 2)));

                    case TextJustification.Right:
                        return new Vector2(-((Width / 2) + (width / 2)), 0);
                }
            }
        }

        private string shortenedText
        {
            get
            {
                var tWidth = Font.MeasureString(Text).X;
                if (tWidth > Width)
                {
                    string testingCur = "";
                    string ret = "";
                    foreach (char t in Text)
                    {
                        testingCur += t;
                        tWidth = Font.MeasureString(testingCur).X;
                        if(tWidth > Width)
                        {
                            string test = testingCur + "\na";
                            var height = Font.MeasureString(ret + test).Y;
                            if(height > Height)
                            {
                                for (int i = 0; i < testingCur.Length; i++ )
                                {
                                    string t2 = testingCur.Substring(0, testingCur.Length - (i + 1));
                                    var w = Font.MeasureString(t2 + "...").X;
                                    if (w <= Width) return ret + t2 + "...";
                                }
                                    return ret;
                            }
                            ret += testingCur + "\n";
                            testingCur = "";
                        }
                    }
                    return ret + testingCur;
                }
                return Text;
            }
        }

        public TextView(View rootView, string text, Vector2 position, SpriteFont font)
            : base(rootView)
        {
            Text = text;
            Position = position;
            Font = font;

        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
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
            spriteBatch.DrawString(Font, shortenedText, AbsolutePosition, TextColor, 0f, TextOrigin, 1f, SpriteEffects.None, 0f);
            base.Draw(spriteBatch);
        }

        internal static string TruncateString(string input, SpriteFont font, int widthAllowed, string ellipsis = "...")
        {
            string cur = "";
            foreach (char t in input)
            {
                float width = font.MeasureString(cur + t + ellipsis).X;
                if (width > widthAllowed)
                    break;
                cur += t;
            }
            return cur + (cur.Length != input.Length ? ellipsis : "");
        }
    }
    public enum TextJustification
    {
        Left,
        Center,
        Right
    }
}
