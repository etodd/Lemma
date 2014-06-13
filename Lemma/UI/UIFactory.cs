using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class UIFactory : Component<Main>
	{
		public TextElement CreateLink(string text, string url)
		{
			System.Windows.Forms.Form winForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.main.Window.Handle);

			TextElement element = new TextElement();
			element.FontFile.Value = "Font";
			element.Text.Value = text;
			element.Add(new Binding<Color, bool>(element.Tint, x => x ? new Color(1.0f, 0.0f, 0.0f) : new Color(0.0f, 0.0f, 0.0f), element.Highlighted));
			element.Add(new CommandBinding(element.MouseLeftUp, delegate()
			{
				//this.main.ExitFullscreen();
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url));
			}));
			element.Add(new CommandBinding(element.MouseOver, delegate()
			{
				winForm.Cursor = System.Windows.Forms.Cursors.Hand;
			}));
			element.Add(new CommandBinding(element.MouseOut, delegate()
			{
				winForm.Cursor = System.Windows.Forms.Cursors.Default;
			}));

			return element;
		}

		public Container CreateMenuButton<Type>(string label, Property<Type> property)
		{
			return this.CreateMenuButton<Type>(label, property, x => x.ToString());
		}

		public TextElement CreateLabel(string label = null)
		{
			TextElement text = new TextElement();
			text.FontFile.Value = "Font";
			if (label != null)
				text.Text.Value = label;
			return text;
		}

		public Container CreateMenuButton<Type>(string label, Property<Type> property, Func<Type, string> conversion)
		{
			Container result = this.CreateButton();

			TextElement text = this.CreateLabel(label);
			result.Children.Add(text);

			TextElement value = this.CreateLabel();
			value.Add(new Binding<Vector2>(value.Position, () => new Vector2(result.Size.Value.X - result.PaddingRight.Value, value.Position.Value.Y), result.Size, result.PaddingRight));
			value.AnchorPoint.Value = new Vector2(1, 0);
			value.Add(new Binding<string, Type>(value.Text, conversion, property));
			result.Children.Add(value);

			return result;
		}

		public Container CreateContainer()
		{
			Container result = new Container();
			result.Tint.Value = Color.Black;
			return result;
		}

		private static Color highlightColor = new Color(0.0f, 0.175f, 0.35f);

		public Container CreateButton(Action action = null)
		{
			Container result = this.CreateContainer();

			result.Add(new Binding<Color, bool>(result.Tint, x => x ? UIFactory.highlightColor : new Color(0.0f, 0.0f, 0.0f), result.Highlighted));
			result.Add(new Binding<float, bool>(result.Opacity, x => x ? 1.0f : 0.5f, result.Highlighted));
			result.Add(new NotifyBinding(delegate()
			{
				if (result.Highlighted)
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_UI_MOUSEOVER);
			}, result.Highlighted));
			result.Add(new CommandBinding(result.MouseLeftUp, delegate()
			{
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_UI_CLICK);
				if (action != null)
					action();
			}));
			return result;
		}

		public Container CreateButton(string label, Action action = null)
		{
			Container result = this.CreateButton(action);
			TextElement text = new TextElement();
			text.FontFile.Value = "Font";
			text.Name.Value = "Text";
			text.Text.Value = label;
			result.Children.Add(text);

			return result;
		}
	}
}
