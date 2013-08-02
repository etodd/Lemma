using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

namespace Lemma.Components
{
	[XmlInclude(typeof(AI.Task))]
	[XmlInclude(typeof(AI.State))]
	public class AI : Component, IUpdateableComponent
	{
		public class Task
		{
			public float _leftOverIntervalTime;
			[XmlIgnore]
			public float Interval = 0.0f;
			[XmlIgnore]
			public Action Action;
		}

		public class State
		{
			public string Name;
			[XmlIgnore]
			public Action<State> Enter;
			[XmlIgnore]
			public Action<State> Exit;
			public Task[] Tasks;
			[XmlIgnore]
			public bool _valid;
		}

		[XmlArray("States")]
		[XmlArrayItem("State", Type = typeof(State))]
		public State[] States
		{
			get
			{
				return this.states.Values.ToArray();
			}
			set
			{
				foreach (State state in value)
				{
					State existingState = null;
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

		private Dictionary<string, State> states = new Dictionary<string, State>();

		private State currentState;

		public void Setup(params State[] states)
		{
			foreach (State state in states)
				this.Add(state);
		}

		public void Add(State state)
		{
			if (this.currentState == null && this.CurrentState.InternalValue == null)
			{
				this.currentState = state;
				this.CurrentState.InternalValue = state.Name;
			}
			State existingState = null;
			if (this.states.TryGetValue(state.Name, out existingState))
			{
				existingState._valid = true;
				existingState.Enter = state.Enter;
				existingState.Exit = state.Exit;
				for (int i = 0; i < Math.Min(state.Tasks.Length, existingState.Tasks.Length); i++)
				{
					Task existingTask = existingState.Tasks[i], task = state.Tasks[i];
					task._leftOverIntervalTime = existingTask._leftOverIntervalTime;
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
		public override void InitializeProperties()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
			this.Serialize = true;

			foreach (State s in this.states.Values.ToList())
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
				State oldState = this.currentState;
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
			State originalState = this.currentState;
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
