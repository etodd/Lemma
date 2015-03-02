using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using ComponentBind;
using Lemma.Util;

namespace Lemma.Components
{
	[XmlInclude(typeof(Phone.Message))]
	[XmlInclude(typeof(ListProperty<Phone.Message>))]
	[XmlInclude(typeof(Phone.Schedule))]
	[XmlInclude(typeof(ListProperty<Phone.Schedule>))]
	[XmlInclude(typeof(Phone.Ans))]
	[XmlInclude(typeof(ListProperty<Phone.Ans>))]
	public class Phone : ComponentBind.Component<Main>, IUpdateableComponent, DialogueForest.IClient
	{
		public class Message
		{
			public bool Incoming;
			public string ID;
			public string Name;
		}

		public class Schedule
		{
			public float Delay;
			public Message Message;
		}

		public class Ans
		{
			public string Name;

			[DefaultValue(true)]
			public bool Exclusive = true;

			[DefaultValue(null)]
			public string ParentID;

			public string ID;

			public Ans()
				: this(null, null)
			{
			}

			public Ans(string name, string id = null, bool exclusive = true)
			{
				this.ID = id;
				this.Name = name;
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
					this.variables.Add((string)value[i].Key, (string)value[i].Value);
			}
		}

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
		}

		public ListProperty<Schedule> Schedules = new ListProperty<Schedule>();

		public ListProperty<Message> Messages = new ListProperty<Message>();

		public ListProperty<Ans> ActiveAnswers = new ListProperty<Ans>();

		public Property<string> Photo = new Property<string>();

		public enum Mode { Messages, Photos }

		public Property<Mode> CurrentMode = new Property<Mode>();

		public Property<bool> CanReceiveMessages = new Property<bool>();

		public Property<bool> WaitForAnswer = new Property<bool>();

		[XmlIgnore]
		public Command Show = new Command();

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
				this.Msg(s.Message);
		}

		public void Delay(float delay, string name, string id = null)
		{
			Schedule s = new Schedule
			{
				Delay = delay,
				Message = new Message
				{
					Name = name,
					ID = id,
					Incoming = true,
				},
			};
			this.Schedules.Add(s);
		}

		public void Msg(Message msg)
		{
			if (this.Messages.Length >= 256)
				this.Messages.RemoveAt(0);
			this.Messages.Add(msg);

			Command callback;
			this.messageCallbacks.TryGetValue(msg.Name, out callback);
			if (callback != null)
				callback.Execute();

			Command cmd;
			this.visitCallbacks.TryGetValue(msg.Name, out cmd);
			if (cmd != null)
				cmd.Execute();

			this.MessageReceived.Execute();
		}

		public void ArchivedMsg(string name, string text = null)
		{
			this.Messages.Add(new Message { Incoming = true, Name = name, });
		}

		public void ArchivedAns(string name)
		{
			this.Messages.Add(new Message { Incoming = false, Name = name, });
		}

		private Dictionary<string, Command> messageCallbacks = new Dictionary<string, Command>();
		private Dictionary<string, Command> visitCallbacks = new Dictionary<string, Command>();
		private Dictionary<string, Command<string>> answerCallbacks = new Dictionary<string, Command<string>>();

		public string LastMessageID()
		{
			if (this.Messages.Length > 0)
			{
				Message msg = this.Messages[this.Messages.Length - 1];
				return msg.ID;
			}
			return null;
		}

		public void Answer(Ans answer)
		{
			string messageID;

			if (string.IsNullOrEmpty(answer.ParentID))
				messageID = this.LastMessageID();
			else
				messageID = answer.ParentID;

			this.Messages.Add(new Message { Incoming = false, Name = answer.Name, });
			if (answer.Exclusive)
				this.ActiveAnswers.Clear();
			else
				this.ActiveAnswers.Remove(answer);
			
			this.WaitForAnswer.Value = false;

			if (messageID != null)
			{
				Command<string> callback;
				this.answerCallbacks.TryGetValue(messageID, out callback);
				if (callback != null)
					callback.Execute(answer.Name);

				DialogueForest.Node selectedChoice = this.forest[answer.ID];
				if (selectedChoice != null)
				{
					DialogueForest.Node next = selectedChoice.next != null ? forest[selectedChoice.next] : null;
					if (next != null)
						this.execute(next);
				}
			}
		}

		public void Choices(params Ans[] answers)
		{
			this.ActiveAnswers.Clear();
			foreach (Ans answer in answers)
				this.ActiveAnswers.Add(answer);
			this.WaitForAnswer.Value = true;
		}

		public Command OnMessage(string text)
		{
			Command cmd;
			this.messageCallbacks.TryGetValue(text, out cmd);
			if (cmd == null)
				this.messageCallbacks[text] = cmd = new Command();
			return cmd;
		}

		public Command OnVisit(string text)
		{
			Command cmd;
			this.visitCallbacks.TryGetValue(text, out cmd);
			if (cmd == null)
				this.visitCallbacks[text] = cmd = new Command();
			return cmd;
		}

		public Command<string> OnAnswer(string text)
		{
			Command<string> cmd;
			this.answerCallbacks.TryGetValue(text, out cmd);
			if (cmd == null)
				this.answerCallbacks[text] = cmd = new Command<string>();
			return cmd;
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

		private DialogueForest forest;
		public void Bind(DialogueForest forest)
		{
			this.forest = forest;
		}

		public void Execute(DialogueForest.Node node)
		{
			this.execute(node);
			if (this.Schedules.Length == 0)
			{
				// If there are choices available, they will initiate a conversation.
				// The player should be able to pull up the phone, see the choices, and walk away without picking any of them.
				// Normally, you can't put the phone down until you've picked an answer.
				this.WaitForAnswer.Value = false;
			}
		}

		private void execute(DialogueForest.Node node)
		{
			this.forest.Execute(node, this);
		}

		void DialogueForest.IClient.Visit(DialogueForest.Node node)
		{
			if (!string.IsNullOrEmpty(node.name) && node.type != DialogueForest.Node.Type.Text)
			{
				Command cmd;
				this.visitCallbacks.TryGetValue(node.name, out cmd);
				if (cmd != null)
					cmd.Execute();
			}
		}

		void DialogueForest.IClient.Text(DialogueForest.Node node, int level)
		{
			this.Delay(messageDelay * level, node.name, node.id);
		}

		private const float messageDelay = 3.0f; // seconds in between each message
		void DialogueForest.IClient.Choice(DialogueForest.Node node, IEnumerable<DialogueForest.Node> choices)
		{
			this.ActiveAnswers.Clear();
			this.ActiveAnswers.AddAll(choices.Select(x => new Ans { ParentID = node.id, ID = x.id, Name = x.name }));
			this.WaitForAnswer.Value = true;
		}

		void DialogueForest.IClient.Set(string key, string value)
		{
			this[key] = value;
		}

		string DialogueForest.IClient.Get(string key)
		{
			return this[key];
		}
	}
}