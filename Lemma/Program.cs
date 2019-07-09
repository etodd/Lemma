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
#if !FNA
			System.Windows.Forms.Application.EnableVisualStyles();
#endif
#if VR
			bool vr = args.Select(x => x.ToLower()).Contains("-vr");
#endif

			int monitor = 0;
#if !FNA // TODO: Multiple monitors are supported, but we need something other than a Form :| -flibit
			if (GraphicsAdapter.Adapters.Count > 1)
			{
				AdapterSelectorForm selectorForm = new AdapterSelectorForm(vr);
				System.Windows.Forms.Application.Run(selectorForm);
				if (!selectorForm.Go)
					return;

				vr = selectorForm.VR;
				monitor = selectorForm.Monitor;
			}
#endif

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
#if FNA // TODO: Error reporting -flibit
					SDL2.SDL.SDL_ShowSimpleMessageBox(SDL2.SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "CRASH", e.ToString(), IntPtr.Zero);
#else
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
#endif
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