using System.Windows.Controls;

namespace budget
{
    public class PenniesValidationRule : ValidationRule
    {
        private const string InvalidInput = "Please enter valid currency value!";

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value is string valueString)
            {
                if (string.IsNullOrEmpty(valueString))
                {
                    return new ValidationResult(true, null);
                }

                string poundsStr = valueString;
                string penniesStr = "0";

                if (valueString.Contains("."))
                {
                    var tokens = valueString.Split('.');
                    poundsStr = penniesStr = null;

                    if (tokens.Length == 2 && tokens[1].Length <= 2)
                    {
                        poundsStr = tokens[0];
                        penniesStr = tokens[1];
                    }
                }

                int pennies;
                int pounds;

                if (poundsStr != null && penniesStr != null &&
                    (poundsStr.Length == 0 || int.TryParse(poundsStr, out pounds)) && int.TryParse(penniesStr, out pennies))
                {
                    return new ValidationResult(true, null);
                }
            }

            return new ValidationResult(false, null);
        }
    }
}
