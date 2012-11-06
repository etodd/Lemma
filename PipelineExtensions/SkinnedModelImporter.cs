#region File Description
//-----------------------------------------------------------------------------
// Author: C J Bailey
//-----------------------------------------------------------------------------
// FBX Multi Take importer for XNA 4
// Imports an FBX (ascii) file containing multiple animations and splits out the 
// "takes" data.  It then simply builds temporary FBX files (one for each 
// animation), processes each one with the standard FBX importer and combine 
// them into a single output at the end.  
// This automates what many do manually.
//-----------------------------------------------------------------------------
// Discussion Forum
// http://forums.create.msdn.com/forums/p/68149/426645.aspx
//-----------------------------------------------------------------------------
#endregion

#region MIT License
/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Design;

// The type to import.
using TImport = Microsoft.Xna.Framework.Content.Pipeline.Graphics.NodeContent;

// Change the namespace to suit your project
namespace PipelineExtensions
{
	[ContentImporter(".fbx", DisplayName = "Multi-take FBX Importer", DefaultProcessor = "SkinnedModelProcessor")]
	public class SkinnedModelImporter : FbxImporter
	{
		private List<string> _animfiles;
		private List<string> _fbxheader;
		private TImport _master;
		private ContentImporterContext _context;
		
		public override TImport Import(string filename, ContentImporterContext context)
		{
			_context = context;

			// _animfiles will contain list of new temp anim files.
			_animfiles = new List<string>();

			// Decouple header and animation data.
			ExtractAnimations(filename);
			
			// Process master file (this will also process the first animation)
			_master = base.Import(filename, context);

			// Process the remaining animations.
			foreach (string file in _animfiles) {
				TImport anim = base.Import(file, context);
				
				// Append animation to master NodeContent.
				AppendAnimation(_master, anim);
			}
			
			// Delete the temporary animation files.
			DeleteTempFiles();
			
			return _master;
		}
		
		private void AppendAnimation(NodeContent masternode, NodeContent animnode)
		{
			foreach (KeyValuePair<string, AnimationContent> anim in animnode.Animations)
				masternode.Animations[anim.Key] = anim.Value;
			
			//foreach (NodeContent child in animnode.Children) {
			//    if (child != null) {
			//        AppendAnimation(child);
			//    }
			//}

			for (int i = 0; i < masternode.Children.Count; i++) {
				if (animnode.Children[i] != null) {
					AppendAnimation(masternode.Children[i], animnode.Children[i]);
				}
			}
		}
		
		private void ExtractAnimations(string filename)
		{
			List<string> masterFile = File.ReadAllLines(filename).ToList();
			string path = Path.GetDirectoryName(filename);
			int open_idx = 0,
				length,
				num_open = -1,
				filenum = 0;
			bool foundTake = false;

			int idx = masterFile.IndexOf("Takes:  {") + 1;
			_fbxheader = masterFile.Take(idx).ToList();
			List<string> anims = masterFile.Skip(idx).ToList();
			
			// Extract each animation and create a temporary anim file.
			for (int i = 0; i < anims.Count; i++) {
				if (anims[i].Contains("Take: ")) {
					open_idx = i;
					num_open = 0;
					foundTake = true;
				}
				
				if (anims[i].Contains("{") &&
					foundTake) {
					num_open++;
				}

				if (anims[i].Contains("}") &&
					foundTake) {
					num_open--;
				}
				
				if (num_open == 0 &&
					foundTake) {
					// Skip first animation since this is processed in the master
					// fbx file.
					if (filenum > 0) {
						length = i - open_idx + 1;
						
						// Create temp file from header + anim data.
						CreateTempFile(Path.Combine(path, "tmp.anim." + filenum + ".fbx"),
									   anims.Skip(open_idx).Take(length).ToArray());
					}
					filenum++;
					foundTake = false;
				}
			}
		}
		
		private void CreateTempFile(string filename, string[] data)
		{
			List<string> completefile = new List<string>();
			completefile.AddRange(_fbxheader);
			completefile.AddRange(data);

			try {
				// Write data to new temp file.
				File.WriteAllLines(filename, completefile.ToArray());

				// Store temp file name for processing.
				_animfiles.Add(filename);
			}
			catch {
				// Error while creating temp file.
				_context.Logger.LogWarning(null, null, "Error creating temp file: {0}", filename);
			}
		}
		
		private void DeleteTempFiles()
		{
			foreach (string file in _animfiles) {
				File.Delete(file);
			}
		}
	}
}
