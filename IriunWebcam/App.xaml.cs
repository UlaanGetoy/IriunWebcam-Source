using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace webcam
{
	// Token: 0x02000002 RID: 2
	public partial class App : Application
	{
		// Token: 0x06000001 RID: 1
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		// Token: 0x06000002 RID: 2
		[DllImport("user32.dll")]
		private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		// Token: 0x06000003 RID: 3
		[DllImport("user32.dll")]
		private static extern bool IsIconic(IntPtr hWnd);

		// Token: 0x06000004 RID: 4
		[DllImport("user32.dll")]
		public static extern bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);

		// Token: 0x06000005 RID: 5
		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, UIntPtr wParam, IntPtr lParam);

		// Token: 0x06000006 RID: 6 RVA: 0x00002050 File Offset: 0x00000250
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			bool flag = false;
			foreach (string text in Environment.GetCommandLineArgs())
			{
				if (text == "--quit")
				{
					flag = true;
				}
				if (text == "--tray")
				{
					webcam.MainWindow.hideAtStart = true;
				}
			}
			if (flag)
			{
				this.KillOtherProc();
				base.Shutdown();
				return;
			}
			try
			{
				this._semaphore = Semaphore.OpenExisting("7EAFBC94EFEF41FAAFF10C67DDFE93A4");
			}
			catch (Exception)
			{
			}
			if (this._semaphore == null)
			{
				this._semaphore = new Semaphore(0, 5, "7EAFBC94EFEF41FAAFF10C67DDFE93A4");
				this._threadRunning = true;
				this._thread = new Thread(new ThreadStart(this.ThreadMain));
				this._thread.Start();
				return;
			}
			this._semaphore.Release();
			this._semaphore.Close();
			Environment.Exit(0);
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002130 File Offset: 0x00000330
		private void Application_Exit(object sender, ExitEventArgs e)
		{
			this._threadRunning = false;
			if (this._thread != null)
			{
				this._thread.Join(1000);
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002154 File Offset: 0x00000354
		private void KillOtherProc()
		{
			Process currentProcess = Process.GetCurrentProcess();
			foreach (Process process in Process.GetProcessesByName(Assembly.GetExecutingAssembly().GetName().Name))
			{
				if (currentProcess.Id != process.Id)
				{
					process.Kill();
					process.WaitForExit(5000);
				}
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000021B0 File Offset: 0x000003B0
		private void ThreadMain()
		{
			while (this._threadRunning)
			{
				if (this._semaphore.WaitOne(500))
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(delegate
					{
						IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
						if (mainWindowHandle == IntPtr.Zero)
						{
							base.MainWindow.Show();
							base.MainWindow.WindowState = WindowState.Normal;
							return;
						}
						if (App.IsIconic(mainWindowHandle))
						{
							App.ShowWindowAsync(mainWindowHandle, 9);
						}
						App.SetForegroundWindow(mainWindowHandle);
					}), Array.Empty<object>());
				}
			}
			this._semaphore.Close();
		}

		// Token: 0x04000001 RID: 1
		private const int SW_RESTORE = 9;

		// Token: 0x04000002 RID: 2
		private Semaphore _semaphore;

		// Token: 0x04000003 RID: 3
		private Thread _thread;

		// Token: 0x04000004 RID: 4
		private bool _threadRunning;
	}
}
