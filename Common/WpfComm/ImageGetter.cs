
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Common.WpfComm
{
	public static class ImageGetter
	{
		public static BitmapImage GetBitmapImage(string pathFile)
		{
			var uri = new Uri(@"pack://application:,,,/"
							  + Assembly.GetCallingAssembly().GetName().Name
							  + ";component/"
							  + pathFile, UriKind.Absolute);

			return new BitmapImage(uri);
		}
	}

}
