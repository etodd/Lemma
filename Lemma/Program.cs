using System;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Lemma
{
	public static class Program
	{
		private static GameMain create()
		{
#if DEBUG
			return new GameMain(true, null); // Editor binary
#else
			return new GameMain(false, "start"); // Game binary
#endif
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		public static void Main(string[] args)
		{
			GameMain main = null;
			if (Debugger.IsAttached)
			{
				main = create();
				main.Run();
			}
			else
			{
				try
				{
					main = create();
					main.Run();
				}
				catch (Exception e)
				{

#if ANALYTICS
					if (main.MapFile.Value == null || main.EditorEnabled)
						main.SessionRecorder.Reset();
					main.SessionRecorder.RecordEvent("Crash", e.ToString());
					main.SaveAnalytics();
#endif

#if MONOGAME
					// TODO: MonoGame popup
#else
					System.Windows.Forms.Application.EnableVisualStyles();
					ErrorForm errorForm = new ErrorForm(e.ToString());
					System.Windows.Forms.Application.Run(errorForm);
#endif
				}
			}
		}
	}
}