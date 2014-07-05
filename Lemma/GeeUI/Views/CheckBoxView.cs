using System;
using ComponentBind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Managers;

namespace GeeUI.Views
{
	public class CheckBoxView : View
	{
		public Texture2D TextureDefault;
		public Texture2D TextureChecked;
		public Texture2D TextureDefaultSelected;
		public Texture2D TextureCheckedSelected;

		public Property<bool> IsChecked = new Property<bool>() { Value = false };
		public bool AllowLabelClicking = true;

		private const int SeperationBetweenCbAndText = 3;

		public View CheckBoxContentView
		{
			get
			{
				return Children.Length == 0 ? null : Children[0];
			}
			set
			{
				if (this.Children.Length == 0)
					this.Children.Add(value);
				else
					this.Children[0] = value;
			}
		}

		public Texture2D CurTexture
		{
			get
			{
				if (Selected || MouseOver)
					return IsChecked ? TextureCheckedSelected : TextureDefaultSelected;
				return IsChecked ? TextureChecked : TextureDefault;
			}
		}

		public override Rectangle BoundBox
		{
			get
			{
				View child = CheckBoxContentView;
				if (child == null)
				{
					return CurTexture.Bounds;
				}
				return new Rectangle((int)RealPosition.X, (int)RealPosition.Y,
					CurTexture.Width + SeperationBetweenCbAndText + child.BoundBox.Width,

					Math.Max(CurTexture.Height, child.BoundBox.Height));
			}
		}

		public Rectangle CheckBoundBox
		{
			get
			{
				return new Rectangle(AbsoluteX, AbsoluteY,
					CurTexture.Width,
					CurTexture.Height);
			}
		}

		public override Rectangle ContentBoundBox
		{
			get
			{
				View child = CheckBoxContentView;
				if (child != null)
				{
					return new Rectangle(RealX + CurTexture.Width + SeperationBetweenCbAndText,
										 RealY, child.BoundBox.Width, child.BoundBox.Height);
				}
				return CurTexture.Bounds;
			}
		}

		public CheckBoxView(GeeUIMain GeeUI, View rootView, Vector2 position, string label, SpriteFont labelFont)
			: base(GeeUI, rootView)
		{
			Position.Value = position;
			this.numChildrenAllowed = 1;

			new TextView(GeeUI, this, label, Vector2.Zero, labelFont);

			TextureChecked = GeeUIMain.TextureCheckBoxDefaultChecked;
			TextureCheckedSelected = GeeUIMain.TextureCheckBoxSelectedChecked;
			TextureDefault = GeeUIMain.TextureCheckBoxDefault;
			TextureDefaultSelected = GeeUIMain.TextureCheckBoxSelected;
		}

		public override void OnMClick(Vector2 position, bool fromChild)
		{
			if (AllowLabelClicking || fromChild == false)
				IsChecked.Value = !IsChecked.Value;
			base.OnMClick(position, fromChild);
		}

		public override void OnMOver()
		{
			if (!AllowLabelClicking && !CheckBoundBox.Contains(InputManager.GetMousePos()))
				_mouseOver = false;
			base.OnMOver();
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.Draw(CurTexture, AbsolutePosition, Color.White * EffectiveOpacity);
			base.Draw(spriteBatch);
		}
	}
}
