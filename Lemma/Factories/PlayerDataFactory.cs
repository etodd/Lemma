using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class PlayerDataFactory : Factory
	{
		private Entity instance;

		public PlayerDataFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "PlayerData");

#if DEVELOPMENT
			const bool enabled = true;
#else
			const bool enabled = false;
#endif

			result.Add("EnableRoll", new Property<bool> { Value = enabled });
			result.Add("EnableKick", new Property<bool> { Value = enabled });
			result.Add("EnableWallRun", new Property<bool> { Value = enabled });
			result.Add("EnableWallRunHorizontal", new Property<bool> { Value = enabled });
			result.Add("EnableEnhancedWallRun", new Property<bool> { Value = enabled });
			result.Add("EnableLevitation", new Property<bool> { Value = enabled });
			result.Add("EnableSlowMotion", new Property<bool> { Value = enabled });
			result.Add("EnableStamina", new Property<bool> { Value = enabled });
			result.Add("EnableMoves", new Property<bool> { Value = true });
			result.Add("Stamina", new Property<int> { Value = 100 });
			result.Add("Pistol", new Property<Entity.Handle> { Editable = false });
			result.Add("GameTime", new Property<float> { Editable = false });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main, creating);
			this.instance = result;

			result.CannotSuspend = true;

			Property<float> gameTime = result.GetOrMakeProperty<float>("GameTime", false);
			result.Add(new Updater
			{
				delegate(float dt)
				{
					gameTime.Value += dt;
				}
			});
		}

		public Entity Instance(Main main)
		{
			if (this.instance != null && this.instance.Active)
				return this.instance;
			else
			{
				Entity entity = this.CreateAndBind(main);
				main.Add(entity);
				return entity;
			}
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			result.Delete.Execute();
		}
	}
}
