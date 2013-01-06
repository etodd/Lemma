using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class Phone : Component, IUpdateableComponent
	{
		public class QueuedMessage
		{
			public Message Message;
			public float TimeRemaining;
			public Response[] Responses;
		}

		public class Message
		{
			public bool Incoming;
			public string Text;
		}

		public class Response
		{
			public Response()
			{
				this.Question = "";
				this.ID = "";
			}
			public string Question;
			public string ID;
			public string Text;
			public bool Permanent;
		}

		public override void InitializeProperties()
		{
			this.EnabledWhenPaused.Value = false;
		}

		private Dictionary<string, List<Action<string>>> listeners = new Dictionary<string, List<Action<string>>>();

		private Dictionary<string, string> savedResponses = new Dictionary<string, string>();

		[XmlArray("SavedResponses")]
		[XmlArrayItem("Response", Type = typeof(DictionaryEntry))]
		public DictionaryEntry[] SavedResponses
		{
			get
			{
				// Make an array of DictionaryEntries to return
				DictionaryEntry[] ret = new DictionaryEntry[this.savedResponses.Count];
				int i = 0;
				// Iterate through properties to load items into the array.
				foreach (KeyValuePair<string, string> response in this.savedResponses)
				{
					ret[i] = new DictionaryEntry
					{
						Key = response.Key,
						Value = response.Value
					};
					i++;
				}
				return ret;
			}
			set
			{
				this.savedResponses.Clear();
				for (int i = 0; i < value.Length; i++)
					this.savedResponses.Add((string)value[i].Key, (string)value[i].Value);
			}
		}

		public void ClearAllResponses()
		{
			this.Responses.Clear();
		}

		public void ClearResponses(string question = null)
		{
			if (question == null)
			{
				foreach (Response r in this.Responses.ToList())
				{
					if (!r.Permanent)
						this.Responses.Remove(r);
				}
			}
			else
			{
				foreach (Response r in this.Responses.ToList())
				{
					if (r.Question == question)
						this.Responses.Remove(r);
				}
			}
		}

		public void ClearResponse(string question, string id)
		{
			foreach (Response r in this.Responses)
			{
				if (r.Question == question && r.ID == id)
				{
					this.Responses.Remove(r);
					break;
				}
			}
		}

		public void ClearPermanentResponses()
		{
			foreach (Response r in this.Responses.ToList())
			{
				if (r.Permanent)
					this.Responses.Remove(r);
			}
		}

		public Property<bool> Attached = new Property<bool>();

		public ListProperty<QueuedMessage> QueuedMessages = new ListProperty<QueuedMessage>();

		public ListProperty<Message> Messages = new ListProperty<Message>();

		public ListProperty<Response> Responses = new ListProperty<Response>();

		public Property<bool> HasUnreadMessages = new Property<bool>();

		public Property<float> TimeSinceLastIncomingMessage = new Property<float>();

		private static readonly string[][] idleMessages = new string[][]
		{
			new[]
			{
				"Hello?",
				"You there?",
				"Did you get my message?",
				"Hey, you okay?",
			},
			new[]
			{
				"Hey!",
				"Come on, let me know you're alright.",
				"Don't leave me hanging here!",
				"Silent treatment, I get it.",
				"Was it something I said?",
			},
			new[]
			{
				"Okay cool. Just let me know if you haven't died horribly. Otherwise I'll just assume you got murdered.",
				"Fine, clearly you hate me for some reason. I'll just shut up then. Good luck with your life.",
			}
		};

		private static readonly float[] idleMessageDelays = new float[]
		{
			300.0f,
			300.0f,
			600.0f,
		};

		// A count that keeps track of how many "are you still there" messages we've sent.
		// -1 means the user has responded and we just haven't sent an answer back
		public Property<int> IdleMessageIndex = new Property<int>();

		public void QueueMessage(string msg, float time, params Response[] responses)
		{
			if (responses == null)
				responses = new Response[] { };
			this.QueuedMessages.Add(new QueuedMessage
			{
				Message = new Message
				{
					Incoming = true,
					Text = msg
				},
#if DEVELOPMENT
				TimeRemaining = 0.0f,
#else
				TimeRemaining = time,
#endif
				Responses = responses
			});
		}

		private void addListener(string questionAndId, Action<string> action)
		{
			List<Action<string>> idMapping = null;
			if (!this.listeners.TryGetValue(questionAndId, out idMapping))
			{
				idMapping = new List<Action<string>>();
				this.listeners.Add(questionAndId, idMapping);
			}
			idMapping.Add(action);
		}

		public void Listen(string question, string[] ids, Action<string> action)
		{
			if (ids == null)
				this.addListener(question, action);
			else
			{
				foreach (string id in ids)
					this.addListener(question + '\n' + id, action);
			}
		}

		public string GetResponse(string question)
		{
			string response = null;
			this.savedResponses.TryGetValue(question, out response);
			return response;
		}

		public void SetResponse(string question, string id)
		{
			this.savedResponses[question] = id;
		}

		public void Respond(Response response)
		{
			this.IdleMessageIndex.Value = -1;

			// Add the response to the message list
			this.Messages.Add(new Message
			{
				Incoming = false,
				Text = response.Text
			});

			Session.Recorder.Event(main, "PhoneSentMessage", response.Question + "." + response.ID);

			// Remove the response from the user's response choices
			this.Responses.Remove(response);

			this.savedResponses[response.Question] = response.ID;

			// Call listeners
			List<Action<string>> actions = null;

			if (this.listeners.TryGetValue(response.Question, out actions))
			{
				foreach (Action<string> action in actions)
					action(response.ID);
			}

			if (this.listeners.TryGetValue(response.Question + '\n' + response.ID, out actions))
			{
				foreach (Action<string> action in actions)
					action(response.ID);
			}
		}

		void IUpdateableComponent.Update(float dt)
		{
			if (!this.HasUnreadMessages && this.Attached && this.Responses.Count > 0 && this.IdleMessageIndex > -1)
			{
				// We are waiting for a response from the player
				this.TimeSinceLastIncomingMessage.Value += dt;
				int index = this.IdleMessageIndex;
				if (index < idleMessageDelays.Length && this.TimeSinceLastIncomingMessage > idleMessageDelays[index])
				{
					this.QueueMessage(idleMessages[index][new Random().Next(idleMessages[index].Length)], 0.0f);
					this.IdleMessageIndex.Value++;
				}
			}

			List<QueuedMessage> removals = new List<QueuedMessage>();
			foreach (QueuedMessage qm in this.QueuedMessages)
			{
				qm.TimeRemaining -= dt;
				if (qm.TimeRemaining < 0.0f)
				{
					Session.Recorder.Event(main, "PhoneReceivedMessage");
					this.Messages.Add(qm.Message);
					this.TimeSinceLastIncomingMessage.Value = 0.0f;
					this.IdleMessageIndex.Value = 0;
					foreach (Response r in qm.Responses)
					{
						// Check for duplicates
						bool add = true;
						foreach (Response r2 in this.Responses)
						{
							if (r2.Question == r.Question && r2.ID == r.ID)
							{
								add = false;
								break;
							}
						}
						if (add)
							this.Responses.Add(r);
					}
					removals.Add(qm);
				}
			}
			foreach (QueuedMessage removal in removals)
				this.QueuedMessages.Remove(removal);
		}
	}
}
