using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JETModUpdater
{
    class AnsiColorManager
    {
        public enum AnsiCode
        {
            Reset = 0,
            Bright = 1,
            Dim = 2,
            Underscore = 4,
            Blink = 5,
            Reverse = 7,
            Hidden = 8,

            FgBlack = 30,
            FgRed = 31,
            FgGreen = 32,
            FgYellow = 33,
            FgBlue = 34,
            FgMagenta = 35,
            FgCyan = 36,
            FgWhite = 37,

            BgBlack = 40,
            BgRed = 41,
            BgGreen = 42,
            BgYellow = 43,
            BgBlue = 44,
            BgMagenta = 45,
            BgCyan = 46,
            BgWhite = 47
        }

        public static void WriteColorizedString(string text)
        {
            var textToDisplay = text;

            while (!string.IsNullOrWhiteSpace(textToDisplay))
            {
                if (!Regex.IsMatch(textToDisplay, "\x1b\\[\\d+\\S((\\d\\S(;?))+)?"))
                {
                    Console.WriteLine(textToDisplay);
                    textToDisplay = string.Empty;
                    continue;
                }

                var nextText = Regex.Match(textToDisplay, ".*?(?=\x1b)").Value;
                if (!string.IsNullOrEmpty(nextText))
                    Console.Write(nextText);

                var wholeCodeString = Regex.Match(textToDisplay, "\x1b\\[\\d+\\S((\\d\\S(;?))+)?").Value;
                var shortCodeString = Regex.Match(textToDisplay, "(?<=\x1b\\[)\\d+\\S(?=\\d\\S)?").Value;

                var code = int.Parse(string.Join("", shortCodeString.Take(shortCodeString.Length - 1)));
                var suffix = shortCodeString.Last();

                switch (char.ToLower(suffix))
                {
                    case ';':
                    case 'm':
                        var enumCode = (AnsiCode)code;
                        switch (enumCode)
                        {
                            case AnsiCode.Reset:
                                Console.ResetColor();
                                break;
                            case AnsiCode.Bright:
                            case AnsiCode.Dim:
                            case AnsiCode.Underscore:
                            case AnsiCode.Blink:
                            case AnsiCode.Reverse:
                            case AnsiCode.Hidden:
                            case AnsiCode.FgBlack:
                                Console.ForegroundColor = ConsoleColor.Black;
                                break;
                            case AnsiCode.FgRed:
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                            case AnsiCode.FgGreen:
                                Console.ForegroundColor = ConsoleColor.Green;
                                break;
                            case AnsiCode.FgYellow:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                break;
                            case AnsiCode.FgBlue:
                                Console.ForegroundColor = ConsoleColor.Blue;
                                break;
                            case AnsiCode.FgMagenta:
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                break;
                            case AnsiCode.FgCyan:
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                break;
                            case AnsiCode.FgWhite:
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                            case AnsiCode.BgBlack:
                                Console.BackgroundColor = ConsoleColor.Black;
                                break;
                            case AnsiCode.BgRed:
                                Console.BackgroundColor = ConsoleColor.DarkRed;
                                break;
                            case AnsiCode.BgGreen:
                                Console.BackgroundColor = ConsoleColor.DarkGreen;
                                break;
                            case AnsiCode.BgYellow:
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                break;
                            case AnsiCode.BgBlue:
                                Console.BackgroundColor = ConsoleColor.DarkBlue;
                                break;
                            case AnsiCode.BgMagenta:
                                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                                break;
                            case AnsiCode.BgCyan:
                                Console.BackgroundColor = ConsoleColor.DarkCyan;
                                break;
                            case AnsiCode.BgWhite:
                                Console.BackgroundColor = ConsoleColor.White;
                                break;
                            default:
                                break;
                        }
                        break;
                    case 'j':
                        if (code == 2)
                            Console.Clear();
                        break;

                }

                var pos = textToDisplay.IndexOf(wholeCodeString);
                textToDisplay = textToDisplay.Substring(pos + wholeCodeString.Length);
                if (string.IsNullOrEmpty(textToDisplay))
                    Console.WriteLine();
            }
        }
    }
}
