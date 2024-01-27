using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;
using webcam.Properties;

namespace webcam
{
	// Token: 0x02000006 RID: 6
	public partial class MainWindow : Window
	{
		// Token: 0x06000062 RID: 98
		[DllImport("engine.dll")]
		private static extern void EInitialize(int count, [MarshalAs(UnmanagedType.FunctionPtr)] MainWindow.MainStatusCb mainStatusCb);

		// Token: 0x06000063 RID: 99
		[DllImport("engine.dll")]
		private static extern void ESetActiveIdx(int idx);

		// Token: 0x06000064 RID: 100
		[DllImport("engine.dll")]
		private static extern void ERefresh();

		// Token: 0x06000065 RID: 101 RVA: 0x000048E0 File Offset: 0x00002AE0
		public MainWindow()
		{
			MainWindow._self = this;
			using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Software\\\\Iriun\\\\webcam"))
			{
				if (registryKey != null)
				{
					object value = registryKey.GetValue("cameraCount");
					if (value != null)
					{
						this.cameraCount = int.Parse(value.ToString());
					}
					if (this.cameraCount < 1 || this.cameraCount > 4)
					{
						this.cameraCount = 1;
					}
				}
			}
			MainWindow.EInitialize(this.cameraCount, this.mainStatusCb);
			this.InitializeComponent();
			if (MainWindow.hideAtStart)
			{
				base.WindowState = WindowState.Minimized;
			}
			this.idleTab0.SetIdx(0);
			this.idleTab1.SetIdx(1);
			this.idleTab2.SetIdx(2);
			this.idleTab3.SetIdx(3);
			this.tab0.Visibility = ((this.cameraCount > 1) ? Visibility.Visible : Visibility.Collapsed);
			this.tab1.Visibility = ((this.cameraCount > 1) ? Visibility.Visible : Visibility.Collapsed);
			this.tab2.Visibility = ((this.cameraCount > 2) ? Visibility.Visible : Visibility.Collapsed);
			this.tab3.Visibility = ((this.cameraCount > 3) ? Visibility.Visible : Visibility.Collapsed);
			if (this.cameraCount > 1)
			{
				base.MinHeight = 460.0;
			}
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004A5C File Offset: 0x00002C5C
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.notifyIcon = new NotifyIcon();
			this.notifyIcon.Icon = webcam.Properties.Resources.icon;
			ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Items.Add(new ToolStripMenuItem("Show", null, new EventHandler(this.TrayShowEvent), "Show"));
			contextMenuStrip.Items.Add(new ToolStripMenuItem("Quit", null, new EventHandler(this.TrayQuitEvent), "Quit"));
			this.notifyIcon.ContextMenuStrip = contextMenuStrip;
			this.notifyIcon.Text = "Iriun Webcam";
			this.notifyIcon.MouseClick += this.NotifyIcon_MouseClick;
			this.notifyIcon.Visible = true;
			if (MainWindow.hideAtStart)
			{
				base.Hide();
			}
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004B26 File Offset: 0x00002D26
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			this.CleanUp();
			e.Cancel = false;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00004B3C File Offset: 0x00002D3C
		private void CleanUp()
		{
			if (this.notifyIcon != null)
			{
				this.notifyIcon.Visible = false;
			}
			this.cameraTab3.CleanUp();
			this.cameraTab2.CleanUp();
			this.cameraTab1.CleanUp();
			this.cameraTab0.CleanUp();
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004B89 File Offset: 0x00002D89
		private void TrayQuitEvent(object sender, EventArgs e)
		{
			base.Close();
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004B91 File Offset: 0x00002D91
		private void TrayShowEvent(object sender, EventArgs e)
		{
			MainWindow.ERefresh();
			base.Show();
			base.WindowState = WindowState.Normal;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004BA5 File Offset: 0x00002DA5
		private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				MainWindow.ERefresh();
				base.Show();
				base.WindowState = WindowState.Normal;
				return;
			}
			if (e.Button == MouseButtons.Right)
			{
				MainWindow.ERefresh();
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00004BDC File Offset: 0x00002DDC
		public void HandleClientStatusCb(int idx, int status)
		{
			IdleTab[] array = new IdleTab[] { this.idleTab0, this.idleTab1, this.idleTab2, this.idleTab3 };
			CameraTab[] array2 = new CameraTab[] { this.cameraTab0, this.cameraTab1, this.cameraTab2, this.cameraTab3 };
			TabItem[] array3 = new TabItem[] { this.tab0, this.tab1, this.tab2, this.tab3 };
			array[idx].HandleClientStatus(status);
			array[idx].Visibility = ((status != 3) ? Visibility.Visible : Visibility.Hidden);
			array2[idx].HandleClientStatus(status);
			array2[idx].Visibility = ((status == 3) ? Visibility.Visible : Visibility.Hidden);
			TextBlock textBlock = new TextBlock();
			textBlock.Text = "Camera #" + (idx + 1).ToString();
			textBlock.Foreground = ((status == 3) ? Brushes.Green : Brushes.Black);
			array3[idx].Header = textBlock;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00004CDC File Offset: 0x00002EDC
		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			int selectedIndex = this.tabControl.SelectedIndex;
			if (selectedIndex >= 0)
			{
				MainWindow.ESetActiveIdx((base.WindowState == WindowState.Minimized) ? (-1) : selectedIndex);
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004D0B File Offset: 0x00002F0B
		private void Window_GotFocus(object sender, RoutedEventArgs e)
		{
			MainWindow.ERefresh();
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004D14 File Offset: 0x00002F14
		private void Window_StateChanged(object sender, EventArgs e)
		{
			MainWindow.ERefresh();
			if (base.WindowState == WindowState.Minimized)
			{
				base.Hide();
				this.notifyIcon.BalloonTipText = "Iriun Webcam running on background";
				this.notifyIcon.BalloonTipTitle = "";
				this.notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
				this.notifyIcon.ShowBalloonTip(2000);
				MainWindow.ESetActiveIdx(-1);
				return;
			}
			int selectedIndex = this.tabControl.SelectedIndex;
			if (selectedIndex >= 0)
			{
				MainWindow.ESetActiveIdx(selectedIndex);
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00004D8E File Offset: 0x00002F8E
		private void Window_Activated(object sender, EventArgs e)
		{
			MainWindow.ERefresh();
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00004D95 File Offset: 0x00002F95
		private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			MainWindow.ERefresh();
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00004D9C File Offset: 0x00002F9C
		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (this.cameraTab0.IsVisible)
			{
				this.cameraTab0.KeyPressed(e);
				return;
			}
			if (this.cameraTab1.IsVisible)
			{
				this.cameraTab1.KeyPressed(e);
				return;
			}
			if (this.cameraTab2.IsVisible)
			{
				this.cameraTab2.KeyPressed(e);
				return;
			}
			if (this.cameraTab3.IsVisible)
			{
				this.cameraTab3.KeyPressed(e);
			}
		}

		// Token: 0x04000055 RID: 85
		private MainWindow.MainStatusCb mainStatusCb = delegate(int idx, int status)
		{
			if (idx < 0 || idx > 3 || System.Windows.Application.Current == null)
			{
				return;
			}
			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				MainWindow._self.HandleClientStatusCb(idx, status);
			}), Array.Empty<object>());
		};

		// Token: 0x04000056 RID: 86
		private static MainWindow _self;

		// Token: 0x04000057 RID: 87
		private int cameraCount = 1;

		// Token: 0x04000058 RID: 88
		private NotifyIcon notifyIcon;

		// Token: 0x04000059 RID: 89
		public static bool hideAtStart;

		// Token: 0x0200001D RID: 29
		// (Invoke) Token: 0x060000C0 RID: 192
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void MainStatusCb(int idx, int status);
	}
}
