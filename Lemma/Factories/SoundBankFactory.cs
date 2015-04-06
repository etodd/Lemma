using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;

namespace Lemma.Factories
{
	public class SoundBankFactory : Factory<Main>
	{
		public SoundBankFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "SoundBank");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			SoundBank bank = entity.GetOrCreate<SoundBank>("SoundBank");

			this.SetMain(entity, main);

			entity.Add("Name", bank.Name, new PropertyEntry.EditorData { Options = FileFilter.Get(main, AkBankPath.GetPlatformBasePath(), null, SoundBank.Extension) });
		}
	}
}
