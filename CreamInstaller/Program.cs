using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CreamInstaller
{
    public static class Program
    {
        public static readonly string ApplicationName = Application.CompanyName + " v" + Application.ProductVersion + ": " + Application.ProductName;
        public static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();
        public static readonly Process CurrentProcess = Process.GetCurrentProcess();
        public static readonly string CurrentProcessFilePath = CurrentProcess.MainModule.FileName;
        public static readonly string CurrentProcessDirectory = CurrentProcessFilePath.Substring(0, CurrentProcessFilePath.LastIndexOf("\\"));
        public static readonly string BackupFileExtension = ".creaminstaller.backup";

        [STAThread]
        private static void Main()
        {
            Mutex mutex = new(true, "CreamInstaller", out bool createdNew);
            if (createdNew)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ApplicationExit += new(OnApplicationExit);
            retry:
                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception e)
                {
                    if (ExceptionHandler.OutputException(e)) goto retry;
                    Application.Exit();
                    return;
                }
            }
            mutex.Close();
        }

        public static bool IsProgramRunningDialog(Form form, ProgramSelection selection)
        {
            if (selection.AreSteamApiDllsLocked)
            {
                if (new DialogForm(form).Show(ApplicationName, SystemIcons.Error,
                $"ERROR: {selection.Name} is currently running!" +
                "\n\nPlease close the program/game to continue . . . ",
                "Retry", "Cancel") == DialogResult.OK)
                {
                    return IsProgramRunningDialog(form, selection);
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        public static bool IsFilePathLocked(this string filePath)
        {
            try { File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close(); }
            catch (FileNotFoundException) { return false; }
            catch (IOException) { return true; }
            return false;
        }

        public static SelectForm SelectForm;
        public static InstallForm InstallForm;

        public static List<ProgramSelection> ProgramSelections = new();

        public static bool Canceled = false;

        public static void Cleanup(bool cancel = true)
        {
            Canceled = cancel;
            SteamCMD.Kill();
        }

        private static void OnApplicationExit(object s, EventArgs e) => Cleanup();

        internal static void InheritLocation(this Form form, Form fromForm)
        {
            int X = fromForm.Location.X + fromForm.Size.Width / 2 - form.Size.Width / 2;
            int Y = fromForm.Location.Y + fromForm.Size.Height / 2 - form.Size.Height / 2;
            form.Location = new(X, Y);
        }
    }
}