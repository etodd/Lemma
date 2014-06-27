using System;
using System.Security.Cryptography;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class ConstantFactory : Factory<Main>
	{
		public ConstantFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Constant");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Constant constant = entity.GetOrCreate<Constant>("Constant");

			base.Bind(entity, main, creating);

			entity.Add("Integer", constant.IntProperty, "Integer (whole-number) constant value");
			entity.Add("Float", constant.FloatProperty, "Float (decimal number) constant value");
			entity.Add("Boolean", constant.BoolProperty, "Boolean (true/false) constant value");
			entity.Add("String", constant.StringProperty, "String constant value");
			entity.Add("Vector3", constant.Vector3Property, "Vector3 (X,Y,Z) constant value");
			entity.Add("Vector4", constant.Vector4Property, "Vector4 (X,Y,Z,W) constant value");
			entity.Add("Direction", constant.DirectionProperty, "Integer (whole-number) constant value");
			
		}
	}
}
