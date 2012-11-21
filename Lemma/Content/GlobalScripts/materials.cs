Entity player = main.Get("Player").FirstOrDefault();

if (ParticleSystem.Get(main, "Shatter") == null)
{
	ParticleSystem.Add(main, "Shatter",
	new ParticleSystem.ParticleSettings
	{
		TextureName = "Particles\\spark",
		MaxParticles = 1000,
		Duration = TimeSpan.FromSeconds(1.0f),
		MinHorizontalVelocity = -4.0f,
		MaxHorizontalVelocity = 4.0f,
		MinVerticalVelocity = 0.0f,
		MaxVerticalVelocity = 5.0f,
		Gravity = new Vector3(0.0f, -8.0f, 0.0f),
		MinRotateSpeed = -2.0f,
		MaxRotateSpeed = 2.0f,
		MinStartSize = 0.1f,
		MaxStartSize = 0.3f,
		MinEndSize = 0.0f,
		MaxEndSize = 0.0f,
		BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
		MinColor = new Color(0.75f, 2.0f, 0.75f),
		MaxColor = new Color(0.75f, 2.0f, 0.75f),
	});
}

Action<Entity> bindPlayer = delegate(Entity p)
{
	player = p;
	
	Player playerComponent = player.Get<Player>();

	Updater lavaDamager = new Updater
	{
		delegate(float dt)
		{
			playerComponent.Health.Value -= 0.6f * dt;
		}
	};
	lavaDamager.Enabled.Value = false;

	player.Add(lavaDamager);
	
	player.Add(new CommandBinding<Map, Map.Coordinate?>(player.GetCommand<Map, Map.Coordinate?>("WalkedOn"), delegate(Map map, Map.Coordinate? coord)
	{
		string groundType = map == null ? null : map[coord.Value].Name;

		// Lava. Damage the player character if it steps on lava.
		bool isLava = groundType == "Lava";
		if (isLava)
			playerComponent.Health.Value -= 0.2f;
		lavaDamager.Enabled.Value = isLava;

		// Fragile dirt. Delete the block after a delay.
		if (groundType == "FragileDirt")
		{
			script.Add(new Animation
			(
				new Animation.Delay(1.0f),
				new Animation.Execute(delegate()
				{
					if (map.Empty(coord.Value))
					{
						Vector3 pos = map.GetAbsolutePosition(coord.Value);
						ParticleEmitter.Emit(main, "Smoke", pos, 1.0f, 10);
						map.Regenerate();
						Sound.PlayCue(main, "FragileDirt Crumble", pos);
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

script.Add(new CommandBinding<Map, Map.Coordinate, Map>(Map.GlobalCellEmptied, delegate(Map map, Map.Coordinate coord, Map transferringToNewMap)
{
	if (coord.Data.Name == "Critical" && transferringToNewMap == null) // Critical. Explodes when destroyed.
	{
		// Kaboom
		Vector3 pos = map.GetAbsolutePosition(coord);
		Sound.PlayCue(main, "Explosion", pos, 1.0f, 0.0f);
		
		PointLight light = new PointLight();
		light.Color.Value = new Vector3(1.3f, 1.1f, 0.9f);
		light.Attenuation.Value = 20.0f;
		light.Position.Value = pos;
		script.Add(light);
		script.Add(new Animation
		(
			new Animation.FloatMoveTo(light.Attenuation, 0.0f, 1.0f),
			new Animation.Execute(light.Delete)
		));
		
		if (player != null && player.Active)
			player.GetCommand<Vector3, float>("ShakeCamera").Execute(pos, 50.0f);
		
		Random random = new Random();
		
		const int radius = 6;
		const float physicsRadius = 10.0f;
		const float physicsImpulse = 70.0f;
		const float minPlayerDamage = 0.2f;
		const float playerDamageMultiplier = 2.0f;
		
		// Remove the cells
		foreach (Map m in Map.ActiveMaps.ToList())
		{
			List<Map.Coordinate> removals = new List<Map.Coordinate>();
			
			Map.Coordinate c = m.GetCoordinate(pos);
			Vector3 relativePos = m.GetRelativePosition(c);
			
			for (Map.Coordinate x = c.Move(Direction.NegativeX, radius - 1); x.X < c.X + radius; x.X++)
			{
				for (Map.Coordinate y = x.Move(Direction.NegativeY, radius - 1); y.Y < c.Y + radius; y.Y++)
				{
					for (Map.Coordinate z = y.Move(Direction.NegativeZ, radius - 1); z.Z < c.Z + radius; z.Z++)
					{
						if (m == map && z.Equivalent(coord))
							continue;
						
						Map.CellState s = m[z];
						if (s.ID == 0 || s.Permanent)
							continue;
						
						Vector3 cellPos = m.GetRelativePosition(z);
						if ((cellPos - relativePos).Length() < radius - 1)
						{
							removals.Add(z);
							Entity block = Factory.CreateAndBind(main, "Block");
							block.Get<Transform>().Position.Value = m.GetAbsolutePosition(cellPos);
							block.Get<Transform>().Quaternion.Value = m.Entity.Get<Transform>().Quaternion;
							s.ApplyToBlock(block);
							block.Get<ModelInstance>().GetVector3Parameter("Offset").Value = cellPos;
							main.Add(block);
						}
					}
				}
			}
			if (removals.Count > 0)
			{
				m.Empty(removals);
				m.Regenerate();
			}
		}
		
		// Damage the player
		if (player != null && player.Active)
		{
			float d = (player.Get<Transform>().Position - pos).Length();
			if (d < physicsRadius)
				player.Get<Player>().Health.Value -= minPlayerDamage + (1.0f - (d / physicsRadius)) * playerDamageMultiplier;
		}
		
		// Apply impulse to dynamic maps
		foreach (Map m in Map.ActiveMaps)
		{
			DynamicMap dm = m as DynamicMap;
			if (dm == null)
				continue;
			
			Vector3 toMap = dm.Transform.Value.Translation - pos;
			float distanceToMap = toMap.Length();
			toMap /= distanceToMap;
			
			toMap *= Math.Max(0.0f, 1.0f - (distanceToMap / physicsRadius)) * dm.PhysicsEntity.Mass * physicsImpulse;
			
			dm.PhysicsEntity.ApplyImpulse(dm.Transform.Value.Translation + new Vector3(((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f), toMap);
		}
		
		// Apply impulse to physics blocks
		foreach (Entity b in main.Get("Block"))
		{
			PhysicsBlock block = b.Get<PhysicsBlock>();
			Vector3 fromExplosion = b.Get<Transform>().Position.Value - pos;
			float distance = fromExplosion.Length();
			if (distance > 0.0f && distance < physicsRadius)
			{
				float blend = 1.0f - (distance / physicsRadius);
				block.LinearVelocity.Value += fromExplosion * blend * 10.0f / distance;
				block.AngularVelocity.Value += new Vector3(((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f) * blend;
			}
		}
	}
	else if (coord.Data.Name == "Infected") // Infected. Shatter effects.
	{
		ParticleSystem shatter = ParticleSystem.Get(main, "Shatter");
		Vector3 pos = map.GetAbsolutePosition(coord);
		Random random = new Random();
		Sound.PlayCue(main, "InfectedShatter", pos, 1.0f, 0.05f);
		for (int i = 0; i < 50; i++)
		{
			Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
			shatter.AddParticle(pos + offset, offset);
		}
	}
}));