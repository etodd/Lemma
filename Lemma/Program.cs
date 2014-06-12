using System; using ComponentBind;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Lemma
{
	
	public static class Program
	{
		
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
	    [STAThread]
		public static void Main(string[] args)
		{
			string error = null;
			Main main = null;
			if (Debugger.IsAttached)
			{
				main = new Main();
				try
				{
					main.Run();
				}
				catch (Main.ExitException)
				{

				}
			}
			else
			{
				try
				{
					main = new Main();
					main.Run();
				}
				catch (Exception e)
				{
					if (!(e is Main.ExitException))
						error = e.ToString();
				}
			}
			if (main != null)
				main.Cleanup();
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
			string anonymousId = "";
			if (main.Settings != null)
				anonymousId = main.Settings.UUID;
			AnalyticsForm analyticsForm = new AnalyticsForm(main, anonymousId, error);
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