using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ComponentBind;

namespace Lemma.Components
{
	public class Animation : Component<Main>, IUpdateableComponent
	{
		public abstract class Base
		{
			public bool Done { get; protected set; }
			public abstract void Reset();
			public abstract void Update(float dt);
		}

		public abstract class Interval : Base
		{
			public float Duration;
			private float time;

			public Interval(float duration)
			{
				this.Duration = duration;
			}

			public override void Reset()
			{
				this.time = 0.0f;
				this.Done = false;
			}

			public override void Update(float dt)
			{
				this.time += dt;
				this.UpdateInterval(this.Duration == 0.0f ? 1.0f : Math.Min(this.time / this.Duration, 1.0f));
				this.Done = this.time > this.Duration;
			}

			public abstract void UpdateInterval(float x);
		}

		public class Custom : Interval
		{
			private Action<float> action;

			public Custom(Action<float> action, float duration)
				: base(duration)
			{
				this.action = action;
			}

			public override void UpdateInterval(float x)
			{
				this.action(x);
			}
		}

		public abstract class Move<T> : Interval
		{
			protected Property<T> property;
			protected T start;
			protected T parameter;

			public Move(Property<T> p, T t, float duration)
				: base(duration)
			{
				this.property = p;
				this.parameter = t;
			}

			public override void Reset()
			{
				base.Reset();
				this.start = this.property.Value;
			}
		}

		public class Vector2MoveTo : Move<Vector2>
		{
			public Vector2MoveTo(Property<Vector2> p, Vector2 t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + (this.parameter - this.start) * x;
			}
		}

