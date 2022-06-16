// https://www.codeproject.com/script/Articles/ViewDownloads.aspx?aid=19274

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MapAssist.Types
{
    public static class CTools
    {
        #region Public Methods

        #region IsNumericType

        /// <summary>
        /// Determines whether the specified value is of numeric type.
        /// </summary>
        /// <param name="o">The object to check.</param>
        /// <returns>
        /// 	<c>true</c> if o is a numeric type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNumericType(object o)
        {
            return (o is byte ||
                o is sbyte ||
                o is short ||
                o is ushort ||
                o is int ||
                o is uint ||
                o is long ||
                o is ulong ||
                o is float ||
                o is double ||
                o is decimal);
        }

        #endregion IsNumericType

        #region IsPositive

        /// <summary>
        /// Determines whether the specified value is positive.
        /// </summary>
        /// <param name="Value">The value.</param>
        /// <param name="ZeroIsPositive">if set to <c>true</c> treats 0 as positive.</param>
        /// <returns>
        /// 	<c>true</c> if the specified value is positive; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPositive(object Value, bool ZeroIsPositive)
        {
            switch (Type.GetTypeCode(Value.GetType()))
            {
                case TypeCode.SByte:
                    return (ZeroIsPositive ? (sbyte)Value >= 0 : (sbyte)Value > 0);

                case TypeCode.Int16:
                    return (ZeroIsPositive ? (short)Value >= 0 : (short)Value > 0);

                case TypeCode.Int32:
                    return (ZeroIsPositive ? (int)Value >= 0 : (int)Value > 0);

                case TypeCode.Int64:
                    return (ZeroIsPositive ? (long)Value >= 0 : (long)Value > 0);

                case TypeCode.Single:
                    return (ZeroIsPositive ? (float)Value >= 0 : (float)Value > 0);

                case TypeCode.Double:
                    return (ZeroIsPositive ? (double)Value >= 0 : (double)Value > 0);

                case TypeCode.Decimal:
                    return (ZeroIsPositive ? (decimal)Value >= 0 : (decimal)Value > 0);

                case TypeCode.Byte:
                    return (ZeroIsPositive ? true : (byte)Value > 0);

                case TypeCode.UInt16:
                    return (ZeroIsPositive ? true : (ushort)Value > 0);

                case TypeCode.UInt32:
                    return (ZeroIsPositive ? true : (uint)Value > 0);

                case TypeCode.UInt64:
                    return (ZeroIsPositive ? true : (ulong)Value > 0);

                case TypeCode.Char:
                    return (ZeroIsPositive ? true : (char)Value != '\0');

                default:
                    return false;
            }
        }

        #endregion IsPositive

        #region ToUnsigned

        /// <summary>
        /// Converts the specified values boxed type to its correpsonding unsigned
        /// type.
        /// </summary>
        /// <param name="Value">The value.</param>
        /// <returns>A boxed numeric object whos type is unsigned.</returns>
        public static object ToUnsigned(object Value)
        {
            switch (Type.GetTypeCode(Value.GetType()))
            {
                case TypeCode.SByte:
                    return (byte)((sbyte)Value);

                case TypeCode.Int16:
                    return (ushort)((short)Value);

                case TypeCode.Int32:
                    return (uint)((int)Value);

                case TypeCode.Int64:
                    return (ulong)((long)Value);

                case TypeCode.Byte:
                    return Value;

                case TypeCode.UInt16:
                    return Value;

                case TypeCode.UInt32:
                    return Value;

                case TypeCode.UInt64:
                    return Value;

                case TypeCode.Single:
                    return (uint)((float)Value);

                case TypeCode.Double:
                    return (ulong)((double)Value);

                case TypeCode.Decimal:
                    return (ulong)((decimal)Value);

                default:
                    return null;
            }
        }

        #endregion ToUnsigned

        #region ToInteger

        /// <summary>
        /// Converts the specified values boxed type to its correpsonding integer
        /// type.
        /// </summary>
        /// <param name="Value">The value.</param>
        /// <returns>A boxed numeric object whos type is an integer type.</returns>
        public static object ToInteger(object Value, bool Round)
        {
            switch (Type.GetTypeCode(Value.GetType()))
            {
                case TypeCode.SByte:
                    return Value;

                case TypeCode.Int16:
                    return Value;

                case TypeCode.Int32:
                    return Value;

                case TypeCode.Int64:
                    return Value;

                case TypeCode.Byte:
                    return Value;

                case TypeCode.UInt16:
                    return Value;

                case TypeCode.UInt32:
                    return Value;

                case TypeCode.UInt64:
                    return Value;

                case TypeCode.Single:
                    return (Round ? (int)Math.Round((float)Value) : (int)((float)Value));

                case TypeCode.Double:
                    return (Round ? (long)Math.Round((double)Value) : (long)((double)Value));

                case TypeCode.Decimal:
                    return (Round ? Math.Round((decimal)Value) : (decimal)Value);

                default:
                    return null;
            }
        }

        #endregion ToInteger

        #region UnboxToLong

        public static long UnboxToLong(object Value, bool Round)
        {
            switch (Type.GetTypeCode(Value.GetType()))
            {
                case TypeCode.SByte:
                    return (long)((sbyte)Value);

                case TypeCode.Int16:
                    return (long)((short)Value);

                case TypeCode.Int32:
                    return (long)((int)Value);

                case TypeCode.Int64:
                    return (long)Value;

                case TypeCode.Byte:
                    return (long)((byte)Value);

                case TypeCode.UInt16:
                    return (long)((ushort)Value);

                case TypeCode.UInt32:
                    return (long)((uint)Value);

                case TypeCode.UInt64:
                    return (long)((ulong)Value);

                case TypeCode.Single:
                    return (Round ? (long)Math.Round((float)Value) : (long)((float)Value));

                case TypeCode.Double:
                    return (Round ? (long)Math.Round((double)Value) : (long)((double)Value));

                case TypeCode.Decimal:
                    return (Round ? (long)Math.Round((decimal)Value) : (long)((decimal)Value));

                default:
                    return 0;
            }
        }

        #endregion UnboxToLong

        #region ReplaceMetaChars

        /// <summary>
        /// Replaces the string representations of meta chars with their corresponding
        /// character values.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A string with all string meta chars are replaced</returns>
        public static string ReplaceMetaChars(string input)
        {
            return Regex.Replace(input, @"(\\)(\d{3}|[^\d])?", new MatchEvaluator(ReplaceMetaCharsMatch));
        }

        private static string ReplaceMetaCharsMatch(Match m)
        {
            // convert octal quotes (like \040)
            if (m.Groups[2].Length == 3)
                return Convert.ToChar(Convert.ToByte(m.Groups[2].Value, 8)).ToString();
            else
            {
                // convert all other special meta characters
                //TODO: \xhhh hex and possible dec !!
                switch (m.Groups[2].Value)
                {
                    case "0":           // null
                        return "\0";

                    case "a":           // alert (beep)
                        return "\a";

                    case "b":           // BS
                        return "\b";

                    case "f":           // FF
                        return "\f";

                    case "v":           // vertical tab
                        return "\v";

                    case "r":           // CR
                        return "\r";

                    case "n":           // LF
                        return "\n";

                    case "t":           // Tab
                        return "\t";

                    default:
                        // if neither an octal quote nor a special meta character
                        // so just remove the backslash
                        return m.Groups[2].Value;
                }
            }
        }

        #endregion ReplaceMetaChars

        #region printf

        public static void printf(string Format, params object[] Parameters)
        {
            Console.Write(CTools.sprintf(Format, Parameters));
        }

        #endregion printf

        #region fprintf

        public static void fprintf(TextWriter Destination, string Format, params object[] Parameters)
        {
            Destination.Write(CTools.sprintf(Format, Parameters));
        }

        #endregion fprintf

        #region sprintf

        public static string sprintf(string Format, params object[] Parameters)
        {
            #region Variables

            var f = new StringBuilder();
            var r = new Regex(@"\%(\d*\$)?([\'\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?([dioxXucsfeEgGpn%])");
            //"%[parameter][flags][width][.precision][length]type"
            Match m = null;
            var w = string.Empty;
            var defaultParamIx = 0;
            int paramIx;
            object o = null;

            var flagLeft2Right = false;
            var flagAlternate = false;
            var flagPositiveSign = false;
            var flagPositiveSpace = false;
            var flagZeroPadding = false;
            var flagGroupThousands = false;

            var fieldLength = 0;
            var fieldPrecision = 0;
            var shortLongIndicator = '\0';
            var formatSpecifier = '\0';
            var paddingCharacter = ' ';

            #endregion Variables

            // find all format parameters in format string
            f.Append(Format);
            m = r.Match(f.ToString());
            while (m.Success)
            {
                #region parameter index

                paramIx = defaultParamIx;
                if (m.Groups[1] != null && m.Groups[1].Value.Length > 0)
                {
                    var val = m.Groups[1].Value.Substring(0, m.Groups[1].Value.Length - 1);
                    paramIx = Convert.ToInt32(val) - 1;
                };

                #endregion parameter index

                #region format flags

                // extract format flags
                flagAlternate = false;
                flagLeft2Right = false;
                flagPositiveSign = false;
                flagPositiveSpace = false;
                flagZeroPadding = false;
                flagGroupThousands = false;
                if (m.Groups[2] != null && m.Groups[2].Value.Length > 0)
                {
                    var flags = m.Groups[2].Value;

                    flagAlternate = (flags.IndexOf('#') >= 0);
                    flagLeft2Right = (flags.IndexOf('-') >= 0);
                    flagPositiveSign = (flags.IndexOf('+') >= 0);
                    flagPositiveSpace = (flags.IndexOf(' ') >= 0);
                    flagGroupThousands = (flags.IndexOf('\'') >= 0);

                    // positive + indicator overrides a
                    // positive space character
                    if (flagPositiveSign && flagPositiveSpace)
                        flagPositiveSpace = false;
                }

                #endregion format flags

                #region field length

                // extract field length and
                // pading character
                paddingCharacter = ' ';
                fieldLength = int.MinValue;
                if (m.Groups[3] != null && m.Groups[3].Value.Length > 0)
                {
                    fieldLength = Convert.ToInt32(m.Groups[3].Value);
                    flagZeroPadding = (m.Groups[3].Value[0] == '0');
                }

                #endregion field length

                if (flagZeroPadding)
                    paddingCharacter = '0';

                // left2right allignment overrides zero padding
                if (flagLeft2Right && flagZeroPadding)
                {
                    flagZeroPadding = false;
                    paddingCharacter = ' ';
                }

                #region field precision

                // extract field precision
                fieldPrecision = int.MinValue;
                if (m.Groups[4] != null && m.Groups[4].Value.Length > 0)
                    fieldPrecision = Convert.ToInt32(m.Groups[4].Value);

                #endregion field precision

                #region short / long indicator

                // extract short / long indicator
                shortLongIndicator = char.MinValue;
                if (m.Groups[5] != null && m.Groups[5].Value.Length > 0)
                    shortLongIndicator = m.Groups[5].Value[0];

                #endregion short / long indicator

                #region format specifier

                // extract format
                formatSpecifier = char.MinValue;
                if (m.Groups[6] != null && m.Groups[6].Value.Length > 0)
                    formatSpecifier = m.Groups[6].Value[0];

                #endregion format specifier

                // default precision is 6 digits if none is specified except
                if (fieldPrecision == int.MinValue &&
                    formatSpecifier != 's' &&
                    formatSpecifier != 'c' &&
                    char.ToUpper(formatSpecifier) != 'X' &&
                    formatSpecifier != 'o')
                    fieldPrecision = 6;

                #region get next value parameter

                // get next value parameter and convert value parameter depending on short / long indicator
                if (Parameters == null || paramIx >= Parameters.Length)
                    o = null;
                else
                {
                    o = Parameters[paramIx];

                    if (shortLongIndicator == 'h')
                    {
                        if (o is int)
                            o = (short)((int)o);
                        else if (o is long)
                            o = (short)((long)o);
                        else if (o is uint)
                            o = (ushort)((uint)o);
                        else if (o is ulong)
                            o = (ushort)((ulong)o);
                    }
                    else if (shortLongIndicator == 'l')
                    {
                        if (o is short)
                            o = (long)((short)o);
                        else if (o is int)
                            o = (long)((int)o);
                        else if (o is ushort)
                            o = (ulong)((ushort)o);
                        else if (o is uint)
                            o = (ulong)((uint)o);
                    }
                }

                #endregion get next value parameter

                // convert value parameters to a string depending on the formatSpecifier
                w = string.Empty;
                switch (formatSpecifier)
                {
                    #region % - character

                    case '%':   // % character
                        w = "%";
                        break;

                    #endregion % - character

                    #region d - integer

                    case 'd':   // integer
                        w = FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion d - integer

                    #region i - integer

                    case 'i':   // integer
                        goto case 'd';

                    #endregion i - integer

                    #region o - octal integer

                    case 'o':   // octal integer - no leading zero
                        w = FormatOct("o", flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion o - octal integer

                    #region x - hex integer

                    case 'x':   // hex integer - no leading zero
                        w = FormatHex("x", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion x - hex integer

                    #region X - hex integer

                    case 'X':   // same as x but with capital hex characters
                        w = FormatHex("X", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion X - hex integer

                    #region u - unsigned integer

                    case 'u':   // unsigned integer
                        w = FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        false, false,
                                        paddingCharacter, ToUnsigned(o));
                        defaultParamIx++;
                        break;

                    #endregion u - unsigned integer

                    #region c - character

                    case 'c':   // character
                        if (IsNumericType(o))
                            w = Convert.ToChar(o).ToString();
                        else if (o is char)
                            w = ((char)o).ToString();
                        else if (o is string && ((string)o).Length > 0)
                            w = ((string)o)[0].ToString();
                        defaultParamIx++;
                        break;

                    #endregion c - character

                    #region s - string

                    case 's':   // string
                        var t = "{0" + (fieldLength != int.MinValue ? "," + (flagLeft2Right ? "-" : string.Empty) + fieldLength.ToString() : string.Empty) + ":s}";
                        w = o.ToString();
                        if (fieldPrecision >= 0)
                            w = w.Substring(0, fieldPrecision);

                        if (fieldLength != int.MinValue)
                            if (flagLeft2Right)
                                w = w.PadRight(fieldLength, paddingCharacter);
                            else
                                w = w.PadLeft(fieldLength, paddingCharacter);
                        defaultParamIx++;
                        break;

                    #endregion s - string

                    #region f - double number

                    case 'f':   // double
                        w = FormatNumber((flagGroupThousands ? "n" : "f"), flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion f - double number

                    #region e - exponent number

                    case 'e':   // double / exponent
                        w = FormatNumber("e", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion e - exponent number

                    #region E - exponent number

                    case 'E':   // double / exponent
                        w = FormatNumber("E", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion E - exponent number

                    #region g - general number

                    case 'g':   // double / exponent
                        w = FormatNumber("g", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion g - general number

                    #region G - general number

                    case 'G':   // double / exponent
                        w = FormatNumber("G", flagAlternate,
                                        fieldLength, fieldPrecision, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, o);
                        defaultParamIx++;
                        break;

                    #endregion G - general number

                    #region p - pointer

                    case 'p':   // pointer
                        if (o is IntPtr)
                            w = "0x" + ((IntPtr)o).ToString("x");
                        defaultParamIx++;
                        break;

                    #endregion p - pointer

                    #region n - number of processed chars so far

                    case 'n':   // number of characters so far
                        w = FormatNumber("d", flagAlternate,
                                        fieldLength, int.MinValue, flagLeft2Right,
                                        flagPositiveSign, flagPositiveSpace,
                                        paddingCharacter, m.Index);
                        break;

                    #endregion n - number of processed chars so far

                    default:
                        w = string.Empty;
                        defaultParamIx++;
                        break;
                }

                // replace format parameter with parameter value
                // and start searching for the next format parameter
                // AFTER the position of the current inserted value
                // to prohibit recursive matches if the value also
                // includes a format specifier
                f.Remove(m.Index, m.Length);
                f.Insert(m.Index, w);
                m = r.Match(f.ToString(), m.Index + w.Length);
            }

            return f.ToString();
        }

        #endregion sprintf

        #endregion Public Methods

        #region Private Methods

        #region FormatOCT

        private static string FormatOct(string NativeFormat, bool Alternate,
                                            int FieldLength, int FieldPrecision,
                                            bool Left2Right,
                                            char Padding, object Value)
        {
            var w = string.Empty;
            var lengthFormat = "{0" + (FieldLength != int.MinValue ?
                                            "," + (Left2Right ?
                                                    "-" :
                                                    string.Empty) + FieldLength.ToString() :
                                            string.Empty) + "}";

            if (IsNumericType(Value))
            {
                w = Convert.ToString(UnboxToLong(Value, true), 8);

                if (Left2Right || Padding == ' ')
                {
                    if (Alternate && w != "0")
                        w = "0" + w;
                    w = string.Format(lengthFormat, w);
                }
                else
                {
                    if (FieldLength != int.MinValue)
                        w = w.PadLeft(FieldLength - (Alternate && w != "0" ? 1 : 0), Padding);
                    if (Alternate && w != "0")
                        w = "0" + w;
                }
            }

            return w;
        }

        #endregion FormatOCT

        #region FormatHEX

        private static string FormatHex(string NativeFormat, bool Alternate,
                                            int FieldLength, int FieldPrecision,
                                            bool Left2Right,
                                            char Padding, object Value)
        {
            var w = string.Empty;
            var lengthFormat = "{0" + (FieldLength != int.MinValue ?
                                            "," + (Left2Right ?
                                                    "-" :
                                                    string.Empty) + FieldLength.ToString() :
                                            string.Empty) + "}";
            var numberFormat = "{0:" + NativeFormat + (FieldPrecision != int.MinValue ?
                                            FieldPrecision.ToString() :
                                            string.Empty) + "}";

            if (IsNumericType(Value))
            {
                w = string.Format(numberFormat, Value);

                if (Left2Right || Padding == ' ')
                {
                    if (Alternate)
                        w = (NativeFormat == "x" ? "0x" : "0X") + w;
                    w = string.Format(lengthFormat, w);
                }
                else
                {
                    if (FieldLength != int.MinValue)
                        w = w.PadLeft(FieldLength - (Alternate ? 2 : 0), Padding);
                    if (Alternate)
                        w = (NativeFormat == "x" ? "0x" : "0X") + w;
                }
            }

            return w;
        }

        #endregion FormatHEX

        #region FormatNumber

        private static string FormatNumber(string NativeFormat, bool Alternate,
                                            int FieldLength, int FieldPrecision,
                                            bool Left2Right,
                                            bool PositiveSign, bool PositiveSpace,
                                            char Padding, object Value)
        {
            var w = string.Empty;
            var lengthFormat = "{0" + (FieldLength != int.MinValue ?
                                            "," + (Left2Right ?
                                                    "-" :
                                                    string.Empty) + FieldLength.ToString() :
                                            string.Empty) + "}";
            var numberFormat = "{0:" + NativeFormat + (FieldPrecision != int.MinValue ?
                                            FieldPrecision.ToString() :
                                            "0") + "}";

            if (IsNumericType(Value))
            {
                w = string.Format(numberFormat, Value);

                if (Left2Right || Padding == ' ')
                {
                    if (IsPositive(Value, true))
                        w = (PositiveSign ?
                                "+" : (PositiveSpace ? " " : string.Empty)) + w;
                    w = string.Format(lengthFormat, w);
                }
                else
                {
                    if (w.StartsWith("-"))
                        w = w.Substring(1);
                    if (FieldLength != int.MinValue)
                        w = w.PadLeft(FieldLength - 1, Padding);
                    if (IsPositive(Value, true))
                        w = (PositiveSign ?
                                "+" : (PositiveSpace ?
                                        " " : (FieldLength != int.MinValue ?
                                                Padding.ToString() : string.Empty))) + w;
                    else
                        w = "-" + w;
                }
            }

            return w;
        }

        #endregion FormatNumber

        #endregion Private Methods
    }
}
