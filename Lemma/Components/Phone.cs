using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using ComponentBind;

namespace Lemma.Components
{
	[XmlInclude(typeof(Phone.Message))]
	[XmlInclude(typeof(ListProperty<Phone.Message>))]
	[XmlInclude(typeof(Phone.Schedule))]
	[XmlInclude(typeof(ListProperty<Phone.Schedule>))]
	[XmlInclude(typeof(Phone.Ans))]
	[XmlInclude(typeof(ListProperty<Phone.Ans>))]
	public class Phone : ComponentBind.Component<Main>, IUpdateableComponent
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

			// Not null if the answer is an initiating one (the player is starting a conversation)
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

		private Dictionary<string, string> variables = new Dictionary<string,string>();

		[XmlArray("Variables")]
		[XmlArrayItem("Variable", Type = typeof(DictionaryEntry))]
		public DictionaryEntry[] Variables
		{
			get
			{
				// Make an array of DictionaryEntries to return
				DictionaryEntry[] ret = new DictionaryEntry[this.variables.Count];
				int i = 0;
				DictionaryEntry de;
				// Iterate through properties to load items into the array.
				foreach (KeyValuePair<string, string> pair in this.variables)
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
				this.variables.Clear();
				for (int i = 0; i < value.Length; i++)
				{
					string answer = (string)value[i].Value;
					this.variables.Add((string)value[i].Key, answer);
				}
			}
		}

		public override void InitializeProperties()
		{
			base.InitializeProperties();
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

			Action callback;
			this.messageCallbacks.TryGetValue(id, out callback);
			if (callback != null)
				callback();
		}

		public void ArchivedMsg(string id, string text = null)
		{
			this.Messages.Add(new Message { Incoming = true, ID = id, Text = text });
		}

		public void ArchivedAns(string id, string text = null)
		{
			this.Messages.Add(new Message { Incoming = false, ID = id, Text = text });
		}

		private Dictionary<string, Action> messageCallbacks = new Dictionary<string, Action>();
		private Dictionary<string, Action<string>> answerCallbacks = new Dictionary<string, Action<string>>();

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

			this.Messages.Add(new Message { Incoming = false, ID = answer.ID, Text = answer.Text });
			if (answer.Exclusive)
				this.ActiveAnswers.Clear();
			else
				this.ActiveAnswers.Remove(answer);

			if (messageID != null)
			{
				Action<string> callback;
				this.answerCallbacks.TryGetValue(messageID, out callback);
				if (callback != null)
					callback(answer.ID);
			}
		}

		public void Choices(params Ans[] answers)
		{
			this.ActiveAnswers.Clear();
			foreach (Ans answer in answers)
				this.ActiveAnswers.Add(answer);
		}

		public void OnMessage(string text, Action callback)
		{
			this.messageCallbacks[text] = callback;
		}

		public void OnAnswer(string text, Action<string> callback)
		{
			this.answerCallbacks[text] = callback;
		}

		public string this[string variable]
		{
			get
			{
				string value;
				this.variables.TryGetValue(variable, out value);
				return value;
			}
			set
			{
				this.variables[variable] = value;
			}
		}

		public void Load(DialogueForest forest)
		{
			foreach (DialogueForest.Node node in forest.Nodes)
			{
				if (node.type == DialogueForest.Node.Type.Text && node.choices != null && node.choices.Count > 0)
				{
					this.OnAnswer(node.name, delegate(string c)
					{
						DialogueForest.Node selectedChoice = forest.GetByName(c);
						DialogueForest.Node next = selectedChoice.next != null ? forest[selectedChoice.next] : null;
						if (next != null)
							this.visit(forest, next);
					});
				}
			}
		}

		private const float messageDelay = 2.0f; // 2 seconds in between each message
		private void visit(DialogueForest forest, DialogueForest.Node node, int textLevel = 1)
		{
			switch (node.type)
			{
				case DialogueForest.Node.Type.Text:
					this.Delay(messageDelay * textLevel, node.id, node.name);

					if (node.choices != null)
					{
						this.ActiveAnswers.Clear();
						this.ActiveAnswers.AddAll(node.choices.Select(x => forest[x]).Select(y => new Ans(y.name)));
					}

					if (node.next != null)
						this.visit(forest, forest[node.next], textLevel + 1);

					break;
				case DialogueForest.Node.Type.Set:
					this[node.variable] = node.value;
					if (node.next != null)
						this.visit(forest, forest[node.next]);
					break;
				case DialogueForest.Node.Type.Branch:
					string next;
					if (!node.branches.TryGetValue(this[node.variable], out next))
						node.branches.TryGetValue("_default", out next);
					if (next != null)
						this.visit(forest, forest[next], textLevel);
					break;
				default:
					break;
			}
		}
	}
}
