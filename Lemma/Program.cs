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
			bool vr = args.Contains("-vr");
			string error = null;
			Main main = null;
			if (Debugger.IsAttached)
			{
				main = new Main(vr);
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
					main = new Main(vr);
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