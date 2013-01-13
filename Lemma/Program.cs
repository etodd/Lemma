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
#if DEVELOPMENT
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
				string error = null;
				try
				{
					main = create();
					main.Run();
				}
				catch (Exception e)
				{
					if (!(e is GameMain.ExitException))
						error = e.ToString();
				}
#if ANALYTICS
				if (main.MapFile.Value == null || main.EditorEnabled)
					main.SessionRecorder.Reset();
				if (error == null)
					main.SessionRecorder.RecordEvent("Exit");
				else
					main.SessionRecorder.RecordEvent("Crash", error);
				main.SaveAnalytics();

#if MONOGAME
				// TODO: MonoGame analytics form
#else
				System.Windows.Forms.Application.EnableVisualStyles();
				AnalyticsForm analyticsForm = new AnalyticsForm(main, error);
				System.Windows.Forms.Application.Run(analyticsForm);
#endif
#else
#if MONOGAME
				// TODO: MonoGame error form
#else
				if (error != null)
				{
					System.Windows.Forms.Application.EnableVisualStyles();
					ErrorForm errorForm = new ErrorForm(error);
					System.Windows.Forms.Application.Run(errorForm);
				}
#endif
#endif
			}
		}
	}
}