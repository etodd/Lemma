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
				return _drawText;
			}
			set
			{
				//Only let text be drawn if the user has set the font.
				_drawText = value;
			}
		}

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
				return new Rectangle(RealX, RealY, Width, (int)MathHelper.Max(SliderRange.Texture.Height, SliderDefault.Height));
			}
		}

		public SliderView(GeeUIMain GeeUI, View rootView, Vector2 position, int min, int max)
			: base(GeeUI, rootView)
		{
			SliderRange = GeeUIMain.NinePatchSliderRange;
			SliderDefault = GeeUIMain.TextureSliderDefault;
			SliderSelected = GeeUIMain.TextureSliderSelected;

			_min = min;
			_max = max;

			Position.Value = position;
		}

		public override void OnMClick(Vector2 position, bool fromChild)
		{
			SliderCalc(position);
			_clicked = true;
			base.OnMClick(position, fromChild);
		}
		public override void OnMClickAway()
		{
			_clicked = false;
			base.OnMClickAway();
		}

		public override void OnMOver()
		{
			Vector2 position = InputManager.GetMousePosV();
			SliderCalc(position);
			base.OnMOver();
		}
		public override void OnMOff()
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


		public override void Update(float dt)
		{
			if (_min > _max)
			{
				throw new Exception("The minimum value of a slider cannot be above the maximum.");
			}
			if (SliderPosition > Width) SliderPosition = Width;
			if (SliderPosition < 0) SliderPosition = 0;
			base.Update(dt);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			//We want to preserve the slider skin's original height.
			SliderRange.Draw(spriteBatch, AbsolutePosition, Width, SliderRange.BottomMostPatch - SliderRange.TopMostPatch, 0f, EffectiveOpacity);
			spriteBatch.Draw(CurSliderTexture, new Vector2(AbsoluteX + SliderRange.LeftWidth - (CurSliderTexture.Width) + SliderPosition, AbsoluteY), null, Color.White * EffectiveOpacity, 0f, new Vector2(CurSliderTexture.Width / -2, 0), 1f, SpriteEffects.None, 0f);
			if (DrawText)
			{
				int drawX = AbsoluteX + (Width) / 2;
				int drawY = AbsoluteY;
				Vector2 offset = GeeUIMain.Font.MeasureString(CurrentValue.ToString());
				offset.X = (int)(offset.X / 2);
				spriteBatch.DrawString(GeeUIMain.Font, CurrentValue.ToString(), new Vector2(drawX, drawY), TextColor * EffectiveOpacity, 0f, offset, 1f, SpriteEffects.None, 0f);
			}
			base.Draw(spriteBatch);
		}
	}
}
