using System;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Lemma
{
	public static class Program
	{
		private static void run()
		{
#if DEBUG
			using (Main main = new GameMain(true, null)) // Editor binary
#else
			using (Main main = new GameMain(false, "test")) // Game binary
#endif
				main.Run();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		public static void Main(string[] args)
		{
			if (Debugger.IsAttached)
				Program.run();
			else
			{
				try
				{
					Program.run();
				}
				catch (Exception)
				{
					// TODO: Error popup
				}
			}
		}
	}
}