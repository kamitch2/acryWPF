using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace AcryWPF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FrontMenu : Window
    {

        System.Threading.Thread cuteThread;

        public FrontMenu()
        {
            InitializeComponent();

            VersionStat.Text = "v "+System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //  I want to begin the database prompt as the application launches but allow the window to fully render
            //  To do this I am spawning a thread to go perform the database checking work.
            cuteThread = new System.Threading.Thread(doTheThing);
            cuteThread.IsBackground = true;
            cuteThread.Start();
            usrLock = new SpinLock();
        }


        public string asciFileName = "Symbol.txt";
        public string dataFileKey = "AcryDataFilev0.0";
        public System.Threading.SpinLock usrLock;
        SearchWindow newWin; 


        void doTheThing()
        {
            string rootDir = Directory.GetCurrentDirectory();

            int errCode = boot(rootDir);

            if (errCode < 0)
            {
                shutdown(rootDir);
                return;
            }


            acryWrite("Let's get started!");

            launchSearchWindow();

            this.Dispatcher.BeginInvoke((Action) (() =>
                this.LaunchBttn.IsEnabled = true
                ));
        }

        void shutdown(string rootDir)
        {
            acryWrite("Goodbye!");

            //Console.Clear();

            //  Write the ASCI text
            //writeName(rootDir + @"\" + asciFileName);

            System.Threading.Thread.Sleep(2000);

            this.Dispatcher.BeginInvoke((Action)(() =>
                this.Close()
                ));
        }

        int boot(string rootDir)
        {
            //Console.Title = "Acry - The Acronym Tool";

            acryWrite();

            //  Pause to give the user 2 seconds to read the text
            System.Threading.Thread.Sleep(2000);

            acryWrite("Hi!  Welcome to Acry, the acronym tool.");

            System.Threading.Thread.Sleep(2000);


            acryWrite("...Searching for database file...");

            string dataFileName = "";

            bool validFile = false;

            while (!validFile)
            {
                dataFileName = getFileName();

                if (dataFileName == null)
                {
                    acryWrite("Please select a non-empty file name", true);
                }
                else if (dataFileName.Equals("forcequit"))
                {
                    acryWrite("You've cancelled selecting a file");

                    System.Threading.Thread.Sleep(1500);

                    acryWrite("Acry will now shut down");

                    System.Threading.Thread.Sleep(1500);

                    return -1;
                }
                else if (!File.Exists(dataFileName))
                {
                    acryWrite("Cannot open data file " + dataFileName + ": File does not exist!", true);
                    Properties.Settings.Default.databaseFile = "";
                    Properties.Settings.Default.Save();
                }
                else
                {
                    try
                    {
                        acryWrite("Attempting to open data file " + dataFileName + "...");

                        using (StreamReader sr = File.OpenText(dataFileName))
                        {
                            string firstLine = sr.ReadLine();

                            if (!firstLine.Equals(dataFileKey))
                            {
                                //  The file that the user selected is not a data file that we can use, remove it from the user settings
                                Properties.Settings.Default.databaseFile = "";
                                Properties.Settings.Default.Save();
                                throw new Exception("Invalid data file!  Please select an Acry data file.");
                            }
                            else
                            {
                                acryWrite("Valid database file selected!");
                                validFile = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        acryWrite(ex.Message, true);
                    }
                }
            }

            return 1;

        }

        string getFileName()
        {
            //  Find name of database file
            try
            {
                //string databaseFilePath = (string)ConfigurationManager.AppSettings["databaseFile"];

                string databaseFilePath = Properties.Settings.Default.databaseFile;

                if (databaseFilePath == null || databaseFilePath.Length == 0)
                    throw new Exception("No database file path exists in user settings");

                bool done = false;

                string messageToDisplay = "An Acry database has been used on this user's profile before.  The database was:\n\n" + databaseFilePath + "\n\nWould you like to continue to use that data file?";

                //  We need to ask the user a yes/no question, but we are running on the background worker thread.  
                //  Init the Messagebox with a value that a yes/no could never give
                MessageBoxResult usrRslt = MessageBoxResult.None;

                //  Outermost loop that forces the worker to raise a single request to the UI thread and then wait in a loop
                do
                {
                    bool slaveHasLock = false;
                    
                    //  Tell the program dispatcher to get the main thread to handle the UI dialog
                    this.Dispatcher.BeginInvoke((Action)(() =>
                       {
                           bool lockAcq = false;
                           //   Grab the spinlock
                           usrLock.Enter(ref lockAcq);
                           //   If we have the spinlock, update the value of usrRslt with the response
                           if (lockAcq)
                               usrRslt = System.Windows.MessageBox.Show(messageToDisplay, "Acry Database file found", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                           usrLock.Exit();
                       }
                           ));

                   do
                   {
                       //   After queueing the work for the main thread the worker thread enters here and tries to grab the spinlock
                       usrLock.Enter(ref slaveHasLock);
                       //   If we have the lock (must be checked first before checking value of usrRslt to ensure sync with main thread) then proceed with our logic
                       if (slaveHasLock && usrRslt != MessageBoxResult.None)
                       {  
                           switch (usrRslt)
                           {
                               case System.Windows.MessageBoxResult.Yes:
                                   //   Make sure we release the lock before we return
                                   usrLock.Exit();
                                   return databaseFilePath;
                               case System.Windows.MessageBoxResult.No:
                                   done = true;
                                   break;
                               default:
                                   acryWrite("Invalid response.");
                                   done = true;
                                   break;
                           }
                       }
                       //   Release the lock
                       usrLock.Exit();
                       slaveHasLock = false;
                   } while (!done);
               } while (!done);
                throw new Exception("A new database file must be selected!");
            }
            catch (Exception ex)
            {
                //  For one reason or another we cannot find the database file.  Select a new one.
                acryWrite(ex.Message, true);

                acryWrite("Please select database file");

                //  Set the file name to something impossible
                string toBeOutput = "merp";

                //  Ask the dispatcher to send the main thread to go get the file name
                this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        bool haveLock = false;
                        usrLock.Enter(ref haveLock);
                        if(haveLock)
                            toBeOutput = getNewFilePath();
                        usrLock.Exit();
                    }
                    ));

                bool done = false; 

                //  Send the worker thread into a look of acquiring the lock and checking the output
                do 
                {
                    bool haveLock = false;
                    usrLock.Enter(ref haveLock);
                    if (haveLock && !toBeOutput.Equals("merp"))
                    {
                        usrLock.Exit();
                        done = true;
                    }
                    else
                    {
                        usrLock.Exit();
                        System.Threading.Thread.Sleep(100);
                    }

                } while (!done);

                return toBeOutput;
               
            }
        }

        [STAThread]
        string getNewFilePath()
        {
            OpenFileDialog fd = new OpenFileDialog();

            fd.InitialDirectory = @"C:\";
            fd.Title = "Select database file";

            fd.CheckFileExists = true;
            fd.CheckPathExists = true;

            fd.DefaultExt = "txt";
            fd.Filter = "Text files (*.txt)|*.txt";
            fd.FilterIndex = 2;
            fd.RestoreDirectory = true;

            fd.ReadOnlyChecked = true;
            fd.ShowReadOnly = true;

            DialogResult response = fd.ShowDialog();

            if (response == System.Windows.Forms.DialogResult.OK)
            {
                //  Gather the file path from the OpenFileDialog
                Properties.Settings.Default.databaseFile = fd.FileName;
                Properties.Settings.Default.Save();

                acryWrite("You have selected the file " + fd.FileName);

                return fd.FileName;
            }
            else if (response == System.Windows.Forms.DialogResult.Cancel)
            {
                return "forcequit";
            }

            return null;
        }

        void acryWrite()
        {
            acryWrite(null, null);
        }

        void acryWrite(string text)
        {
            acryWrite(text, null);
        }

        void acryWrite(string text, bool? err)
        {
           // System.Windows.Window mywin = new Window();

            string newText = "";

            if (text != null && text.Length == 0)
            {
                newText = "";
                //Console.WriteLine();
                
            }

            else if (err.HasValue && err.Value)
            {
                newText = "Error: " + text;
                //Console.WriteLine("-------------------------------------");
                //Console.WriteLine("Error: " + text);
                //Console.WriteLine("-------------------------------------");
            }
            else
            {
                newText = text;
            }

            if (newText != null)
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                    this.UpdateStat.Text = newText
                ));
            }

            //int i = 0;

            //Console.WriteLine();
           //Console.WriteLine(text);
            //Console.WriteLine();
        }

        void writeName(string namePath)
        {
            try
            {
                List<string> logo = File.ReadAllLines(namePath).ToList();

                foreach (string line in logo)
                    Console.WriteLine(line);
            }
            catch (Exception ex)
            {
                acryWrite("Hey!  :(  The logo file seems to be missing.  The program will continue to run normally, though.", true);
            }

        }

        private void LaunchBttn_Click(object sender, RoutedEventArgs e)
        {
            launchSearchWindow();
        }

        private void launchSearchWindow()
        {
            try
            {
                this.Hide();

                newWin = new SearchWindow();

                newWin.ShowDialog();

                newWin = null;

                this.Close();
            }
            catch
            {
                try
                {
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.Hide();

                        newWin = new SearchWindow();

                        newWin.ShowDialog();

                        newWin = null;

                        this.Close();
                    }
                    ));
                }
                catch
                {
                    acryWrite("Error while launching search window", true);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cuteThread.Abort();
            cuteThread = null;
        }

    }
}
