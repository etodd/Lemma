using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma
{
	public interface IDrawableComponent : IGraphicsComponent
	{
		void Draw(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawableAlphaComponent : IGraphicsComponent
	{
		void DrawAlpha(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawablePostAlphaComponent : IGraphicsComponent
	{
		void DrawPostAlpha(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawablePreFrameComponent : IGraphicsComponent
	{
		void DrawPreFrame(GameTime time, RenderParameters parameters);
	}

	public interface INonPostProcessedDrawableComponent : IGraphicsComponent
	{
		void DrawNonPostProcessed(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}
}
