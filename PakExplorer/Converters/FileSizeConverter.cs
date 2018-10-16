using System;
using Windows.UI.Xaml.Data;

namespace PakExplorer.Converters
{
    public sealed class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Source: https://stackoverflow.com/a/11124118
            long i = (long)value;
            long absolute_i = (i < 0 ? -i : i);
            string suffix;
            double readable;
            if (absolute_i >= 0x40000000)
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000)
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400)
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B");
            }
            readable = (readable / 1024);
            return readable.ToString("0.## ") + suffix;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
