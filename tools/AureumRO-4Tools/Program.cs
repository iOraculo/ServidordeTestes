using System;
using System.Threading;
using System.Windows.Forms;
using _4RTools.Utils;

namespace _4RTools
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Model.AppSettings startupSettings = Model.AppSettings.Load();
            LocalizationManager.SetLanguage(startupSettings.Language);

            // AutoPatcher is intentionally disabled in the Test Server build:
            // official AureumRO-4Tools releases restrict servers to "AureumRO" only
            // and would silently overwrite FroggoClient/Ragexe support on launch.

            using (Forms.ClientUpdaterForm updater = new Forms.ClientUpdaterForm())
            {
                updater.ShowDialog();
            }

            Application.Run(new Forms.Container());
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Logger.Error("Program", "Unhandled UI thread exception", e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("Program", "Unhandled domain exception", e.ExceptionObject as Exception);
        }
    }
}
