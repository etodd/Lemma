using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using ComponentBind;

namespace Lemma.Components
{
	[XmlInclude(typeof(AI.Task))]
	[XmlInclude(typeof(AI.AIState))]
	public class AI : Component<Main>, IUpdateableComponent
	{
		public class Task
		{
			public float _leftOverIntervalTime;
			[XmlIgnore]
			public float Interval = 0.0f;
			[XmlIgnore]
			public Action Action;
		}

		public class AIState
		{
			public string Name;
			[XmlIgnore]
			public Action<AIState> Enter;
			[XmlIgnore]
			public Action<AIState> Exit;
			public Task[] Tasks;
			[XmlIgnore]
			public bool _valid;

			public AIState()
			{
				this.Tasks = new AI.Task[] { };
			}
		}

		[XmlArray("States")]
		[XmlArrayItem("State", Type = typeof(AIState))]
		public AIState[] States
		{
			get
			{
				return this.states.Values.ToArray();
			}
			set
			{
				foreach (AIState state in value)
				{
					AIState existingState = null;
					if (this.states.TryGetValue(state.Name, out existingState))
					{
						for (int i = 0; i < Math.Min(state.Tasks.Length, existingState.Tasks.Length); i++)
							existingState.Tasks[i]._leftOverIntervalTime = state.Tasks[i]._leftOverIntervalTime;
					}
					else
						this.states[state.Name] = state;
				}
			}
		}

		public Property<string> CurrentState = new Property<string> { Serialize = true, Editable = false };

		public Property<float> TimeInCurrentState = new Property<float> { Serialize = true, Editable = false };

		private Dictionary<string, AIState> states = new Dictionary<string, AIState>();

		private AIState currentState;

		public void Setup(params AIState[] states)
		{
			foreach (AIState state in states)
				this.Add(state);
		}

		public void Add(AIState state)
		{
			if (this.currentState == null && this.CurrentState.InternalValue == null)
			{
				this.currentState = state;
				this.CurrentState.InternalValue = state.Name;
			}
			AIState existingState = null;
			if (this.states.TryGetValue(state.Name, out existingState))
			{
				existingState._valid = true;
				existingState.Enter = state.Enter;
				existingState.Exit = state.Exit;
				if (state.Tasks != null && existingState.Tasks != null)
				{
					for (int i = 0; i < Math.Min(state.Tasks.Length, existingState.Tasks.Length); i++)
					{
						Task existingTask = existingState.Tasks[i], task = state.Tasks[i];
						task._leftOverIntervalTime = existingTask._leftOverIntervalTime;
					}
				}
				existingState.Tasks = state.Tasks;
				if (existingState == this.currentState && this.TimeInCurrentState == 0.0f && existingState.Enter != null)
					existingState.Enter(null);
			}
			else
			{
				this.states[state.Name] = state;
				state._valid = true;
			}
		}

		private bool switching = false;
		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Serialize = true;

			foreach (AIState s in this.states.Values.ToList())
			{
				if (!s._valid)
					this.states.Remove(s.Name);
			}

			if (!this.states.ContainsKey(this.CurrentState))
				this.CurrentState.Value = this.states.Keys.First();

			this.CurrentState.Set = delegate(string value)
			{
				if (this.switching)
					throw new Exception("Cannot switch states from inside a state exit function.");
				if ((value == this.CurrentState.InternalValue && !this.CurrentState.IsInitializing) || value == null || main.EditorEnabled)
					return;
				this.CurrentState.InternalValue = value;
				AIState oldState = this.currentState;
				this.currentState = this.states[value];
				if (!this.CurrentState.IsInitializing || this.TimeInCurrentState == 0.0f)
				{
					this.TimeInCurrentState.Value = 0.0f;
					this.switching = true;
					foreach (Task t in this.currentState.Tasks)
						t._leftOverIntervalTime = 0.0f;
					if (oldState != null && oldState.Exit != null)
						oldState.Exit(this.currentState);
					this.switching = false;
					if (this.currentState.Enter != null)
						this.currentState.Enter(oldState);
				}
			};
		}

		public void Update(float elapsedTime)
		{
			AIState originalState = this.currentState;
			this.TimeInCurrentState.Value += elapsedTime;
			foreach (Task t in this.currentState.Tasks)
			{
				if (t.Action != null)
				{
					if (t.Interval == 0.0f)
					{
						// Call once per frame
						t.Action();
						if (this.currentState != originalState || !this.Active)
							return;
					}
					else
					{
						float timeToSpend = t._leftOverIntervalTime + elapsedTime;
						while (timeToSpend > t.Interval)
						{
							t.Action();
							if (this.currentState != originalState || !this.Active)
								return;
							timeToSpend -= t.Interval;
						}
						t._leftOverIntervalTime = timeToSpend;
					}
				}
			}
		}
	}
}
