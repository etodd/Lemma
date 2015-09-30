using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma
{
	public class OculusGraphicsDeviceManager : GraphicsDeviceManager
	{
		private ulong luid;

		public OculusGraphicsDeviceManager(Game game, ulong luid)
			: base(game)
		{
			this.luid = luid;
		}

		protected override GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)
		{
			foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
			{
				GraphicsDeviceInformation info = new GraphicsDeviceInformation();
				info.Adapter = adapter;
				info.GraphicsProfile = this.GraphicsProfile;
				info.PresentationParameters.MultiSampleCount = 0;
				info.PresentationParameters.IsFullScreen = this.IsFullScreen;
				info.PresentationParameters.PresentationInterval = (this.SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate);
				return info;
			}
			throw new Exception("Failed to find Oculus graphics adapter.");
		}
	}
}
