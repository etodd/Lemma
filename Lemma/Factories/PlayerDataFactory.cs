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
		private static Entity instance;

		public PlayerDataFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "PlayerData");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			base.Bind(entity, main, creating);
			entity.GetOrCreate<PlayerData>("Data");
			entity.GetOrCreate<Data>("OpaqueData");
			entity.GetOrCreate<Phone>("Phone");
			instance = entity;

			entity.CannotSuspend = true;
		}

		public static Entity Instance
		{
			get
			{
				if (instance != null && !instance.Active)
					instance = null;
				return instance;
			}
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			entity.Delete.Execute();
		}
	}
}
