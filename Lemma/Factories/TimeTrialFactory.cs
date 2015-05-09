using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using Steamworks;

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
			entity.CannotSuspend = true;

			Transform transform = entity.GetOrCreate<Transform>("Transform");
			TimeTrial trial = entity.GetOrCreate<TimeTrial>("TimeTrial");
			TimeTrialUI ui = entity.GetOrCreate<TimeTrialUI>("UI");
			this.SetMain(entity, main);
			entity.Add("EndTimeTrial", trial.Disable);
			entity.Add("StartTimeTrial", trial.Enable);
			ui.Add(new Binding<float>(ui.ElapsedTime, trial.ElapsedTime));
			ui.Add(new Binding<float>(ui.BestTime, trial.BestTime));
			ui.Add(new Binding<string>(ui.NextMap, trial.NextMap));
			ui.Add(new CommandBinding(trial.Enable, ui.Show));
			ui.Add(new CommandBinding(trial.ShowUI, ui.ShowEnd));

#if STEAMWORKS
			ui.Add(new CommandBinding(trial.OnLeaderboardError, ui.OnLeaderboardError));
			ui.Add(new CommandBinding<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t>(trial.OnLeaderboardSync, ui.OnLeaderboardSync));
#endif

			ui.Add(new CommandBinding(ui.Retry, trial.Retry));

			ui.Add(new CommandBinding(ui.MainMenu, delegate()
			{
				main.CurrentSave.Value = null;
				main.EditorEnabled.Value = false;
				IO.MapLoader.Load(main, Main.MenuMap);
				main.Menu.Show();
			}));

			ui.Add(new CommandBinding(ui.LoadNextMap, delegate()
			{
				main.CurrentSave.Value = null;
				main.EditorEnabled.Value = false;
				IO.MapLoader.Load(main, trial.NextMap);
			}));

			ui.Add(new CommandBinding(ui.Edit, delegate()
			{
				main.CurrentSave.Value = null;
				main.EditorEnabled.Value = true;
				IO.MapLoader.Load(main, main.MapFile);
			}));

			entity.Add("NextMap", trial.NextMap, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.MapDirectory, new string[] { "", "Challenge" }, IO.MapLoader.MapExtension),
			});
		}
	}
}