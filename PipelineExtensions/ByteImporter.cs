using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace PipelineExtensions
{
	[ContentImporter(".*", DisplayName = "Byte Importer", DefaultProcessor = "PassThroughProcessor")]
	public class ByteImporter : ContentImporter<byte[]>
	{
		public override byte[] Import(string filename, ContentImporterContext context)
		{
			return System.IO.File.ReadAllBytes(filename);
		}
	}
}
