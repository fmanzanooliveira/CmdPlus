using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandPlus
{
    public class CmdPlus
    {
        #region Propertys and Fields
        static SemaphoreSlim SemaphoreSlim { get; set; } = new SemaphoreSlim(1);
        static ConcurrentQueue<CmdMessage> ConcurrentQueueMsgs { get; set; } = new ConcurrentQueue<CmdMessage>();
        static ConcurrentQueue<CmdMessage> ConcurrentQueueTab { get; set; } = new ConcurrentQueue<CmdMessage>();
        static List<ConsoleKeyInfo> KeysBuffer { get; set; } = new List<ConsoleKeyInfo>();
        static string LastBuffer { get; set; } = "";
        static string LastPrinted { get; set; } = "";
        static ConsoleColor DefaultForegroundColor { get; set; } = ConsoleColor.DarkGray;
        static List<string> Commands { get; set; } = new List<string>();
        static int TabsPressed { get; set; } = -1;
        static List<string> TabResults { get; set; }
        static string ApplicationName { get; set; }
        public enum Types { Log, Info, Warning, Error, Debug, DefaultColor };
        public delegate void KeyEnteredEventHandler();
        public static event KeyEnteredEventHandler EnterPressed;
        #endregion Propertys and Fields

        // Handles the read buffer.
        static bool KeyHandle()
        {
            // No use keys

            switch (KeysBuffer.Last().Key)
            {
                case ConsoleKey.Oem7:
                case ConsoleKey.Oem4:
                case ConsoleKey.LeftWindows:
                case ConsoleKey.RightWindows:
                case ConsoleKey.RightArrow:
                case ConsoleKey.LeftArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.Escape:
                case ConsoleKey.Insert:
                case ConsoleKey.Delete:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                case ConsoleKey.F1:
                case ConsoleKey.F2:
                case ConsoleKey.F3:
                case ConsoleKey.F4:
                case ConsoleKey.F5:
                case ConsoleKey.F6:
                case ConsoleKey.F7:
                case ConsoleKey.F8:
                case ConsoleKey.F9:
                case ConsoleKey.F10:
                case ConsoleKey.F12:
                    KeysBuffer.Remove(KeysBuffer.Last());
                    return true;
            }

            // Function keys

            switch (KeysBuffer.Last().Key)
            {
                case ConsoleKey.Tab:
                    KeysBuffer.Remove(KeysBuffer.Last());
                    TabScroller();
                    return true;
                case ConsoleKey.UpArrow:
                    KeysBuffer.Remove(KeysBuffer.Last());
                    TabWriter(LastBuffer, Types.Log, false, true, true, false, false);
                    SetBuffer(LastBuffer);
                    return true;
                case ConsoleKey.Backspace:
                    if (TabsPressed >= 0) // Tab verification to reset the commands list when the tab key is pressed
                    {
                        TabsPressed = -1;
                    }
                    if (KeysBuffer.Count == 1)
                    {
                        KeysBuffer.Remove(KeysBuffer.Last());
                        return true;
                    }
                    KeysBuffer.RemoveAt(KeysBuffer.Count - 1);
                    KeysBuffer.RemoveAt(KeysBuffer.Count - 1);
                    Console.Write("\b" + " " + "\b"); // Delete the last key printed
                    return true;
                case ConsoleKey.Enter:
                    if (GetBuffer(false) == "") // If there is no buffer, clear the line above to avoid some bugs and dont save the empty line as a last command sended
                    {
                        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r"); // Return to initial cursor position
                        WriteLine(GetBuffer(false), Types.DefaultColor);
                        KeysBuffer.Remove(KeysBuffer.Last());
                        EnterPressed();
                        KeysBuffer.Clear();
                        return true;
                    }
                    if (TabsPressed >= 0) // Tab verification to reset the commands list when the tab key is pressed
                    {
                        TabsPressed = -1;
                    }
                    KeysBuffer.Remove(KeysBuffer.Last());
                    LastBuffer = GetBuffer(false);
                    EnterPressed();
                    KeysBuffer.Clear();
                    WriteLine(LastBuffer, Types.DefaultColor);
                    return true;
            }

            return false;
        }

        // Writes a string with the given string, type and parameters. Used when the CmdPlus needs to write a message in the current line or in the line above
        static void TabWriter(string msgToWrite, Types type, bool newLine, bool clearCurrentLine, bool cmd, bool saveLastLine, bool overrideLastLine)
        {
            ConcurrentQueueTab.Enqueue(new CmdMessage(msgToWrite, type));

            Task.Run(() =>
            {
                SemaphoreSlim.Wait();

                CmdMessage item;
                while (ConcurrentQueueTab.TryDequeue(out item))
                {
                    if (saveLastLine)
                    {
                        LastBuffer = msgToWrite;
                    }

                    if (overrideLastLine)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1); // Set the cursor to the line above at the first position
                    }

                    if (clearCurrentLine)
                    {
                        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r"); // Return to initial cursor position
                    }
                    if (cmd)
                    {
                        Console.Write(ApplicationName + ": ");
                    }

                    TabOrWriter(item.Type, "tab", item.Msg);

                    if (newLine)
                    {
                        Console.WriteLine();
                    }
                }

                SemaphoreSlim.Release();
            });


        }

        // Handle the messages received from the TabWriter or WriterLine methods
        static void TabOrWriter(Types type, string tabOrWriter, string msgToWrite)
        {
            switch (type)
            {
                case Types.Log:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case Types.Info:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case Types.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case Types.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case Types.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case Types.DefaultColor:
                    break;
            }

            if (tabOrWriter == "tab") Console.Write(msgToWrite);
            if (tabOrWriter == "writer") Console.Write(msgToWrite);
            Console.ForegroundColor = DefaultForegroundColor;
        }

        // Gets current line buffer as string
        static string GetBuffer(bool clearBuffer)
        {
            string buffer = "";
            if (!(KeysBuffer.Count == 0))
            {
                //Console.WriteLine("KeyBuffer: " + KeysBuffer.Count);
                for (int i = 0; i < KeysBuffer.Count; i++)
                {
                    if (KeysBuffer[i].Key == ConsoleKey.Enter)
                    {
                        continue;
                    }
                    buffer = buffer + KeysBuffer[i].KeyChar;
                }
            }
            if (KeysBuffer.Count == 1) // To check if the last key was an Enter key
            {
                if (KeysBuffer.Last().Key == ConsoleKey.Enter)
                {
                    buffer = "";
                }
                
            }
            if (clearBuffer)
            {
                KeysBuffer.Clear();
            }
            return buffer;
        }

        // Sets the new buffer
        static void SetBuffer(string newBuffer)
        {
            KeysBuffer.Clear();
            byte[] newBufferB = Encoding.Unicode.GetBytes(newBuffer);
            char[] newBufferC = Encoding.Unicode.GetChars(newBufferB);
            for (int i = 0; i < newBufferC.Length; i++)
            {
                KeysBuffer.Add(new ConsoleKeyInfo(newBufferC[i], ConsoleKey.Process, false, false, false));
            }
        }

        // Handles the AutoComplete function
        static void TabScroller()
        {
            if (TabsPressed < 0)
            {
                TabResults = new List<string>();
                string lowerLine = GetBuffer(false);

                if (KeysBuffer.Count == 0) // If there is no key buffer sets the string to find all the commands to be showed
                {
                    lowerLine = "/";
                }
                TabResults = Commands.FindAll(_string => _string.Contains(lowerLine));
                TabsPressed = 0;
            }
            if (TabResults.Count == 0)
            {
                return;
            }
            if (TabsPressed == TabResults.Count)
            {
                TabsPressed = 0;
            }

            SetBuffer(TabResults[TabsPressed]);

            TabWriter(GetBuffer(false), Types.DefaultColor, false, true, true, false, false);

            TabsPressed += 1;
        }

        // Public Methods

        /// <summary>
        /// Reads the input, print and store in a key buffer.
        /// </summary>
        /// <param name="applicationName">The name of the application.</param>
        public static async void Start(string applicationName)
        {
            ApplicationName = applicationName; // Set the name of the application
            Console.ForegroundColor = DefaultForegroundColor;
            Console.Write(applicationName + ": ");
            while (true) // Keep the reading of the keys and handle them
            {
                await Task.Run(() => { KeysBuffer.Add(Console.ReadKey(true)); });
                if (!KeyHandle())
                {
                    Console.Write(KeysBuffer.Last().KeyChar);
                }
            }
        }

        /// <summary>
        /// Writes a string with the given string and type, then start a new line.
        /// </summary>
        /// <param name="msgToWrite">The string to be written.</param>
        /// <param name="type">The type of writing.</param>
        public static void WriteLine(string msgToWrite, Types type)
        {
            ConcurrentQueueMsgs.Enqueue(new CmdMessage(msgToWrite, type));

            Task.Run(() =>
            {
                SemaphoreSlim.Wait();

                CmdMessage item;
                while (ConcurrentQueueMsgs.TryDequeue(out item))
                {
                    int result = 0;

                    if ((ApplicationName.Length + 2 + msgToWrite.Length) > (Console.WindowWidth - 1))
                    {
                        result = (ApplicationName.Length + 2 + msgToWrite.Length) / (Console.WindowWidth - 1);
                    }

                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - result);

                    Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r"); // Return to initial cursor position
                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);

                    TabOrWriter(item.Type, "writer", item.Msg);
                    LastPrinted = item.Msg;

                    while (Console.CursorLeft + 1 < Console.WindowWidth)
                    {
                        Console.Write(' '); // Fill the remaining line space
                    }

                    Console.Write(' '); // Fill the remaining line space

                    Console.Write(ApplicationName + ": " + GetBuffer(false));
                }

                SemaphoreSlim.Release();
            });


        }

        /// <summary>
        /// Initialize the List with the values from stringCommands.
        /// </summary>
        /// /// <param name="cmds">The array of commands to be configured.</param>
        public static void AutoCompleteInitialize(string[] cmds)
        {
            Commands.Clear();
            for (int i = 0; i < cmds.Length; i++)
            {
                Commands.Add(cmds[i]);
            }
        }

        /// <summary>
        /// Set the default foreground color.
        /// </summary>
        /// <param name="color">The console color.</param>
        public static void ChangeDefaultForegroundColor(ConsoleColor color)
        {
            DefaultForegroundColor = color;
        }

        /// <summary>
        /// Return the last line written if it's a command.
        /// </summary>
        public static string CheckLastLine()
        {
            foreach (var item in Commands)
            {
                if (GetBuffer(false) == item) // Check if the buffer has any of the commands configured
                {
                    string lastBufferToSend = GetBuffer(true); // Get the current buffer and remove from the CmdPlus buffer
                    return lastBufferToSend;
                }
            }

            return null;
        }

        /// <summary>
        /// Return the last line printed.
        /// </summary>
        public static string GetLastLinePrinted()
        {
            return LastPrinted;
        }

        /// <summary>
        /// Return true if there is a key on the buffer.
        /// </summary>
        public static bool CheckBuffer()
        {
            if (GetBuffer(false).Length > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // A class to organize the messages to be printed
    class CmdMessage
    {
        public string Msg { get; set; }
        public CmdPlus.Types Type { get; set; }

        public CmdMessage(string msgToWrite, CmdPlus.Types type)
        {
            Msg = msgToWrite;
            Type = type;
        }
    }
}
