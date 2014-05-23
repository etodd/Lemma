using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.Managers;

namespace GeeUI.Views
{
	/// <summary>
	/// An empty class used as a template. I guess.
	/// </summary>
    public class EmptyView : View
    {

		public EmptyView(GeeUIMain GeeUI, View rootView)
            : base(GeeUI, rootView)
        {
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
            
            base.Draw(spriteBatch);
        }
    }
}
