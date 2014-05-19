using System;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GeeUI.Managers;

namespace GeeUI.Structs
{
    public class NinePatch
    {
        public int LeftMostPatch;
        public int RightMostPatch;
        public int TopMostPatch;
        public int BottomMostPatch;

        public int LeftWidth
        {
            get
            {
                return LeftMostPatch - 1;
            }
        }
        public int RightWidth
        {
            get
            {
                if (Texture != null)
                    return Texture.Width - (RightMostPatch + 1);
                return 0;
            }
        }
        public int TopHeight
        {
            get
            {
                return TopMostPatch - 1;
            }
        }
        public int BottomHeight
        {
            get
            {
                if (Texture != null)
                    return Texture.Height - (BottomMostPatch + 1);
                return 0;
            }
        }

        public Texture2D Texture;

        public NinePatch()
        {
            LeftMostPatch = -1;
            RightMostPatch = -1;
            TopMostPatch = -1;
            BottomMostPatch = -1;
            Texture = null;
        }

        /// <summary>
        /// Method to determine if a texture has ninepatch data inside of it
        /// </summary>
        /// <param name="texture">The texture to test against</param>
        /// <returns>True if the texture is compatible with ninepatches, false otherwise</returns>
        public static bool IsAlreadyNinepatch(Texture2D texture)
        {
            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);

