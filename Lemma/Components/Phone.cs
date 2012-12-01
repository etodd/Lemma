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
			public Message Message { get; set; }
			public float TimeRemaining { get; set; }
			public Response[] Responses { get; set; }
		}

		public class Message
		{
			public bool Incoming { get; set; }
			public string Text { get; set; }
		}

		public class Response
		{
			public Response()
			{
				this.Question = "";
				this.ID = "";
			}
			public string Question { get; set; }
			public string ID { get; set; }
			public string Text { get; set; }
			public bool Permanent { get; set; }
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

		public ListProperty<QueuedMessage> QueuedMessages = new ListProperty<QueuedMessage>();

		public ListProperty<Message> Messages = new ListProperty<Message>();

		public ListProperty<Response> Responses = new ListProperty<Response>();

		public Property<bool> HasUnreadMessages = new Property<bool>();

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
#if DEBUG
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
			// Add the response to the message list
			this.Messages.Add(new Message
			{
				Incoming = false,
				Text = response.Text
			});

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
			List<QueuedMessage> removals = new List<QueuedMessage>();
			foreach (QueuedMessage qm in this.QueuedMessages)
			{
				qm.TimeRemaining -= dt;
				if (qm.TimeRemaining < 0.0f)
				{
					this.Messages.Add(qm.Message);
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
