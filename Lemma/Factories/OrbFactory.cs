using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class OrbFactory : Factory
	{
		public OrbFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Orb");

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.IsInstanced.Value = false;
			model.Scale.Value = new Vector3(0.25f);
			model.Editable = false;
			result.Add("Model", model);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.GetOrCreate<PointLight>("PointLight");
			Transform transform = result.GetOrCreate<Transform>("Transform");
			light.Add(new TwoWayBinding<Vector3>(light.Position, transform.Position));

			VoxelChaseAI chase = result.GetOrCreate<VoxelChaseAI>("VoxelChaseAI");
			chase.Add(new TwoWayBinding<Vector3>(transform.Position, chase.Position));
			result.Add(new CommandBinding(chase.Delete, result.Delete));

			AI ai = result.GetOrCreate<AI>();

			ai.Add(new AI.State
			{
				Name = "Chase",
				Tasks = new[]
				{ 
					new AI.Task
					{
						Action = delegate()
						{
							Entity player = PlayerFactory.Instance;
							if (player != null)
							{
								chase.Target.Value = player.Get<Transform>().Position;
								chase.TargetActive.Value = true;
							}
							else
								chase.TargetActive.Value = false;
						}
					}
				}
			});

			Model model = result.Get<Model>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Add(new Binding<Vector3>(model.Color, light.Color));

			this.SetMain(result, main);
		}
	}
}
