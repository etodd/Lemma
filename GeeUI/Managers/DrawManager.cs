using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace GeeUI.Managers
{
    public static class DrawManager
    {
        public static void DrawBezier(Vector2 point1, Vector2 point2, Color c, SpriteBatch b, byte alpha = 255)
        {
            Vector2 midpoint = ConversionManager.MidPoint(point1, point2);


            var c1 = new Vector2(midpoint.X, point1.Y);
            var c2 = new Vector2(midpoint.X, point2.Y);

            Vector2 lastPoint = point1;
            for (double t1 = 0; t1 <= 1; t1 += (double)1 / (double)30)
            {
                var t = (float)t1;

                Vector2 point = ((1 - t) * (1 - t) * (1 - t)) * point1 + 3 * ((1 - t) * (1 - t)) * t * c1 + 3 * (1 - t) * (t * t) * c2 + (t * t * t) * point2;

                DrawLine(point, lastPoint, c, b, alpha);

                lastPoint = point;
            }
        }

        public static void DrawLine(Vector2 point1, Vector2 point2, Color c, SpriteBatch b, byte alpha = 255)
        {
            Vector2 midpoint = ConversionManager.MidPoint(point1, point2);
            float xd = midpoint.X - point2.X;
            float yd = midpoint.Y - point2.Y;
            double rotation = Math.Atan2(yd, xd);
            float dist = Vector2.Distance(midpoint, point2) * 2;
            Texture2D t = GeeUI.White;
            c.A = alpha;
            b.Draw(t, point2, null, c, (float)rotation, Vector2.Zero, new Vector2(dist, 1), SpriteEffects.None, 0);
        }

        public static void DrawBox(Vector2 position, float width, float height, Color c, SpriteBatch b, float rotation = 0, byte alpha = 255, Effect e = null, bool wrong = false)
        {
            var topLeft = new Vector2(position.X - (width / 2), position.Y - (height / 2));
            var bottomRight = new Vector2(position.X + (width / 2), position.Y + (height / 2));

            DrawBox(topLeft, bottomRight, c, b, rotation, alpha, e, wrong);
        }

        public static void DrawBox(Rectangle box, Color c, SpriteBatch b, float rotation = 0, byte alpha = 255, Effect e = null, bool wrong = false)
        {
            var topLeft = new Vector2(box.Left, box.Top);
            var bottomRight = new Vector2(box.Right, box.Bottom);

            DrawBox(topLeft, bottomRight, c, b, rotation, alpha, e, wrong);
        }

        public static void DrawBox(Vector2 topLeft, Vector2 bottomRight, Color c, SpriteBatch b, float rotation = 0, byte alpha = 255, Effect e = null, bool wrong = false)
        {
            if (e != null)
            {
                b.End();
                b.Begin(0, BlendState.NonPremultiplied, null, null, null, e);
            }
            float height = topLeft.Y - bottomRight.Y;
            float width = topLeft.X - bottomRight.X;
            Texture2D t = GeeUI.White;
            c.A = alpha;
            Vector2 pos = bottomRight;
            b.Draw(t, pos, null, c, rotation, Vector2.Zero, new Vector2(width, height), SpriteEffects.None, 0);

            if (e == null) return;
            b.End();
            b.Begin();
        }

        public static void DrawCircle(Vector2 position, float radius, Color c, Color cOutline, SpriteBatch b, byte alpha = 255, byte cutoff = 0)
        {
            Effect circleShader = GeeUI.CircleShader;

            b.End();
            b.Begin(0, BlendState.NonPremultiplied, null, null, null, circleShader);
            float diameter = radius * 2;
            position.X -= radius;
            position.Y -= radius;
            circleShader.Parameters["aspect"].SetValue(diameter / diameter);
            circleShader.Parameters["cutoff"].SetValue(cutoff);
            circleShader.Parameters["outlineColor"].SetValue(new float[] { (float)cOutline.R / 255, (float)cOutline.G / 255, (float)cOutline.B / 255, (float)cOutline.A / 255 });
            Texture2D t = GeeUI.White;
            c.A = alpha;
            b.Draw(t, position, null, c, 0f, Vector2.Zero, diameter, SpriteEffects.None, 0);
            b.End();
            b.Begin();
        }

        public static void DrawOutline(Vector2 position, float width, float height, Color c, SpriteBatch b, byte alpha = 255, bool up = true, bool down = true, bool left = true, bool right = true)
        {
            var topLeft = new Vector2(position.X - (width / 2), position.Y - (height / 2));
            var bottomRight = new Vector2(position.X + (width / 2), position.Y + (height / 2));

            DrawOutline(topLeft, bottomRight, c, b, alpha, up, down, left, right);
        }

        public static void DrawOutline(Vector2 topLeft, Vector2 bottomRight, Color c, SpriteBatch b, byte alpha = 255, bool up = true, bool down = true, bool left = true, bool right = true)
        {
            var topRight = new Vector2(bottomRight.X, topLeft.Y);
            var bottomLeft = new Vector2(topLeft.X, bottomRight.Y);
            c.A = alpha;
            if (up)
                DrawLine(topLeft, new Vector2(topRight.X + 1, topRight.Y), c, b, alpha);
            if (left)
                DrawLine(topLeft, bottomLeft, c, b, alpha);
            if (right)
                DrawLine(topRight, bottomRight, c, b, alpha);
            if (down)
                DrawLine(bottomLeft, bottomRight, c, b, alpha);
        }

        public static bool PointInCircle(Vector2 origin, float radius, Vector2 point)
        {

            return ((point.X - origin.X) * (point.X - origin.X)) + ((point.Y - origin.Y) * (point.Y - origin.Y)) <= (radius * radius);
        }

        public static bool PointOnLine(Vector2 linePoint1, Vector2 linePoint2, Vector2 point)
        {
            float y1 = linePoint1.Y;
            float y2 = linePoint2.Y;
            float x1 = linePoint1.X;
            float x2 = linePoint2.X;
            float slope = (y1 - y2) / (x1 - x2);
            float intersect = -slope * x1 / y2;
            if (x1 - x2 == 0)
            {
                return point.Y == x1 || point.Y == x2;
            }
            return point.Y == (slope * point.X + intersect);
        }
    }
}
