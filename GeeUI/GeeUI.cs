using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GeeUI.Views;
using GeeUI.Structs;
using GeeUI.Managers;
namespace GeeUI
{
    public delegate void OnKeyPressed(string keyPressed, Keys key);
    public delegate void OnKeyReleased(string keyReleased, Keys key);
    public delegate void OnKeyContinuallyPressed(string keyContinuallyPressed, Keys key);

    public static class GeeUI
    {
        public static event OnKeyPressed OnKeyPressedHandler;
        public static event OnKeyReleased OnKeyReleasedHandler;
        public static event OnKeyContinuallyPressed OnKeyContinuallyPressedHandler;

        public static Texture2D White;
        public static Effect CircleShader;

        public static View RootView = new View();

        internal static Game TheGame;

        public static NinePatch NinePatchTextFieldDefault = new NinePatch();
        public static NinePatch NinePatchTextFieldSelected = new NinePatch();
        public static NinePatch NinePatchTextFieldRight = new NinePatch();
        public static NinePatch NinePatchTextFieldWrong = new NinePatch();

        public static NinePatch NinePatchBtnDefault = new NinePatch();
        public static NinePatch NinePatchBtnHover = new NinePatch();
        public static NinePatch NinePatchBtnClicked = new NinePatch();

        public static NinePatch NinePatchWindowSelected = new NinePatch();
        public static NinePatch NinePatchWindowUnselected = new NinePatch();

        public static NinePatch NinePatchPanelSelected = new NinePatch();
        public static NinePatch NinePatchPanelUnselected = new NinePatch();

        public static Texture2D TextureCheckBoxDefault;
        public static Texture2D TextureCheckBoxSelected;
        public static Texture2D TextureCheckBoxDefaultChecked;
        public static Texture2D TextureCheckBoxSelectedChecked;

        public static NinePatch NinePatchTabDefault = new NinePatch();
        public static NinePatch NinePatchTabSelected = new NinePatch();

        public static Texture2D TextureSliderSelected;
        public static Texture2D TextureSliderDefault;
        public static NinePatch NinePatchSliderRange = new NinePatch();

        private static InputManager _inputManager = new InputManager();

        internal static void InitializeKeybindings()
        {
            string[] toBindUpper = "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z ) ! @ # $ % ^ & * ( ? > < \" : } { _ + 0 1 2 3 4 5 6 7 8 9       ".Split(' ');
            string[] toBindLower = "a b c d e f g h i j k l m n o p q r s t u v w x y z 0 1 2 3 4 5 6 7 8 9 / . , ' ; ] [ - = 0 1 2 3 4 5 6 7 8 9       ".Split(' ');
            Keys[] toBind = {Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z
                            , Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.OemQuestion, Keys.OemPeriod, Keys.OemComma, Keys.OemQuotes,
                            Keys.OemSemicolon, Keys.OemCloseBrackets, Keys.OemOpenBrackets, Keys.OemMinus, Keys.OemPlus, Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9
                            , Keys.Space, Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Enter, Keys.Back};

            for (int i = 0; i < toBindUpper.Length; i++)
            {
                String upper = toBindUpper[i];
                String lower = toBindLower[i];
                Keys bind = toBind[i];

                InputManager.BindKey(() =>
                {
                    bool shiftHeld = InputManager.IsKeyPressed(Keys.LeftShift) || InputManager.IsKeyPressed(Keys.RightShift);
                    if (OnKeyPressedHandler == null) return;
                    OnKeyPressedHandler(shiftHeld ? upper : lower, bind);
                }, bind);
                InputManager.BindKey(() =>
                {
                    bool shiftHeld = InputManager.IsKeyPressed(Keys.LeftShift) || InputManager.IsKeyPressed(Keys.RightShift);
                    if (OnKeyReleasedHandler == null) return;
                    OnKeyReleasedHandler(shiftHeld ? upper : lower, bind);
                }, bind, false, false);
                InputManager.BindKey(() =>
                {
                    bool shiftHeld = InputManager.IsKeyPressed(Keys.LeftShift) || InputManager.IsKeyPressed(Keys.RightShift);
                    if (OnKeyContinuallyPressedHandler == null) return;
                    OnKeyContinuallyPressedHandler(shiftHeld ? upper : lower, bind);
                }, bind, true);
            }
        }

