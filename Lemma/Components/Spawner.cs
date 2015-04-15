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
		public Property<ulong> StartSpawnPointGUID = new Property<ulong>();

		public Command PlayerSpawned = new Command();

		private float respawnTimer;

		public const int RespawnMemoryLength = 200;
		public const float DefaultRespawnDistance = 0.0f;
		public const float DefaultRespawnInterval = 0.5f;
		public const float KilledRespawnDistance = 60.0f;
		public const float KilledRespawnInterval = 4.0f;

		public float RespawnDistance = DefaultRespawnDistance;
		public float RespawnInterval = DefaultRespawnInterval;

		private const float spawnHeightOffset = 2;

		private Vector3 lastPlayerPosition;

		private Entity editor;

		private Vector3 lastEditorPosition;
		private Vector2 lastEditorMouse;
		private string lastEditorSpawnPoint;
		private ulong lastEditorSpawnPointGUID;

		private bool mapJustLoaded = false;

		public void ResetTimer()
		{
			this.respawnTimer = 0.01f; // To avoid spawning duplicate flash animations
		}

		public override void Awake()
		{
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;
			base.Awake();

			string lastMap = this.main.MapFile;
			this.Add(new CommandBinding(this.main.MapLoaded, delegate()
			{
				CameraStop.CinematicActive.Value = false;
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

				if (PlayerFactory.Instance == null)
					this.RespawnInterval = 0.0f;

				if (this.main.MapFile != lastMap)
				{
					this.lastEditorPosition = Vector3.Zero;
					this.lastEditorMouse = Vector2.Zero;
					this.lastEditorSpawnPoint = null;
					this.lastEditorSpawnPointGUID = 0;
					lastMap = this.main.MapFile;
				}

				this.respawnTimer = 0.0f;
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

		public Animation.Parallel EndFlashAnimation()
		{
			return new Animation.Parallel
			(
				new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.One, 0.5f),
				new Animation.FloatMoveTo(this.main.Renderer.InternalGamma, 0.0f, 0.5f),
				new Animation.FloatMoveTo(this.main.Renderer.Brightness, 0.0f, 0.5f)
			);
		}

		private void spawn()
		{
			Factory.Get<PlayerFactory>().CreateAndBind(this.main);
			this.main.Add(PlayerFactory.Instance);
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
					this.StartSpawnPointGUID.Value = this.lastEditorSpawnPointGUID;
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
				if (this.main.MapFile.Value == null || !this.CanSpawn || CameraStop.CinematicActive)
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
					}
					this.respawnTimer = 0;
				}
				else if (createPlayer)
				{
					if (this.respawnTimer == 0)
						this.main.AddComponent(new Animation(this.FlashAnimation()));

					if (this.respawnTimer > this.RespawnInterval)
					{
						bool spawnFound = false;

						RespawnLocation foundSpawnLocation = default(RespawnLocation);
						Vector3 foundSpawnAbsolutePosition = Vector3.Zero;

						if (string.IsNullOrEmpty(this.StartSpawnPoint) && this.StartSpawnPointGUID == 0)
						{
							// Look for an autosaved spawn point
							Entity playerData = PlayerDataFactory.Instance;
							if (playerData != null)
							{
								ListProperty<RespawnLocation> respawnLocations = playerData.Get<PlayerData>().RespawnLocations;
								int supportedLocations = 0;
								float lowerLimit = Factory.Get<LowerLimitFactory>().GetLowerLimit();
								while (respawnLocations.Length > 0)
								{
									RespawnLocation respawnLocation = respawnLocations[respawnLocations.Length - 1];
									Entity respawnMapEntity = respawnLocation.Map.Target;
									if (respawnMapEntity != null && respawnMapEntity.Active)
									{
										Voxel respawnMap = respawnMapEntity.Get<Voxel>();
										Vector3 absolutePos = respawnMap.GetAbsolutePosition(respawnLocation.Coordinate);
										if (respawnMap.Active
											&& absolutePos.Y > lowerLimit
											&& respawnMap.GetAbsoluteVector(respawnMap.GetRelativeDirection(Direction.PositiveY).GetVector()).Y > 0.5f
											&& Agent.Query(absolutePos, 0.0f, 20.0f) == null
											&& Rift.Query(absolutePos) == null
											&& Zone.CanSpawnAt(absolutePos))
										{
											Voxel.State state = respawnMap[respawnLocation.Coordinate];
											if (state != Voxel.States.Empty && state != Voxel.States.Infected && state != Voxel.States.HardInfected && state != Voxel.States.Floater)
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
									}
									if (supportedLocations >= 40 || (spawnFound && (foundSpawnAbsolutePosition - this.lastPlayerPosition).Length() > this.RespawnDistance))
									{
										if (supportedLocations > 3) // We should try to spawn the player back at least a few steps
											break;
									}

									respawnLocations.RemoveAt(respawnLocations.Length - 1);
								}
							}
						}

						if (spawnFound)
						{
							// Spawn at an autosaved location
							if (createPlayer)
								this.spawn();
							Vector3 absolutePos = foundSpawnLocation.Map.Target.Get<Voxel>().GetAbsolutePosition(foundSpawnLocation.Coordinate);
							PlayerFactory.Instance.Get<Transform>().Position.Value = this.main.Camera.Position.Value = absolutePos + new Vector3(0, spawnHeightOffset, 0);

							FPSInput.RecenterMouse();
							PlayerFactory.Instance.Get<FPSInput>().Mouse.Value = new Vector2(foundSpawnLocation.Rotation, 0);
						}
						else
						{
							// Spawn at a spawn point
							PlayerSpawn spawn = null;
							Entity spawnEntity = null;
							if (this.StartSpawnPointGUID != 0)
							{
								spawnEntity = this.main.GetByGUID(this.StartSpawnPointGUID);
								if (spawnEntity != null)
									spawn = spawnEntity.Get<PlayerSpawn>();
								this.lastEditorSpawnPointGUID = this.StartSpawnPointGUID;
								this.StartSpawnPointGUID.Value = 0;
							}
							else if (!string.IsNullOrEmpty(this.StartSpawnPoint.Value))
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
							{
								Vector3 pos = spawnEntity.Get<Transform>().Position;
								main.Camera.Position.Value = pos;
								WorldFactory.Instance.Get<World>().UpdateZones();
								Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(pos + new Vector3(0, 2, 0), Vector3.Down, 8, null, false, true);
								if (hit.Voxel == null)
								{
									// There is nowhere to spawn. Reload the map.
									this.respawnTimer = 0;
									IO.MapLoader.Load(this.main, this.main.MapFile);
									return;
								}
								else
								{
									if (createPlayer)
										this.spawn();
									pos = hit.Position + new Vector3(0, spawnHeightOffset, 0);
									PlayerFactory.Instance.Get<Transform>().Position.Value = this.main.Camera.Position.Value = pos;

									if (spawn != null)
									{
										spawn.IsActivated.Value = true;
										FPSInput.RecenterMouse();
										PlayerFactory.Instance.Get<FPSInput>().Mouse.Value = new Vector2(spawn.Rotation, 0);
										spawn.OnSpawn.Execute();
									}
								}
							}
						}

						// When the player teleports to a new map, show the number of orbs and notes on that map
						// If mapJustLoaded is true, we just loaded a save game
						if (this.main.TotalTime < Spawner.DefaultRespawnInterval * 2 && !this.mapJustLoaded)
						{
							WorldFactory.Instance.Add(new Animation
							(
								new Animation.Delay(1.5f),
								new Animation.Execute(delegate()
								{
									int notes = Note.UncollectedCount;
									if (notes > 0)
										this.main.Menu.HideMessage(WorldFactory.Instance, this.main.Menu.ShowMessageFormat(WorldFactory.Instance, notes == 1 ? "\\one note" : "\\note count", notes), 3.0f);

									int orbs = Collectible.ActiveCount;
									if (orbs > 0)
										this.main.Menu.HideMessage(WorldFactory.Instance, this.main.Menu.ShowMessageFormat(WorldFactory.Instance, orbs == 1 ? "\\one orb" : "\\orb count", orbs), 3.0f);
								})
							));
						}

						WorldFactory.Instance.Add(new Animation(this.EndFlashAnimation()));
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