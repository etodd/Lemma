using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace PipelineExtensions
{
	[ContentImporter(".*", DisplayName = "String Importer", DefaultProcessor = "PassThroughProcessor")]
	public class TextImporter : ContentImporter<string>
	{
		public override string Import(string filename, ContentImporterContext context)
		{
			return System.IO.File.ReadAllText(filename);
		}
	}
}
