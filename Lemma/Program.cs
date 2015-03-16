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
				catch (Main.ExitException)
				{
#if ANALYTICS
					main.SessionRecorder.RecordEvent("Exit");
					if (!main.IsChallengeMap(main.MapFile))
						main.SaveAnalytics();
#endif
				}
#if !DEBUG
				catch (Exception e)
				{
					System.Windows.Forms.Application.EnableVisualStyles();
					ErrorForm errorForm = new ErrorForm(e.ToString(), main != null ? (main.Settings != null ? main.Settings.UUID : null) : null);
#if ANALYTICS
					if (main != null)
					{
						main.SessionRecorder.RecordEvent("Crash", e.ToString());
						main.SaveAnalytics();
						errorForm.Session = main.SessionRecorder;
					}
#endif
					System.Windows.Forms.Application.Run(errorForm);
				}
#endif
				finally
				{
					if (main != null)
						main.Cleanup();
				}
			}
		}
	}
}