            for (int i = 0; i < texture.Width; i++)
            {
                Color curPixel = data[i];
                int a = curPixel.A;
                int r = curPixel.R;
                int g = curPixel.G;
                int b = curPixel.B;

                if (a != 0 && (r != 0 || g != 0 || b != 0))
                {
                    //Is not black and is not transparent.
                    return false;
                }
            }
            for (int i = data.Length - (texture.Width + 1); i < data.Length; i++)
            {
                Color curPixel = data[i];
                int a = curPixel.A;
                int r = curPixel.R;
                int g = curPixel.G;
                int b = curPixel.B;
                if (a != 0 && (r != 0 || g != 0 || b != 0))
                {
                    //Is not black and is not transparent.
                    return false;
                }
            }
            for (int i = 0; i < data.Length; i += texture.Width)
            {
                Color curPixel = data[i];
                int a = curPixel.A;
                int r = curPixel.R;
                int g = curPixel.G;
                int b = curPixel.B;
                if (a != 0 && (r != 0 || g != 0 || b != 0))
                {
                    //Is not black and is not transparent.
                    return false;
                }
            }
            for (int i = texture.Width - 1; i < data.Length; i += texture.Width)
            {
                Color curPixel = data[i];
                int a = curPixel.A;
                int r = curPixel.R;
                int g = curPixel.G;
                int b = curPixel.B;
                if (a != 0 && (r != 0 || g != 0 || b != 0))
                {
                    //Is not black and is not transparent.
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Load the NinePatch data from a texture.
        /// </summary>
        /// <param name="texture">The NinePatch-compatible texture to load from.</param>
        public void LoadFromTexture(Texture2D texture)
        {
            LeftMostPatch = -1;
            RightMostPatch = -1;
            TopMostPatch = -1;
            BottomMostPatch = -1;
            Texture = texture;
            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            for (int i = 0; i < texture.Width; i++)
            {
                Color curPixel = data[i];
                if (curPixel.A != 0)
                {
                    if (LeftMostPatch == -1) LeftMostPatch = i;
                }
                if (curPixel.A != 0 && LeftMostPatch != -1)
                {
                    RightMostPatch = i;
                }
                if (curPixel.A == 0 && LeftMostPatch != -1 && RightMostPatch != -1)
                    break;
            }
            for (int i = 0; i < data.Length; i += texture.Width)
            {
                Color curPixel = data[i];
                if (curPixel.A != 0)
                {
                    if (TopMostPatch == -1) TopMostPatch = i / texture.Width;
                }
                if (curPixel.A != 0 && TopMostPatch != -1)
                {
                    BottomMostPatch = (i / texture.Width);
                }
                if (curPixel.A == 0 && TopMostPatch != -1 && BottomMostPatch != -1)
                    break;
            }
        }

        /// <summary>
        /// Draws the ninepatch at the specified point
        /// </summary>
        /// <param name="sb">The spritebatch to use for drawing</param>
        /// <param name="position">The position to draw it at (top left)</param>
        /// <param name="contentWidth">The width of the content inside the Ninepatch</param>
        /// <param name="contentHeight">The height of the content inside the Ninepatch</param>
        /// <param name="angle">The angle in degrees to rotate the ninepatch.</param>
        public void Draw(SpriteBatch sb, Vector2 position, int contentWidth, int contentHeight, float angle = 0, float alpha = 1)
        {
            var topLeft = new Rectangle(1, 1, LeftMostPatch - 1, TopMostPatch - 1);
            var topMiddle = new Rectangle(LeftMostPatch, 1, (RightMostPatch - LeftMostPatch), TopMostPatch - 1);
            var topRight = new Rectangle(RightMostPatch + 1, 1, (Texture.Width - 1) - RightMostPatch, TopMostPatch - 1);

            var left = new Rectangle(1, TopMostPatch, LeftMostPatch - 1, (BottomMostPatch - TopMostPatch));
            var middle = new Rectangle(LeftMostPatch, TopMostPatch, (RightMostPatch - LeftMostPatch), (BottomMostPatch - TopMostPatch));
            var right = new Rectangle(RightMostPatch + 1, TopMostPatch, (Texture.Width - 1) - RightMostPatch, (BottomMostPatch - TopMostPatch));

            var bottomLeft = new Rectangle(1, BottomMostPatch, LeftMostPatch - 1, (Texture.Height - 1) - BottomMostPatch);
            var bottomMiddle = new Rectangle(LeftMostPatch, BottomMostPatch, (RightMostPatch - LeftMostPatch), (Texture.Height - 1) - BottomMostPatch);
            var bottomRight = new Rectangle(RightMostPatch + 1, BottomMostPatch, (Texture.Width - 1) - RightMostPatch, (Texture.Height - 1) - BottomMostPatch);

            int topMiddleWidth = topMiddle.Width;
            int leftMiddleHeight = left.Height;
            float scaleMiddleByHorizontally = (contentWidth / (float)topMiddleWidth);
            float scaleMiddleByVertically = (contentHeight / (float)leftMiddleHeight);

            Vector2 drawTl = position;
            Vector2 drawT = drawTl + new Vector2(topLeft.Width, 0);
            Vector2 drawTr = drawT + new Vector2(topMiddle.Width * scaleMiddleByHorizontally, 0);

            Vector2 drawL = drawTl + new Vector2(0, topLeft.Height);
            Vector2 drawM = drawT + new Vector2(0, topMiddle.Height);
            Vector2 drawR = drawTr + new Vector2(0, topRight.Height);

            Vector2 drawBl = drawL + new Vector2(0, leftMiddleHeight * scaleMiddleByVertically);
            Vector2 drawBm = drawM + new Vector2(0, leftMiddleHeight * scaleMiddleByVertically);
            Vector2 drawBr = drawR + new Vector2(0, leftMiddleHeight * scaleMiddleByVertically);

            drawTl = RotateAroundOrigin(drawTl, position, angle);
            drawT = RotateAroundOrigin(drawT, position, angle);
            drawTr = RotateAroundOrigin(drawTr, position, angle);

            drawL = RotateAroundOrigin(drawL, position, angle);
            drawM = RotateAroundOrigin(drawM, position, angle);
            drawR = RotateAroundOrigin(drawR, position, angle);

            drawBl = RotateAroundOrigin(drawBl, position, angle);
            drawBm = RotateAroundOrigin(drawBm, position, angle);
            drawBr = RotateAroundOrigin(drawBr, position, angle);

            var angR = (float)ConversionManager.DegreeToRadians(angle);

            sb.Draw(Texture, drawTl, topLeft, Color.White * alpha, angR, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
			sb.Draw(Texture, drawT, topMiddle, Color.White * alpha, angR, Vector2.Zero, new Vector2(scaleMiddleByHorizontally, 1), SpriteEffects.None, 0f);
			sb.Draw(Texture, drawTr, topRight, Color.White * alpha, angR, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

			sb.Draw(Texture, drawL, left, Color.White * alpha, angR, Vector2.Zero, new Vector2(1, scaleMiddleByVertically), SpriteEffects.None, 0f);
			sb.Draw(Texture, drawM, middle, Color.White * alpha, angR, Vector2.Zero, new Vector2(scaleMiddleByHorizontally, scaleMiddleByVertically), SpriteEffects.None, 0f);
			sb.Draw(Texture, drawR, right, Color.White * alpha, angR, Vector2.Zero, new Vector2(1, scaleMiddleByVertically), SpriteEffects.None, 0f);

			sb.Draw(Texture, drawBl, bottomLeft, Color.White * alpha, angR, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
			sb.Draw(Texture, drawBm, bottomMiddle, Color.White * alpha, angR, Vector2.Zero, new Vector2(scaleMiddleByHorizontally, 1), SpriteEffects.None, 0f);
			sb.Draw(Texture, drawBr, bottomRight, Color.White * alpha, angR, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Draws a box where the content of a NinePatch will be placed.
        /// Does not draw the NinePatch.
        /// </summary>
        /// <param name="sb">The drawing SpriteBatch</param>
        /// <param name="position">The position to draw (top left)</param>
        /// <param name="contentWidth">The width of the content inside the Ninepatch</param>
        /// <param name="contentHeight">The height of the content inside the Ninepatch</param>
        /// <param name="drawColor">The color to draw </param>
        public void DrawContent(SpriteBatch sb, Vector2 position, int contentWidth, int contentHeight, Color drawColor, float alpha)
        {
			//Multiplying by alpha
			drawColor.A = 255;
            var topLeft = new Rectangle(1, 1, LeftMostPatch - 1, TopMostPatch - 1);
            var topMiddle = new Rectangle(LeftMostPatch, 1, (RightMostPatch - LeftMostPatch), TopMostPatch - 1);

            var left = new Rectangle(1, TopMostPatch, LeftMostPatch - 1, (BottomMostPatch - TopMostPatch));
            var middle = new Rectangle(LeftMostPatch, TopMostPatch, (RightMostPatch - LeftMostPatch), (BottomMostPatch - TopMostPatch));

            int topMiddleWidth = topMiddle.Width;
            int leftMiddleHeight = left.Height;
            float scaleMiddleByHorizontally = (contentWidth / (float)topMiddleWidth);
            float scaleMiddleByVertically = (contentHeight / (float)leftMiddleHeight);


            Vector2 drawTL = position;
            Vector2 drawT = drawTL + new Vector2(topLeft.Width, 0);

            Vector2 drawM = drawT + new Vector2(0, topMiddle.Height);

            var bottomRight = new Vector2(drawM.X + (middle.Width * scaleMiddleByHorizontally), drawM.Y + (middle.Height * scaleMiddleByVertically));
			DrawManager.DrawBox(drawM, bottomRight, drawColor * alpha, sb, 0f, 150);
        }

        /// <summary>
        /// Gets the center of a NinePatch with the defined width and height
        /// </summary>
        /// <param name="contentWidth">The width of the NinePatch content</param>
        /// <param name="contentHeight">The height of the NinePatch content</param>
        /// <returns></returns>
        public Vector2 GetCenter(int contentWidth, int contentHeight)
        {
            var topLeft = new Rectangle(1, 1, LeftMostPatch - 1, TopMostPatch - 1);
            var topMiddle = new Rectangle(LeftMostPatch, 1, (RightMostPatch - LeftMostPatch), TopMostPatch - 1);
            var left = new Rectangle(1, TopMostPatch, LeftMostPatch - 1, (BottomMostPatch - TopMostPatch));


            int topMiddleWidth = topMiddle.Width;
            int leftMiddleHeight = left.Height;
            float scaleMiddleByHorizontally = (contentWidth / (float)topMiddleWidth);
            float scaleMiddleByVertically = (contentHeight / (float)leftMiddleHeight);
            if (scaleMiddleByVertically < 1) scaleMiddleByVertically = 1;
            if (scaleMiddleByHorizontally < 1) scaleMiddleByHorizontally = 1;

            var drawMMiddle = new Vector2(topLeft.Width, topLeft.Height);
            drawMMiddle += new Vector2(topMiddleWidth * (scaleMiddleByHorizontally / 2), leftMiddleHeight * (scaleMiddleByVertically / 2));
            return drawMMiddle;
        }

        /// <summary>
        /// Rotates a point around a specified origin with the specified angle
        /// </summary>
        /// <param name="point">The point to rotate</param>
        /// <param name="origin">The rotation point of the point</param>
        /// <param name="angle">The angle in degrees of the angle to rotate the point by.</param>
        /// <returns></returns>
        public static Vector2 RotateAroundOrigin(Vector2 point, Vector2 origin, double angle)
        {
            Vector2 real = point - origin;
            Vector2 ret = Vector2.Zero;

            //We need to use radians for Math.* functions
            angle = ConversionManager.DegreeToRadians(angle);
            ret.X = (float)((real.X * Math.Cos(angle)) - (real.Y * Math.Sin(angle)));
            ret.Y = (float)((real.X * Math.Sin(angle)) + (real.Y * Math.Cos(angle)));

            ret += origin;
            return ret;
        }

        /// <summary>
        /// Rotates a point with the specified angle
        /// </summary>
        /// <param name="point">The point to rotate</param>
        /// <param name="angle">The angle in degrees of the angle to rotate the point by.</param>
        /// <returns></returns>
        public static Vector2 RotatePoint(Vector2 point, double angle)
        {
            Vector2 real = point;
            Vector2 ret = Vector2.Zero;

            //We need to use radians for Math.* functions
            angle = ConversionManager.DegreeToRadians(angle);
            ret.X = (float)((real.X * Math.Cos(angle)) - (real.Y * Math.Sin(angle)));
            ret.Y = (float)((real.X * Math.Sin(angle)) + (real.Y * Math.Cos(angle)));

            return ret;
        }
    }
}
