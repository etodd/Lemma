using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class LogicGateFactory : Factory<Main>
	{
		public LogicGateFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "LogicGate");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
			EntityConnectable.AttachEditorComponents(entity, "Input1Target", entity.Get<LogicGate>().Input1Target);
			EntityConnectable.AttachEditorComponents(entity, "Input2Target", entity.Get<LogicGate>().Input2Target);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			LogicGate gate = entity.GetOrCreate<LogicGate>("LogicGate");

			base.Bind(entity, main, creating);

			entity.Add("Input1", gate.Input1, new PropertyEntry.EditorData { Readonly = true });
			entity.Add("Input1On", gate.Input1On);
			entity.Add("Input1Off", gate.Input1Off);

			entity.Add("Input2", gate.Input2, new PropertyEntry.EditorData { Readonly = true });
			entity.Add("Input2On", gate.Input2On);
			entity.Add("Input2Off", gate.Input2Off);

			entity.Add("Output", gate.Output, new PropertyEntry.EditorData { Readonly = true });
			entity.Add("OutputOn", gate.OutputOn);
			entity.Add("OutputOff", gate.OutputOff);

			if (main.EditorEnabled)
			{
				Action<ListProperty<string>, Entity.Handle> populateOptions = delegate(ListProperty<string> options, Entity.Handle target)
				{
					options.Clear();
					Entity e = target.Target;
					if (e != null && e.Active)
					{
						foreach (KeyValuePair<string, PropertyEntry> p in e.Properties)
						{
							if (p.Value.Property.GetType().GetGenericArguments().First() == typeof(bool))
								options.Add(p.Key);
						}
					}
				};

				ListProperty<string> input1Options = new ListProperty<string>();
				entity.Add("Input1TargetProperty", gate.Input1TargetProperty, new PropertyEntry.EditorData
				{
					Options = input1Options,
				});

				entity.Add(new NotifyBinding(delegate()
				{
					populateOptions(input1Options, gate.Input1Target);
				}, gate.Input1Target));

				ListProperty<string> input2Options = new ListProperty<string>();
				entity.Add("Input2TargetProperty", gate.Input2TargetProperty, new PropertyEntry.EditorData
				{
					Options = input2Options,
				});

				entity.Add(new NotifyBinding(delegate()
				{
					populateOptions(input2Options, gate.Input2Target);
				}, gate.Input2Target));


				entity.Add(new PostInitialization
				{
					delegate()
					{
						populateOptions(input1Options, gate.Input1Target);
						populateOptions(input2Options, gate.Input2Target);
					}
				});
			}
			else
			{
				entity.Add("Input1TargetProperty", gate.Input1TargetProperty);
				entity.Add("Input2TargetProperty", gate.Input2TargetProperty);
			}
		}
	}
}