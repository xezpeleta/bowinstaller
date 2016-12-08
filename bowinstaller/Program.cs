/*
 *  @name bowinstaller
 *  @author Xabi Ezpeleta <xezpeleta@gmail.com>
 *  @license GPL-3.0
 * 
 */


using System;
using System.Linq;
using CommandLine;
using System.Management.Automation;
using System.Collections.ObjectModel;
using CommandLine.Text;

namespace bowinstaller
{
    class Program
    {
        const string appname = "bowinstaller";
        const string appversion = "0.1-preAlpha";
        const string appauthor = "Xabi Ezpeleta <xezpeleta@gmail.com>";
        const int winvermajor = 10;
        const int winverbuild = 14316; // 2016/April/06 Bash on Ubuntu

        class Options
        {
            [Option('v', "verbose", DefaultValue = false,
                HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Option('u', "user",
                HelpText = "Default user.")]
            public string User { get; set; }

            [Option('p', "password",
                HelpText = "Default user's password")]
            public string Password { get; set; }

            [Option('y', "assumeyes", DefaultValue = false,
                HelpText = "Assume yes. Attention: computer will be restarted")]
            public bool AssumeYes { get; set; }

            [Option('n', "noreboot", DefaultValue = false,
                HelpText = "Do not reboot automatically")]
            public bool NoReboot { get; set; }

            [Option('r', "resume", DefaultValue = false,
                HelpText = "Resume installation after the reboot")]
            public bool Resume { get; set; }

            [Option('s', "postinstall",
                HelpText = "Batch script to run after the installation")]
            public string PostInstall { get; set; }

            [Option('d', "uninstall", DefaultValue = false,
                HelpText = "Uninstall Bash on Windows")]
            public bool Uninstall { get; set; }

            // Apparently CommandLineParser is not case sensitive
            // and I cannot use V for version
            [Option("version", DefaultValue = false,
                HelpText = "Prints version information to standard output")]
            public bool Version { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                var help = new HelpText
                {
                    Heading = new HeadingInfo(appname, appversion),
                    Copyright = new CopyrightInfo(appauthor, 2016),
                    AdditionalNewLineAfterOption = true,
                    AddDashesToOption = true
                };

                if (this.LastParserState?.Errors.Any() == true)
                {
                    var errors = help.RenderParsingErrorsText(this, 2); // indent with two spaces

                    if (!string.IsNullOrEmpty(errors))
                    {
                        help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                        help.AddPreOptionsLine(errors);
                    }
                }

                help.AddPreOptionsLine("GPL-3.0");
                help.AddPreOptionsLine("Usage: bowinstaller.exe <options>");
                help.AddOptions(this);
                return help;
            }

        }

        public static class Utils
        {
            public static bool RunWinBat (string batfile)
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = batfile;

                p.StartInfo = startInfo;
                p.Start();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Exit code: {0}", p.ExitCode);
                    return false;
                }
            }

            public static bool RunWinCmd(string command)
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;

