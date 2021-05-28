using System;
using System.Threading;

using CommandPlus;
using RemoveQuickEdit;

namespace CommandPlusImplementation
{
    class CmdPlusImplementation
    {
        static void Main()
        {
            #region CmdPlus
            NoQuickEdit.RemoveQuickEdit(); // Remove the click thread block from cmd
            CmdPlus.AutoCompleteInitialize(new string[] { "/help", "/print", "/nice", "/demo", "/exit" }); // Fill the commands List
            CmdPlus.EnterPressed += OnEnterPressed; // Sign the method to the event
            CmdPlus.ChangeDefaultForegroundColor(ConsoleColor.DarkGray); // If you want to change the default foreground color
            CmdPlus.Start("CmdPlus"); // Start the CommandPlus thread
            #endregion

            CmdPlus.WriteLine("This is a example with \"Debug\" type!", CmdPlus.Types.Debug);
            CmdPlus.WriteLine("This is a example with \"Error\" type!", CmdPlus.Types.Error);
            CmdPlus.WriteLine("This is a example with \"Info\" type!", CmdPlus.Types.Info);
            CmdPlus.WriteLine("This is a example with \"Log\" type!", CmdPlus.Types.Log);
            CmdPlus.WriteLine("This is a example with \"Warning\" type!", CmdPlus.Types.Warning);

            // Keep the console app running, zero cost to performance
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            token.WaitHandle.WaitOne();
        }

        public static void OnEnterPressed()
        {
            switch (CmdPlus.CheckLastLine()) // You can call specific methods here
            {
                case "/help":
                    CmdPlus.WriteLine("Asked help!", CmdPlus.Types.Info);
                    break;
                case "/print":
                    CmdPlus.WriteLine("Oh boy! It's printed!", CmdPlus.Types.Warning);
                    break;
                case "/nice":
                    CmdPlus.WriteLine("Nice one!", CmdPlus.Types.Debug);
                    break;
                case "/demo":
                    CmdPlus.WriteLine("This is a example with \"Debug\" type!", CmdPlus.Types.Debug);
                    CmdPlus.WriteLine("This is a example with \"Error\" type!", CmdPlus.Types.Error);
                    CmdPlus.WriteLine("This is a example with \"Info\" type!", CmdPlus.Types.Info);
                    CmdPlus.WriteLine("This is a example with \"Log\" type!", CmdPlus.Types.Log);
                    CmdPlus.WriteLine("This is a example with \"Warning\" type!", CmdPlus.Types.Warning);
                    break;
                case "/exit":
                    CmdPlus.WriteLine("Bye! =D", CmdPlus.Types.Info);
                    Thread.Sleep(1500);
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
