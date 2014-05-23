using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeeUI.Views;
using GeeUI.Managers;
using GeeUI.Structs;
using Microsoft.Xna.Framework;

namespace GeeUI.ViewLayouts
{
    public class SpinViewLayout : ViewLayout
    {
        private int _radius = 10;
        private float timeStep = 0;
        private float step = 1;

        /// <summary>
        /// Creates a new SpinViewLayout with the specified radius
        /// </summary>
        /// <param name="radius">Radius of the spin</param>
        public SpinViewLayout(int radius = 10, float step = 1)
        {
            _radius = radius;
            this.step = step;
        }

        public override void OrderChildren(View parentView)
        {
            timeStep += step;
            double angle = timeStep % 360;
            double angleStep = (360/parentView.Children.Length);

            Vector2 center = new Vector2(parentView.ContentBoundBox.Width / 2, parentView.ContentBoundBox.Height / 2);

            foreach(View child in parentView.Children)
            {
                double rads = ConversionManager.DegreeToRadians(angle);

                child.X = (int) (-_radius * Math.Sin(rads));
                child.Y = (int) (_radius * Math.Cos(rads));

                child.Position += center;
                child.Position -= new Vector2(child.Width / 2, child.Height / 2);

                angle += angleStep;
            }
        }
    }
}
