using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Factories
{
	public class RainFactory : Factory<Main>
	{
		public RainFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Rain");

			result.Add("Transform", new Transform());
			ParticleEmitter emitter = new ParticleEmitter();
			emitter.ParticleType.Value = "Rain";
			emitter.ParticlesPerSecond.Value = 12000;
			result.Add("Emitter", emitter);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspendByDistance = true;

			const float kernelSpacing = 8.0f;
			const int kernelSize = 10;
			const float raycastHeight = 30.0f;
			const float rainStartHeight = 25.0f;
			const float raycastInterval = 0.25f;
			const float verticalSpeed = 90.0f;
			const float maxLifetime = 1.0f;

			ParticleEmitter emitter = result.Get<ParticleEmitter>("Emitter");
			emitter.Jitter.Value = new Vector3(kernelSpacing * kernelSize * 0.5f, 0.0f, kernelSpacing * kernelSize * 0.5f);
			Transform transform = result.Get<Transform>();

			Sound rainSound = result.GetOrCreate<Sound>("RainSound");
			rainSound.Cue.Value = "Rain";
			rainSound.Is3D.Value = false;
			rainSound.IsPlaying.Value = true;
			Property<float> rainSoundVolume = rainSound.GetProperty("Volume");

			Components.DirectionalLight lightning = result.GetOrCreate<Components.DirectionalLight>("Lightning");
			lightning.Enabled.Value = false;
			Vector3 originalLightningColor = lightning.Color;

			float[,] audioKernel = new float[kernelSize, kernelSize];
			float sum = 0.0f;
			for (int x = 0; x < kernelSize; x++)
			{
				for (int y = 0; y < kernelSize; y++)
				{
					float cell = (kernelSize / 2) - new Vector2(x - (kernelSize / 2), y - (kernelSize / 2)).Length();
					audioKernel[x, y] = cell;
					sum += cell;
				}
			}

			for (int x = 0; x < kernelSize; x++)
			{
				for (int y = 0; y < kernelSize; y++)
					audioKernel[x, y] /= sum;
			}

			float[,] raycastHeights = new float[kernelSize, kernelSize];

			float raycastTimer = raycastInterval;
			Vector3 kernelOffset = Vector3.Zero;
			Updater updater = new Updater
			{
				delegate(float dt)
				{
					raycastTimer += dt;
					if (raycastTimer > raycastInterval)
					{
						raycastTimer = 0.0f;
						Vector3 cameraPos = main.Camera.Position;
						float averageHeight = 0.0f;
						kernelOffset = main.Camera.Position + new Vector3(kernelSize * kernelSpacing * -0.5f, raycastHeight + rainStartHeight, kernelSize * kernelSpacing * -0.5f);
						for (int x = 0; x < kernelSize; x++)
						{
							for (int y = 0; y < kernelSize; y++)
							{
								Vector3 pos = kernelOffset + new Vector3(x * kernelSpacing, 0, y * kernelSpacing);
								Map.GlobalRaycastResult raycast = Map.GlobalRaycast(pos, Vector3.Down, rainStartHeight + raycastHeight + (verticalSpeed * maxLifetime));
								float height = raycast.Map == null ? float.MinValue : raycast.Position.Y;
								raycastHeights[x, y] = height;
								averageHeight += Math.Max(cameraPos.Y, Math.Min(height, cameraPos.Y + rainStartHeight)) * audioKernel[x, y];
							}
						}
						rainSoundVolume.Value = 1.0f - ((averageHeight - cameraPos.Y) / rainStartHeight);
					}
				}
			};
			updater.EnabledInEditMode.Value = true;
			result.Add(updater);

			Random random = new Random();
			Property<float> thunderIntervalMin = result.GetOrMakeProperty<float>("ThunderIntervalMin", true, 12.0f);
			Property<float> thunderIntervalMax = result.GetOrMakeProperty<float>("ThunderIntervalMax", true, 36.0f);
			Property<float> thunderMaxDelay = result.GetOrMakeProperty<float>("ThunderMaxDelay", true, 5.0f);
			Timer timer = result.GetOrCreate<Timer>();
			timer.Serialize = false;
			timer.Repeat.Value = true;
			timer.Interval.Value = (float)random.NextDouble() * thunderIntervalMax;
			timer.Add(new CommandBinding(timer.Command, delegate()
			{
				float volume = 0.5f + ((float)random.NextDouble() * 0.5f);
				result.Add(new Animation
				(
					new Animation.Set<bool>(lightning.Enabled, true),
					new Animation.Vector3MoveTo(lightning.Color, originalLightningColor * volume, 0.2f),
					new Animation.Vector3MoveTo(lightning.Color, Vector3.Zero, 0.25f),
					new Animation.Set<bool>(lightning.Enabled, false),
					new Animation.Delay((1.0f - volume) * thunderMaxDelay),
					new Animation.Execute(delegate()
					{
						Sound thunder = Sound.PlayCue(main, "Thunder", main.Camera.Position + Vector3.Normalize(new Vector3(2.0f * ((float)random.NextDouble() - 0.5f), 1.0f, 2.0f * ((float)random.NextDouble() - 0.5f))) * 1000.0f, volume);
						if (thunder != null)
							result.Add(thunder);
					})
				));
				timer.Interval.Value = thunderIntervalMin + ((float)random.NextDouble() * (thunderIntervalMax - thunderIntervalMin));
			}));

			if (ParticleSystem.Get(main, "Rain") == null)
			{
				ParticleSystem.Add(main, "Rain",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\default",
					EffectFile = "Effects\\ParticleRain",
					MaxParticles = 25000,
					Duration = TimeSpan.FromSeconds(maxLifetime),
					MinHorizontalVelocity = 0.0f,
					MaxHorizontalVelocity = 0.0f,
					MinVerticalVelocity = -verticalSpeed,
					MaxVerticalVelocity = -verticalSpeed,
					Gravity = new Vector3(0.0f, 0.0f, 0.0f),
					MinRotateSpeed = 0.0f,
					MaxRotateSpeed = 0.0f,
					MinStartSize = 0.3f,
					MaxStartSize = 0.3f,
					MinEndSize = 0.3f,
					MaxEndSize = 0.3f,
					BlendState = BlendState.Opaque,
					MinColor = new Vector4(0.5f, 0.6f, 0.7f, 1.0f),
					MaxColor = new Vector4(0.5f, 0.6f, 0.7f, 1.0f),
				});
				emitter.ParticleType.Reset();
			}

			emitter.AddParticle = delegate(Vector3 position, Vector3 velocity) 
			{
				Vector3 kernelCoord = (position - kernelOffset) / kernelSpacing;
				float height = raycastHeights[Math.Max(0, Math.Min(kernelSize - 1, (int)kernelCoord.X)), Math.Max(0, Math.Min(kernelSize - 1, (int)kernelCoord.Z))];
				if (height < position.Y)
					emitter.ParticleSystem.AddParticle(position, Vector3.Zero, Math.Min((position.Y - height) / verticalSpeed, maxLifetime));
			};

			emitter.Add(new Binding<Vector3>(emitter.Position, x => x + new Vector3(0.0f, rainStartHeight, 0.0f), main.Camera.Position));

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			Property<bool> selected = new Property<bool> { Value = false, Editable = false, Serialize = false };
			result.Add("EditorSelected", selected);

			Components.DirectionalLight lightning = result.Get<Components.DirectionalLight>("Lightning");
			lightning.Add(new Binding<bool>(lightning.Enabled, selected));
		}
	}
}