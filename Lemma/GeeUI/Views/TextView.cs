using ComponentBind;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GeeUI.Managers;

namespace GeeUI.Views
{
	public class TextView : View
	{
		public SpriteFont Font;

		public Property<string> Text = new Property<string>() { Value = "" };

		public Color TextColor;

		public TextJustification TextJustification = TextJustification.Left;

		public Property<float> TextScale = new Property<float>() { Value = 1f };

		private Vector2 TextOrigin
		{
			get
			{
				var width = (int)(Font.MeasureString(Text).X * TextScale.Value);
				var height = (int)(Font.MeasureString(Text).Y * TextScale.Value);
				switch (TextJustification)
				{
					default:
						return new Vector2(0, 0);

					case TextJustification.Center:
						return new Vector2((Width / 2) - (width / 2), (Height / 2) - (height / 2)) * -1;

					case TextJustification.Right:
						return new Vector2(Width.Value - width, 0) * -1;
				}
			}
		}

		public Property<bool> AutoSize = new Property<bool>() { Value = true };

		public TextView(GeeUIMain GeeUI, View rootView, string text, Vector2 position, SpriteFont font)
			: base(GeeUI, rootView)
		{
			Text.Value = text;
			Position.Value = position;
			Font = font;
			TextColor = GeeUI.TextColorDefault;

			Text.AddBinding(new NotifyBinding(HandleResize, () => AutoSize.Value, Text));
			if(AutoSize.Value) HandleResize();
		}

		private void HandleResize()
		{
			var width = (int)(Font.MeasureString(Text).X * TextScale.Value);
			var height = (int)(Font.MeasureString(Text).Y * TextScale.Value);
			this.Width.Value = width;
			this.Height.Value = height;
		}

		internal static string TruncateString(string input, SpriteFont font, int widthAllowed, string ellipsis = "...")
		{
			string cur = "";
			foreach (char t in input)
			{
				float width = font.MeasureString(cur + t + ellipsis).X;
				if (width > widthAllowed)
					break;
				cur += t;
			}
			return cur + (cur.Length != input.Length ? ellipsis : "");
		}

		public override void OnMClick(Vector2 position, bool fromChild = false)
		{
			base.OnMClick(position);
		}
		public override void OnMClickAway(bool fromChild = false)
		{
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
			spriteBatch.DrawString(Font, Text, AbsolutePosition, TextColor * EffectiveOpacity, 0f, TextOrigin, TextScale.Value, SpriteEffects.None, 0f);
			base.Draw(spriteBatch);
		}

	}
	public enum TextJustification
	{
		Left,
		Center,
		Right
	}
}
