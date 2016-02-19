using System;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

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
			System.Windows.Forms.Application.EnableVisualStyles();
#if VR
			bool vr = args.Select(x => x.ToLower()).Contains("-vr");
#endif

			int monitor = 0;
			if (GraphicsAdapter.Adapters.Count > 1)
			{
				AdapterSelectorForm selectorForm = new AdapterSelectorForm(vr);
				System.Windows.Forms.Application.Run(selectorForm);
				if (!selectorForm.Go)
					return;

				vr = selectorForm.VR;
				monitor = selectorForm.Monitor;
			}

			Main main = null;
			if (Debugger.IsAttached)
			{
#if VR
				main = new Main(monitor, vr);
#else
				main = new Main(monitor);
#endif
				try
				{
					main.Run();
				}
				catch (Main.ExitException)
				{
				}
				main.Cleanup();
			}
			else
			{
				try
				{
#if VR
					main = new Main(monitor, vr);
#else
					main = new Main(monitor);
#endif
					main.Run();
				}
				catch (Main.ExitException)
				{
#if ANALYTICS
					main.SessionRecorder.RecordEvent("Exit");
					if (!main.IsChallengeMap(main.MapFile) && main.MapFile.Value != Lemma.Main.MenuMap)
						main.SaveAnalytics();
#endif
				}
#if !DEBUG
				catch (Exception e)
				{
					string uuid = main != null ? (main.Settings != null ? main.Settings.UUID : null) : null;
					Lemma.Main.Config.RecordAnalytics analytics = main != null ? (main.Settings != null ? main.Settings.Analytics : Lemma.Main.Config.RecordAnalytics.Off) : Lemma.Main.Config.RecordAnalytics.Off;
					ErrorForm errorForm = new ErrorForm(e.ToString(), uuid, analytics == Lemma.Main.Config.RecordAnalytics.On);
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