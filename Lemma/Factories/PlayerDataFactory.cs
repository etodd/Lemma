using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;
using Lemma.Util;

namespace Lemma.Factories
{
	public class PlayerDataFactory : Factory<Main>
	{
		private Entity instance;

		public PlayerDataFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "PlayerData");

			const bool enabled = true;

			entity.Add("EnableRoll", new Property<bool> { Value = enabled });
			entity.Add("EnableCrouch", new Property<bool> { Value = enabled });
			entity.Add("EnableKick", new Property<bool> { Value = enabled });
			entity.Add("EnableWallRun", new Property<bool> { Value = enabled });
			entity.Add("EnableWallRunHorizontal", new Property<bool> { Value = enabled });
			entity.Add("EnableEnhancedWallRun", new Property<bool> { Value = enabled });
			entity.Add("EnableSlowMotion", new Property<bool> { Value = enabled });
			entity.Add("EnableStamina", new Property<bool> { Value = enabled });
			entity.Add("EnableMoves", new Property<bool> { Value = true });
			entity.Add("EnablePhone", new Property<bool> { Value = enabled });
			entity.Add("MaxSpeed", new Property<float> { Value = Character.DefaultMaxSpeed, Editable = false });
			entity.Add("GameTime", new Property<float> { Editable = false });
			entity.Add("Phone", new Phone());

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			base.Bind(entity, main, creating);
			this.instance = entity;

			entity.CannotSuspend = true;

			Property<float> gameTime = entity.GetOrMakeProperty<float>("GameTime", false);
			entity.Add(new Updater
			{
				delegate(float dt)
				{
					gameTime.Value += dt;
				}
			});
		}

		public Entity Instance
		{
			get
			{
				if (this.instance != null && !this.instance.Active)
					this.instance = null;
				return this.instance;
			}
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			entity.Delete.Execute();
		}
	}
}
