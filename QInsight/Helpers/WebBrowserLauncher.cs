using System.Diagnostics;

namespace Qenex.QInsight.Helpers;

/// <summary>Opens web links in the user's default browser.</summary>
public static class WebBrowserLauncher
{
	/// <summary>Opens the given URL in the default browser. Returns false when the shell
	/// refuses to open it (e.g. no browser registered); never throws.</summary>
	public static bool OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			return true;
		}
		catch
		{
			return false;
		}
	}
}
