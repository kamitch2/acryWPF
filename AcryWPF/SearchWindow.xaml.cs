using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace AcryWPF
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private SpinLock usrLock;
        private string userInput = "KyleWroteThis";
        private System.Threading.Thread cuteThread;
        private Dictionary<string, List<string>> dataDict;

        public SearchWindow()
        {
            InitializeComponent();

            FocusManager.SetFocusedElement(this, UsrInptTxtBox);

            Keyboard.Focus(UsrInptTxtBox);

            cuteThread = new System.Threading.Thread(doStuff);
            cuteThread.IsBackground = true;
            cuteThread.Start();
            usrLock = new SpinLock();
        }

        public void doStuff()
        {
            string rootDir = Directory.GetCurrentDirectory();

            string databaseFile = Properties.Settings.Default.databaseFile;

            dataDict = new Dictionary<string, List<string>>();

            acryWrite("Attempting to import database...");

            this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    UsrInptTxtBox.IsEnabled = false;
                    SearchBttn.IsEnabled = false;
                }
                ));
                

            int errCode = ingestData(databaseFile, dataDict);

            if (errCode < 0)
            {
                acryWrite("An error occurred while importing the data", true);
                shutdown(rootDir);
                return;
            }

            acryWrite("Database import succeeded!  You may run your query.");

            this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    UsrInptTxtBox.IsEnabled = true;
                    SearchBttn.IsEnabled = true;
                }
                ));

            System.Threading.Thread.Sleep(1500);

            acryWrite();

            errCode = runSearch(dataDict);

            dataDict.Clear();

            dataDict = null;

            if (errCode == 1)
            {
                shutdown(rootDir);
                return;
            }
        }

        int ingestData(string inputFile, Dictionary<string, List<string>> dict)
        {
            string dataStartLine = "<Data>";

            try
            {
                using (StreamReader sr = File.OpenText(inputFile))
                {
                    //  Skip any header in the file
                    while (!sr.ReadLine().Equals(dataStartLine)) ;

                    bool importFinished = false;

                    string expandedVersion = "";

                    while (!importFinished)
                    {
                        string newKey = sr.ReadLine();

                        if (newKey == null || newKey.Length == 0)
                        {
                            importFinished = true;
                        }
                        else
                        {
                            if (dict.ContainsKey(newKey.ToLower()))
                            {
                                expandedVersion = sr.ReadLine();

                                if (expandedVersion.Contains(';'))
                                {
                                    foreach (string addMe in expandedVersion.Split(';').ToList())
                                    {
                                        dict[newKey.ToLower()].Add(addMe);
                                    }
                                }
                                else
                                {
                                    dict[newKey.ToLower()].Add(expandedVersion);
                                }
                            }
                            else
                            {
                                List<string> newList = new List<string>();

                                string newSet = sr.ReadLine();

                                if(newSet.Contains(';'))
                                {
                                    newList = newSet.Split(';').ToList();
                                }
                                else
                                {
                                    newList.Add(newSet);
                                }

                                dict.Add(newKey.ToLower(), newList);
                            }
                        }
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                acryWrite(ex.Message, true);
                return -1;
            }
        }

        int runSearch(Dictionary<string, List<string>> dict)
        {
            string prevInput = userInput;

            bool haveLock = false;

            string searchTemplate = "" +
                "Search results for: {0}\n" +
                "{1}";


            while (true)
            {
                System.Threading.Thread.Sleep(100);

                haveLock = false;

                usrLock.Enter(ref haveLock);

                if (haveLock && !prevInput.Equals(userInput))
                {
                    prevInput = userInput;

                    acryWrite();

                    string searchOption;

                    if(userInput != null) searchOption = userInput.ToLower();
                    else searchOption = "";

                    switch(searchOption)
                    {
                        case "exit":
                            usrLock.Exit();
                            return 1;
                            break;
                        case "acryadmin":
                            usrLock.Exit();
                            this.Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    UpdateDatabaseWindow newWin = new UpdateDatabaseWindow();
                                    this.Hide();
                                    newWin.ShowDialog();
                                    this.Show();
                                    newWin = null;
                                    this.UsrInptTxtBox.Text = "";
                                }
                            ));
                            break;
                        case "kamitch2":
                            usrLock.Exit();
                            acryWrite("Hey there, Kyle.");
                            break;
                        default:
                            if(dict.ContainsKey(searchOption))
                            {
                                string listItems = "";
                                int ansNum = 1;
                                int charCount = 0;

                                foreach (string item in dict[searchOption])
                                {
                                    listItems = listItems + "(" + ansNum.ToString() + ") " + item + "\n";
                                    charCount += item.Length;
                                    ansNum++;
                                }

                                string output = String.Format(searchTemplate, userInput, listItems);

                                acryWrite(output);
                            }
                            else
                            {
                                acryWrite("Acronym '" + userInput + "' not found.");
                            }
                            usrLock.Exit();
                            break;
                    }   
                }
                else if (haveLock)
                    usrLock.Exit();
            }
        }

        void shutdown(string rootDir)
        {
            acryWrite("Goodbye!");

            System.Threading.Thread.Sleep(2000);

            this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.Close();   
                }
                ));
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

            if ((text != null && text.Length == 0) || text == null)
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
                    {
                        this.SrchRsltsBlk.Text = newText;
                        this.ScrollViewerHelper.ClipToBounds = true;
                    }

                ));
            }

        }

        public void SearchBttn_Click(object sender, RoutedEventArgs e)
        {
            bool changed = false;
            bool haveLock = false;
            do
            {
                haveLock = false;

                usrLock.Enter(ref haveLock);
                if(haveLock)
                {
                    userInput = this.UsrInptTxtBox.Text;
                    changed = true;
                }
                usrLock.Exit();

            }while(!changed);


        }

        private void UsrInptTxtBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            
            SearchBttn_Click(sender, e);
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(dataDict != null)
            {
                dataDict.Clear();
                dataDict = null;
            }

            cuteThread.Abort();
            cuteThread = null;
        }

        private void AddTxtBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            acryWrite("Did you add an acronym?  Thanks!");

            string dbFile = Properties.Settings.Default.databaseFile;

            string dbFolder = "";

            for(int i = 0; i < dbFile.Split('\\').Length-1;i++)
            {
                dbFolder += dbFile.Split('\\')[i] + "\\";
            }

            bool exists = System.IO.Directory.Exists(dbFolder);

            if (!exists)
            {
                acryWrite("Sorry, I don't seem to be able to add acronyms right now", true);
                return;
            }
            else
            {
                try
                {
                    if (!System.IO.Directory.Exists(dbFolder + Properties.Settings.Default.newAcryDirName))
                        System.IO.Directory.CreateDirectory(dbFolder + Properties.Settings.Default.newAcryDirName);

                    this.Dispatcher.BeginInvoke((Action)(() => 
                        {
                            AddWindow newWin = new AddWindow();
                            this.Hide();
                            newWin.ShowDialog();
                            this.Show();
                            newWin = null;
                        }
                    ));


                }
                catch
                {
                    acryWrite("I cant seem to access the new acronym folder.  We can't add new acronyms right now.  Sorry!", true);
                }
            }

        }

    }
}
