using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JETModUpdater
{
    public static class Logger
    {
        public enum LogType
        {
            Info,
            Write,
            Move,
            Success,
            Error,
            Warning,
            Raw
        }
        public static void WriteConsole(LogType type, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            switch (type)
            {
                case LogType.Info:
                    {
                        Console.Write("[");
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                        Console.Write("Info");
                        Console.ResetColor();
                        Console.WriteLine("] " + text);
                        break;
                    }
                case LogType.Write:
                {
                    Console.Write("[");
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write("Write");
                    Console.ResetColor();
                    Console.WriteLine("] " + text);
                    break;
                }
                case LogType.Move:
                {
                    Console.Write("[");
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write("Move");
                    Console.ResetColor();
                    Console.WriteLine("] " + text);
                    break;
                }
                case LogType.Error:
                    {
                        Console.Write("[");
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("Error");
                        Console.ResetColor();
                        Console.WriteLine("] " + text);
                        break;
                    }
                case LogType.Warning:
                    {
                        Console.Write("[");
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("Warning");
                        Console.ResetColor();
                        Console.WriteLine("] " + text);
                        break;
                    }
                case LogType.Success:
                    {
                        Console.Write("[");
                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("Success");
                        Console.ResetColor();
                        Console.WriteLine("] " + text);
                        break;
                    }
                case LogType.Raw:
                    {
                        Console.WriteLine(text);
                        break;
                    }
            }
        }
    }
}
