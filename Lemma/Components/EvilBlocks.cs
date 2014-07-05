using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class EvilBlocks : Component<Main>, IUpdateableComponent
	{
		public ListProperty<Entity.Handle> Blocks = new ListProperty<Entity.Handle>();
		
		public Property<int> OperationalRadius = new Property<int> { Value = 100 };

		[XmlIgnore]
		public Property<Vector3> Position = new Property<Vector3>();

		public Property<Entity.Handle> TargetAgent = new Property<Entity.Handle>();

		private List<PhysicsBlock> blocks = new List<PhysicsBlock>();

		private const int totalBlocks = 30;

		private static Random random = new Random();

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
		}

		public override void Start()
		{
			if (!main.EditorEnabled && this.Blocks.Length < totalBlocks)
			{
				SceneryBlockFactory factory = Factory.Get<SceneryBlockFactory>();
				Random random = new Random();
				Vector3 scale = new Vector3(0.6f);
				Vector3 blockSpawnPoint = this.Position;
				for (int i = 0; i < totalBlocks; i++)
				{
					Entity block = factory.CreateAndBind(main);
					block.Get<Transform>().Position.Value = blockSpawnPoint + new Vector3(((float)EvilBlocks.random.NextDouble() - 0.5f) * 2.0f, ((float)EvilBlocks.random.NextDouble() - 0.5f) * 2.0f, ((float)EvilBlocks.random.NextDouble() - 0.5f) * 2.0f);
					block.Get<PhysicsBlock>().Size.Value = scale;
					block.Get<ModelInstance>().Scale.Value = scale;
					block.Get<SceneryBlock>().Type.Value = Voxel.t.Black;
					this.Blocks.Add(block);
					main.Add(block);
				}
			}
		}

		public void Update(float dt)
		{
			if (this.blocks.Count < this.Blocks.Length)
			{
				foreach (Entity.Handle e in this.Blocks)
				{
					PhysicsBlock block = e.Target.Get<PhysicsBlock>();
					if (!this.blocks.Contains(block))
					{
						block.Add(new CommandBinding<BEPUphysics.BroadPhaseEntries.Collidable, BEPUphysics.NarrowPhaseSystems.Pairs.ContactCollection>(block.Collided, delegate(BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.ContactCollection contacts)
						{
							if (other.Tag != null && other.Tag.GetType().IsAssignableFrom(typeof(Character)))
							{
								// Damage the player
								Entity p = PlayerFactory.Instance;
								if (p != null && p.Active)
									p.Get<Player>().Health.Value -= 0.1f;
							}
						}));
						this.blocks.Add(block);
					}
				}
			}

			foreach (PhysicsBlock block in blocks)
			{
				if (!block.Suspended)
				{
					Vector3 toCenter = this.Position - block.Box.Position;
					if (toCenter.Length() > 10.0f)
						block.Box.Position = this.Position + Vector3.Normalize(toCenter) * -10.0f;
					Vector3 force = toCenter * main.ElapsedTime * 4.0f;
					block.Box.ApplyLinearImpulse(ref force);
				}
			}
		}
	}
}
