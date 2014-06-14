using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class TimeTrialFactory : Factory<Main>
	{
		public TimeTrialFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "TimeTrial");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			TimeTrial trial = entity.GetOrCreate<TimeTrial>("TimeTrial");
			BindCommand(entity, trial.EndTimeTrial, "EndTimeTrial");
			BindCommand(entity, trial.StartTimeTrial, "StartTimeTrial");
			BindCommand(entity, trial.PauseTimer, "Pause");
			BindCommand(entity, trial.ResumeTimer, "Resume");
			SetMain(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			
		}

	}
}