		public class MatrixMoveTo : Move<Matrix>
		{
			public MatrixMoveTo(Property<Matrix> p, Matrix t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				Matrix input = Matrix.Lerp(this.start, this.parameter, x);
				Matrix result = input;
				result.Forward = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0, 0, -1.0f), input));
				result.Up = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0.0f, 1.0f, 0), input));
				result.Right = Vector3.Normalize(Vector3.Cross(result.Forward, result.Up));
				this.property.Value = result;
			}
		}

		public class Vector2MoveToSpeed : Vector2MoveTo
		{
			private float speed;
			public Vector2MoveToSpeed(Property<Vector2> p, Vector2 t, float speed)
				: base(p, t, 1.0f)
			{
				this.speed = speed;
			}

			public override void Reset()
			{
				base.Reset();
				this.Duration = (this.parameter - this.start).Length() / this.speed;
			}
		}

		public class Vector2MoveBy : Move<Vector2>
		{
			public Vector2MoveBy(Property<Vector2> p, Vector2 t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + this.parameter * x;
			}
		}

		public class Vector2MoveBySpeed : Vector2MoveBy
		{
			public Vector2MoveBySpeed(Property<Vector2> p, Vector2 t, float speed)
				: base(p, t, t.Length() / speed)
			{

			}
		}

		public class Vector3MoveTo : Move<Vector3>
		{
			public Vector3MoveTo(Property<Vector3> p, Vector3 t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + (this.parameter - this.start) * x;
			}
		}

		public class Vector3MoveToSpeed : Vector3MoveTo
		{
			private float speed;
			public Vector3MoveToSpeed(Property<Vector3> p, Vector3 t, float speed)
				: base(p, t, 1.0f)
			{
				this.speed = speed;
			}

			public override void Reset()
			{
				base.Reset();
				this.Duration = (this.parameter - this.start).Length() / this.speed;
			}
		}

		public class Vector3MoveBy : Move<Vector3>
		{
			public Vector3MoveBy(Property<Vector3> p, Vector3 t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + this.parameter * x;
			}
		}

		public class Vector3MoveBySpeed : Vector3MoveBy
		{
			public Vector3MoveBySpeed(Property<Vector3> p, Vector3 t, float speed)
				: base(p, t, t.Length() / speed)
			{

			}
		}

		public class Vector4MoveTo : Move<Vector4>
		{
			public Vector4MoveTo(Property<Vector4> p, Vector4 t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + (this.parameter - this.start) * x;
			}
		}

		public class Vector4MoveToSpeed : Vector4MoveTo
		{
			private float speed;

			public Vector4MoveToSpeed(Property<Vector4> p, Vector4 t, float speed)
				: base(p, t, 1.0f)
			{
				this.speed = speed;
			}

			public override void Reset()
			{
				base.Reset();
				this.Duration = (this.parameter - this.start).Length() / this.speed;
			}
		}

		public class Vector4MoveBy : Move<Vector4>
		{
			public Vector4MoveBy(Property<Vector4> p, Vector4 t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + this.parameter * x;
			}
		}

		public class Vector4MoveBySpeed : Vector4MoveBy
		{
			public Vector4MoveBySpeed(Property<Vector4> p, Vector4 t, float speed)
				: base(p, t, t.Length() / speed)
			{

			}
		}

		public class ColorMoveTo : Move<Color>
		{
			public ColorMoveTo(Property<Color> p, Color t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = new Color(this.start.ToVector4() + (this.parameter.ToVector4() - this.start.ToVector4()) * x);
			}
		}

		public class ColorMoveToSpeed : ColorMoveTo
		{
			private float speed;
			public ColorMoveToSpeed(Property<Color> p, Color t, float speed)
				: base(p, t, 1.0f)
			{
				this.speed = speed;
			}

			public override void Reset()
			{
				base.Reset();
				this.Duration = (new Vector3(this.start.R, this.start.G, this.start.B) - new Vector3(this.parameter.R, this.parameter.G, this.parameter.B)).Length() / this.speed;
			}
		}

		public class FloatMoveTo : Move<float>
		{
			public FloatMoveTo(Property<float> p, float t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + (this.parameter - this.start) * x;
			}
		}

		public class FloatMoveToSpeed : FloatMoveTo
		{
			private float speed;
			public FloatMoveToSpeed(Property<float> p, float t, float speed)
				: base(p, t, 1.0f)
			{
				this.speed = speed;
			}

			public override void Reset()
			{
				base.Reset();
				this.Duration = Math.Abs(this.parameter - this.start) / this.speed;
			}
		}

		public class FloatMoveBy : Move<float>
		{
			public FloatMoveBy(Property<float> p, float t, float duration)
				: base(p, t, duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				this.property.Value = this.start + this.parameter * x;
			}
		}

		public class FloatMoveBySpeed : FloatMoveBy
		{
			public FloatMoveBySpeed(Property<float> p, float t, float speed)
				: base(p, t, Math.Abs(t / speed))
			{

			}
		}

		public class Delay : Interval
		{
			public Delay(float duration)
				: base(duration)
			{

			}

			public override void UpdateInterval(float x)
			{
				// Do nothing!
			}
		}

		public class Execute : Interval
		{
			private Command action;
			private bool executed;

			public Execute(Action action)
				: this(new Command { Action = action })
			{

			}

			public Execute(Command action)
				: base(0)
			{
				this.action = action;
			}

			public override void Reset()
			{
				base.Reset();
				this.executed = false;
			}

			public override void UpdateInterval(float x)
			{
				if (!this.executed)
					this.action.Execute();
				this.executed = true;
			}
		}

		public class Execute<T> : Interval
		{
			private Command<T> action;
			private bool executed;
			private T parameter;

			public Execute(Action<T> action, T parameter)
				: this(new Command<T> { Action = action }, parameter)
			{

			}

			public Execute(Command<T> action, T parameter)
				: base(0)
			{
				this.action = action;
				this.parameter = parameter;
			}

			public override void Reset()
			{
				base.Reset();
				this.executed = false;
			}

			public override void UpdateInterval(float x)
			{
				if (!this.executed)
					this.action.Execute(this.parameter);
				this.executed = true;
			}
		}

		public class Set<T> : Interval
		{
			private Property<T> property;
			private T value;
			private bool executed;

			public Set(Property<T> p, T t)
				: base(0)
			{
				this.property = p;
				this.value = t;
			}

			public override void Reset()
			{
				base.Reset();
				this.executed = false;
			}

			public override void UpdateInterval(float x)
			{
				if (!this.executed)
					this.property.Value = this.value;
				this.executed = true;
			}
		}

		public class Parallel : Interval
		{
			private bool[] finished;
			private Interval[] intervals;

			public Parallel(params Interval[] intervals)
				: base(intervals.Max(x => x.Duration))
			{
				this.intervals = intervals;
				this.finished = new bool[this.intervals.Length];
			}

			public override void Reset()
			{
				base.Reset();
				foreach (Interval i in this.intervals)
					i.Reset();
				for (int i = 0; i < this.finished.Length; i++)
					this.finished[i] = false;
				this.Duration = this.intervals.Max(x => x.Duration);
			}

			public override void UpdateInterval(float x)
			{
				float time = x * this.Duration;
				for (int i = 0; i < this.intervals.Length; i++)
				{
					Interval interval = this.intervals[i];
					float intervalTime = interval.Duration == 0.0f ? 1.0f : time / interval.Duration;
					if (intervalTime >= 1.0f)
					{
						if (!this.finished[i])
						{
							interval.UpdateInterval(1.0f);
							this.finished[i] = true;
						}
					}
					else
						interval.UpdateInterval(intervalTime);
				}
			}
		}

		public class Sequence : Interval
		{
			private float[] criticalPoints;
			private Interval[] intervals;
			private int currentIndex;

			public Sequence(params Interval[] intervals)
				: base(Math.Max(intervals.Sum(x => x.Duration), 0.001f))
			{
				this.intervals = intervals;
			}

			public override void Reset()
			{
				base.Reset();
				this.criticalPoints = new float[this.intervals.Length];
				foreach (Interval interval in this.intervals)
					interval.Reset();
				this.Duration = this.intervals.Sum(x => x.Duration);
				float total = 0.0f;
				int i = 0;
				foreach (Interval interval in this.intervals)
				{
					this.criticalPoints[i] = total;
					total += this.Duration == 0.0f ? 0.0f : interval.Duration / this.Duration;
					i++;
				}
				this.currentIndex = 0;
			}

			public override void UpdateInterval(float x)
			{
				// Advance the index if necessary
				int maxIndex = this.intervals.Length - 1;
				if (this.currentIndex < maxIndex)
				{
					while (x >= this.criticalPoints[this.currentIndex + 1])
					{
						this.intervals[this.currentIndex].UpdateInterval(1.0f);
						this.currentIndex++;
						this.intervals[this.currentIndex].Reset();
						if (this.currentIndex == maxIndex)
							break;
					}
				}

				// Retreat the index if necessary
				if (this.currentIndex > 0)
				{
					while (x < this.criticalPoints[this.currentIndex])
					{
						this.intervals[this.currentIndex].UpdateInterval(0.0f);
						this.currentIndex--;
						this.intervals[this.currentIndex].Reset();
					}
				}

				// Update the current interval
				float lastCriticalPoint = this.criticalPoints[this.currentIndex];
				float nextCriticalPoint = this.currentIndex >= maxIndex ? 1.0f : this.criticalPoints[this.currentIndex + 1];
				this.intervals[this.currentIndex].UpdateInterval((x - lastCriticalPoint) / (nextCriticalPoint - lastCriticalPoint));
			}
		}

		public class Repeat : Interval
		{
			private Interval interval;
			private int count;
			private int index;

			public Repeat(Interval interval, int count)
				: base(interval.Duration * count)
			{
				this.interval = interval;
				this.count = count;
			}

			public override void Reset()
			{
				base.Reset();
				this.interval.Reset();
				this.Duration = this.interval.Duration;
				this.index = 0;
			}

			public override void UpdateInterval(float x)
			{
				float d = x / (1.0f / this.count);
				int newIndex = (int)Math.Floor(d);
				if (newIndex != this.index)
					this.interval.Reset();
				this.index = newIndex;
				this.interval.UpdateInterval(d - newIndex);
			}
		}

		public class Reverse : Interval
		{
			private Interval interval;

			public Reverse(Interval interval)
				: base(interval.Duration)
			{
				this.interval = interval;
			}

			public override void Reset()
			{
				base.Reset();
				this.interval.Reset();
				this.Duration = this.interval.Duration;
			}

			public override void UpdateInterval(float x)
			{
				this.interval.UpdateInterval(1.0f - x);
			}
		}

		public class Speed : Interval
		{
			private Interval interval;
			private float speed;

			public Speed(Interval interval, float speed)
				: base(interval.Duration / speed)
			{
				this.interval = interval;
				this.speed = speed;
			}

			public override void Reset()
			{
				base.Reset();
				this.interval.Reset();
				this.Duration = this.interval.Duration;
			}

			public override void UpdateInterval(float x)
			{
				this.interval.UpdateInterval(x);
			}
		}

		public class Ease : Interval
		{
			public enum Type
			{
				InQuadratic,
				OutQuadratic,
				InCubic,
				OutCubic,
				InSin,
				OutSin,
				InExponential,
				OutExponential,
			}

			private Interval interval;
			private Type type;
			
			public Ease(Interval interval, Type type = Type.InQuadratic)
				: base(interval.Duration)
			{
				this.interval = interval;
				this.type = type;
			}

			public override void Reset()
			{
				base.Reset();
				this.interval.Reset();
				this.Duration = this.interval.Duration;
			}

			public override void UpdateInterval(float x)
			{
				switch (this.type)
				{
					case Type.InQuadratic:
						this.interval.UpdateInterval(x * x);
						break;
					case Type.OutQuadratic:
						this.interval.UpdateInterval(-1 * x * (x - 2));
						break;
					case Type.InCubic:
						this.interval.UpdateInterval(x * x * x);
						break;
					case Type.OutCubic:
						x--;
						this.interval.UpdateInterval(x * x * x + 1);
						break;
					case Type.InSin:
						this.interval.UpdateInterval((float)-Math.Cos(x * Math.PI * 0.5) + 1.0f);
						break;
					case Type.OutSin:
						this.interval.UpdateInterval((float)Math.Sin(x * Math.PI * 0.5));
						break;
					case Type.InExponential:
						this.interval.UpdateInterval((float)Math.Pow(2, 10 * (x - 1.0)));
						break;
					case Type.OutExponential:
						this.interval.UpdateInterval((float)-Math.Pow(2, -10 * x) + 1.0f);
						break;
				}
			}
		}

		public class RepeatForever : Base
		{
			private Interval interval;
			private float time;

			public RepeatForever(Interval interval)
			{
				this.interval = interval;
			}

			public override void Reset()
			{
				this.interval.Reset();
				this.time = 0.0f;
			}

			public sealed override void Update(float dt)
			{
				if (this.time > this.interval.Duration)
				{
					this.interval.Reset();
					this.time = 0.0f;
				}
				this.time += dt;
				this.interval.UpdateInterval(this.interval.Duration == 0.0f ? 1.0f : this.time / this.interval.Duration);
			}
		}

		private Base action;
		public float RunTime { get; private set; }

		public override Entity Entity
		{
			get
			{
				return base.Entity;
			}
			set
			{
				base.Entity = value;

				// By default, animations that are attached to entities will pause when the game pauses.
				// Other animations, like for the pause menu UI, will continue.
				this.EnabledWhenPaused = false;
			}
		}

		public Base Action
		{
			get
			{
				return this.action;
			}
		}

		public Animation(Base action)
		{
			this.Serialize = false;
			this.action = action;
			this.action.Reset();
		}

		public Animation(params Interval[] intervals)
			: this((Base)(intervals.Length == 1 ? intervals[0] : new Sequence(intervals)))
		{

		}

		void IUpdateableComponent.Update(float dt)
		{
			if (this.action.Done)
				this.Delete.Execute();
			else
			{
				this.RunTime += dt;
				this.action.Update(dt);
			}
		}
	}
}
