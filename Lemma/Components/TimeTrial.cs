using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.GInterfaces;

namespace Lemma.Components
{
	public class TimeTrial : Component<Main>, IUpdateableComponent
	{
		public Property<string> NextMap = new Property<string>();

		[XmlIgnore]
		public Property<float> ElapsedTime = new Property<float>();

		[XmlIgnore]
		public Command Retry = new Command();

		public override void Awake()
		{
			base.Awake();

			this.Retry.Action = this.retry;

			this.Add(new CommandBinding(this.Disable, delegate()
			{
				this.main.BaseTimeMultiplier.Value = 0.0f;
				this.main.Menu.CanPause.Value = false;
			}));

			this.Add(new CommandBinding(this.main.Spawner.PlayerSpawned, delegate()
			{
				PlayerFactory.Instance.Add(new CommandBinding(PlayerFactory.Instance.Get<Player>().Die, (Action)this.retryDeath));
			}));
		}

		private void retryDeath()
		{
			this.main.Spawner.CanSpawn = false;
			this.Entity.Add(new Animation
			(
				new Animation.Delay(0.5f),
				new Animation.Execute(delegate()
				{
					this.main.CurrentSave.Value = null;
					this.main.EditorEnabled.Value = false;
					IO.MapLoader.Load(this.main, this.main.MapFile);
				})
			));
		}

		private void retry()
		{
			this.main.Spawner.CanSpawn = false;
			this.main.CurrentSave.Value = null;
			this.main.EditorEnabled.Value = false;
			IO.MapLoader.Load(this.main, this.main.MapFile);
		}

		public void Update(float dt)
		{
			this.ElapsedTime.Value += dt;
		}

		public override void delete()
		{
			base.delete();
			this.main.BaseTimeMultiplier.Value = 1.0f;
			this.main.Menu.CanPause.Value = true;
		}
	}
}