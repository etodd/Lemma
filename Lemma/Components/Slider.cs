using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using BEPUphysics;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.Constraints.TwoEntity.Motors;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Slider : Component<Main>, IUpdateableComponent
	{
		[XmlIgnore]
		public Property<Direction> Direction = new Property<Direction>();

		public Property<uint> MovementLoop = new Property<uint> { Value = AK.EVENTS.SLIDER2_LOOP };
		public Property<uint> MovementStop = new Property<uint> { Value = AK.EVENTS.SLIDER2_STOP };
		public Property<int> Minimum = new Property<int>();
		public Property<int> Maximum = new Property<int>();
		public Property<bool> Locked = new Property<bool>();
		public Property<float> Speed = new Property<float>();
		public Property<int> Goal = new Property<int>();
		public Property<bool> Servo = new Property<bool>();
		public Property<bool> StartAtMinimum = new Property<bool>();
		public Property<float> MaxForce = new Property<float>();

		[XmlIgnore]
		public Command OnHitMin = new Command();

		[XmlIgnore]
		public Command OnHitMax = new Command();

		[XmlIgnore]
		public Command Forward = new Command();

		[XmlIgnore]
		public Command Backward = new Command();

		private PrismaticJoint joint = null;
		private float lastX;
		private bool soundPlaying;

		private void setLimits()
		{
			if (this.joint != null)
			{
				int min = this.Minimum, max = this.Maximum;
				if (max > min)
				{
					this.joint.Limit.IsActive = true;
					this.joint.Limit.Minimum = this.Minimum;
					this.joint.Limit.Maximum = this.Maximum;
				}
				else
					this.joint.Limit.IsActive = false;
			}
		}

		private void updateMaterial()
		{
			if (!this.main.EditorEnabled)
			{
				DynamicVoxel map = this.Entity.Get<DynamicVoxel>();
				if (map != null)
				{
					bool active = this.Locked && (!this.Servo || (this.Servo && this.Goal.Value != this.Minimum.Value));

					Voxel.State desired = active ? Voxel.States.SliderPowered : Voxel.States.Slider;
					Voxel.t currentID = map[0, 0, 0].ID;
					if (currentID != desired.ID & (currentID == Voxel.t.Slider || currentID == Voxel.t.SliderPowered))
					{
						List<Voxel.Coord> coords = map.GetContiguousByType(new[] { map.GetBox(0, 0, 0) }).SelectMany(x => x.GetCoords()).ToList();
						map.Empty(coords, true, true, null, false);
						foreach (Voxel.Coord c in coords)
							map.Fill(c, desired);
						map.Regenerate();
					}
					map.PhysicsEntity.ActivityInformation.Activate();
				}
			}
		}

		private void setSpeed()
		{
			if (this.joint != null)
			{
				this.joint.Motor.Settings.Servo.BaseCorrectiveSpeed = this.Speed;
				this.joint.Motor.Settings.VelocityMotor.GoalVelocity = this.Speed;
			}
			this.updateMaterial();
		}

		private void setLocked()
		{
			if (this.joint != null)
				this.joint.Motor.IsActive = this.Locked;
			this.updateMaterial();
		}

		private void setGoal()
		{
			if (this.joint != null)
				this.joint.Motor.Settings.Servo.Goal = this.Goal;
			this.updateMaterial();
		}

		private void setMode()
		{
			if (this.joint != null)
				this.joint.Motor.Settings.Mode = this.Servo ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
			this.updateMaterial();
		}

		private void setMaxForce()
		{
			if (this.joint != null)
			{
				float f = this.MaxForce;
				this.joint.Motor.Settings.MaximumForce = f == 0.0f ? float.MaxValue : f;
			}
		}

		public void Move(int value)
		{
			if (this.Locked)
				this.Goal.Value = value;
		}

		private BEPUphysics.Entities.Entity physicsEntity;
		public ISpaceObject CreateJoint(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
		{
			// entity1 is us
			// entity2 is the main map we are attaching to
			this.physicsEntity = entity1;
			Vector3 originalPos = entity1.Position;
			entity1.Position = pos;
			this.joint = new PrismaticJoint(entity2, entity1, pos, direction, pos);
			entity1.Position = originalPos;
			this.setLimits();
			this.setLocked();
			this.setSpeed();
			this.setGoal();
			this.setMode();
			this.setMaxForce();
			this.joint.Motor.Settings.Servo.SpringSettings.StiffnessConstant = 0.03f;
			this.joint.Limit.Update(0.0f);
			Vector3 separation = this.joint.Limit.AnchorB - this.joint.Limit.AnchorA;
			this.lastX = Vector3.Dot(separation, this.joint.Limit.Axis);
			return this.joint;
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new NotifyBinding(this.setLimits, this.Minimum, this.Maximum));
			this.Add(new NotifyBinding(this.setSpeed, this.Speed));
			this.Add(new NotifyBinding(this.setLocked, this.Locked));
			this.Add(new NotifyBinding(this.setGoal, this.Goal));
			this.Add(new NotifyBinding(this.setMode, this.Servo));
			this.Add(new NotifyBinding(this.setMaxForce, this.MaxForce));

			this.Forward.Action = delegate() { this.Move(this.Maximum); };
			this.Backward.Action = delegate() { this.Move(this.Minimum); };
		}

		public override void Start()
		{
			if (!this.main.EditorEnabled)
			{
				if (this.StartAtMinimum)
				{
					this.StartAtMinimum.Value = false;
					Transform transform = this.Entity.GetOrCreate<Transform>("MapTransform");
					transform.Selectable.Value = false;
					DynamicVoxel map = this.Entity.Get<DynamicVoxel>();
					transform.Position.Value = map.GetAbsolutePosition(new Voxel.Coord().Move(this.Direction, this.Minimum));
				}

				Action stopMovement = delegate()
				{
					if (this.main.TotalTime > 0.1f && this.MovementStop.Value != 0)
						AkSoundEngine.PostEvent(this.MovementStop, this.Entity);
					else
						AkSoundEngine.PostEvent(AK.EVENTS.STOP_ALL_OBJECT, this.Entity);
					this.soundPlaying = false;
				};
				this.Add(new CommandBinding(this.OnHitMax, stopMovement));
				this.Add(new CommandBinding(this.OnHitMin, stopMovement));
			}
		}

		public void Update(float dt)
		{
			if (this.joint != null)
			{
				Vector3 separation = this.joint.Limit.AnchorB - this.joint.Limit.AnchorA;

				float x = Vector3.Dot(separation, this.joint.Limit.Axis);

				bool nearEdge = false;
				const float threshold = 0.1f;
				if (x > this.Maximum - threshold)
				{
					if (this.lastX <= this.Maximum - threshold)
						this.OnHitMax.Execute();
					nearEdge = true;
				}
				
				if (x < this.Minimum + threshold)
				{
					if (this.lastX >= this.Minimum + threshold)
						this.OnHitMin.Execute();
					nearEdge = true;
				}

				this.lastX = x;

				if (!nearEdge)
				{
					float speed = Vector3.Dot(this.physicsEntity.LinearVelocity, this.joint.Limit.Axis);
					bool moving = speed > 0.5f;
					if (this.soundPlaying && !moving)
					{
						AkSoundEngine.PostEvent(AK.EVENTS.STOP_ALL_OBJECT, this.Entity);
						this.soundPlaying = false;
					}
					else if (!this.soundPlaying && moving)
					{
						if (this.MovementLoop.Value != 0)
							AkSoundEngine.PostEvent(this.MovementLoop, this.Entity);
						this.soundPlaying = true;
					}
				}
			}
		}
	}
}