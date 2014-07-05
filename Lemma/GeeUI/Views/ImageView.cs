using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GeeUI.Views
{
    public class ImageView : View
    {
        public Texture2D Texture;

        public override Rectangle BoundBox
        {
            get
            {
                return new Rectangle(RealX, RealY, (int)ScaledImageSize.X, (int)ScaledImageSize.Y);
            }
        }

        public Vector2 ScaleVector
        {
            get
            {
                return new Vector2(Width / (float)Texture.Width, Height / (float)Texture.Height);
            }
            set
            {
				Width.Value = (int)(Texture.Width * value.X);
				Height.Value = (int)(Texture.Height * value.Y);
            }
        }

        public Vector2 ScaledImageSize
        {
            get
            {
                return new Vector2(Width * ScaleVector.X, Height * ScaleVector.Y);
            }
        }


		public ImageView(GeeUIMain GeeUI, View rootView, Texture2D texture)
			: base(GeeUI, rootView)
        {
            Texture = texture;
			Width.Value = texture.Width;
			Height.Value = texture.Height;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {

            spriteBatch.Draw(Texture, AbsolutePosition, null, Color.White * EffectiveOpacity, 0f, Vector2.Zero, ScaleVector, SpriteEffects.None, 0f);

            base.Draw(spriteBatch);
        }
    }
}
