using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.Xna.Framework;
using System.ComponentModel;

namespace Lemma.Components
{
	public class Phone : Component, IUpdateableComponent
	{
		public class Message
		{
			public bool Incoming;
			public string ID;
			public string Text;
		}

		public class Schedule
		{
			public float Delay;
			public Message Message;
		}

		public class Ans
		{
			public string ID;

			[DefaultValue(null)]
			public string Text;

			[DefaultValue(true)]
			public bool Exclusive = true;

			[DefaultValue(false)]
			public bool IsInitiating = false;

			// True if the answer is an initiating one (the player is starting a conversation)
			[DefaultValue(null)]
			public string QuestionID;

			public Ans()
				: this(null)
			{
			}

			public Ans(string id, string text = null, bool exclusive = true)
			{
				this.ID = id;
				this.Text = text;
				this.Exclusive = exclusive;
			}
		}

		private Dictionary<string, string> answers = new Dictionary<string,string>();

		[XmlArray("Answers")]
		[XmlArrayItem("Answer", Type = typeof(DictionaryEntry))]
		public DictionaryEntry[] Answers
		{
			get
			{
				// Make an array of DictionaryEntries to return
				DictionaryEntry[] ret = new DictionaryEntry[answers.Count];
				int i = 0;
				DictionaryEntry de;
				// Iterate through properties to load items into the array.
				foreach (KeyValuePair<string, string> pair in answers)
				{
					de = new DictionaryEntry();
					de.Key = pair.Key;
					de.Value = pair.Value;
					ret[i] = de;
					i++;
				}
				return ret;
			}
			set
			{
				this.answers.Clear();
				for (int i = 0; i < value.Length; i++)
				{
					string answer = (string)value[i].Value;
					this.answers.Add((string)value[i].Key, answer);
				}
			}
		}

		public override void InitializeProperties()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
		}

		public ListProperty<Schedule> Schedules = new ListProperty<Schedule>();

		public ListProperty<Message> Messages = new ListProperty<Message>();

		public ListProperty<Ans> ActiveAnswers = new ListProperty<Ans>();

		public Property<bool> CanReceiveMessages = new Property<bool>();

		public Property<bool> TutorialShown = new Property<bool>();

		[XmlIgnore]
		public Command MessageReceived = new Command();

		void IUpdateableComponent.Update(float dt)
		{
			List<Schedule> removals = new List<Schedule>();
			foreach (Schedule s in this.Schedules)
			{
				s.Delay -= dt;
				if (this.CanReceiveMessages && s.Delay < 0.0f)
					removals.Add(s);
			}

			foreach (Schedule s in removals)
				this.Schedules.Remove(s);

			foreach (Schedule s in removals)
				this.msg(s.Message.ID, s.Message.Text);
		}

		public void Delay(float delay, string id, string text = null)
		{
			Schedule s = new Schedule
			{
				Delay = delay,
				Message = new Message
				{
					ID = id,
					Text = text,
				},
			};
			this.Schedules.Add(s);
		}

		public void Msg(string id, string text = null)
		{
			this.msg(id, text);
		}

		private void msg(string id, string text)
		{
			if (this.Messages.Count >= 256)
				this.Messages.RemoveAt(0);
			this.Messages.Add(new Message { Incoming = true, ID = id, Text = text });
			this.MessageReceived.Execute();
		}

		public void ArchivedMsg(string id, string text = null)
		{
			this.Messages.Add(new Message { Incoming = true, ID = id, Text = text });
		}

		public void ArchivedAns(string id, string text = null)
		{
			this.Messages.Add(new Message { Incoming = false, ID = id, Text = text });
		}

		private Dictionary<string, Action<string>> callbacks = new Dictionary<string, Action<string>>();

		public string LastMessageID()
		{
			if (this.Messages.Count > 0)
			{
				Message msg = this.Messages[this.Messages.Count - 1];
				return msg.ID;
			}
			return null;
		}

		public void Answer(Ans answer)
		{
			string messageID;

			if (string.IsNullOrEmpty(answer.QuestionID))
				messageID = this.LastMessageID();
			else
				messageID = answer.QuestionID;

			if (messageID != null)
				this.answers[messageID] = answer.ID;

			this.Messages.Add(new Message { Incoming = false, ID = answer.ID, Text = answer.Text });
			if (answer.Exclusive)
				this.ActiveAnswers.Clear();
			else
				this.ActiveAnswers.Remove(answer);

			if (messageID != null)
			{
				Action<string> callback;
				if (this.callbacks.TryGetValue(messageID, out callback))
					callback(answer.ID);
			}
		}

		public void Choices(params Ans[] answers)
		{
			this.ActiveAnswers.Clear();
			foreach (Ans answer in answers)
				this.ActiveAnswers.Add(answer);
		}

		public void On(string question, Action<string> callback)
		{
			this.callbacks[question] = callback;
		}

		public string this[string messageId]
		{
			get
			{
				string answer;
				this.answers.TryGetValue(messageId, out answer);
				return answer;
			}
		}
	}
}
