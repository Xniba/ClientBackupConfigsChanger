using System.Diagnostics;
using System.IO.Compression;

namespace ClientBackupConfigsChanger
{
    internal class Program
    {
        static void CreateNewDirectory(string newDirectoryPath)
        {
            // 1.1. Try create new directory in location 
            try
            {
                // Check if directory exist
                if (Directory.Exists(newDirectoryPath))
                {
                    // If Yes, delet it (with files in) and create new 
                    Directory.Delete(newDirectoryPath, true); //true, give permision to delete all content in directory
                    Directory.CreateDirectory(newDirectoryPath);
                }
                else
                {   // If No, create new 
                    Directory.CreateDirectory(newDirectoryPath);
                }
            }
            // 1.2. Information, then end .exe
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!Can't delete or create new directory");
                Console.WriteLine($"Close all files from directory: {newDirectoryPath}");
                Console.WriteLine($"Or give permission to write in: {newDirectoryPath.Substring(0, 35 - 10)}");
                CloseApp();
            }
        }
        static string[] ReadAndSort(string filePath, int[] linesToRead)
        {
            // 0. Parameters
            //LinesToRead: { 8, 10, 21, 1168, 1791, 1805, 4136, 7535 }
            string[] readedLines = new string[linesToRead.Length];

            string[] value = new string[9];
            int start, stop;

            // 1. Read all fille and find statements
            using (StreamReader sr = new StreamReader(filePath))
            {
                int lineNumber = 0;
                int tableCounter = 0;
                string lineText;

                //reading all lines one by one and compare with conditions
                while ((lineText = sr.ReadLine()) != null)
                {
                    lineNumber++;

                    if (lineNumber == linesToRead[tableCounter])//Condition changed if statements is true
                    {
                        readedLines[tableCounter] = lineText;
                        tableCounter++;
                    }

                    if (tableCounter == linesToRead.Length)  // Upewnienie się, że tableCounter nie przekracza długości tablicy
                    {
                        break;
                    }
                }
            }

            // 2. Preaper strings
            // 2.1.
            //System Name=CA0001
            start = readedLines[0].IndexOf("=") + 1;
            value[0] = readedLines[0].Substring(start, 4);                      //CA00
            value[1] = readedLines[0].Substring(readedLines[0].Length - 2, 2);  //01
            // 2.2.
            //Ip Address (In-/Out-Band)=192.168.32.201/0.0.0.0
            start = readedLines[1].IndexOf("=") + 1;
            stop = readedLines[1].IndexOf("/0");
            value[2] = readedLines[1].Substring(start, stop - start);   //192.168.32.201

            // 2.3.
            //END OF HEADER - Checksum:24f2
            start = readedLines[2].IndexOf(":") + 1;
            stop = readedLines[2].Length - start;
            value[3] = readedLines[2].Substring(start, stop);           //24f2

            // 2.4.
            //END OF FILE - Checksum:fb7a
            start = readedLines[7].IndexOf(":") + 1;
            stop = readedLines[7].Length - start;
            value[4] = readedLines[7].Substring(start, stop);           //24f2
            // 2.5.
            //dividing IP into octets
            value[8] = value[2];
            for (int i = 0; i <= 3; i++)
            {
                int j = i + 5;
                if (i == 3)
                {
                    value[j] = value[5];
                }
                else
                {
                    value[j] = value[8].Substring(0, value[8].IndexOf("."));
                    value[8] = value[8].Substring(value[8].IndexOf(".") + 1);
                }
            }

            /*
            //value[0] = "CA00";
            //value[1] = "01";
            //value[2] = "192.168.32.201";
            //value[3] = "24f2";
            //value[4] = "24f2";
            //value[5] = "192";
            //value[6] = "168";
            //value[7] = "31";
            //value[8] = "201";
            */

            return value;
        }
        static void ChangingMachineUp(string newDirectoryPathUnzip, string newDirectoryPath, string configPath, string[] value, int[] lineToEdit, int amountOfCarrier)
        {
            /* Arguments:
             * string newDirectoryPathUnzip - path from create ZIP file
             * string newDirectoryPath - path to place created ZIP file
             * string configPath - path to reded config file
             * string[] value - reded value from file
             * int[] lineToEdit - number lines to edit
             * int amountOfCarrier - number of carrier, determinate amount of loops
             * 
             * value[0] = "CA00";
             * value[1] = "01";
             * value[2] = "192.168.32.201";
             * value[3] = "24f2";
             * value[4] = "24f2";
             * value[5] = "192";
             * value[6] = "168";
             * value[7] = "31";
             * value[8] = "201";
            *
            * linesToEdit: { 8, 10, 21, 1168, 1791, 1805, 4136, 7535 }
            */

            // 0. Parameters
            string[] readyData = new string[lineToEdit.Count()];

            int nameNumber = Int32.Parse(value[1]);                                           //name
            int ip3Octet = Int32.Parse(value[7]);                                             //4'th octet
            int ip4Octet = Int32.Parse(value[8]);                                             //4'th octet
            int decHead = Int32.Parse(value[3], System.Globalization.NumberStyles.HexNumber); //hex na dec
            int decBody = Int32.Parse(value[4], System.Globalization.NumberStyles.HexNumber); //hex na dec
            string nameFile;

            // 1. Read all lines and save to string[]
            string[] arrLine = File.ReadAllLines(configPath);

            // 2. Loop for change value and save file as a ZIP
            for (int i = 1; i <= amountOfCarrier; i++)
            {
                // 2.1. Changing IP
                if (ip4Octet == 255)
                {
                    ip3Octet += 1;
                    ip4Octet = 1;
                }
                else
                {
                    ip4Octet -= 1;
                }
                ip4Octet += 1;

                // 2.2. Changing Name
                nameNumber += 1;
                if (nameNumber < 10)
                {
                    nameFile = value[0] + "0" + nameNumber;
                }
                else
                {
                    nameFile = value[0] + nameNumber;
                }

                // 2.3. Changing checksum for head and body
                // 2.3.1. Modify checksum cause Name changed
                if (nameNumber % 10 == 0) //if changing from 9 to 10, or 19 to 20 etc.
                {
                    decHead += 2303; //head +2303
                    decBody += 2303; //body +2303
                }
                else
                {
                    decHead -= 256; //head -256
                    decBody -= 256; //body -256
                }

                // 2.3.2. Modify checksum cause IP changed
                if (ip4Octet % 100 == 0) //if changing from 199 to 200, etc.
                {
                    decHead += 2057; //head +2057
                    decBody += 4369;  //body +4369
                }
                else if (ip4Octet % 10 == 0) //if changing from 209 to 210, etc.
                {
                    decHead += 2303; //head +2303
                    decBody += 2056; //body +2056
                }
                else
                {
                    decHead -= 256; //head -256
                    decBody -= 257; //body -257
                }

                value[3] = (decHead.ToString("X")).ToLower(); //dec na hex(string)
                value[4] = (decBody.ToString("X")).ToLower(); //dec na hex(string)

                // 2.4. Prepare lines to change in file
                value[5] = $"{value[5]}.{value[6]}.{value[7]}.";                            //Preperate 192.168.31.
                readyData[0] = $"System Name={nameFile}";                                   //System Name=CA0001
                readyData[1] = $"Ip Address (In-/Out-Band)={value[5]}{ip4Octet}/0.0.0.0";   //Ip Address (In-/Out-Band)=192.168.31.191/0.0.0.0
                readyData[2] = $"END OF HEADER - Checksum:{value[3]}";                      //END OF HEADER - Checksum:d7ee
                readyData[3] = $"Value={nameFile}";                                         //Value=CA0001
                readyData[4] = $"Value={value[5]}{ip4Octet}";                               //Value=192.168.31.191
                readyData[5] = $"Value={value[5]}255";                                      //Value=192.168.31.255
                readyData[6] = $"Value={value[5]}{ip4Octet}";                               //Value=192.168.31.191
                readyData[7] = $"END OF FILE - Checksum:{value[4]}";                        //END OF FILE - Checksum:5fe4

                // 2.5. Changing lines in array
                for (int j = 0; j <= 7; j++)
                {
                    arrLine[lineToEdit[j] - 1] = readyData[j];
                }

                // 2.6 Write file from array with changed lines
                using (StreamWriter sw = new StreamWriter(configPath))
                {
                    for (int j = 0; j < 7534; j++)
                    {
                        sw.Write(arrLine[j] + "\n");
                    }
                    sw.Write(arrLine[7534]);
                }

                // 2.7.Save cahnged file and zip it with new name to director
                ZipFile.CreateFromDirectory(newDirectoryPathUnzip, newDirectoryPath + $@"\configpack_SCALANCE_W700_{nameFile}.zip");

                // 2.8. Info: witch carrier was created 
                Console.WriteLine($"Config for carrier {nameFile} saved");
            }

            return;
        }
        static void ChangingMachineDown(string newDirectoryPathUnzip, string newDirectoryPath, string configPath, string[] value, int[] lineToEdit, int amountOfCarrier)
        {

            // 0. Parameters
            string[] readyData = new string[lineToEdit.Length];                                //array

            int nameNumber = Int32.Parse(value[1]);                                             //name
            int ip3Octet = Int32.Parse(value[7]);                                               //3'th octet
            int ip4Octet = Int32.Parse(value[8]);                                               //4'th octet
            int decHead = Int32.Parse(value[3], System.Globalization.NumberStyles.HexNumber);   //hex na dec
            int decBody = Int32.Parse(value[4], System.Globalization.NumberStyles.HexNumber);   //hex na dec
            string nameFile;                                                                    //name

            // 0.1. Fast Exit
            if (amountOfCarrier == 1)
            {
                return;
            }
            if (ip4Octet == 0)
            {
                Console.WriteLine("Ip adress corrupted, check it bro");
                return;
            }

            // 1. Read all lines and save to string[]
            string[] arrLine = File.ReadAllLines(configPath);

            // 2. Loop for change value and save file as a ZIP
            amountOfCarrier -= 1;   //-1 for calculate to 0001
            for (int i = 1; i <= amountOfCarrier; i++)
            {
                // 2.1. Changing IP
                if (ip4Octet == 1)
                {
                    ip3Octet -= 1;
                    ip4Octet = 255;
                }
                else
                {
                    ip4Octet -= 1;
                }

                // 2.2. Changing Name
                nameNumber -= 1;
                if (nameNumber < 10)
                {
                    nameFile = value[0] + "0" + nameNumber;
                }
                else
                {
                    nameFile = value[0] + nameNumber;
                }

                // 2.3. Changing checksum for head and body
                // 2.3.1. Modify checksum cause Name changed
                if (nameNumber % 10 == 0) //if changing from 9 to 10, or 19 to 20 etc.
                {
                    decHead -= 2303; //head +2303
                    decBody -= 2303; //body +2303
                }
                else
                {
                    decHead += 256; //head -256
                    decBody += 256; //body -256
                }

                // 2.3.2. Modify checksum cause IP changed
                if (ip4Octet % 100 == 0) //if changing from 199 to 200, etc.
                {
                    decHead -= 2057; //head +2057
                    decBody -= 4369;  //body +4369
                }
                else if (ip4Octet % 10 == 0) //if changing from 209 to 210, etc.
                {
                    decHead -= 2303; //head +2303
                    decBody -= 2056; //body +2056
                }
                else
                {
                    decHead += 256; //head -256
                    decBody += 257; //body -257
                }

                value[3] = (decHead.ToString("X")).ToLower(); //dec na hex(string)
                value[4] = (decBody.ToString("X")).ToLower(); //dec na hex(string)

                // 2.4. Prepare lines to change in file
                value[5] = $"{value[5]}.{value[6]}.{value[7]}.";                            //Preperate 192.168.31.
                readyData[0] = $"System Name={nameFile}";                                   //System Name=CA0001
                readyData[1] = $"Ip Address (In-/Out-Band)={value[5]}{ip4Octet}/0.0.0.0";   //Ip Address (In-/Out-Band)=192.168.31.191/0.0.0.0
                readyData[2] = $"END OF HEADER - Checksum:{value[3]}";                      //END OF HEADER - Checksum:d7ee
                readyData[3] = $"Value={nameFile}";                                         //Value=CA0001
                readyData[4] = $"Value={value[5]}{ip4Octet}";                               //Value=192.168.31.191
                readyData[5] = $"Value={value[5]}255";                                      //Value=192.168.31.255
                readyData[6] = $"Value={value[5]}{ip4Octet}";                               //Value=192.168.31.191
                readyData[7] = $"END OF FILE - Checksum:{value[4]}";                        //END OF FILE - Checksum:5fe4

                // 2.5. Changing lines in array
                for (int j = 0; j <= 7; j++)
                {
                    arrLine[lineToEdit[j] - 1] = readyData[j];
                }

                // 2.6 Write file from array with changed lines
                using (StreamWriter sw = new StreamWriter(configPath))
                {
                    for (int j = 0; j < 7534; j++)
                    {
                        sw.Write(arrLine[j] + "\n");
                    }
                    sw.Write(arrLine[7534]);
                }

                // 2.7.Save cahnged file and zip it with new name to director
                ZipFile.CreateFromDirectory(newDirectoryPathUnzip, newDirectoryPath + $@"\configpack_SCALANCE_W700_{nameFile}.zip");

                // 2.8. Info: witch carrier was created 
                Console.WriteLine($"Config for carrier {nameFile} saved");
            }

            return;
        }
        static string FindFileWithExtension(string directoryPath, string extension)