        public static void Initialize(Game theGame)
        {
            TheGame = theGame;
            White = new Texture2D(theGame.GraphicsDevice, 1, 1);
            White.SetData(new Color[] { Color.White });

            RootView.Width = theGame.Window.ClientBounds.Width;
            RootView.Height = theGame.Window.ClientBounds.Height;

            Texture2D textFieldDefault = ConversionManager.BitmapToTexture(Resource1.textfield_default_9);
            Texture2D textFieldSelected = ConversionManager.BitmapToTexture(Resource1.textfield_selected_9);
            Texture2D textFieldRight = ConversionManager.BitmapToTexture(Resource1.textfield_selected_right_9);
            Texture2D textFieldWrong = ConversionManager.BitmapToTexture(Resource1.textfield_selected_wrong_9);

            Texture2D windowSelected = ConversionManager.BitmapToTexture(Resource1.window_selected_9);
            Texture2D windowUnselected = ConversionManager.BitmapToTexture(Resource1.window_unselected_9);

            Texture2D panelSelected = ConversionManager.BitmapToTexture(Resource1.panel_selected_9);
            Texture2D panelUnselected = ConversionManager.BitmapToTexture(Resource1.panel_unselected_9);

            Texture2D btnDefault = ConversionManager.BitmapToTexture(Resource1.btn_default_9);
            Texture2D btnClicked = ConversionManager.BitmapToTexture(Resource1.btn_clicked_9);
            Texture2D btnHover = ConversionManager.BitmapToTexture(Resource1.btn_hover_9);

            Texture2D sliderRange = ConversionManager.BitmapToTexture(Resource1.sliderRange_9);
            TextureSliderDefault = ConversionManager.BitmapToTexture(Resource1.slider);
            TextureSliderSelected = ConversionManager.BitmapToTexture(Resource1.sliderSelected);

            NinePatchSliderRange.LoadFromTexture(sliderRange);

            TextureCheckBoxDefault = ConversionManager.BitmapToTexture(Resource1.checkbox_default);
            TextureCheckBoxSelected = ConversionManager.BitmapToTexture(Resource1.checkbox_default_selected);
            TextureCheckBoxDefaultChecked = ConversionManager.BitmapToTexture(Resource1.checkbox_checked);
            TextureCheckBoxSelectedChecked = ConversionManager.BitmapToTexture(Resource1.checkbox_checked_selected);

            NinePatchTextFieldDefault.LoadFromTexture(textFieldDefault);
            NinePatchTextFieldSelected.LoadFromTexture(textFieldSelected);
            NinePatchTextFieldRight.LoadFromTexture(textFieldRight);
            NinePatchTextFieldWrong.LoadFromTexture(textFieldWrong);

            NinePatchWindowSelected.LoadFromTexture(windowSelected);
            NinePatchWindowUnselected.LoadFromTexture(windowUnselected);

            NinePatchPanelUnselected.LoadFromTexture(panelUnselected);
            NinePatchPanelSelected.LoadFromTexture(panelSelected);

            NinePatchBtnDefault.LoadFromTexture(btnDefault);
            NinePatchBtnClicked.LoadFromTexture(btnClicked);
            NinePatchBtnHover.LoadFromTexture(btnHover);

            NinePatchTabDefault.LoadFromTexture(btnDefault);
            NinePatchTabSelected.LoadFromTexture(btnHover);

            InitializeKeybindings();

            InputManager.BindMouse(() =>
            {
                HandleClick(RootView, InputManager.GetMousePos());
                //When we click, we want to re-evaluate what control the mouse is over.
                HandleMouseMovement(RootView, InputManager.GetMousePos());
            }, MouseButton.Left);
            InputManager.BindMouse(() => HandleMouseMovement(RootView, InputManager.GetMousePos()), MouseButton.Movement);
        }

        internal static void HandleClick(View view, Point mousePos)
        {
            if (!view.Active)
                return;
            View[] sortedChildren = view.Children;
            Array.Sort(sortedChildren, ViewDepthComparer.CompareDepths);
            bool didLower = false;
            foreach (View child in sortedChildren)
            {
                if (!child.AbsoluteBoundBox.Contains(mousePos) || !child.Active) continue;
                HandleClick(child, mousePos);
                didLower = true;
                break;
            }
            if (didLower) return;
            List<View> allOthers = GetAllViews(RootView);
            foreach (View t in allOthers)
            {
                if (t != view)
                    t.OnMClickAway();
            }
            view.OnMClick(ConversionManager.PtoV(mousePos));
        }

        internal static void HandleMouseMovement(View view, Point mousePos)
        {
            if (!view.Active) return;
            View[] sortedChildren = view.Children;
            Array.Sort(sortedChildren, ViewDepthComparer.CompareDepths);
            bool didLower = false;
            if (view.ParentView == null)
            {
                //The first call
                List<View> allViews = GetAllViews(RootView);
                foreach (View t in allViews)
                {
                    t.MouseOver = false;
                }
            }
            foreach (View child in sortedChildren)
            {
                if (!child.AbsoluteBoundBox.Contains(mousePos) || !child.Active) continue;
                HandleMouseMovement(child, mousePos);
                didLower = true;
                child.MouseOver = true;
                break;
            }
            if (!didLower)
            {
                view.MouseOver = true;
            }
        }

        public static void Update(GameTime gameTime)
        {
            _inputManager.Update(gameTime);
            RootView.Update(gameTime);
            UpdateView(RootView, gameTime);
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            DrawView(RootView, spriteBatch);
        }

        internal static void UpdateView(View toUpdate, GameTime gameTime)
        {
            View[] sortedChildren = toUpdate.Children;
            foreach (View updating in sortedChildren)
            {
                if (!updating.Active) continue;
                updating.Update(gameTime);
                UpdateView(updating, gameTime);
            }
        }

        internal static void DrawView(View toDraw, SpriteBatch spriteBatch)
        {
            View[] sortedChildren = toDraw.Children;
            Array.Sort(sortedChildren, ViewDepthComparer.CompareDepthsInverse);
            foreach (View drawing in sortedChildren)
            {
                if (!drawing.Active) continue;
                drawing.Draw(spriteBatch);
                DrawView(drawing, spriteBatch);
            }
        }

        internal static List<View> GetAllViews(View rootView)
        {
            var ret = new List<View>();
            if (!rootView.Active) return ret;
            ret.Add(rootView);
            foreach (View child in rootView.Children)
            {
                ret.AddRange(GetAllViews(child));
            }
            return ret;
        }
    }
}
