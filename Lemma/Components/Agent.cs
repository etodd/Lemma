using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.Xna.Framework;
using ComponentBind;

namespace Lemma.Components
{
	public class Agent : Component<Main>
	{
		private static List<Agent> agents = new List<Agent>();

		public static bool Query(Vector3 pos, float visionRadius, float soundRadius, Agent agent)
		{
			return Agent.query(pos, visionRadius, soundRadius, new[] { agent }) != null;
		}

		public static Agent Query(Vector3 pos, float visionRadius, float soundRadius, Func<Agent, bool> filter = null)
		{
			return Agent.query(pos, visionRadius, soundRadius, Agent.agents, filter);
		}

		private static Agent query(Vector3 pos, float visionRadius, float soundRadius, IEnumerable<Agent> agents, Func<Agent, bool> filter = null)
		{
			visionRadius *= visionRadius;
			soundRadius *= soundRadius;
			foreach (Agent agent in agents)
			{
				if (agent.Active && agent.Enabled && !agent.Suspended && (filter == null || filter(agent)))
				{
					Vector3 toAgent = agent.Position - pos;
					float distance = toAgent.LengthSquared();
					if (distance < soundRadius && agent.Loud)
						return agent;
					else if (distance < visionRadius)
					{
						distance = (float)Math.Sqrt(distance);
						toAgent /= distance;
						Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(pos, toAgent, distance, delegate(int i, Voxel.t t)
						{
							if (i < 5 && (t == Voxel.t.Neutral || t == Voxel.t.Infected || t == Voxel.t.Hard || t == Voxel.t.HardInfected))
								return false;
							return true;
						});
						if (hit.Voxel == null || hit.Voxel.Entity == agent.Entity)
							return agent;
					}
				}
			}
			return null;
		}

		public Property<Vector3> Position = new Property<Vector3>();

		public Property<float> Health = new Property<float> { Value = 1.0f };

		[XmlIgnore]
		public Command<float> Damage = new Command<float>();

		public Property<bool> Loud = new Property<bool> { Value = true };

		[XmlIgnore]
		public Property<bool> Killed = new Property<bool>();

		public override void Awake()
		{
			base.Awake();
			Agent.agents.Add(this);
			this.Add(new CommandBinding(this.Delete, delegate()
			{
				Agent.agents.Remove(this);
			}));

			this.Damage.Action = delegate(float value)
			{
				float health = Math.Max(this.Health - value, 0);
				if (health == 0.0f)
					this.Killed.Value = true;
				this.Health.Value = health;
			};
		}
	}
}
