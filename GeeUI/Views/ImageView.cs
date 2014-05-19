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
                return new Rectangle(X, Y, (int)ScaledImageSize.X, (int)ScaledImageSize.Y);
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
                Width = (int)(Texture.Width * value.X);
                Height = (int)(Texture.Height * value.Y);
            }
        }

        public Vector2 ScaledImageSize
        {
            get
            {
                return new Vector2(Width * ScaleVector.X, Height * ScaleVector.Y);
            }
        }


        public ImageView(View rootView, Texture2D texture)
            : base(rootView)
        {
            Texture = texture;
            Width = texture.Width;
            Height = texture.Height;
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
            //base.onMClickAway();
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

            spriteBatch.Draw(Texture, AbsolutePosition, null, Color.White, 0f, Vector2.Zero, ScaleVector, SpriteEffects.None, 0f);

            base.Draw(spriteBatch);
        }
    }
}
