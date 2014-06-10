using System;
using System.Collections.Generic;
using System.ComponentModel;
using ComponentBind;
using Lemma;
using Lemma.Components;
using Lemma.GeeUI;
using Lemma.Util;
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

	public class GeeUIMain : Component<Main>, IUpdateableComponent, INonPostProcessedDrawableComponent
	{
		public event OnKeyPressed OnKeyPressedHandler;
		public event OnKeyReleased OnKeyReleasedHandler;
		public event OnKeyContinuallyPressed OnKeyContinuallyPressedHandler;

		public static Texture2D White;
		public static Effect CircleShader;

		public View RootView;

		internal Game TheGame;

		public Color TextColorDefault = Color.Black;

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

		private InputManager _inputManager = new InputManager();

		public SpriteBatch Batch;

		public RasterizerState RasterizerState;

		private int LastScrollValue = 0;
		private const int OneScrollValue = 120;

		public EditorProperty<int> DrawOrder { get; private set; }

		/// <summary>
		/// Will be true if the latest click within the game bounds resided within an active direct child of the root view.
		/// </summary>
		public bool LastClickCaptured = false;

		internal void InitializeKeybindings()
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

		public void Initialize(Game theGame)
		{
			this.DrawOrder = new EditorProperty<int>() { Editable = false, Value = 0 };

			TheGame = theGame;
			White = new Texture2D(theGame.GraphicsDevice, 1, 1);
			White.SetData(new Color[] { Color.White });


			TextColorDefault = Color.White;

			RootView = new View(this);
			RootView.Width.Value = theGame.Window.ClientBounds.Width;
			RootView.Height.Value = theGame.Window.ClientBounds.Height;

			Texture2D textFieldDefault = ConversionManager.BitmapToTexture(Resource1.textfield_default_9, theGame.GraphicsDevice);
			Texture2D textFieldSelected = ConversionManager.BitmapToTexture(Resource1.textfield_selected_9, theGame.GraphicsDevice);
			Texture2D textFieldRight = ConversionManager.BitmapToTexture(Resource1.textfield_selected_right_9, theGame.GraphicsDevice);
			Texture2D textFieldWrong = ConversionManager.BitmapToTexture(Resource1.textfield_selected_wrong_9, theGame.GraphicsDevice);

			Texture2D windowSelected = ConversionManager.BitmapToTexture(Resource1.window_selected_9, theGame.GraphicsDevice);
			Texture2D windowUnselected = ConversionManager.BitmapToTexture(Resource1.window_unselected_9, theGame.GraphicsDevice);

			Texture2D panelSelected = ConversionManager.BitmapToTexture(Resource1.panel_selected_9, theGame.GraphicsDevice);
			Texture2D panelUnselected = ConversionManager.BitmapToTexture(Resource1.panel_unselected_9, theGame.GraphicsDevice);

			Texture2D btnDefault = ConversionManager.BitmapToTexture(Resource1.btn_default_9, theGame.GraphicsDevice);
			Texture2D btnClicked = ConversionManager.BitmapToTexture(Resource1.btn_clicked_9, theGame.GraphicsDevice);
			Texture2D btnHover = ConversionManager.BitmapToTexture(Resource1.btn_hover_9, theGame.GraphicsDevice);

			Texture2D sliderRange = ConversionManager.BitmapToTexture(Resource1.sliderRange_9, theGame.GraphicsDevice);
			TextureSliderDefault = ConversionManager.BitmapToTexture(Resource1.slider, theGame.GraphicsDevice);
			TextureSliderSelected = ConversionManager.BitmapToTexture(Resource1.sliderSelected, theGame.GraphicsDevice);

			NinePatchSliderRange.LoadFromTexture(sliderRange);

			TextureCheckBoxDefault = ConversionManager.BitmapToTexture(Resource1.checkbox_default, theGame.GraphicsDevice);
			TextureCheckBoxSelected = ConversionManager.BitmapToTexture(Resource1.checkbox_default_selected, theGame.GraphicsDevice);
			TextureCheckBoxDefaultChecked = ConversionManager.BitmapToTexture(Resource1.checkbox_checked, theGame.GraphicsDevice);
			TextureCheckBoxSelectedChecked = ConversionManager.BitmapToTexture(Resource1.checkbox_checked_selected, theGame.GraphicsDevice);

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
				HandleClick(RootView, InputManager.GetMousePos(), new List<View>());
				//When we click, we want to re-evaluate what control the mouse is over.
				HandleMouseMovement(RootView, InputManager.GetMousePos());
			}, MouseButton.Left);
			InputManager.BindMouse(() => HandleMouseMovement(RootView, InputManager.GetMousePos()), MouseButton.Movement);

			InputManager.BindMouse(() =>
			{
				int newScroll = _inputManager.GetScrollValue();
				int delta = newScroll - LastScrollValue;
				int numTimes = (delta / OneScrollValue);
				HandleScroll(RootView, InputManager.GetMousePos(), numTimes);
				LastScrollValue = newScroll;
			}, MouseButton.Scroll);

			this.RasterizerState = new RasterizerState() { ScissorTestEnable = true };

		}

		internal void HandleScroll(View view, Point mousePos, int scrollDelta)
		{
			if (!view.Active)
				return;

			View[] sortedChildren = view.Children;
			Array.Sort(sortedChildren, ViewDepthComparer.CompareDepths);
			bool didLower = false;
			foreach (View child in sortedChildren)
			{
				if (!child.AbsoluteBoundBox.Contains(mousePos) || !child.Active) continue;
				HandleScroll(child, mousePos, scrollDelta);
				didLower = true;
				break;
			}
			if (didLower) return;
			view.OnMScroll(ConversionManager.PtoV(mousePos), scrollDelta);
		}

		internal void HandleClick(View view, Point mousePos, List<View> DidClick )
		{
			if (!view.Active)
				return;

			if (view == RootView) LastClickCaptured = false;

			View[] sortedChildren = view.Children;
			Array.Sort(sortedChildren, ViewDepthComparer.CompareDepths);
			bool didLower = false;
			foreach (View child in sortedChildren)
			{
				if (!child.AbsoluteBoundBox.Contains(mousePos) || !child.Active || !child.AllowMouseEvents) continue;
				if (view == RootView) LastClickCaptured = true;
				DidClick.Add(child);
				HandleClick(child, mousePos, DidClick);
				didLower = true;
				break;
			}
			if (didLower) return;
			List<View> allOthers = GetAllViews(RootView);
			foreach (View t in allOthers)
			{
				if (t != view && !DidClick.Contains(t))
					t.OnMClickAway();
			}
			view.OnMClick(ConversionManager.PtoV(mousePos));
		}

		internal void HandleMouseMovement(View view, Point mousePos)
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
				if (!child.AbsoluteBoundBox.Contains(mousePos) || !child.Active || !child.AllowMouseEvents) continue;
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

		public void Update(float dt)
		{
			_inputManager.Update(dt, this);
			RootView.Update(dt);
			UpdateView(RootView, dt);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			DrawChildren(RootView, spriteBatch, RootView.AbsoluteBoundBox);
		}

		internal void UpdateView(View toUpdate, float dt)
		{
			View[] sortedChildren = toUpdate.Children;
			foreach (View updating in sortedChildren)
			{
				if (!updating.Active) continue;
				updating.Update(dt);
				UpdateView(updating, dt);
			}
		}

		internal Rectangle CorrectScissor(Rectangle scissor, Point screenSize)
		{
			if (scissor.Right > screenSize.X)
				scissor.Width -= (scissor.Right - screenSize.X);
			if (scissor.Bottom > screenSize.Y)
				scissor.Height -= (scissor.Bottom - screenSize.Y);
			if (scissor.Top < 0)
			{
				int diff = scissor.Top;
				scissor.Y -= diff;
				scissor.Height += diff;
			}
			if (scissor.Left < 0)
			{
				int diff = scissor.Left;
				scissor.X -= diff;
				scissor.Width += diff;
			}
			return scissor;
		}

		internal void DrawChildren(View toDrawParent, SpriteBatch spriteBatch, Rectangle origParentScissor)
		{
			View[] sortedChildren = toDrawParent.Children;
			Array.Sort(sortedChildren, ViewDepthComparer.CompareDepthsInverse);

			//Intersect the parent scissor with the current scissor.
			//This will ensure that we can ONLY constrict the scissor...
			//This solves the problem where a parent can be outside of the bounds of HIS parent, etc. etc., but his children only adhere to HIS boundbox, so they still get drawn.
			var parentScissor = origParentScissor.Intersect(toDrawParent.AbsoluteContentBoundBox);
			parentScissor = CorrectScissor(parentScissor, main.ScreenSize);
			if (parentScissor.Height > 0 && parentScissor.Width > 0)
			{
				foreach (View drawing in sortedChildren)
				{
					if (!drawing.Active || parentScissor.Height <= 0 || parentScissor.Width <= 0) continue;

					if (drawing.EnabledScissor)
					{
						spriteBatch.End();
						spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
							this.RasterizerState, null, Matrix.Identity);
						this.main.GraphicsDevice.ScissorRectangle = parentScissor;
					}

					drawing.Draw(spriteBatch);
					if (drawing.ContentMustBeScissored)
					{
						var newScissor = CorrectScissor(drawing.AbsoluteContentBoundBox, main.ScreenSize);
						if (newScissor.Width <= 0 || newScissor.Height <= 0) continue;
						spriteBatch.End();
						spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.RasterizerState, null, Matrix.Identity);
						this.main.GraphicsDevice.ScissorRectangle = newScissor;
					}
					drawing.DrawContent(spriteBatch);
					DrawChildren(drawing, spriteBatch, parentScissor);
				}
			}
		}

		internal List<View> GetAllViews(View rootView)
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

		public View FindViewByName(string name)
		{
			if (RootView == null) return null;
			var finds = RootView.FindChildrenByName(name);
			if (finds == null || finds.Length == 0) return null;
			return finds[0];
		}

		public View[] FindViewsByName(string name)
		{
			if (RootView == null) return new View[0];
			return RootView.FindChildrenByName(name);
		}

		public void LoadContent(bool reload)
		{
			this.Batch = new SpriteBatch(this.main.GraphicsDevice);
		}

		public void DrawNonPostProcessed(GameTime time, RenderParameters parameters)
		{
			var originalRasterizer = main.GraphicsDevice.RasterizerState;
			Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.RasterizerState, null, Matrix.Identity);
			Draw(Batch);
			Batch.End();
			main.GraphicsDevice.RasterizerState = originalRasterizer;
		}

		public override void Awake()
		{
			this.Initialize(main);
		}
	}
}
