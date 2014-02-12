Random random = new Random();

Entity player = main.Get("Player").FirstOrDefault();

int criticalID = WorldFactory.StatesByName["Critical"].ID,
	infectedCriticalID = WorldFactory.StatesByName["InfectedCritical"].ID,
	neutralID = WorldFactory.StatesByName["Neutral"].ID,
	whiteID = WorldFactory.StatesByName["White"].ID,
	temporaryID = WorldFactory.StatesByName["Temporary"].ID,
	poweredID = WorldFactory.StatesByName["Powered"].ID,
	permanentPoweredID = WorldFactory.StatesByName["PermanentPowered"].ID,
	infectedID = WorldFactory.StatesByName["Infected"].ID,
	fragileID = WorldFactory.StatesByName["Fragile"].ID;

Action<Entity> bindPlayer = delegate(Entity p)
{
	player = p;
	
	Player playerComponent = player.Get<Player>();

	AnimatedModel playerModel = player.Get<AnimatedModel>("Model");

	Updater lavaDamager = new Updater
	{
		delegate(float dt)
		{
			if (!playerModel.IsPlaying("Kick") && (playerComponent.IsSupported || playerComponent.WallRunState.Value != Player.WallRun.None))
				playerComponent.Health.Value -= 0.6f * dt;
		}
	};
	lavaDamager.Enabled.Value = false;

	player.Add(lavaDamager);
	
	player.Add(new CommandBinding<Map, Map.Coordinate?>(player.GetCommand<Map, Map.Coordinate?>("WalkedOn"), delegate(Map map, Map.Coordinate? coord)
	{
		int groundType = map == null ? 0 : map[coord.Value].ID;

		// Lava. Damage the player character if it steps on lava.
		bool isLava = groundType == infectedID || groundType == infectedCriticalID;
		if (isLava)
			playerComponent.Health.Value -= 0.2f;
		lavaDamager.Enabled.Value = isLava;

		// Fragile dirt. Delete the block after a delay.
		if (groundType == fragileID)
		{
			script.Add(new Animation
			(
				new Animation.Delay(1.0f),
				new Animation.Execute(delegate()
				{
					Vector3 start = map.GetAbsolutePosition(coord.Value);
					Vector3 end = start - new Vector3(0, 5, 0);

					bool regenerate = false;
					foreach (Map.Coordinate c in map.Rasterize(start, end))
					{
						if (map[c].ID == fragileID)
						{
							map.Empty(c);
							regenerate = true;
						}
						else
							break;
					}

					if (regenerate)
					{
						ParticleEmitter.Emit(main, "Smoke", start, 1.0f, 10);
						Sound.PlayCue(main, "FragileDirt Crumble", start);
						map.Regenerate();
					}
				})
			));
		}
	}));
};

if (player != null)
	bindPlayer(player);

script.Add(new CommandBinding<Entity>(main.EntityAdded, delegate(Entity e)
{
	if (e.Type == "Player")
		bindPlayer(e);
}));

const float propagateDelay = 0.07f;

ListProperty<PlayerFactory.ScheduledBlock> blockQueue = script.GetOrMakeListProperty<PlayerFactory.ScheduledBlock>("PowerQueue");

script.Add(new CommandBinding<Map, IEnumerable<Map.Coordinate>, Map>(Map.GlobalCellsFilled, delegate(Map map, IEnumerable<Map.Coordinate> coords, Map transferredFromMap)
{
	foreach (Map.Coordinate c in coords)
	{
		int id = c.Data.ID;
		if (id == temporaryID || id == poweredID || id == infectedID)
		{
			blockQueue.Add(new PlayerFactory.ScheduledBlock
			{
				Map = map.Entity,
				Coordinate = c,
				Time = propagateDelay,
			});
		}
	}
}));

Action<Vector3> sparks = delegate(Vector3 pos)
{
	ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
	for (int j = 0; j < 50; j++)
	{
		Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
		shatter.AddParticle(pos + offset, offset);
	}

	PointLight light = new PointLight();
	script.Add(light);
	light.Serialize = false;
	light.Shadowed.Value = false;
	light.Color.Value = new Vector3(0.8f);
	light.Attenuation.Value = 6.0f;
	light.Position.Value = pos;
	script.Add(new Animation
	(
		new Animation.FloatMoveTo(light.Attenuation, 0.0f, 0.75f),
		new Animation.Execute(light.Delete)
	));

	Sound.PlayCue(main, "Sparks", pos, 1.0f, 0.05f);
};

