using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;

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
			SetMain(entity, main);
			entity.Add("EndTimeTrial", trial.EndTimeTrial);
			entity.Add("StartTimeTrial", trial.StartTimeTrial);
			entity.Add("Pause", trial.PauseTimer);
			entity.Add("Resume", trial.ResumeTimer);
			entity.Add("ParTime", trial.ParTime, "Base par time");
			entity.Add("KourTime", trial.KourTime, "Gold medal time");
			entity.Add("NextMap", trial.NextMap, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.MapDirectory, new string[] { "", "Challenge" }, IO.MapLoader.MapExtension),
			});
		}
	}
}
