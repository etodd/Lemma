using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace Lemma.Components
{
	public class SignalTower : Component, IEditorUIComponent
	{
		public ListProperty<Phone.Ans> Answers = new ListProperty<Phone.Ans>();
		public Property<string> MessageID = new Property<string> { Editable = true };
		public Property<string> MessageText = new Property<string> { Editable = true };
		public Property<Entity.Handle> Player = new Property<Entity.Handle> { Editable = false };

		private const float messageDelay = 2.0f;

		[XmlIgnore]
		public Command<Entity> PlayerEnteredRange = new Command<Entity>();
		[XmlIgnore]
		public Command<Entity> PlayerExitedRange = new Command<Entity>();

		public override void InitializeProperties()
		{
			this.PlayerEnteredRange.Action = delegate(Entity p)
			{
				Phone phone = Factory.Get<PlayerDataFactory>().Instance(this.main).Get<Phone>();
				if (!string.IsNullOrEmpty(this.MessageID))
				{
					phone.Delay(SignalTower.messageDelay, this.MessageID, this.MessageText);
					this.MessageID.Value = this.MessageText.Value = null;
					p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = this.Entity;
				}
				else if (this.Answers.Count > 0)
				{
					phone.ActiveAnswers.AddAll(this.Answers);
					p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = this.Entity;
				}
			};

			this.PlayerExitedRange.Action = delegate(Entity p)
			{
				Phone phone = Factory.Get<PlayerDataFactory>().Instance(this.main).Get<Phone>();
				if (phone.ActiveAnswers.Count > 0)
				{
					// The player didn't pick an answer
					phone.ActiveAnswers.Clear();
				}
				else
					this.Answers.Clear(); // The player picked an answer. Don't add the answers again the next time they come by.

				p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = null;
			};
		}

		protected override void delete()
		{
			Entity player = this.Player.Value.Target;
			if (player != null && player.Active)
			{
				Property<Entity.Handle> signalTowerHandle = player.GetOrMakeProperty<Entity.Handle>("SignalTower");
				if (signalTowerHandle.Value.Target == this.Entity)
					signalTowerHandle.Value = null;
			}
			base.delete();
		}

		void IEditorUIComponent.AddEditorElements(UIComponent propertyList, EditorUI ui)
		{
			propertyList.Children.Add(ui.BuildLabel("Answers"));

			ListContainer container = new ListContainer();
			propertyList.Children.Add(container);

			container.Add(new ListBinding<UIComponent, Phone.Ans>(container.Children, this.Answers, delegate(Phone.Ans answer)
			{
				ListContainer item = new ListContainer();
				item.Orientation.Value = ListContainer.ListOrientation.Horizontal;

				Property<string> answerId = new Property<string>();
				answerId.Value = answer.ID;
				answerId.Set = delegate(string value)
				{
					answerId.InternalValue = value;
					answer.ID = value;
				};

				Property<string> answerText = new Property<string>();
				answerText.Value = answer.Text;
				answerText.Set = delegate(string value)
				{
					answerText.InternalValue = value;
					answer.Text = value;
				};

				item.Children.Add(ui.BuildLabel("ID"));
				item.Children.Add(ui.BuildValueField(answerId));
				item.Children.Add(ui.BuildLabel("Text"));
				item.Children.Add(ui.BuildValueField(answerText));
				item.Children.Add(ui.BuildButton(new Command
				{
					Action = delegate()
					{
						this.Answers.Remove(answer);
					}
				}, "[Delete]"));

				return new[] { item };
			}));

			propertyList.Children.Add(ui.BuildButton(new Command
			{
				Action = delegate()
				{
					this.Answers.Add(new Phone.Ans { IsInitiating = true });
				}
			},
			"[Add new]"));
		}
	}
}