script.Add(new Updater
{
	delegate(float dt)
	{
		for (int i = 0; i < blockQueue.Count; i++)
		{
			PlayerFactory.ScheduledBlock entry = blockQueue[i];
			entry.Time -= dt;
			if (entry.Time < 0.0f)
			{
				blockQueue.RemoveAt(i);
				i--;

				Entity mapEntity = entry.Map.Target;
				if (mapEntity != null && mapEntity.Active)
				{
					Map map = mapEntity.Get<Map>();
					Map.Coordinate c = entry.Coordinate;
					int id = map[c].ID;

					bool isTemporary = id == temporaryID;
					bool isNeutral = id == neutralID;
					bool isInfected = id == infectedID || id == infectedCriticalID;
					bool isPowered = id == poweredID || id == permanentPoweredID;

					if (isTemporary
						|| isNeutral
						|| isInfected
						|| isPowered)
					{
						bool regenerate = false;

						if (isTemporary || isPowered)
						{
							int newID = id;
							foreach (Direction dir in DirectionExtensions.Directions)
							{
								Map.Coordinate adjacent = c.Move(dir);
								int adjacentID = map[adjacent].ID;

								if (adjacentID == poweredID || adjacentID == permanentPoweredID)
								{
									newID = poweredID;
									break;
								}
								else if (newID != poweredID && (adjacentID == infectedID || adjacentID == infectedCriticalID))
									newID = infectedID;
							}

							if (newID != id)
							{
								map.Empty(c);
								map.Fill(c, WorldFactory.States[newID]);
								sparks(map.GetAbsolutePosition(c));
								regenerate = true;
							}
							else
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									int adjacentID = map[adjacent].ID;

									if (!isPowered && adjacentID == neutralID)
									{
										map.Empty(adjacent);
										map.Fill(adjacent, WorldFactory.States[temporaryID]);
										sparks(map.GetAbsolutePosition(adjacent));
										regenerate = true;
									}
									else if (isPowered && (adjacentID == temporaryID || adjacentID == neutralID || adjacentID == infectedID))
									{
										map.Empty(adjacent);
										map.Fill(adjacent, WorldFactory.States[poweredID]);
										sparks(map.GetAbsolutePosition(adjacent));
										regenerate = true;
									}
									else if (adjacentID == criticalID)
									{
										map.Empty(adjacent);
										regenerate = true;
									}
								}
							}
						}
						else if (isNeutral)
						{
							int newID = id;
							foreach (Direction dir in DirectionExtensions.Directions)
							{
								Map.Coordinate adjacent = c.Move(dir);
								int adjacentID = map[adjacent].ID;

								if (adjacentID == temporaryID)
									newID = temporaryID;
								else if (adjacentID == poweredID || adjacentID == permanentPoweredID)
									newID = poweredID;
								else if (newID != poweredID && (adjacentID == infectedID || adjacentID == infectedCriticalID))
									newID = infectedID;
							}

							if (newID != id)
							{
								map.Empty(c);
								map.Fill(c, WorldFactory.States[newID]);
								sparks(map.GetAbsolutePosition(c));
								regenerate = true;
							}
						}
						else if (isInfected)
						{
							bool changed = false;
							foreach (Direction dir in DirectionExtensions.Directions)
							{
								Map.Coordinate adjacent = c.Move(dir);
								int adjacentID = map[adjacent].ID;
								if (adjacentID == poweredID || adjacentID == permanentPoweredID)
								{
									map.Empty(c);
									map.Fill(c, WorldFactory.States[poweredID]);
									sparks(map.GetAbsolutePosition(adjacent));
									regenerate = true;
									changed = true;
									break;
								}
							}

							if (!changed)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									int adjacentID = map[adjacent].ID;
									if (adjacentID == temporaryID || adjacentID == neutralID)
									{
										map.Empty(adjacent);
										map.Fill(adjacent, WorldFactory.States[infectedID]);
										sparks(map.GetAbsolutePosition(adjacent));
										regenerate = true;
									}
									else if (adjacentID == criticalID)
									{
										map.Empty(adjacent);
										regenerate = true;
									}
								}
							}
						}

						if (regenerate)
							map.Regenerate();
					}
				}
			}
			i++;
		}
	}
});

script.Add(new CommandBinding<Map, IEnumerable<Map.Coordinate>, Map>(Map.GlobalCellsEmptied, delegate(Map map, IEnumerable<Map.Coordinate> coords, Map transferringToNewMap)
{
	if (transferringToNewMap != null)
		return;
	
	bool handlePowered = false;
	foreach (Map.Coordinate coord in coords)
	{
		if (coord.Data.ID == criticalID) // Critical. Explodes when destroyed.
			Explosion.Explode(main, map, coord);
		else if (coord.Data.ID == infectedCriticalID) // Infected. Shatter effects.
		{
			ParticleSystem shatter = ParticleSystem.Get(main, "InfectedShatter");
			Vector3 pos = map.GetAbsolutePosition(coord);
			Sound.PlayCue(main, "InfectedShatter", pos, 1.0f, 0.05f);
			for (int i = 0; i < 50; i++)
			{
				Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
				shatter.AddParticle(pos + offset, offset);
			}
		}
		else if (coord.Data.ID == poweredID)
			handlePowered = true;
		else if (coord.Data.ID == whiteID) // White. Shatter effects.
		{
			ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
			Vector3 pos = map.GetAbsolutePosition(coord);
			Sound.PlayCue(main, "WhiteShatter", pos, 1.0f, 0.05f);
			for (int i = 0; i < 50; i++)
			{
				Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
				shatter.AddParticle(pos + offset, offset);
			}
		}
	}

	if (handlePowered)
	{
		IEnumerable<IEnumerable<Map.Box>> poweredIslands = map.GetAdjacentIslands(coords.Where(x => x.Data.ID == poweredID), WorldFactory.StatesByName["Powered"], WorldFactory.StatesByName["PermanentPowered"]);
		List<Map.Coordinate> poweredCoords = poweredIslands.SelectMany(x => x).SelectMany(x => x.GetCoords()).ToList();
		if (poweredCoords.Count > 0)
		{
			Map.CellState temporaryState = WorldFactory.StatesByName["Temporary"];
			map.Empty(poweredCoords);
			foreach (Map.Coordinate coord in poweredCoords)
				map.Fill(coord, temporaryState);
			map.Regenerate();
		}
	}
}));