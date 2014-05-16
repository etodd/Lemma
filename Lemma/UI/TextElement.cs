using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Globalization;

namespace Lemma.Components
{
	public class TextElement : UIComponent
	{
		protected string fontFile;
		public Property<string> FontFile = new Property<string> { Editable = true };
		public Property<string> Text = new Property<string> { Editable = true, Value = "" };
		public Property<Color> Tint = new Property<Color> { Editable = true, Value = Color.White };
		public Property<float> Opacity = new Property<float> { Editable = true, Value = 1.0f };
		public Property<float> WrapWidth = new Property<float> { Editable = true, Value = 0.0f };
		private SpriteFont font;

		private Property<string> internalText = new Property<string> { Editable = false, Value = "" };
		private Binding<string> internalTextBinding;
		private NotifyBinding languageBinding;

		public static Dictionary<string, IProperty> BindableProperties = new Dictionary<string, IProperty>();

		private string wrappedText;

		public TextElement()
		{
			this.Position.Value = new Vector2(10.0f, 10.0f);
		}

		private String wrapText(String text, float width)
		{
			String line = String.Empty;
			String returnString = String.Empty;
			String[] wordArray = text.Split(' ');

			foreach (String word in wordArray)
			{
				if (this.font.MeasureString(line + word).Length() > width)
				{
					returnString = returnString + line + '\n';
					line = String.Empty;
				}

				line = line + word + ' ';
			}

			return returnString + line;
		}

		private void updateText()
		{
			if (this.font == null)
				return;

			string text = this.internalText;
			float wrapWidth = this.WrapWidth;
			if (text == null)
				this.wrappedText = null;
			else if (wrapWidth > 0.0f)
				this.wrappedText = this.wrapText(text, wrapWidth);
			else
				this.wrappedText = text;
			this.Size.Value = this.font.MeasureString(this.wrappedText ?? "");
		}

		public override void Awake()
		{
			base.Awake();

			this.FontFile.Get = delegate()
			{
				return this.fontFile;
			};

			this.FontFile.Set = delegate(string value)
			{
				this.fontFile = value;
				if (this.main != null)
					this.loadFont();
			};

			this.internalText.Set = delegate(string value)
			{
				this.internalText.InternalValue = value;
				this.updateText();
			};

			this.WrapWidth.Set = delegate(float value)
			{
				this.WrapWidth.InternalValue = value;
				this.updateText();
			};

			this.Text.Set = delegate(string value)
			{
				this.Text.InternalValue = value;

				List<IProperty> dependencies = new List<IProperty>();

				bool dependsOnLanguage = false;

				StringBuilder builder;
				if (value != null && value.Length > 0 && value[0] == '\\')
				{
					string key = value.Substring(1);
					string translated = this.main.Strings.Get(key);
					if (translated == null)
						translated = key;
					builder = new StringBuilder(translated);
					dependsOnLanguage = true;
				}
				else
					builder = new StringBuilder(value);

				for (int i = 0; i < builder.Length; i++)
				{
					if (builder[i] == '{' && builder[i + 1] == '{')
					{
						// Grab the key
						string key = null;
						StringBuilder keyBuilder = new StringBuilder();
						for (int j = i + 2; j < builder.Length; j++)
						{
							if (builder[j] == '}' && builder[j + 1] == '}')
							{
								key = keyBuilder.ToString();
								break;
							}
							else
								keyBuilder.Append(builder[j]);
						}
						if (key != null)
						{
							IProperty property;
							if (TextElement.BindableProperties.TryGetValue(key, out property))
							{
								string oldKey = string.Format("{{{{{0}}}}}", key);
								string argumentIndexKey = string.Format("{{{0}}}", dependencies.Count);
								builder.Replace(oldKey, argumentIndexKey, i, key.Length + 4);
								dependencies.Add(property);
								i += argumentIndexKey.Length - 1;
							}
						}
					}
				}

				if (this.internalTextBinding != null)
					this.Remove(this.internalTextBinding);

				if (this.languageBinding != null)
					this.Remove(this.languageBinding);

				if (dependencies.Count > 0)
				{
					dependsOnLanguage = true;
					string format = builder.ToString();
					IProperty[] dependenciesArray = dependencies.ToArray();
					this.internalTextBinding = new Binding<string>(this.internalText, delegate()
					{
						string[] strings = new string[dependenciesArray.Length];
						for (int i = 0; i < dependenciesArray.Length; i++)
						{
							string dependency = dependenciesArray[i].ToString();
							if (dependency[0] == '\\')
							{
								string key = dependency.Substring(1);
								dependency = this.main.Strings.Get(key);
								if (dependency == null)
									dependency = key;
							}
							strings[i] = dependency;
						}
						return string.Format(CultureInfo.CurrentCulture, format, strings);
					}, dependenciesArray);
					this.Add(this.internalTextBinding);
				}
				else
				{
					this.internalTextBinding = null;
					this.internalText.Value = builder.ToString();
				}

				if (dependsOnLanguage)
				{
					this.languageBinding = new NotifyBinding(this.Text.Reset, this.main.Strings.Language);
					this.Add(this.languageBinding);
				}
				else
					this.languageBinding = null;
			};

		}

		public override void LoadContent(bool reload)
		{
			base.LoadContent(reload);
			this.loadFont();
		}

		protected void loadFont()
		{
			try
			{
				this.font = this.main.Content.Load<SpriteFont>(this.FontFile);
				this.Size.Value = this.font.MeasureString(this.Text.Value ?? "");
			}
			catch (Exception)
			{
				this.font = null;
			}
		}

		protected override void draw(GameTime time, Matrix parent, Matrix transform)
		{
			Vector2 position = Vector2.Transform(this.Position, parent);
			position.X = (float)Math.Round(position.X);
			position.Y = (float)Math.Round(position.Y);
			float rotation = this.Rotation + (float)Math.Atan2(parent.M12, parent.M11);
			Vector2 scale = this.Scale;
			scale.X *= (float)Math.Sqrt((parent.M11 * parent.M11) + (parent.M12 * parent.M12));
			scale.Y *= (float)Math.Sqrt((parent.M21 * parent.M21) + (parent.M22 * parent.M22));

			Vector2 origin = this.AnchorPoint.Value * this.Size;
			origin.X = (float)Math.Round(origin.X);
			origin.Y = (float)Math.Round(origin.Y);

			this.renderer.Batch.DrawString(
				this.font,
				this.wrappedText ?? "",
				position,
				this.Tint.Value * this.Opacity.Value,
				rotation,
				origin,
				scale,
				SpriteEffects.None,
				this.DrawOrder);
		}
	}
}
