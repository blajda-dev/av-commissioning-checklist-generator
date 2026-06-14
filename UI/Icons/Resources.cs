using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CommissioningChecklistGenerator.UI.Icons
{
    internal static class Resources
    {
        internal static readonly ImageSource? BlackChecklistIcon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/ui/icons/icon_black.ico") as ImageSource;
        internal static readonly ImageSource? ColorChecklistIcon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/ui/icons/icon_color.ico") as ImageSource;
        internal static readonly ImageSource? CloudDownloadIcon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/ui/icons/icon_cloud_download.ico") as ImageSource;
        internal static readonly ImageSource? SettingsIcon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/ui/icons/icon_settings.ico") as ImageSource;
        internal static readonly ImageSource? ConvertIcon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/ui/icons/icon_convert.ico") as ImageSource;
    }
}
