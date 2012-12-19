using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Factories;

namespace Lemma.Components
{
	public class EnemyBase : Component
	{
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<float> Offset = new Property<float> { Editable = true, Value = 4.0f };
		public Property<Entity.Handle> Map = new Property<Entity.Handle> { Editable = false };
		public ListProperty<Map.Box> BaseBoxes = new ListProperty<Map.Box> { Editable = false };

		private CommandBinding<IEnumerable<Map.Coordinate>, Map> cellEmptiedBinding;

		public bool EnableCellEmptyBinding
		{
			get
			{
				return this.cellEmptiedBinding != null && this.cellEmptiedBinding.Enabled;
			}
			set
			{
				if (this.cellEmptiedBinding != null)
					this.cellEmptiedBinding.Enabled = value;
			}
		}

		public bool IsValid
		{
			get
			{
				if (this.Map.Value.Target == null || !this.Map.Value.Target.Active)
					return false;

				Map m = this.Map.Value.Target.Get<Map>();

				bool found = false;
				foreach (Map.Box box in this.BaseBoxes)
				{
					foreach (Map.Coordinate coord in box.GetCoords())
					{
						if (m[coord].Name == "Infected")
						{
							found = true;
							break;
						}
					}
				}

				if (!found)
					return false;

				return true;
			}
		}

		public static void AttachEditorComponents(Entity result, Main main, Vector3 color)
		{
			Property<float> offset = result.Get<EnemyBase>().Offset;

			Model model = new Model();
			model.Filename.Value = "Models\\cone";
			model.Color.Value = color;
			model.IsInstanced.Value = false;
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(1.0f, 1.0f, x), offset));
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel2", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<EnemyBase>().Transform));
		}

		public override void InitializeProperties()
		{
			this.Add(new Binding<Vector3>(this.Position, () => Vector3.Transform(new Vector3(0.0f, 0.0f, this.Offset), this.Transform), this.Offset, this.Transform));
			if (!this.main.EditorEnabled)
			{
				Action setupMap = delegate()
				{
					Entity entity = this.Map.Value.Target;
					if (entity == null || !entity.Active)
					{
						this.Delete.Execute();
						return;
					}
					else
					{
						if (this.cellEmptiedBinding != null)
							this.Remove(this.cellEmptiedBinding);
						this.cellEmptiedBinding = new CommandBinding<IEnumerable<Map.Coordinate>, Map>(entity.Get<Map>().CellsEmptied, delegate(IEnumerable<Map.Coordinate> coords, Map newMap)
						{
							bool check = false;
							foreach (Map.Coordinate coord in coords)
							{
								if (coord.Data.Name == "Infected")
								{
									check = true;
									break;
								}
							}
							if (check && !this.IsValid)
								this.Delete.Execute();
						});
						this.Add(this.cellEmptiedBinding);
					}
				};
				this.Add(new NotifyBinding(setupMap, this.Map));
				if (this.Map.Value.Target != null)
					setupMap();

				this.main.AddComponent(new PostInitialization
				{
					delegate()
					{
						if (this.Map.Value.Target == null || !this.Map.Value.Target.Active)
						{
							this.BaseBoxes.Clear();

							bool found = false;
							foreach (Map m in Lemma.Components.Map.Maps)
							{
								Map.Box box = m.GetBox(this.Position);
								if (box != null && box.Type.Name == "Infected")
								{
									foreach (Map.Box b in m.GetContiguousByType(new[] { box }))
										this.BaseBoxes.Add(b);
									this.Map.Value = m.Entity;
									found = true;
									break;
								}
							}
							if (!found)
								this.Delete.Execute();
						}
					}
				});
			}
		}

		public static void SpawnPickupsOnDeath(Main main, Entity entity, int minCount = 3, int maxCount = 10, int minEnergy = 2, int maxEnergy = 10, float chanceOfAmmo = 0.05f)
		{
			if (!main.EditorEnabled)
			{
				EnemyBase enemyBase = entity.Get<EnemyBase>();
				enemyBase.Add(new CommandBinding(enemyBase.Delete, delegate()
				{
					Vector3 pos = enemyBase.Position;
					Random r = new Random();
					int count = r.Next(minCount, maxCount);
					EnergyPickupFactory energyPickupFactory = Factory.Get<EnergyPickupFactory>();
					MagazineFactory magazineFactory = Factory.Get<MagazineFactory>();
					for (int i = 0; i < count; i++)
					{
						Vector3 direction = new Vector3((float)r.NextDouble() - 0.5f, 0.0f, (float)r.NextDouble() - 0.5f);
						direction.Normalize();
						direction *= enemyBase.Offset;
						direction.Y = (float)r.NextDouble() * enemyBase.Offset;

						bool playerHasPistol = Factory.Get<PlayerDataFactory>().Instance(main).GetProperty<Entity.Handle>("Pistol").Value.Target != null;
						bool isAmmo = playerHasPistol && (r.NextDouble() < chanceOfAmmo);

						Entity pickup = isAmmo ? magazineFactory.CreateAndBind(main) : energyPickupFactory.CreateAndBind(main);
						pickup.Get<Transform>().Position.Value = pos + direction;

						if (!isAmmo)
						{
							pickup.GetProperty<bool>("Respawn").Value = false;
							pickup.GetProperty<int>("Energy").Value = r.Next(minEnergy, maxEnergy);
							direction.Normalize();
							pickup.Add(new Animation
							(
								new Animation.Ease
								(
									new Animation.Vector3MoveBySpeed(pickup.Get<Transform>().Position, Vector3.Normalize(direction) * enemyBase.Offset * (float)r.NextDouble(), 2.0f),
									Animation.Ease.Type.OutQuadratic
								)
							));
						}
						main.Add(pickup);
					}
				}));
			}
		}
	}
}
