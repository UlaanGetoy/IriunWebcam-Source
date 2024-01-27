using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Win32;

namespace webcam
{
	// Token: 0x02000005 RID: 5
	public partial class IdleTab : UserControl
	{
		// Token: 0x0600004F RID: 79
		[DllImport("engine.dll")]
		private static extern void SetIdleCallback(int idx, [MarshalAs(UnmanagedType.FunctionPtr)] IdleTab.PasscodeCb passcodeCb);

		// Token: 0x06000050 RID: 80
		[DllImport("engine.dll")]
		public static extern bool ESetBlank(bool blank);

		// Token: 0x06000051 RID: 81
		[DllImport("engine.dll")]
		public static extern ulong ESetPasscode(byte[] uid, string passcode);

		// Token: 0x06000052 RID: 82 RVA: 0x000042BC File Offset: 0x000024BC
		public IdleTab()
		{
			this.InitializeComponent();
			Process currentProcess = Process.GetCurrentProcess();
			this.moduleName = currentProcess.MainModule.FileName;
			this._passcodeTimer = new DispatcherTimer();
			this._passcodeTimer.Tick += this.PasscodeTimer_Tick;
			this._passcodeTimer.Interval = new TimeSpan(0, 0, 0, 2, 0);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00004348 File Offset: 0x00002548
		public void SetIdx(int idx)
		{
			this.idx = idx;
			IdleTab._idleTabs[idx] = this;
			IdleTab.SetIdleCallback(idx, this.passcodeCb);
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00004368 File Offset: 0x00002568
		public void HandleClientStatus(int status)
		{
			if (status == 4)
			{
				this._passcodeTimer.Start();
				this.disconnectedText.Visibility = Visibility.Visible;
				return;
			}
			this._passcodeTimer.Stop();
			this.passcodePanel.Visibility = Visibility.Collapsed;
			this.disconnectedText.Visibility = ((status < 3) ? Visibility.Visible : Visibility.Collapsed);
			this.initFailedText.Visibility = ((status < 0) ? Visibility.Visible : Visibility.Collapsed);
			if (status < 0)
			{
				this.errorCodeText.Text = "Error code " + status.ToString();
			}
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000043F0 File Offset: 0x000025F0
		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue.Equals(true))
			{
				using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\\\Iriun\\\\webcam"))
				{
					if (registryKey != null)
					{
						object value = registryKey.GetValue("blankVideo");
						if (value != null)
						{
							bool flag = bool.Parse(value.ToString());
							this.blankCheckBox.IsChecked = new bool?(flag);
						}
					}
				}
				using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Run"))
				{
					if (registryKey2 != null)
					{
						object value2 = registryKey2.GetValue("Iriun webcam");
						this.autoStartCheckBox.IsChecked = new bool?(value2 != null);
					}
				}
			}
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000044BC File Offset: 0x000026BC
		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			this.SetBlank(true);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000044C5 File Offset: 0x000026C5
		private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			this.SetBlank(false);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000044D0 File Offset: 0x000026D0
		private void SetBlank(bool blank)
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\\\Iriun\\\\webcam", true))
			{
				if (registryKey != null)
				{
					registryKey.SetValue("blankVideo", blank);
				}
				IdleTab.ESetBlank(blank);
			}
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004528 File Offset: 0x00002728
		private void autoStartCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			this.SetAutoStart(true);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004531 File Offset: 0x00002731
		private void autoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			this.SetAutoStart(false);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x0000453C File Offset: 0x0000273C
		private void SetAutoStart(bool start)
		{
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Run", true))
			{
				if (registryKey != null)
				{
					if (start)
					{
						registryKey.SetValue("Iriun webcam", this.moduleName + " --tray");
					}
					else
					{
						registryKey.DeleteValue("Iriun webcam", false);
					}
				}
			}
		}

		// Token: 0x0600005C RID: 92 RVA: 0x000045A8 File Offset: 0x000027A8
		private void PasscodeTimer_Tick(object sender, EventArgs e)
		{
			this._passcodeTimer.Stop();
			this.disconnectedText.Visibility = Visibility.Collapsed;
			this.initFailedText.Visibility = Visibility.Collapsed;
			this.passcodePanel.Visibility = Visibility.Visible;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000045DC File Offset: 0x000027DC
		private void Passcode_Click(object sender, RoutedEventArgs e)
		{
			this.passcodePanel.Visibility = Visibility.Collapsed;
			this.disconnectedText.Visibility = Visibility.Visible;
			string password = this.passcodeBox.Password;
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\\\Iriun\\\\webcam"))
			{
				if (registryKey != null)
				{
					ulong num = IdleTab.ESetPasscode(this.passcodeUid, password);
					for (int i = 0; i < 50; i++)
					{
						byte[] array = (byte[])registryKey.GetValue("pu" + i.ToString());
						if (array == null)
						{
							break;
						}
						if (array.SequenceEqual(this.passcodeUid))
						{
							registryKey.SetValue("ph" + i.ToString(), num);
							return;
						}
					}
					int num2 = (int)registryKey.GetValue("pIdx", 0);
					registryKey.SetValue("pu" + num2.ToString(), this.passcodeUid, RegistryValueKind.Binary);
					registryKey.SetValue("ph" + num2.ToString(), num);
					num2 = ((num2 < 49) ? (num2 + 1) : 0);
					registryKey.SetValue("pIdx", num2);
				}
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00004720 File Offset: 0x00002920
		private void passcodeBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			this.passcodeButton.IsEnabled = this.passcodeBox.Password.Length > 0;
		}

		// Token: 0x04000045 RID: 69
		private readonly IdleTab.PasscodeCb passcodeCb = delegate(int idx, byte[] uid, string name)
		{
			if (idx < 0 || idx > 3 || IdleTab._idleTabs[idx] == null || Application.Current == null)
			{
				return 0UL;
			}
			IdleTab._idleTabs[idx].passcodeUid = uid.ToArray<byte>();
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				IdleTab._idleTabs[idx].passcodeText.Text = name + " requires passcode.";
			}), Array.Empty<object>());
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\\\Iriun\\\\webcam"))
			{
				if (registryKey != null)
				{
					for (int i = 0; i < 50; i++)
					{
						byte[] array = (byte[])registryKey.GetValue("pu" + i.ToString());
						if (array == null)
						{
							break;
						}
						if (array.SequenceEqual(uid))
						{
							string text = (string)registryKey.GetValue("ph" + i.ToString(), "0");
							if (text != null)
							{
								return ulong.Parse(text);
							}
						}
					}
				}
			}
			return 0UL;
		};

		// Token: 0x04000046 RID: 70
		public int idx;

		// Token: 0x04000047 RID: 71
		private string moduleName;

		// Token: 0x04000048 RID: 72
		private byte[] passcodeUid;

		// Token: 0x04000049 RID: 73
		private DispatcherTimer _passcodeTimer;

		// Token: 0x0400004A RID: 74
		private static readonly IdleTab[] _idleTabs = new IdleTab[4];

		// Token: 0x0200001A RID: 26
		// (Invoke) Token: 0x060000B7 RID: 183
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate ulong PasscodeCb(int idx, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] byte[] uid, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
	}
}
