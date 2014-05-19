using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.Managers;

namespace GeeUI.Views
{
    public class SliderView : View
    {
        public delegate void SliderValueChangedHandler(object sender, EventArgs e);

        public event SliderValueChangedHandler OnSliderValueChanged;

        public Texture2D SliderDefault;
        public Texture2D SliderSelected;

        public NinePatch SliderRange = new NinePatch();

        private int _min;
        private int _max;

        private bool _clicked;

        private bool _drawText;
        public bool DrawText
        {
            get
            {
                return _drawText && TextFont != null;
            }
            set
            {
                //Only let text be drawn if the user has set the font.
                _drawText = TextFont != null && value;
                if (TextFont == null)
                {
                    throw new Exception("Cannot set SliderView.drawText to true unless textFont is set.");
                }       
            }
        }

        public SpriteFont TextFont;
        public Color TextColor = Color.Black;

        public int CurrentValue
        {
            get
            {
                float percent = (SliderPosition) / (float)(Width - SliderRange.LeftWidth - SliderRange.RightWidth);
                return (int)(_min + ((_max - _min) * percent));
            }
        }

        public int SliderPosition;

        public Texture2D CurSliderTexture
        {
            get
            {
                return MouseOver || Selected || _clicked ? SliderSelected : SliderDefault;
            }
        }

        public override Rectangle BoundBox
        {
            get
            {
                return new Rectangle(X, Y, Width, (int)MathHelper.Max(SliderRange.Texture.Height, SliderDefault.Height));
            }
        }

        public SliderView(View rootView, Vector2 position, int min, int max)
            : base(rootView)
        {
            SliderRange = GeeUI.NinePatchSliderRange;
            SliderDefault = GeeUI.TextureSliderDefault;
            SliderSelected = GeeUI.TextureSliderSelected;

            _min = min;
            _max = max;

            Position = position;
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            SliderCalc(position);
            _clicked = true;
            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
            _clicked = false;
            //base.onMClickAway();
        }

        public override void OnMOver(bool fromChild = false)
        {
            Vector2 position = InputManager.GetMousePosV();
            SliderCalc(position);
            base.OnMOver();
        }
        public override void OnMOff(bool fromChild = false)
        {
            Vector2 position = InputManager.GetMousePosV();
            SliderCalc(position);
            base.OnMOff();
        }

        private void SliderCalc(Vector2 position)
        {
            if (!_clicked) return;
            if (InputManager.IsMousePressed(MouseButton.Left))
            {
                SliderPosition = (int)MathHelper.Clamp((int)(position.X - AbsoluteX + SliderRange.LeftWidth), 0, Width - SliderRange.RightWidth);
                if (OnSliderValueChanged != null)
                    OnSliderValueChanged(null, null);
            }
            else
            {
                _clicked = false;
            }
        }


        public override void Update(GameTime theTime)
        {
            if (_min > _max)
            {
                throw new Exception("The minimum value of a slider cannot be above the maximum.");
            }
            if (SliderPosition > Width) SliderPosition = Width;
            if (SliderPosition < 0) SliderPosition = 0;
            base.Update(theTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            //We want to preserve the slider skin's original height.
            SliderRange.Draw(spriteBatch, AbsolutePosition, Width, SliderRange.BottomMostPatch - SliderRange.TopMostPatch);
            spriteBatch.Draw(CurSliderTexture, new Vector2(AbsoluteX + SliderRange.LeftWidth - (CurSliderTexture.Width) + SliderPosition, AbsoluteY), null, Color.White, 0f, new Vector2(CurSliderTexture.Width / -2, 0), 1f, SpriteEffects.None, 0f);
            if (DrawText)
            {
                int drawX = AbsoluteX + (Width) / 2;
                int drawY = AbsoluteY;
                Vector2 offset = TextFont.MeasureString(CurrentValue.ToString());
                offset.X = (int)(offset.X / 2);
                spriteBatch.DrawString(TextFont, CurrentValue.ToString(), new Vector2(drawX, drawY), TextColor, 0f, offset, 1f, SpriteEffects.None, 0f);
            }
            base.Draw(spriteBatch);
        }
    }
}
