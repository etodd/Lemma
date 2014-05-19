using GeeUI.Structs;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GeeUI.Managers;

namespace GeeUI.Views
{
    public class WindowView : View
    {
        public NinePatch NinePatchSelected = new NinePatch();
        public NinePatch NinePatchNormal = new NinePatch();

        public string WindowText = "Hello this is a VIEW!";
        public SpriteFont WindowTextFont;

        protected internal bool SelectedOffChildren;
        protected internal Vector2 LastMousePosition = Vector2.Zero;
        protected internal Vector2 MouseSelectedOffset = Vector2.Zero;

        /// <summary>
        /// If true, the window can be dragged by the user.
        /// </summary>
        public bool Draggable = true;

        public View WindowContentView
        {
            get
            {
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

        public override Rectangle BoundBox
        {
            get
            {
                return new Rectangle(X, Y, Width, Height);
            }
        }

        public override Rectangle ContentBoundBox
        {
            get
            {
                NinePatch patch = Selected ? NinePatchSelected : NinePatchNormal;
                Vector2 windowTextSize = WindowTextFont.MeasureString(WindowText);
                var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);
                return new Rectangle(X, Y + (int)barHeight, Width, Height - (int)barHeight);
            }
        }


        public WindowView(View rootView, Vector2 position, SpriteFont windowTextFont)
            : base(rootView)
        {
            Position = position;
            WindowTextFont = windowTextFont;
            NinePatchNormal = GeeUI.NinePatchWindowUnselected;
            NinePatchSelected = GeeUI.NinePatchWindowSelected;
        }

        protected internal void FollowMouse()
        {
            if (!Draggable) return;
            Vector2 newMousePosition = InputManager.GetMousePosV();
            if (SelectedOffChildren && Selected && InputManager.IsMousePressed(MouseButton.Left))
            {
                Position = (newMousePosition - MouseSelectedOffset);
            }
            LastMousePosition = newMousePosition;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            NinePatch patch = Selected ? NinePatchSelected : NinePatchNormal;

            patch.Draw(spriteBatch, AbsolutePosition, Width - patch.LeftWidth - patch.RightWidth, (int)WindowTextFont.MeasureString(WindowText).Y);

            string text = TextView.TruncateString(WindowText, WindowTextFont, WindowContentView.ContentBoundBox.Width);
            spriteBatch.DrawString(WindowTextFont, text, AbsolutePosition + new Vector2(patch.LeftWidth, patch.TopHeight), Color.Black);

            if(WindowContentView != null)
            {
                WindowContentView.Width = Width;
                Vector2 windowTextSize = WindowTextFont.MeasureString(WindowText);
                var barHeight = (patch.TopHeight + patch.BottomHeight + windowTextSize.Y);
                WindowContentView.Height = Height - (int)barHeight;
            }

            base.Draw(spriteBatch);
        }

        public override void Update(GameTime theTime)
        {
            FollowMouse();
            if (WindowContentView != null)
            {
                WindowContentView.Position = new Vector2(0, 0);
            }

            base.Update(theTime);
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            SelectedOffChildren = !fromChild;
            Selected = true;
            WindowContentView.Selected = true;
            LastMousePosition = position;
            MouseSelectedOffset = position - Position;

            if (ParentView != null)
                ParentView.BringChildToFront(this);
            FollowMouse();
            base.OnMClick(position, true);
        }

        public override void OnMClickAway(bool fromChild = false)
        {
            SelectedOffChildren = false;
            Selected = false;
            WindowContentView.Selected = false;
            base.OnMClickAway(true);
        }
        public override void OnMOff(bool fromChild = false)
        {
            FollowMouse();
            base.OnMOff(true);
        }

        public override void OnMOver(bool fromChild = false)
        {
            FollowMouse();
            base.OnMOver(fromChild);
        }
    }
}
