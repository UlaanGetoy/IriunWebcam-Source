using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace webcam.Properties
{
	// Token: 0x02000007 RID: 7
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		// Token: 0x06000076 RID: 118 RVA: 0x00005018 File Offset: 0x00003218
		internal Resources()
		{
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00005020 File Offset: 0x00003220
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (Resources.resourceMan == null)
				{
					Resources.resourceMan = new ResourceManager("webcam.Properties.Resources", typeof(Resources).Assembly);
				}
				return Resources.resourceMan;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000078 RID: 120 RVA: 0x0000504C File Offset: 0x0000324C
		// (set) Token: 0x06000079 RID: 121 RVA: 0x00005053 File Offset: 0x00003253
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return Resources.resourceCulture;
			}
			set
			{
				Resources.resourceCulture = value;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600007A RID: 122 RVA: 0x0000505B File Offset: 0x0000325B
		internal static Icon icon
		{
			get
			{
				return (Icon)Resources.ResourceManager.GetObject("icon", Resources.resourceCulture);
			}
		}

		// Token: 0x04000069 RID: 105
		private static ResourceManager resourceMan;

		// Token: 0x0400006A RID: 106
		private static CultureInfo resourceCulture;
	}
}
