using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Spawner : Component<Main>, IUpdateableComponent
	{
		public bool CanSpawn = true;

		private const float StartGamma = 10.0f;
		private static Vector3 StartTint = new Vector3(2.0f);

		public Property<string> StartSpawnPoint = new Property<string>();

		public Command PlayerSpawned = new Command();

		private float respawnTimer = -1.0f;

		public const int RespawnMemoryLength = 200;
		public const float DefaultRespawnDistance = 0.0f;
		public const float DefaultRespawnInterval = 0.5f;
		public const float KilledRespawnDistance = 40.0f;
		public const float KilledRespawnInterval = 3.0f;

		public float RespawnDistance = DefaultRespawnDistance;
		public float RespawnInterval = DefaultRespawnInterval;

		private Vector3 lastPlayerPosition;

		private Entity editor;

		private Vector3 lastEditorPosition;
		private Vector2 lastEditorMouse;
		private string lastEditorSpawnPoint;

		private bool mapJustLoaded = false;

		public override void Awake()
		{
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;
			base.Awake();

			this.Add(new CommandBinding(this.main.MapLoaded, delegate()
			{
				if (this.main.MapFile.Value == Main.MenuMap)
				{
					this.CanSpawn = false;
					this.main.Renderer.InternalGamma.Value = 0.0f;
					this.main.Renderer.Brightness.Value = 0.0f;
				}
				else
				{
					this.CanSpawn = true;
					this.main.Renderer.InternalGamma.Value = Spawner.StartGamma;
					this.main.Renderer.Brightness.Value = 1.0f;
				}

				this.respawnTimer = -1.0f;
				this.mapJustLoaded = true;
			}));
		}

		public Animation.Parallel FlashAnimation()
		{
			return new Animation.Parallel
			(
				new Animation.Vector3MoveTo(this.main.Renderer.Tint, Spawner.StartTint, 0.5f),
				new Animation.FloatMoveTo(this.main.Renderer.InternalGamma, Spawner.StartGamma, 0.5f),
				new Animation.FloatMoveTo(this.main.Renderer.Brightness, 1.0f, 0.5f)
			);
		}

		public void Update(float dt)
		{
			// Spawn an editor or a player if needed
			if (this.main.EditorEnabled)
			{
				this.main.Renderer.InternalGamma.Value = 0.0f;
				this.main.Renderer.Brightness.Value = 0.0f;
				if (this.editor == null || !this.editor.Active)
				{
					this.editor = Factory.Get<EditorFactory>().CreateAndBind(this.main);
					FPSInput.RecenterMouse();
					this.editor.Get<Editor>().Position.Value = this.lastEditorPosition;
					this.editor.Get<FPSInput>().Mouse.Value = this.lastEditorMouse;
					this.StartSpawnPoint.Value = this.lastEditorSpawnPoint;
					this.main.Add(this.editor);
				}
				else
				{
					this.lastEditorPosition = this.editor.Get<Editor>().Position;
					this.lastEditorMouse = this.editor.Get<FPSInput>().Mouse;
				}
			}
			else
			{
				if (this.main.MapFile.Value == null || !this.CanSpawn)
					return;

				this.editor = null;

				bool createPlayer = PlayerFactory.Instance == null;

				if (this.mapJustLoaded)
				{
					if (!createPlayer)
					{
						// We just loaded a save game
						this.main.Renderer.InternalGamma.Value = 0.0f;
						this.main.Renderer.Brightness.Value = 0.0f;
						this.PlayerSpawned.Execute();
						this.respawnTimer = 0;
					}
				}
				else if (createPlayer)
				{
					if (this.respawnTimer <= 0)
						this.main.AddComponent(new Animation(this.FlashAnimation()));

					if (this.respawnTimer > this.RespawnInterval || this.respawnTimer < 0)
					{
						if (createPlayer)
						{
							Factory.Get<PlayerFactory>().CreateAndBind(this.main);
							this.main.Add(PlayerFactory.Instance);
						}

						bool spawnFound = false;

						RespawnLocation foundSpawnLocation = default(RespawnLocation);
						Vector3 foundSpawnAbsolutePosition = Vector3.Zero;

						if (string.IsNullOrEmpty(this.StartSpawnPoint.Value))
						{
							// Look for an autosaved spawn point
							Entity playerData = PlayerDataFactory.Instance;
							if (playerData != null)
							{
								ListProperty<RespawnLocation> respawnLocations = playerData.Get<PlayerData>().RespawnLocations;
								int supportedLocations = 0;
								while (respawnLocations.Count > 0)
								{
									RespawnLocation respawnLocation = respawnLocations[respawnLocations.Count - 1];
									Entity respawnMapEntity = respawnLocation.Map.Target;
									if (respawnMapEntity != null && respawnMapEntity.Active)
									{
										Voxel respawnMap = respawnMapEntity.Get<Voxel>();
										Vector3 absolutePos = respawnMap.GetAbsolutePosition(respawnLocation.Coordinate);
										if (respawnMap.Active
											&& respawnMap[respawnLocation.Coordinate] != Voxel.EmptyState
											&& respawnMap.GetAbsoluteVector(respawnMap.GetRelativeDirection(Direction.PositiveY).GetVector()).Y > 0.5f
											&& Agent.Query(absolutePos, 0.0f, 20.0f) == null)
										{
											supportedLocations++;
											DynamicVoxel dynamicMap = respawnMap as DynamicVoxel;
											if (dynamicMap == null || absolutePos.Y > respawnLocation.OriginalPosition.Y - 1.0f)
											{
												Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(absolutePos + new Vector3(0, 1, 0), Vector3.Up, 2);
												if (hit.Voxel == null)
												{
													// We can spawn here
													spawnFound = true;
													foundSpawnLocation = respawnLocation;
													foundSpawnAbsolutePosition = absolutePos;
												}
											}
										}
									}
									if (supportedLocations >= 40 || (spawnFound && (foundSpawnAbsolutePosition - this.lastPlayerPosition).Length() > this.RespawnDistance))
										break;
									else
										respawnLocations.RemoveAt(respawnLocations.Count - 1);
								}
							}
						}

						if (spawnFound)
						{
							// Spawn at an autosaved location
							Vector3 absolutePos = foundSpawnLocation.Map.Target.Get<Voxel>().GetAbsolutePosition(foundSpawnLocation.Coordinate);
							PlayerFactory.Instance.Get<Transform>().Position.Value = this.main.Camera.Position.Value = absolutePos + new Vector3(0, 3, 0);

							FPSInput.RecenterMouse();
							PlayerFactory.Instance.Get<FPSInput>().Mouse.Value = new Vector2(foundSpawnLocation.Rotation, 0);
						}
						else
						{
							// Spawn at a spawn point
							PlayerSpawn spawn = null;
							Entity spawnEntity = null;
							if (!string.IsNullOrEmpty(this.StartSpawnPoint.Value))
							{
								spawnEntity = this.main.GetByID(this.StartSpawnPoint);
								if (spawnEntity != null)
									spawn = spawnEntity.Get<PlayerSpawn>();
								this.lastEditorSpawnPoint = this.StartSpawnPoint;
								this.StartSpawnPoint.Value = null;
							}

							if (spawnEntity == null)
							{
								spawn = PlayerSpawn.FirstActive();
								spawnEntity = spawn == null ? null : spawn.Entity;
							}

							if (spawnEntity != null)
								PlayerFactory.Instance.Get<Transform>().Position.Value = this.main.Camera.Position.Value = spawnEntity.Get<Transform>().Position;

							if (spawn != null)
							{
								spawn.IsActivated.Value = true;
								FPSInput.RecenterMouse();
								PlayerFactory.Instance.Get<FPSInput>().Mouse.Value = new Vector2(spawn.Rotation, 0);
							}
						}

						this.main.AddComponent(new Animation
						(
							new Animation.Parallel
							(
								new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.One, 0.5f),
								new Animation.FloatMoveTo(this.main.Renderer.InternalGamma, 0.0f, 0.5f),
								new Animation.FloatMoveTo(this.main.Renderer.Brightness, 0.0f, 0.5f)
							)
						));
						this.respawnTimer = 0;

						this.PlayerSpawned.Execute();

						this.RespawnInterval = Spawner.DefaultRespawnInterval;
						this.RespawnDistance = Spawner.DefaultRespawnDistance;
					}
					else
						this.respawnTimer += dt;
				}
				else
					this.lastPlayerPosition = PlayerFactory.Instance.Get<Transform>().Position;
			}
			this.mapJustLoaded = false;
		}
	}
}
