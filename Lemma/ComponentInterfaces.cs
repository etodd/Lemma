using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma
{
	public interface IDrawableComponent : IComponent
	{
		void Draw(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawableAlphaComponent : IComponent
	{
		void DrawAlpha(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawablePreFrameComponent : IComponent
	{
		void DrawPreFrame(GameTime time, RenderParameters parameters);
	}

	public interface INonPostProcessedDrawableComponent : IComponent
	{
		void DrawNonPostProcessed(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}
}
