using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace webcam
{
	// Token: 0x02000003 RID: 3
	public partial class CameraTab : UserControl
	{
		// Token: 0x0600000E RID: 14
		[DllImport("engine.dll")]
		private static extern int EOpen();

		// Token: 0x0600000F RID: 15
		[DllImport("engine.dll")]
		private static extern int EFocus(int idx, float x, float y, bool af);

		// Token: 0x06000010 RID: 16
		[DllImport("engine.dll")]
		private static extern int GetBackBufferNoRef(out IntPtr pSurface);

		// Token: 0x06000011 RID: 17
		[DllImport("engine.dll")]
		private static extern int SetSize(uint width, uint height);

		// Token: 0x06000012 RID: 18
		[DllImport("engine.dll")]
		private static extern int SetAlpha(bool useAlpha);

		// Token: 0x06000013 RID: 19
		[DllImport("engine.dll")]
		private static extern int ESetVideoFormat(int idx, int formatIdx);

		// Token: 0x06000014 RID: 20
		[DllImport("engine.dll")]
		private static extern int ESelectPhone(int idx, string tag);

		// Token: 0x06000015 RID: 21
		[DllImport("engine.dll")]
		private static extern int SetNumDesiredSamples(uint numSamples);

		// Token: 0x06000016 RID: 22
		[DllImport("engine.dll")]
		private static extern int SetAdapter(CameraTab.POINT screenSpacePoint);

		// Token: 0x06000017 RID: 23
		[DllImport("engine.dll")]
		private static extern int Render();

		// Token: 0x06000018 RID: 24
		[DllImport("engine.dll")]
		private static extern void Destroy();

		// Token: 0x06000019 RID: 25
		[DllImport("engine.dll")]
		private static extern void ButtonEvent(int idx, int button, int value);

		// Token: 0x0600001A RID: 26
		[DllImport("engine.dll")]
		private static extern void EUninitialize(int idx);

		// Token: 0x0600001B RID: 27
		[DllImport("engine.dll")]
		private static extern void SetCallback(int idx, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.ButtonCb buttonCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.StatusCb statusCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.PeerListCb peerListCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.IsoCb isoCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.ExpTimeCb expTimeCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.ExpBiasCb expBiasCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.WBCb WBCb, [MarshalAs(UnmanagedType.FunctionPtr)] CameraTab.CamListCb camListCb);

		// Token: 0x0600001C RID: 28 RVA: 0x000022E0 File Offset: 0x000004E0
		public CameraTab()
		{
			this.InitializeComponent();
			HRESULT.Check(CameraTab.SetSize(256U, 256U));
			HRESULT.Check(CameraTab.SetAlpha(false));
			HRESULT.Check(CameraTab.SetNumDesiredSamples(4U));
			this._adapterTimer = new DispatcherTimer();
			this._adapterTimer.Tick += this.AdapterTimer_Tick;
			this._adapterTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
			this._adapterTimer.Start();
			this._sizeTimer = new DispatcherTimer(DispatcherPriority.Render);
			this._sizeTimer.Tick += this.SizeTimer_Tick;
			this._sizeTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
			this._sizeTimer.Start();
			this._focusTimer = new DispatcherTimer();
			this._focusTimer.Tick += this.FocusTimer_Tick;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002504 File Offset: 0x00000704
		~CameraTab()
		{
			CameraTab.Destroy();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002530 File Offset: 0x00000730
		public void CleanUp()
		{
			CameraTab.EUninitialize(this._idx);
			CameraTab.Destroy();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002542 File Offset: 0x00000742
		public void HandleClientStatus(int status)
		{
			if (status == 0)
			{
				this.ResetUI();
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002550 File Offset: 0x00000750
		private void ResetUI()
		{
			this.mEnableFocus = false;
			this.closeAppPanel.Visibility = Visibility.Collapsed;
			this.menuPanel.Visibility = Visibility.Hidden;
			this.cameraMenu.Visibility = Visibility.Collapsed;
			this.cameraButton.Visibility = Visibility.Collapsed;
			this.micButton.Visibility = Visibility.Collapsed;
			this.flipButton.Visibility = Visibility.Collapsed;
			this.flashButton.Visibility = Visibility.Collapsed;
			this.isoButton.Visibility = Visibility.Collapsed;
			this.expTimeButton.Visibility = Visibility.Collapsed;
			this.expBiasButton.Visibility = Visibility.Collapsed;
			this.wbButton.Visibility = Visibility.Collapsed;
			this.zoomButton.Visibility = Visibility.Collapsed;
			this.batteryIndicator.Visibility = Visibility.Collapsed;
			this.HideImageControls();
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002608 File Offset: 0x00000808
		private void UserControl_Initialized(object sender, EventArgs e)
		{
			this.ResetUI();
			this._idx = CameraTab.EOpen();
			if (this._idx < 0)
			{
				return;
			}
			CameraTab._cameraTabs[this._idx] = this;
			CameraTab.SetCallback(this._idx, this.buttonCb, this.statusCb, this.peerListCb, this.isoCb, this.expTimeCb, this.expBiasCb, this.wbCb, this.camListCb);
			int num = 0;
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\\\Iriun\\\\webcam"))
			{
				if (registryKey != null)
				{
					object value = registryKey.GetValue("videoFormat" + this._idx.ToString());
					if (value != null)
					{
						num = int.Parse(value.ToString());
						if (num < 0 || num >= this.formatComboBox.Items.Count)
						{
							num = 0;
						}
					}
				}
			}
			this.formatComboBox.SelectedIndex = num;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000026FC File Offset: 0x000008FC
		public void KeyPressed(KeyEventArgs e)
		{
			if (e.Key == Key.Add)
			{
				this.zoomSlider.Value += 1.0;
			}
			if (e.Key == Key.Subtract)
			{
				this.zoomSlider.Value -= 1.0;
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002754 File Offset: 0x00000954
		private void UpdatePreviewSize(double width, double height)
		{
			if (width > 0.0 && height > 0.0 && base.ActualWidth > 0.0 && base.ActualHeight > 0.0)
			{
				double num = width / height;
				double num2 = base.ActualWidth / num;
				if (num2 <= base.ActualHeight)
				{
					this.imgelt.Width = base.ActualWidth;
					this.imgelt.Height = num2;
					return;
				}
				this.imgelt.Width = base.ActualHeight * num;
				this.imgelt.Height = base.ActualHeight;
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x000027F6 File Offset: 0x000009F6
		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			this.UpdatePreviewSize(this.imgelt.ActualWidth, this.imgelt.ActualHeight);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002814 File Offset: 0x00000A14
		private void AdapterTimer_Tick(object sender, EventArgs e)
		{
			if (!this.imgelt.IsVisible)
			{
				return;
			}
			HRESULT.Check(CameraTab.SetAdapter(new CameraTab.POINT(this.imgelt.PointToScreen(new Point(0.0, 0.0)))));
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002860 File Offset: 0x00000A60
		private void SizeTimer_Tick(object sender, EventArgs e)
		{
			uint num = (uint)this.imgelt.ActualWidth;
			uint num2 = (uint)this.imgelt.ActualHeight;
			if (num > 0U && num2 > 0U && (num != (uint)this.d3dimg.Width || num2 != (uint)this.d3dimg.Height))
			{
				HRESULT.Check(CameraTab.SetSize(num, num2));
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000028B9 File Offset: 0x00000AB9
		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				CompositionTarget.Rendering += this.CompositionTarget_Rendering;
				return;
			}
			CompositionTarget.Rendering -= this.CompositionTarget_Rendering;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000028EC File Offset: 0x00000AEC
		private void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			RenderingEventArgs renderingEventArgs = (RenderingEventArgs)e;
			if (this.d3dimg.IsFrontBufferAvailable && this._lastRender != renderingEventArgs.RenderingTime)
			{
				IntPtr zero = IntPtr.Zero;
				CameraTab.GetBackBufferNoRef(out zero);
				if (zero != IntPtr.Zero)
				{
					this.imgelt.Visibility = Visibility.Visible;
					this.noPreview.Visibility = Visibility.Hidden;
					this.d3dimg.Lock();
					this.d3dimg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, zero);
					HRESULT.Check(CameraTab.Render());
					this.d3dimg.AddDirtyRect(new Int32Rect(0, 0, this.d3dimg.PixelWidth, this.d3dimg.PixelHeight));
					this.d3dimg.Unlock();
					this._lastRender = renderingEventArgs.RenderingTime;
					return;
				}
				this.imgelt.Visibility = Visibility.Hidden;
				this.noPreview.Visibility = Visibility.Visible;
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000029D4 File Offset: 0x00000BD4
		public void HandleButtonCallback(int button, int state)
		{
			if (button == 2)
			{
				if (state == 0)
				{
					this.SetButtonImage(this.flashButton, "/Images/btn_flash.png");
					this.flashButton.Visibility = Visibility.Visible;
				}
				else if (state == 1)
				{
					this.SetButtonImage(this.flashButton, "/Images/btn_flash_on.png");
					this.flashButton.Visibility = Visibility.Visible;
				}
				else if (state == 2)
				{
					this.flashButton.Visibility = Visibility.Collapsed;
				}
				else if (state == 3)
				{
					this.SetButtonImage(this.flashButton, "/Images/btn_flash.png");
				}
				else if (state == 4)
				{
					this.SetButtonImage(this.flashButton, "/Images/btn_flash_on.png");
				}
			}
			if (button == 3)
			{
				if (state == 0)
				{
					this.flipButton.Visibility = Visibility.Visible;
					this.SetButtonImage(this.flipButton, "/Images/btn_flip.png");
				}
				else if (state == 1)
				{
					this.flipButton.Visibility = Visibility.Visible;
					this.SetButtonImage(this.flipButton, "/Images/btn_flip_on.png");
				}
				else if (state == 3)
				{
					this.flipButton.Visibility = Visibility.Collapsed;
					this.SetButtonImage(this.flipButton, null);
				}
			}
			if (button == 4)
			{
				this.blockSliderEvent = true;
				this.zoomSlider.Value = (double)state;
				this.blockSliderEvent = false;
				this.zoomButton.Visibility = Visibility.Visible;
			}
			if (button == 5)
			{
				if (state == 0 || this.cameraMenu.Visibility == Visibility.Visible)
				{
					this.cameraButton.Visibility = Visibility.Collapsed;
				}
				else if (state == 1)
				{
					this.ResetUI();
					this.cameraButton.Visibility = Visibility.Visible;
				}
			}
			if (button == 10)
			{
				if (state == 0)
				{
					this.SetButtonImage(this.micButton, "/Images/btn_mic_off.png");
				}
				else if (state == 1)
				{
					this.SetButtonImage(this.micButton, "/Images/btn_mic_on.png");
				}
				this.micButton.Visibility = Visibility.Visible;
			}
			if (button == 11)
			{
				this.closeAppPanel.Visibility = Visibility.Visible;
			}
			if (button == 12)
			{
				this.mEnableFocus = true;
			}
			if (button == 13)
			{
				if (state >= 0 && state <= 100)
				{
					this.batteryIndicator.Visibility = Visibility.Visible;
					this.batteryText.Content = state.ToString() + "%";
				}
				else
				{
					this.batteryIndicator.Visibility = Visibility.Collapsed;
				}
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002BD8 File Offset: 0x00000DD8
		private void SetButtonImage(Button button, string image)
		{
			if (image != null)
			{
				button.Content = new Image
				{
					Source = new BitmapImage(new Uri(image, UriKind.Relative)),
					VerticalAlignment = VerticalAlignment.Center,
					Stretch = Stretch.Fill,
					Height = 32.0,
					Width = 32.0
				};
				return;
			}
			button.Visibility = Visibility.Collapsed;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002C39 File Offset: 0x00000E39
		private void cameraButton_Click(object sender, RoutedEventArgs e)
		{
			this.ResetUI();
			CameraTab.ButtonEvent(this._idx, 5, 0);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002C4E File Offset: 0x00000E4E
		private void micButton_Click(object sender, RoutedEventArgs e)
		{
			CameraTab.ButtonEvent(this._idx, 10, 0);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002C5E File Offset: 0x00000E5E
		private void flipButton_Click(object sender, RoutedEventArgs e)
		{
			CameraTab.ButtonEvent(this._idx, 3, 0);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002C6D File Offset: 0x00000E6D
		private void flashButton_Click(object sender, RoutedEventArgs e)
		{
			CameraTab.ButtonEvent(this._idx, 2, 0);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002C7C File Offset: 0x00000E7C
		private void HideImageControls()
		{
			this.sliderPanel.Visibility = Visibility.Collapsed;
			this.isoSlider.Visibility = Visibility.Hidden;
			this.SetButtonImage(this.isoButton, "/Images/btn_iso.png");
			this.expTimeSlider.Visibility = Visibility.Hidden;
			this.SetButtonImage(this.expTimeButton, "/Images/btn_exp_time.png");
			this.expBiasPanel.Visibility = Visibility.Collapsed;
			this.SetButtonImage(this.expBiasButton, "/Images/btn_exp_bias.png");
			this.wbSlider.Visibility = Visibility.Hidden;
			this.SetButtonImage(this.wbButton, "/Images/btn_wb.png");
			this.zoomPanel.Visibility = Visibility.Collapsed;
			this.SetButtonImage(this.zoomButton, "/Images/btn_zoom.png");
			this.sliderText.Text = "";
			this.sliderText.Visibility = Visibility.Hidden;
			this.autoButton.Content = "";
			this.autoButton.Visibility = Visibility.Hidden;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002D60 File Offset: 0x00000F60
		private void isoButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.isoSlider.IsVisible)
			{
				this.HideImageControls();
				return;
			}
			this.HideImageControls();
			this.SetButtonImage(this.isoButton, "/Images/btn_iso_on.png");
			this.isoSlider.Visibility = Visibility.Visible;
			this.sliderText.Visibility = Visibility.Visible;
			this.autoButton.Visibility = Visibility.Visible;
			this.sliderPanel.Visibility = Visibility.Visible;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002DC8 File Offset: 0x00000FC8
		private void HandleIsoCb(int mini, int maxi, int value, int manual)
		{
			this.mIsoAuto = manual == 0;
			if (this.isoSlider.IsVisible)
			{
				this.blockSliderEvent = true;
				this.isoSlider.Minimum = (double)mini;
				this.isoSlider.Maximum = (double)maxi;
				this.isoSlider.TickFrequency = (double)(maxi - mini);
				this.isoSlider.Value = (double)value;
				this.blockSliderEvent = false;
				this.sliderText.Text = value.ToString();
				this.autoButton.Content = ((manual != 0) ? "MANUAL" : "AUTO");
			}
			this.expBiasButton.IsEnabled = this.mIsoAuto && this.mExpTimeAuto;
			this.isoButton.Visibility = Visibility.Visible;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002E88 File Offset: 0x00001088
		private void isoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.blockSliderEvent)
			{
				return;
			}
			int num = (int)e.NewValue;
			this.sliderText.Text = num.ToString();
			this.autoButton.Content = "MANUAL";
			this.mIsoAuto = false;
			this.expBiasButton.IsEnabled = false;
			CameraTab.ButtonEvent(this._idx, 6, num);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002EE8 File Offset: 0x000010E8
		private void expTimeButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.expTimeSlider.IsVisible)
			{
				this.HideImageControls();
				return;
			}
			this.HideImageControls();
			this.SetButtonImage(this.expTimeButton, "/Images/btn_exp_time_on.png");
			this.expTimeSlider.Visibility = Visibility.Visible;
			this.sliderText.Visibility = Visibility.Visible;
			this.autoButton.Visibility = Visibility.Visible;
			this.sliderPanel.Visibility = Visibility.Visible;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002F50 File Offset: 0x00001150
		private void HandleExpTimeCb(float mini, float maxi, float value, int manual)
		{
			this.mExpTimeMin = mini;
			this.mExpTimeMax = maxi;
			this.mExpTimeAuto = manual == 0;
			if (this.expTimeSlider.IsVisible)
			{
				this.blockSliderEvent = true;
				float num = (value - mini) / (maxi - mini);
				this.expTimeSlider.Value = Math.Pow((double)num, 0.20000000298023224);
				this.blockSliderEvent = false;
				if (value > 0f && value < 1f)
				{
					this.sliderText.Text = "1/" + ((int)(1f / value)).ToString();
				}
				else
				{
					this.sliderText.Text = value.ToString("f2");
				}
				this.autoButton.Content = ((manual != 0) ? "MANUAL" : "AUTO");
			}
			this.expBiasButton.IsEnabled = this.mIsoAuto && this.mExpTimeAuto;
			this.expTimeButton.Visibility = Visibility.Visible;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003048 File Offset: 0x00001248
		private void expTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.blockSliderEvent)
			{
				return;
			}
			float num = (float)Math.Pow(e.NewValue, 5.0) * (this.mExpTimeMax - this.mExpTimeMin) + this.mExpTimeMin;
			if (num > 0f && num < 1f)
			{
				this.sliderText.Text = "1/" + ((int)(1f / num)).ToString();
			}
			else
			{
				this.sliderText.Text = num.ToString("f2");
			}
			this.autoButton.Content = "MANUAL";
			this.mExpTimeAuto = false;
			this.expBiasButton.IsEnabled = false;
			CameraTab.ButtonEvent(this._idx, 7, (int)(num * 10000f));
		}

		// Token: 0x06000036 RID: 54 RVA: 0x0000310E File Offset: 0x0000130E
		private void expBiasButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.expBiasPanel.IsVisible)
			{
				this.HideImageControls();
				return;
			}
			this.HideImageControls();
			this.SetButtonImage(this.expBiasButton, "/Images/btn_exp_bias_on.png");
			this.expBiasPanel.Visibility = Visibility.Visible;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003148 File Offset: 0x00001348
		private void HandleExpBiasCb(float mini, float maxi, float step, float value)
		{
			if (this.expBiasPanel.IsVisible)
			{
				this.blockSliderEvent = true;
				this.expBiasSlider.Minimum = (double)mini;
				this.expBiasSlider.Maximum = (double)maxi;
				this.expBiasSlider.Value = (double)value;
				this.expBiasSlider.SmallChange = (double)step;
				this.expBiasSlider.LargeChange = (double)step;
				this.blockSliderEvent = false;
				this.sliderText.Text = ((value > 0f) ? "+" : "") + value.ToString("f1");
			}
			this.expBiasButton.Visibility = Visibility.Visible;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000031F4 File Offset: 0x000013F4
		private void expBiasSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.blockSliderEvent)
			{
				return;
			}
			float num = (float)e.NewValue;
			this.sliderText.Text = ((num > 0f) ? "+" : "") + num.ToString("f1");
			CameraTab.ButtonEvent(this._idx, 8, (int)(num * 10000f));
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003258 File Offset: 0x00001458
		private void wbButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.wbSlider.IsVisible)
			{
				this.HideImageControls();
				return;
			}
			this.HideImageControls();
			this.SetButtonImage(this.wbButton, "/Images/btn_wb_on.png");
			this.wbSlider.Visibility = Visibility.Visible;
			this.sliderText.Visibility = Visibility.Visible;
			this.autoButton.Visibility = Visibility.Visible;
			this.sliderPanel.Visibility = Visibility.Visible;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000032C0 File Offset: 0x000014C0
		private void HandleWBCb(int mini, int maxi, int value, int manual)
		{
			this.mWBAuto = manual == 0;
			if (this.wbSlider.IsVisible)
			{
				this.blockSliderEvent = true;
				this.wbSlider.Minimum = (double)mini;
				this.wbSlider.Maximum = (double)maxi;
				this.wbSlider.TickFrequency = (double)(maxi - mini);
				this.wbSlider.Value = (double)value;
				this.blockSliderEvent = false;
				this.sliderText.Text = ((value > 0) ? value.ToString() : "");
				this.autoButton.Content = ((manual != 0) ? "MANUAL" : "AUTO");
			}
			this.wbButton.Visibility = Visibility.Visible;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003370 File Offset: 0x00001570
		private void wbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.blockSliderEvent)
			{
				return;
			}
			int num = (int)e.NewValue;
			this.sliderText.Text = num.ToString();
			this.autoButton.Content = "MANUAL";
			this.mWBAuto = false;
			CameraTab.ButtonEvent(this._idx, 9, num);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000033C5 File Offset: 0x000015C5
		private void zoomButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.zoomPanel.IsVisible)
			{
				this.HideImageControls();
				return;
			}
			this.HideImageControls();
			this.SetButtonImage(this.zoomButton, "/Images/btn_zoom_on.png");
			this.zoomPanel.Visibility = Visibility.Visible;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000033FE File Offset: 0x000015FE
		private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!this.blockSliderEvent)
			{
				CameraTab.ButtonEvent(this._idx, 4, (int)e.NewValue);
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x0000341B File Offset: 0x0000161B
		private void HandleStatusCb(int width, int height)
		{
			this.UpdatePreviewSize((double)width, (double)height);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003428 File Offset: 0x00001628
		private void imgelt_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (this.menuPanel.IsVisible)
			{
				this.menuPanel.Visibility = Visibility.Hidden;
				return;
			}
			if (this.mEnableFocus && this.imgelt.ActualWidth > 0.0 && this.imgelt.ActualHeight > 0.0)
			{
				Point position = e.GetPosition(this.imgelt);
				float num = (float)(position.X / this.imgelt.ActualWidth);
				float num2 = (float)(position.Y / this.imgelt.ActualHeight);
				if (num >= 0f && num2 >= 0f && num <= 1f && num2 <= 1f)
				{
					this.mFocusTime = DateTime.Now;
					this.mFocusPointX = num;
					this.mFocusPointY = num2;
					UIElement uielement = this.focusImg;
					UIElement uielement2 = this.focusLockImg;
					UIElement uielement3 = this.focusUnlockImg;
					TranslateTransform translateTransform = new TranslateTransform();
					translateTransform.X = position.X - this.focusImg.Width / 2.0;
					translateTransform.Y = position.Y - this.focusImg.Height / 2.0;
					Transform transform = translateTransform;
					uielement3.RenderTransform = translateTransform;
					uielement.RenderTransform = (uielement2.RenderTransform = transform);
					this.focusImg.Opacity = 1.0;
					this.focusImg.Visibility = Visibility.Visible;
					this.focusLockImg.Visibility = Visibility.Collapsed;
					this.focusUnlockImg.Visibility = Visibility.Collapsed;
					this._focusTimer.Interval = TimeSpan.FromMilliseconds(2000.0);
					this._focusTimer.Start();
					this.mLongClick = true;
				}
			}
			this.HideImageControls();
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000035E8 File Offset: 0x000017E8
		private void focusImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if ((DateTime.Now - this.mFocusTime).TotalSeconds < 1.0)
			{
				this.mLongClick = false;
				this._focusTimer.Interval = TimeSpan.FromMilliseconds(1000.0);
			}
			else
			{
				this._focusTimer.Interval = TimeSpan.FromMilliseconds(0.0);
			}
			this._focusTimer.Start();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003660 File Offset: 0x00001860
		private void FocusTimer_Tick(object sender, EventArgs e)
		{
			if (this.focusImg.Visibility == Visibility.Visible)
			{
				this.focusImg.Visibility = Visibility.Collapsed;
				if (this.mLongClick)
				{
					CameraTab.EFocus(this._idx, this.mFocusPointX, this.mFocusPointY, true);
					this.focusUnlockImg.Visibility = Visibility.Visible;
					this.focusUnlockImg.Opacity = 1.0;
				}
				else
				{
					CameraTab.EFocus(this._idx, this.mFocusPointX, this.mFocusPointY, false);
					this.focusLockImg.Visibility = Visibility.Visible;
					this.focusLockImg.Opacity = 1.0;
				}
				this._focusTimer.Interval = TimeSpan.FromMilliseconds(2000.0);
				this._focusTimer.Start();
				return;
			}
			if (this.focusUnlockImg.Opacity == 1.0)
			{
				this.focusUnlockImg.Opacity = 0.95;
				this._focusTimer.Interval = TimeSpan.FromMilliseconds(40.0);
				this._focusTimer.Start();
				return;
			}
			if (this.focusLockImg.Opacity == 1.0)
			{
				this.focusLockImg.Opacity -= 0.05;
				this._focusTimer.Interval = TimeSpan.FromMilliseconds(40.0);
				this._focusTimer.Start();
				return;
			}
			if (this.focusLockImg.Opacity > 0.05)
			{
				this.focusLockImg.Opacity -= 0.05;
				return;
			}
			if (this.focusUnlockImg.Opacity > 0.05)
			{
				this.focusUnlockImg.Opacity -= 0.05;
				return;
			}
			this._focusTimer.Stop();
			this.focusLockImg.Visibility = Visibility.Collapsed;
			this.focusUnlockImg.Visibility = Visibility.Collapsed;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003856 File Offset: 0x00001A56
		private void shutDownButton_Click(object sender, RoutedEventArgs e)
		{
			CameraTab.ButtonEvent(this._idx, 11, 0);
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003868 File Offset: 0x00001A68
		private void autoButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.isoSlider.IsVisible)
			{
				this.mIsoAuto = !this.mIsoAuto;
				int num = (int)this.isoSlider.Value;
				this.sliderText.Text = (this.mIsoAuto ? "" : num.ToString());
				this.autoButton.Content = (this.mIsoAuto ? "AUTO" : "MANUAL");
				this.expBiasButton.IsEnabled = this.mIsoAuto && this.mExpTimeAuto;
				CameraTab.ButtonEvent(this._idx, 6, this.mIsoAuto ? (-1) : num);
				return;
			}
			if (this.expTimeSlider.IsVisible)
			{
				this.mExpTimeAuto = !this.mExpTimeAuto;
				float num2 = (float)Math.Pow(this.expTimeSlider.Value, 5.0);
				float num3 = num2 * (this.mExpTimeMax - this.mExpTimeMin) + this.mExpTimeMin;
				if (num2 > 0f && num2 < 1f)
				{
					this.sliderText.Text = "1/" + ((int)(1f / num3)).ToString();
				}
				else
				{
					this.sliderText.Text = num3.ToString("f2");
				}
				this.autoButton.Content = (this.mExpTimeAuto ? "AUTO" : "MANUAL");
				this.expBiasButton.IsEnabled = this.mIsoAuto && this.mExpTimeAuto;
				CameraTab.ButtonEvent(this._idx, 7, this.mExpTimeAuto ? (-1) : ((int)(num3 * 10000f)));
				return;
			}
			if (this.wbSlider.IsVisible)
			{
				this.mWBAuto = !this.mWBAuto;
				int num4 = (int)this.wbSlider.Value;
				this.sliderText.Text = (this.mWBAuto ? "" : num4.ToString());
				this.sliderText.Text = "";
				this.autoButton.Content = (this.mWBAuto ? "AUTO" : "MANUAL");
				CameraTab.ButtonEvent(this._idx, 9, this.mWBAuto ? (-1) : num4);
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003AA7 File Offset: 0x00001CA7
		private void menuButton_Click(object sender, RoutedEventArgs e)
		{
			this.menuPanel.Visibility = (this.menuPanel.IsVisible ? Visibility.Hidden : Visibility.Visible);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003AC8 File Offset: 0x00001CC8
		private void formatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this._idx < 0)
			{
				return;
			}
			int num = this.formatComboBox.SelectedIndex;
			if (num < 0 || num >= this.formatComboBox.Items.Count)
			{
				num = 0;
			}
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\\\Iriun\\\\webcam"))
			{
				if (registryKey != null)
				{
					registryKey.SetValue("videoFormat" + this._idx.ToString(), num);
					registryKey.Close();
				}
			}
			CameraTab.ESetVideoFormat(this._idx, num);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003B68 File Offset: 0x00001D68
		private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			this.zoomSlider.Value += (double)((e.Delta > 0) ? 1 : (-1));
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003B8C File Offset: 0x00001D8C
		private void HandlePeerListCb(int nameIdx, string tag, string name, int selected)
		{
			this._disablePeerListCb = true;
			if (nameIdx == 0)
			{
				this.phoneComboBox.Items.Clear();
			}
			ComboBoxItem comboBoxItem = new ComboBoxItem();
			comboBoxItem.Content = name;
			comboBoxItem.Tag = tag;
			this.phoneComboBox.Items.Add(comboBoxItem);
			if (selected != 0)
			{
				this.phoneComboBox.SelectedIndex = this.phoneComboBox.Items.Count - 1;
			}
			this._disablePeerListCb = false;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003C04 File Offset: 0x00001E04
		private void phoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.Handled = true;
			if (this._disablePeerListCb || this._idx < 0)
			{
				return;
			}
			ComboBoxItem comboBoxItem = (ComboBoxItem)this.phoneComboBox.SelectedItem;
			CameraTab.ESelectPhone(this._idx, (string)comboBoxItem.Tag);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003C54 File Offset: 0x00001E54
		private void cameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.Handled = true;
			if (this._disablePeerListCb || this._idx < 0)
			{
				return;
			}
			this.isoButton.Visibility = Visibility.Collapsed;
			this.expTimeButton.Visibility = Visibility.Collapsed;
			this.expBiasButton.Visibility = Visibility.Collapsed;
			this.wbButton.Visibility = Visibility.Collapsed;
			this.zoomButton.Visibility = Visibility.Collapsed;
			this.flipButton.Visibility = Visibility.Collapsed;
			this.menuPanel.Visibility = Visibility.Hidden;
			this.HideImageControls();
			int num = int.Parse((string)((ComboBoxItem)this.cameraComboBox.SelectedItem).Tag);
			CameraTab.ButtonEvent(this._idx, 14, num);
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003D04 File Offset: 0x00001F04
		private void HandleCamListCb(string nameList, int currentIdx)
		{
			string[] array = nameList.Split('/', StringSplitOptions.None);
			this._disablePeerListCb = true;
			this.cameraComboBox.Items.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				ComboBoxItem comboBoxItem = new ComboBoxItem();
				comboBoxItem.Content = array[i];
				comboBoxItem.Tag = i.ToString() ?? "";
				this.cameraComboBox.Items.Add(comboBoxItem);
			}
			if (currentIdx < array.Length)
			{
				this.cameraComboBox.SelectedIndex = currentIdx;
			}
			this.cameraMenu.Visibility = Visibility.Visible;
			this.cameraButton.Visibility = Visibility.Collapsed;
			this._disablePeerListCb = false;
		}

		// Token: 0x04000006 RID: 6
		private static readonly CameraTab[] _cameraTabs = new CameraTab[4];

		// Token: 0x04000007 RID: 7
		private readonly CameraTab.ButtonCb buttonCb = delegate(int idx, int button, int value)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleButtonCallback(button, value);
			}), Array.Empty<object>());
		};

		// Token: 0x04000008 RID: 8
		private readonly CameraTab.StatusCb statusCb = delegate(int idx, int width, int height)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleStatusCb(width, height);
			}), Array.Empty<object>());
		};

		// Token: 0x04000009 RID: 9
		private readonly CameraTab.PeerListCb peerListCb = delegate(int idx, int nameIdx, string tag, string name, int selected)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandlePeerListCb(nameIdx, tag, name, selected);
			}), Array.Empty<object>());
		};

		// Token: 0x0400000A RID: 10
		private readonly CameraTab.IsoCb isoCb = delegate(int idx, int mini, int maxi, int value, int manual)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleIsoCb(mini, maxi, value, manual);
			}), Array.Empty<object>());
		};

		// Token: 0x0400000B RID: 11
		private readonly CameraTab.ExpTimeCb expTimeCb = delegate(int idx, float mini, float maxi, float value, int manual)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleExpTimeCb(mini, maxi, value, manual);
			}), Array.Empty<object>());
		};

		// Token: 0x0400000C RID: 12
		private readonly CameraTab.ExpBiasCb expBiasCb = delegate(int idx, float mini, float maxi, float step, float value)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleExpBiasCb(mini, maxi, step, value);
			}), Array.Empty<object>());
		};

		// Token: 0x0400000D RID: 13
		private readonly CameraTab.WBCb wbCb = delegate(int idx, int mini, int maxi, int value, int manual)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleWBCb(mini, maxi, value, manual);
			}), Array.Empty<object>());
		};

		// Token: 0x0400000E RID: 14
		private readonly CameraTab.CamListCb camListCb = delegate(int idx, string nameList, int currentIdx)
		{
			if (idx < 0 || idx > 3 || CameraTab._cameraTabs[idx] == null || Application.Current == null)
			{
				return;
			}
			Application.Current.Dispatcher.BeginInvoke(new Action(delegate
			{
				CameraTab._cameraTabs[idx].HandleCamListCb(nameList, currentIdx);
			}), Array.Empty<object>());
		};

		// Token: 0x0400000F RID: 15
		private int _idx = -1;

		// Token: 0x04000010 RID: 16
		private bool mIsoAuto;

		// Token: 0x04000011 RID: 17
		private bool mExpTimeAuto;

		// Token: 0x04000012 RID: 18
		private bool mWBAuto;

		// Token: 0x04000013 RID: 19
		private float mExpTimeMin;

		// Token: 0x04000014 RID: 20
		private float mExpTimeMax;

		// Token: 0x04000015 RID: 21
		private bool mEnableFocus;

		// Token: 0x04000016 RID: 22
		private DateTime mFocusTime;

		// Token: 0x04000017 RID: 23
		private float mFocusPointX;

		// Token: 0x04000018 RID: 24
		private float mFocusPointY;

		// Token: 0x04000019 RID: 25
		private bool mLongClick;

		// Token: 0x0400001A RID: 26
		private DispatcherTimer _sizeTimer;

		// Token: 0x0400001B RID: 27
		private DispatcherTimer _adapterTimer;

		// Token: 0x0400001C RID: 28
		private DispatcherTimer _focusTimer;

		// Token: 0x0400001D RID: 29
		private TimeSpan _lastRender;

		// Token: 0x0400001E RID: 30
		private bool blockSliderEvent;

		// Token: 0x0400001F RID: 31
		private const float kExposureDurationPower = 5f;

		// Token: 0x04000020 RID: 32
		private bool _disablePeerListCb;

		// Token: 0x02000008 RID: 8
		private struct POINT
		{
			// Token: 0x0600007B RID: 123 RVA: 0x00005076 File Offset: 0x00003276
			public POINT(Point p)
			{
				this.x = (int)p.X;
				this.y = (int)p.Y;
			}

			// Token: 0x0400006B RID: 107
			public int x;

			// Token: 0x0400006C RID: 108
			public int y;
		}

		// Token: 0x02000009 RID: 9
		// (Invoke) Token: 0x0600007D RID: 125
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void ButtonCb(int idx, int button, int value);

		// Token: 0x0200000A RID: 10
		// (Invoke) Token: 0x06000081 RID: 129
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void StatusCb(int idx, int width, int height);

		// Token: 0x0200000B RID: 11
		// (Invoke) Token: 0x06000085 RID: 133
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void PeerListCb(int idx, int nameIdx, string tag, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int selected);

		// Token: 0x0200000C RID: 12
		// (Invoke) Token: 0x06000089 RID: 137
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void IsoCb(int idx, int mini, int maxi, int value, int manual);

		// Token: 0x0200000D RID: 13
		// (Invoke) Token: 0x0600008D RID: 141
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void ExpTimeCb(int idx, float mini, float maxi, float value, int manual);

		// Token: 0x0200000E RID: 14
		// (Invoke) Token: 0x06000091 RID: 145
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void ExpBiasCb(int idx, float mini, float maxi, float step, float value);

		// Token: 0x0200000F RID: 15
		// (Invoke) Token: 0x06000095 RID: 149
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void WBCb(int idx, int mini, int maxi, int value, int manual);

		// Token: 0x02000010 RID: 16
		// (Invoke) Token: 0x06000099 RID: 153
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate void CamListCb(int idx, string nameList, int currentIdx);
	}
}
