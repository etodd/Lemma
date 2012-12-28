using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Agent : Component
	{
		private static List<Agent> agents = new List<Agent>();

		const float speedThreshold = 4.0f;

		[XmlIgnore]
		public Command Die = new Command();

		public static Agent Query(Vector3 pos, float visionRadius, float soundRadius, Agent ignore = null)
		{
			visionRadius *= visionRadius;
			soundRadius *= soundRadius;
			foreach (Agent agent in Agent.agents)
			{
				if (agent != ignore && agent.Active && agent.Enabled && !agent.Suspended && agent.Speed > speedThreshold)
				{
					Vector3 toAgent = agent.Position - pos;
					float distance = toAgent.LengthSquared();
					if (distance < soundRadius)
						return agent;
					else if (distance < visionRadius)
					{
						distance = (float)Math.Sqrt(distance);
						toAgent /= distance;
						Map.GlobalRaycastResult hit = Map.GlobalRaycast(pos + toAgent * 2.0f, toAgent, distance);
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

		public Property<Vector3> Position = new Property<Vector3>();

		public Property<float> Speed = new Property<float>();

		public Property<float> Health = new Property<float> { Value = 1.0f };
	}
}
