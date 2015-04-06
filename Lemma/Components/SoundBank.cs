using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class SoundBank : Component<Main>
	{
		public Property<string> Name = new Property<string>();

		public const string Extension = ".bnk";

		private uint bank_id;

		public override void Awake()
		{
			base.Awake();
			if (!this.main.EditorEnabled && !string.IsNullOrEmpty(this.Name))
			{
				AKRESULT result = AkSoundEngine.LoadBank(this.Name, AkSoundEngine.AK_DEFAULT_POOL_ID, out this.bank_id);
				if (result != AKRESULT.AK_Success)
					Log.d(string.Format("Failed to load soundbank {0}: {1}", this.Name, result));
			}
		}

		public override void delete()
		{
			base.delete();
			if (this.bank_id != 0)
			{
				int memory_pool_id;
				AkSoundEngine.UnloadBank(this.bank_id, IntPtr.Zero, out memory_pool_id);
			}
		}
	}
}
