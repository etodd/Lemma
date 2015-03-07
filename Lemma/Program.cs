using System;
using ComponentBind;
using System.Linq;
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
#if VR
			bool vr = args.Contains("-vr");
#endif
			string error = null;
			Main main = null;
			if (Debugger.IsAttached)
			{
#if VR
				main = new Main(vr);
#else
				main = new Main();
#endif
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
#if VR
					main = new Main(vr);
#else
					main = new Main();
#endif
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
			if (main != null)
			{
				if (error == null)
					main.SessionRecorder.RecordEvent("Exit");
				else
					main.SessionRecorder.RecordEvent("Crash", error);
				if (!(error == null && main.IsChallengeMap(main.MapFile)))
					main.SaveAnalytics();
			}

			System.Windows.Forms.Application.EnableVisualStyles();
			string anonymousId = "";
			if (main != null && main.Settings != null)
				anonymousId = main.Settings.UUID;
			AnalyticsForm analyticsForm = new AnalyticsForm(main, anonymousId, error);
			System.Windows.Forms.Application.Run(analyticsForm);
#else
			if (error != null)
			{
				System.Windows.Forms.Application.EnableVisualStyles();
				ErrorForm errorForm = new ErrorForm(error);
				System.Windows.Forms.Application.Run(errorForm);
			}
#endif
		}
	}
}