using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;

namespace Lemma.Components
{
	public class VoiceActor : Component, IUpdateableComponent, IEditorUIComponent
	{
		private AnimatedModel model;

		public VoiceActor()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
			this.Enabled.Editable = true;
		}

		private float time = 0.0f;

		private string[] clips = new string[] { "E", "Consonant", "MBP", "AI", "FV", "O", "QUW", "L", };

		public void Update(float elapsedTime)
		{
			if (this.model == null)
				this.model = this.Entity.Get<AnimatedModel>();

			this.time += elapsedTime;
			if (this.time > 0.2f)
			{
				this.time = 0.0f;
				this.model.Stop(this.clips);
				this.model.StartClip(this.clips[new Random().Next(0, this.clips.Length)], 0, false, 0.15f);
			}
		}

		void IEditorUIComponent.AddEditorElements(UIComponent propertyList)
		{
			TextElement label = new TextElement();
			label.FontFile.Value = "Font";
			label.Text.Value = "HEY!";
			propertyList.Children.Add(label);
		}
	}
}