                p.StartInfo = startInfo;
                p.Start();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Exit code: {0}", p.ExitCode);
                    return false;
                }
            }


            public static bool RunPowerShell(string command)
            {
                using (PowerShell PowerShellInstance = PowerShell.Create())
                {
                    PowerShellInstance.AddScript(command);

                    Collection<PSObject> PSOutput = PowerShellInstance.Invoke();

                    if (PowerShellInstance.Streams.Error.Count > 0)
                    {
                        foreach (ErrorRecord error in PowerShellInstance.Streams.Error)
                        {
                            Console.WriteLine("ERROR [RunPowerShell] " + error.Exception.Message);
                        }

                        return false;
                    }

                    return true;
                }
            }
        }

        static int Install(Options options)
        {
            /*
             * Installation 1st step
             */

            var exepath = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
            var exeargs = String.Join(" ", Environment.GetCommandLineArgs().Skip(1));

            Console.WriteLine("\nInstalling Bash on Windows...");

            // Enable Dev Mode
            if (!Utils.RunPowerShell("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock\" /t REG_DWORD /f /v \"AllowDevelopmentWithoutDevLicense\" /d \"1\""))
            {
                Console.WriteLine("ERROR: Cannot enable Developer Mode");
                return 1;
            }
            else
            {
                Console.WriteLine("[OK] Enable Developer Mode");
            }

            // Enable WSL
            if (!Utils.RunPowerShell("Enable-WindowsOptionalFeature -Online -NoRestart -FeatureName Microsoft-Windows-Subsystem-Linux"))
            //if (! Utils.RunWinCmd("@powershell Enable-WindowsOptionalFeature -Online -NoRestart -FeatureName Microsoft-Windows-Subsystem-Linux"))
            {
                Console.WriteLine("ERROR: Cannot enable Microsoft WSL");
                return 1;
            }
            else
            {
                Console.WriteLine("[OK] Enable WSL");
            }

            // Continue installation after reboot
            // Add reg RunOnce bowInstaller.exe --resume
            if (!Utils.RunPowerShell("set-itemproperty \"HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\\" \"NextRun\" ('" + exepath + " " + exeargs + " --resume')"))
            {
                Console.WriteLine("ERROR: Cannot add bowInstaller to runOnce registry");
                return 1;
            }
            else
            {
                Console.WriteLine("[OK] Configure reg to continue after rebooting");
            }

            // Reboot computer
            if (!options.NoReboot)
            {
                if (!options.AssumeYes)
                {
                    ConsoleKeyInfo cki;
                    Console.WriteLine("The computer will be rebooted.");
                    Console.Write("Press <y> to continue, <n> to abort: ");
                    do
                    {
                        cki = Console.ReadKey();
                    } while (cki.Key != ConsoleKey.Y && cki.Key != ConsoleKey.N);

                    if (cki.Key == ConsoleKey.N)
                    {
                        return 0;
                    }
                }

                if (!Utils.RunWinCmd("shutdown -r -f -t 0"))
                {
                    Console.WriteLine("ERROR: Cannot restart the computer");
                    return 1;
                }
                else
                {
                    Console.WriteLine("Rebooting the computer");
                }
            }

            if (!options.AssumeYes)
            {
                // Keep the console window open
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            return 0;
        }

        static int Resume(Options options)
        {
            /*
             *  Installation 2nd step
             */

            Console.WriteLine("Installing Bash on Windows. Please, do not close this window...");

            // Install Bash
            if (!Utils.RunWinCmd("lxrun /install /y"))
            {
                Console.WriteLine("ERROR: Cannot install Bash");
                return 1;
            }
            else
            {
                Console.WriteLine("[OK] Bash installed");
            }

            // Create user
            if (options.User != null)
            {
                if (!Utils.RunWinCmd("bash -c \"useradd " + options.User + " -G adm,cdrom,sudo,dip,plugdev -s /bin/bash\""))
                {
                    Console.WriteLine("ERROR: Cannot create user " + options.User);
                    return 1;
                }
                else
                {
                    Console.WriteLine("[OK] User " + options.User + " created");
                }
            }

            // Set password
            if (options.Password != null)
            {
                if (!Utils.RunWinCmd("bash -c \"echo '" + options.User + ":" + options.Password + "' | chpasswd\""))
                {
                    Console.WriteLine("ERROR: Cannot set password to user " + options.User);
                    return 1;
                }
                else
                {
                    Console.WriteLine("[OK] Set password");
                }
            }

            // Make 'user' the default user
            if (options.User != null)
            {
                if (!Utils.RunWinCmd("lxrun /setdefaultuser " + options.User + " /y"))
                {
                    Console.WriteLine("ERROR: Cannot make " + options.User + " default user");
                    return 1;
                }
                else
                {
                    Console.WriteLine("[OK] Make " + options.User + " default user");
                }
            }


            // PostInstall script
            if (options.PostInstall != null)
            {
                if (options.PostInstall.Length != 0)
                {
                    if (!Utils.RunWinBat(options.PostInstall))
                    {
                        Console.WriteLine("ERROR: Cannot run PostInstall script");
                        return 1;
                    }
                    else
                    {
                        Console.WriteLine("[OK] Run PostInstall script");
                    }
                }
            }

            // Installation finished succesfully
            Console.WriteLine("Installation finished succesfully!");

            if (!options.AssumeYes)
            {
                // Keep the console window open
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            return 0;
        }

        static int Uninstall(Options options)
        {
            Console.WriteLine("\nUninstaliling Bash...");

            if (!Utils.RunWinCmd("lxrun /uninstall /full /y"))
            {
                Console.WriteLine("ERROR: Cannot uninstall Bash");
                return 1;
            }
            else
            {
                Console.WriteLine("[OK] Bash uninstalled succesfully");
                return 0;
            }

            // TODO (optional): Disable Dev mode etc...
        }

        static int Main(string[] args)
        {
            var exepath = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
            var exeargs = String.Join(" ", Environment.GetCommandLineArgs().Skip(1));
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Debug paths and params
                if (options.Verbose)
                {
                    Console.WriteLine("Path: {0}", exepath);
                    Console.WriteLine("Args: {0}", String.Join(" ", Environment.GetCommandLineArgs()));
                    Console.WriteLine("User: {0}", options.User);
                    Console.WriteLine("Password: {0}", options.Password);
                    Console.WriteLine("Assumeyes: {0}", options.AssumeYes);
                    Console.WriteLine("NoReboot: {0}", options.NoReboot);
                    Console.WriteLine("Resume: {0}", options.Resume);
                    Console.WriteLine("PostInstall: {0}", options.PostInstall);
                    Console.WriteLine("Uninstall: {0}", options.Uninstall);
                }
                

                if (options.Version)
                {
                    Console.WriteLine(appversion);
                    return 0;
                }

                if (options.Uninstall)
                {
                    if (! options.AssumeYes)
                    {
                        ConsoleKeyInfo cki;
                        Console.WriteLine("This will uninstall Bash on Windows. Do you want to continue?");
                        Console.Write("Press <y> to continue, <n> to abort: ");
                        do
                        {
                            cki = Console.ReadKey();
                        } while (cki.Key != ConsoleKey.Y && cki.Key != ConsoleKey.N);

                        if (cki.Key == ConsoleKey.N)
                        {
                            return 0;
                        }
                    }
                    return Uninstall(options);
                }
                else if (options.Resume)
                {
                    return Resume(options);

                }
                else
                {
                    if (!options.AssumeYes)
                    {
                        // Check Windows version > 10.0.14316
                        if (Environment.OSVersion.Version.Major != 10 &&
                            Environment.OSVersion.Version.Build < 14316)
                        {
                            Console.WriteLine("[ERROR] Windows 10 Build 14316 or higher required");
                            Console.WriteLine("Update your Windows: https://www.microsoft.com/software-download/windows10");
                            return 1;
                        }

                        ConsoleKeyInfo cki;
                        Console.WriteLine("This will install Bash on Windows. Do you want to continue?");
                        Console.Write("Press <y> to continue, <n> to abort: ");
                        do
                        {
                            cki = Console.ReadKey();
                        } while (cki.Key != ConsoleKey.Y && cki.Key != ConsoleKey.N);

                        if (cki.Key == ConsoleKey.N)
                        {
                            return 0;
                        }
                    }
                    return Install(options);
                }
            }
            else
            {
                // Incorrect args
                return 1;
            }

           
        }
    }
}
