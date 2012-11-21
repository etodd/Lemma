GameMain.Config settings = ((GameMain)main).Settings;

Entity playerData = Factory.Get<PlayerDataFactory>().Instance(main);

Property<bool> phoneMessageShown = playerData.GetOrMakeProperty<bool>("PhoneTutorialShown");
Property<bool> pistolMessageShown = playerData.GetOrMakeProperty<bool>("PistolTutorialShown");
Property<bool> ammoMessageShown = playerData.GetOrMakeProperty<bool>("AmmoTutorialShown");

script.Add(new CommandBinding<Entity>(((GameMain)main).PlayerSpawned, delegate(Entity player)
{
	if (!phoneMessageShown)
	{
		script.Add
		(
			new NotifyBinding(delegate()
			{
				if (phoneMessageShown || player.GetProperty<Entity.Handle>("Phone").Value.Target == null)
					return;
				
				phoneMessageShown.Value = true;
				hideMessage
				(
					showMessage
					(
						() => "Press " + settings.TogglePhone.Value.ToString() + " to toggle the phone.",
						settings.TogglePhone
					),
					5.0f
				);
			}, player.GetProperty<Entity.Handle>("Phone"))
		);
	}
	
	Property<Entity.Handle> pistol = player.GetProperty<Entity.Handle>("Pistol");
	NotifyBinding ammoChanged = null;
	script.Add
	(
		new NotifyBinding(delegate()
		{
			if (pistol.Value.Target == null)
				return;
			
			if (ammoChanged != null)
				script.Remove(ammoChanged);
			
			Property<int> mags = pistol.Value.Target.GetProperty<int>("Magazines");
			
			if (!ammoMessageShown)
			{
				ammoChanged = new NotifyBinding(delegate()
				{
					if (mags > 0 && !ammoMessageShown)
					{
						hideMessage
						(
							showMessage
							(
								() => "Press " + settings.Reload.Value.ToString() + " to load a mag into the pistol.",
								settings.Reload
							),
							5.0f
						);
						ammoMessageShown.Value = true;
					}
				}, mags);
				script.Add(ammoChanged);
			}
			
			if (!pistolMessageShown)
			{
				hideMessage
				(
					showMessage
					(
						() => "Press " + settings.TogglePistol.Value.ToString() + " to toggle the pistol. Hold " + settings.Aim.Value.ToString() + " to aim. Press " + settings.FireBuildRoll.Value.ToString() + " to fire.",
						settings.TogglePistol, settings.Aim, settings.FireBuildRoll
					),
					8.0f
				);
				pistolMessageShown.Value = true;
			}
		}, pistol)
	);
}));