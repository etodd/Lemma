using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Phone : Component
	{
		public class Message
		{
			public string Text;
			public bool Incoming;
			public string ID;
		}

		public class Schedule
		{
			public float Delay;
			public Message Message;
		}

		public class Ans
		{
			public string Text;
			public string ID;

			public Ans()
				: this(null, null)
			{
			}

			public Ans(string text, string id = null)
			{
				this.Text = text;
				this.ID = id;
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

		public ListProperty<Schedule> Schedules = new ListProperty<Schedule>();

		public ListProperty<Message> Messages = new ListProperty<Message>();

		public ListProperty<Ans> ActiveAnswers = new ListProperty<Ans>();

		[XmlIgnore]
		public Command MessageReceived = new Command();

		public override void InitializeProperties()
		{
			foreach (Schedule s in this.Schedules)
				this.schedule(s);
		}

		private void schedule(Schedule s)
		{
			Animation anim = new Animation
			(
				new Animation.Delay(s.Delay),
				new Animation.Execute(delegate()
				{
					this.Msg(s.Message.Text, s.Message.ID);
					this.Schedules.Remove(s);
				})
			);
			anim.EnabledWhenPaused.Value = false;
			this.main.AddComponent(anim);
		}

		public void Delay(float delay, string text, string id = null)
		{
			Schedule s = new Schedule
			{
				Delay = delay,
				Message = new Message
				{
					Text = text,
					ID = id,
				},
			};
			this.Schedules.Add(s);
			this.schedule(s);
		}

		public void Msg(string text, string id = null)
		{
			this.Messages.Add(new Message { Text = text, Incoming = true, ID = id, });
			this.MessageReceived.Execute();
		}

		private Dictionary<string, Action<string>> callbacks = new Dictionary<string, Action<string>>();

		public void Answer(Ans answer)
		{
			string messageID = null;
			if (answer.ID != null && this.Messages.Count > 0)
			{
				Message msg = this.Messages[this.Messages.Count - 1];
				messageID = msg.ID;
				if (messageID != null)
					this.answers[messageID] = answer.ID;
			}
			this.Messages.Add(new Message { Text = answer.Text, Incoming = false, ID = null, });
			this.ActiveAnswers.Clear();

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
