using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace budget
{
    class PenniesTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(string) && value is int totalPennies)
            {
                int pounds = totalPennies / 100;
                int pennies = totalPennies % 100;

                return string.Format("{0:0}.{1:00}", pounds, Math.Abs(pennies));
            }

            throw new ArgumentException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debug.Assert(Validator.Validate(value, null).IsValid);

            if (targetType == typeof(int) && value is string stringValue && Validator.Validate(value, null).IsValid)
            {
                var tokens = stringValue.Split('.');
                if (tokens.Length == 2)
                {
                    if (tokens[1].Length == 1)
                    {
                        tokens[1] = tokens[1] + "0";
                    }
                    int poundsInPennies = tokens[0].Length == 0 ? 0 : int.Parse(tokens[0]) * 100;
                    int pennies = int.Parse(tokens[1]);

                    return poundsInPennies + (poundsInPennies < 0 ? -pennies : pennies);
                }
                else if (tokens.Length == 1)
                {
                    if (string.IsNullOrEmpty(tokens[0]))
                    {
                        return 0;
                    }
                    return int.Parse(tokens[0]) * 100;
                }
            }

            throw new ArgumentException();
        }

        private static readonly PenniesValidationRule Validator = new PenniesValidationRule();
    }
}
