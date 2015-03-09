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
			PlayerData playerData = entity.GetOrCreate<PlayerData>("Data");
			entity.GetOrCreate<Data>("OpaqueData");
			Phone phone = entity.GetOrCreate<Phone>("Phone");
			playerData.Add(new SetBinding<int>(playerData.CollapseLevel, delegate(int value)
			{
				phone.Set("collapse level", value.ToString());
			}));
			playerData.Add(new SetBinding<int>(playerData.VictimsHandled, delegate(int value)
			{
				phone.Set("victims handled", value.ToString());
			}));

			if (PlayerDataFactory.instance != null)
				PlayerDataFactory.instance.Delete.Execute();

			PlayerDataFactory.instance = entity;
			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				PlayerDataFactory.instance = null;
			}));

			entity.CannotSuspend = true;
		}

		public static Entity Instance
		{
			get
			{
				if (PlayerDataFactory.instance != null && !PlayerDataFactory.instance.Active)
					PlayerDataFactory.instance = null;
				return PlayerDataFactory.instance;
			}
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			entity.Delete.Execute();
		}
	}
}
