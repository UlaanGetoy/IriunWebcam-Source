using System;
using System.Runtime.InteropServices;

namespace webcam
{
	// Token: 0x02000004 RID: 4
	public static class HRESULT
	{
		// Token: 0x0600004E RID: 78 RVA: 0x000042B4 File Offset: 0x000024B4
		public static void Check(int hr)
		{
			Marshal.ThrowExceptionForHR(hr);
		}
	}
}
