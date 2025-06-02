using System;
using System.Globalization;
using System.Windows.Data;

namespace WorkTrackingWpf.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Eğer değer bool ise dönüştür
            if (value is bool boolValue)
            {
                return boolValue ? "Eksik Veri Var" : "Veri Tam";
            }

            // Eğer beklenen türde değilse varsayılan bir değer döndür
            return "Veri Tam";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Bu dönüşüm tek yönlü olduğu için exception fırlatıyoruz
            throw new NotImplementedException();
        }
    }
}
