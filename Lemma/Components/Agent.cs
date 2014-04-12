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

		[XmlIgnore]
		public Command Die = new Command();

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
						Map.GlobalRaycastResult hit = Map.GlobalRaycast(pos + toAgent * 4.0f, toAgent, distance);
						if (hit.Map == null || hit.Map.Entity == agent.Entity)
							return agent;
					}
				}
			}
			return null;
		}

		public override void InitializeProperties()
		{
			Agent.agents.Add(this);
			this.Add(new CommandBinding(this.Delete, delegate()
			{
				Agent.agents.Remove(this);
			}));
			this.Add(new NotifyBinding(delegate()
			{
				if (this.Health <= 0.0f)
					this.Die.Execute();
			}, this.Health));
		}

		public Property<Vector3> Position = new Property<Vector3> { Editable = false };

		public Property<float> Health = new Property<float> { Value = 1.0f };

		public Property<bool> Loud = new Property<bool> { Value = true };
	}
}
