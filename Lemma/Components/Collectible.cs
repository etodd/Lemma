using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class Collectible : Component<Main>
	{

		[XmlIgnore]
		public Command PlayerTouched = new Command();

		public Property<bool> PickedUp = new Property<bool>(); 

		public override void Awake()
		{
			base.Awake();

			PlayerTouched.Action = delegate
			{
				float originalGamma = main.Renderer.InternalGamma.Value;
				float originalBrightness = main.Renderer.Brightness.Value;
				this.Entity.Add(
					new Animation(
						new Animation.Parallel(
							new Animation.FloatMoveTo(main.Renderer.InternalGamma, 10.0f, 0.2f)
							//new Animation.FloatMoveTo(main.Renderer.Brightness, 1.0f, 0.3f)
						),
						new Animation.Parallel(
							new Animation.FloatMoveTo(main.Renderer.InternalGamma, originalGamma, 0.4f)
							//new Animation.FloatMoveTo(main.Renderer.Brightness, originalBrightness, 1.0f)
						),
						new Animation.Execute(this.Entity.Delete.Execute)
					)
				);
				//Increment collectibles picked up or summat
			};
		}
	}
}
