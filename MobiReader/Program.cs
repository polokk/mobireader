using System;
using System.Collections.Generic;
using System.Text;

namespace MobiReader
{
    /// <summary>
    /// Program Class Object
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main Program procedure
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            MobiFile mf = null;
            try
            {
                mf = MobiFile.LoadFile(args[0]);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Error reading file");
                Console.Out.WriteLine(e.Message);
                return;
            }

            ConsoleReader cr = new ConsoleReader(mf);
            Console.Out.Write(cr.GetLines());
            ConsoleKeyInfo cki = Console.ReadKey();
            while (cki.Key != ConsoleKey.Escape)
            {
                if ((cki.Key == ConsoleKey.LeftArrow) ||
                    (cki.Key == ConsoleKey.UpArrow) ||
                    (cki.Key == ConsoleKey.PageUp))
                {
                    cr.PageBack();
                    ClearConsole();
                    Console.Out.Write(cr.GetLines());
                }
                else if ((cki.Key == ConsoleKey.RightArrow) ||
                         (cki.Key == ConsoleKey.DownArrow) ||
                         (cki.Key == ConsoleKey.PageDown))
                {
                    cr.PageForward();
                    ClearConsole();
                    Console.Out.Write(cr.GetLines());
                }
                else if (cki.Key == ConsoleKey.F && cki.Modifiers == ConsoleModifiers.Control)
                {

                    Console.Out.WriteLine("Enter Search String: ");
                    String searchstring = Console.ReadLine();
                    if (cr.Find(searchstring))
                    {
                        Console.Out.Write("Couldn't Find Value");
                    }
                    ClearConsole();
                    Console.Out.Write(cr.GetLines());
                }
                cki = Console.ReadKey();
            }
            cr.SavePosition();
        }

        /// <summary>
        /// Clears the screen
        /// </summary>
        public static void ClearConsole()
        {
            for (int i = 0; i < 25; i++)
            {
                Console.Out.WriteLine("");
            }
        }
    }
}
