// Minimal Fortran Fw.d / Iw edit-descriptor formatters for byte-parity output.
// gfortran's runtime rounds half-away-from-zero and prints "-0.00000" for tiny
// negative residuals near zero; both behaviors are reproduced here.
//
// Port of packages/avl-core/src/output/fortranFormat.ts.

using System.Globalization;

namespace Avl.Core.Output;

public static class FortranFormat
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Fixed-point magnitude with an explicit sign taken from the IEEE sign
    /// bit, so that -0 and tiny negatives that round to zero still print "-...".
    /// This mirrors JS Number.prototype.toFixed plus the outTotal -0 special-case.</summary>
    private static string SignedFixed(double value, int decimals)
    {
        string mag = Math.Abs(value).ToString("F" + decimals, Inv);
        return double.IsNegative(value) ? "-" + mag : mag;
    }

    public static string FortranF(double value, int width, int decimals)
    {
        return SignedFixed(value, decimals).PadLeft(width);
    }

    public static string FortranI(double value, int width)
    {
        return ((long)Math.Truncate(value)).ToString(Inv).PadLeft(width);
    }

    /// <summary>Fortran ESw.d scientific edit descriptor: mantissa normalized to [1,10),
    /// with a signed 2-digit (3 if |exp|>=100) exponent, right-justified in width w.
    /// The IEEE sign bit is preserved (so -0 prints "-0.0...E+00"), matching MRF output.</summary>
    public static string FortranES(double value, int width, int decimals)
    {
        bool neg = double.IsNegative(value);
        double av = Math.Abs(value);
        string mant;
        int exp;
        if (av == 0)
        {
            mant = "0." + new string('0', decimals);
            exp = 0;
        }
        else
        {
            string e = av.ToString("E" + decimals, Inv); // e.g. "5.300000000000000E+002"
            int ep = e.IndexOf('E');
            mant = e.Substring(0, ep);
            exp = int.Parse(e.Substring(ep + 1), Inv);
        }
        string expSign = exp >= 0 ? "+" : "-";
        string expDigits = Math.Abs(exp).ToString(Inv);
        if (expDigits.Length < 2) expDigits = expDigits.PadLeft(2, '0');
        string body = (neg ? "-" : "") + mant + "E" + expSign + expDigits;
        return body.PadLeft(width);
    }

    /// <summary>Fortran Gw.d general format: for a value whose decimal-exponent class k
    /// (10^(k-1) &lt;= |value| &lt; 10^k) satisfies 0&lt;=k&lt;=d, prints as Fw'.d'
    /// (w'=width-4) followed by 4 trailing blanks; otherwise falls back to Fortran's
    /// classic "0.dddddE±dd" exponential form (mantissa normalized to [0.1,1)),
    /// right-justified in the full field width.</summary>
    public static string FortranG(double value, int width, int sig)
    {
        if (value == 0)
        {
            return (0.0).ToString("F" + (sig - 1), Inv).PadLeft(width - 4) + new string(' ', 4);
        }

        double av = Math.Abs(value);
        string sign = value < 0 ? "-" : " ";
        // Correctly-rounded exponential representation (av = mantissa * 10^exp, 1<=mantissa<10)
        string eForm = av.ToString("E" + (sig - 1), Inv); // e.g. "1.23457E+002"
        int ePos = eForm.IndexOf('E');
        string mantissaStr = eForm.Substring(0, ePos);
        int exp = int.Parse(eForm.Substring(ePos + 1), Inv);
        int k = exp + 1; // Fortran's exponent class: 10^(k-1) <= av < 10^k

        if (k >= 0 && k <= sig)
        {
            const int trailing = 4;
            int innerWidth = width - trailing;
            int decimals = Math.Max(0, sig - k);
            string s = SignedFixed(value, decimals);
            return s.PadLeft(innerWidth) + new string(' ', trailing);
        }

        // Exponential form: sign + "0." + sig mantissa digits + "E" + exponent sign + 2-digit exponent.
        string digits = mantissaStr.Replace(".", "");
        string expSign = k >= 0 ? "+" : "-";
        string expAbs = Math.Abs(k).ToString(Inv).PadLeft(2, '0');
        string body = $"{sign}0.{digits}E{expSign}{expAbs}";
        return body.PadLeft(width);
    }
}