        {
            try
            {
                return Directory.GetFiles(directoryPath, extension)[0];   //looking for files in directory, return [0] - first file witch extension
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"!Check if you have file with extension {extension} in directory:");
                Console.WriteLine(directoryPath);
                Console.ResetColor();
                CloseApp();
                return "";  //not neccecery but 
            }

        }
        static void CloseApp()
        {
            Console.WriteLine("Press Enter to close app");
            Console.ReadKey();
            Environment.Exit(0);
        }
        static int ReadIntFromKeyboard()
        {
            try
            {
                return Int32.Parse(Console.ReadLine());
            }
            catch
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!Wrong value");
                CloseApp();
            }
            return 0;
        }
        static string GetPathToFilesDirectory()
        {
            string path = new DirectoryInfo(".").FullName;
            int ile = path.IndexOf("bin") - 1;
            if (ile < 0)
            {
                Console.WriteLine("Error in method - GetDirectoryPath()");
                CloseApp();
                return "";
            }
            else
            {
                path = path.Substring(0, ile);
                path = path + @"\Files";
                return path;
            }
        }
        static void GoToTryAgain(string readedFromKeyboard)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!!Somethinks go wrong, you write: {readedFromKeyboard}");
            Console.WriteLine("Press Enter to try again");
            Console.ReadLine();
            Console.ResetColor();
            Console.Clear();

        }
        static int Menu()
        {
            // Parameters
            int option = 0;
            int safeetyLimitLoop = 5;

            // Avilable option
            string[] nameOfOptions =
            {
                "Exit",                         //0
                "Only up",                      //1
                "Only down to carrier 0001",    //2
                "Up and down to 0001"           //3
            };


        TryAgain:
            if (safeetyLimitLoop <= 0)
            {
                Console.WriteLine("Safety Limit Loop");
                CloseApp();
            }
            safeetyLimitLoop--;

            // Display option
            Console.WriteLine("Witch option:\n");
            for (int i = 0; i < nameOfOptions.Length; i++)
            {
                Console.WriteLine($"{i}. {nameOfOptions[i]}");
            }


            // User choice
            Console.WriteLine("\nWrite your choose and press Enter: ");
            Console.Write("Choose: ");

            // Check if could parse
            string readedFromKeyboard = Console.ReadLine();
            Console.WriteLine();
            try
            {
                option = Int32.Parse(readedFromKeyboard);
            }
            catch
            {
                GoToTryAgain(readedFromKeyboard);
                goto TryAgain;
            }

            // Check range
            if ((option < 0) || (option > nameOfOptions.Length - 1)) //pseudo Default
            {
                GoToTryAgain(option.ToString());
                goto TryAgain;
            }
            // Display
            Console.WriteLine($"Chosen option: {nameOfOptions[option]}");
            if (option == 0)
            {
                CloseApp();
            }

            return option;
        }

        // Main porogram
        static void Main(string[] args)
        {
            ///////////////////////Parameters////////////////////////
            int amountOfCarriers;   //Amount of Carriers
            int option = Menu();

            //Paths
            string filesDirectoryPath = GetPathToFilesDirectory();
            string baseDirectoryPath = filesDirectoryPath + @"\BaseFiles";                                      //Path to directory where is base file
            string newDirectoryPath = filesDirectoryPath + @"\NewFiles";                                        //New directory where operation happen and ZIPs will be saved


            string zipBaseFilePath = FindFileWithExtension(baseDirectoryPath, "*.zip");                         //Path to zip, to unpack
            string newDirectoryPathUnzip = newDirectoryPath + @"\ConfigPack";                                   //New Directory to unziped ConifgPack. Additionaly "\ConfigPack" is for correnct hierarchy after ZIP it
            string scalanceConfPath = newDirectoryPathUnzip + @"\ConfigPack\Config\config_SCALANCE_W700.conf";  //Path to conifg file


            //Lines to change in config file 
            int[] linesToRead = { 8, 10, 21, 1168, 1791, 1805, 4136, 7535 }; //8 numbers

            /////////////////////////Program/////////////////////////
            // 1. If directory exist, delete it and create new
            CreateNewDirectory(newDirectoryPath);

            // 2. Unzip conifg, to new directory
            ZipFile.ExtractToDirectory(zipBaseFilePath, newDirectoryPathUnzip);

            // 3. Read lines from config
            string[] sortedValues = ReadAndSort(scalanceConfPath, linesToRead);
            // 4. Create rest zip's with changed values

            switch (option)
            {
                case 1: //only Up
                    // 4.1. Info
                    Console.WriteLine("\nOryginal file: {0}", zipBaseFilePath.Substring(zipBaseFilePath.LastIndexOf(@"\") + 1));
                    Console.WriteLine("Write below how many files create");

                    // 4.2.
                    Console.Write("Amount: ");
                    amountOfCarriers = ReadIntFromKeyboard(); //read value from keyboard
                    Console.WriteLine();

                    Console.WriteLine("Calculating files Up");

                    // 4.3. First zip is saved without changed in new directory
                    ZipFile.CreateFromDirectory(newDirectoryPathUnzip, newDirectoryPath + $@"\configpack_SCALANCE_W700_{sortedValues[0]}{sortedValues[1]}.zip");
                    Console.WriteLine($"Config for carrier {sortedValues[0]}{sortedValues[1]} saved");

                    // 4.4. run machine
                    ChangingMachineUp(newDirectoryPathUnzip, newDirectoryPath, scalanceConfPath, sortedValues, linesToRead, amountOfCarriers);
                    break;

                case 2: //Only Down
                    // 4.1. Info
                    Console.WriteLine("\nOryginal file: {0}", zipBaseFilePath.Substring(zipBaseFilePath.LastIndexOf(@"\") + 1));

                    Console.WriteLine("Calculating files Down");

                    // 4.2. First zip is saved without changed in new directory
                    ZipFile.CreateFromDirectory(newDirectoryPathUnzip, newDirectoryPath + $@"\configpack_SCALANCE_W700_{sortedValues[0]}{sortedValues[1]}.zip");
                    Console.WriteLine($"Config for carrier {sortedValues[0]}{sortedValues[1]} saved");

                    // 4.3.
                    amountOfCarriers = Int32.Parse(sortedValues[1]); //read number of carrier

                    // 4.4. 
                    ChangingMachineDown(newDirectoryPathUnzip, newDirectoryPath, scalanceConfPath, sortedValues, linesToRead, amountOfCarriers);
                    break;

                case 3: //Up and down
                        // 4.1. Info
                    Console.WriteLine("\nOryginal file: {0}", zipBaseFilePath.Substring(zipBaseFilePath.LastIndexOf(@"\") + 1));
                    Console.WriteLine("Write below how many files create");

                    // 4.2.
                    Console.Write("Amount: ");
                    amountOfCarriers = ReadIntFromKeyboard(); //read value from keyboard
                    Console.WriteLine();

                    // 4.3. First zip is saved without changed in new directory
                    ZipFile.CreateFromDirectory(newDirectoryPathUnzip, newDirectoryPath + $@"\configpack_SCALANCE_W700_{sortedValues[0]}{sortedValues[1]}.zip");
                    Console.WriteLine($"Copy of the original file {sortedValues[0]}{sortedValues[1]} saved\n");

                    //
                    Console.WriteLine($"Calculating Up from {sortedValues[0]}{sortedValues[1]}");
                    ChangingMachineUp(newDirectoryPathUnzip, newDirectoryPath, scalanceConfPath, sortedValues, linesToRead, amountOfCarriers);

                    //
                    Console.WriteLine();
                    Console.WriteLine($"Calculating Down from {sortedValues[0]}{sortedValues[1]}");
                    amountOfCarriers = Int32.Parse(sortedValues[1]); //read number of carrier
                    ChangingMachineDown(newDirectoryPathUnzip, newDirectoryPath, scalanceConfPath, sortedValues, linesToRead, amountOfCarriers);
                    break;

                default:
                    Debug.WriteLine("you forgot that you added new conditions");
                    Console.WriteLine("Call your developer to check the switch");
                    CloseApp();

                    break;
            }

            // 5. Delete folder with unziped config to clear hierarchy
            Directory.Delete(newDirectoryPathUnzip, true);

            // 6. Info
            Console.WriteLine($"\nFiles saved in location:");
            Console.WriteLine(newDirectoryPath);
            Console.WriteLine("Press Enter to close window");
            Console.ReadLine();

            return;
        }
    }
}