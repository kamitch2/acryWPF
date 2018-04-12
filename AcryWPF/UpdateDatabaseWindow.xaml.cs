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
using System.Threading;

namespace AcryWPF
{
    /// <summary>
    /// Interaction logic for UpdateDatabaseWindow.xaml
    /// </summary>
    public partial class UpdateDatabaseWindow : Window
    {
        private SpinLock usrLock;
        private System.Threading.Thread cuteThread;
        private bool accepted = false;
        private bool userClicked = false;

        public UpdateDatabaseWindow()
        {
            InitializeComponent();

            cuteThread = new System.Threading.Thread(doStuff);
            cuteThread.IsBackground = true;
            cuteThread.Start();
            usrLock = new SpinLock();
        }

        private void doStuff()
        {
            bool done = false;
            bool sentenced = false;
            bool haveLock = false;
            
            string dataFile = Properties.Settings.Default.databaseFile;
            string dataDir = "";

            for (int i = 0; i < dataFile.Split('\\').Length - 1;i++ )
            {
                dataDir += dataFile.Split('\\')[i] + "\\";
            }

            dataDir += Properties.Settings.Default.newAcryDirName;

            do
            {
                if(!System.IO.Directory.Exists(dataDir))
                {
                    done = true;
                    acryWrite("There aren't any acronyms for you to review (No folder for new acrys yet).");
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.UsrInptAcrTxtBox.Text = "";
                        this.UsrInptDefTxtBox.Text = "";
                        this.AddBttn.IsEnabled = false;
                        this.CancelBttn.IsEnabled = false;
                    }));
                    continue;
                }

               string[] fileList = System.IO.Directory.GetFiles(dataDir);

               if(fileList.Length == 0)
               {
                   done = true;
                   acryWrite("There aren't any acronyms for you to review.  Thanks!");
                   this.Dispatcher.BeginInvoke((Action)(() =>
                   {
                       this.UsrInptAcrTxtBox.Text = "";
                       this.UsrInptDefTxtBox.Text = "";
                       this.AddBttn.IsEnabled = false;
                       this.CancelBttn.IsEnabled = false;
                   }));
                   continue;
               }
               else
               {
                   acryWrite("Here is the next one to be sentenced");
                   sentenced = false;
                   haveLock = false;
                   string acry;
                   string def;

                   string currFile = fileList[0];

                   using (System.IO.StreamReader sr = new System.IO.StreamReader(currFile))
                   {
                       acry = sr.ReadLine();
                       def = sr.ReadLine();
                   }

                    this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            UsrInptAcrTxtBox.Text = acry;
                            UsrInptDefTxtBox.Text = def;
                        }));

                    while(!sentenced)
                    {
                        Thread.Sleep(100);

                        usrLock.Enter(ref haveLock);

                        if(haveLock)
                        {
                            if(userClicked)
                            {
                                int err;
                                if(accepted)
                                {
                                    err = updateData(acry, def, currFile, true);
                                }
                                else
                                {
                                    err = updateData(acry, def, currFile, false);
                                }
                                sentenced = true;
                                userClicked = false;
                            }
                            haveLock = false;
                            usrLock.Exit();
                        }
                    }
               }
            } while (!done);
        }

        private int updateData(string acry, string def,string filePath, bool add)
        {
            try
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.AddBttn.IsEnabled = false;
                        this.CancelBttn.IsEnabled = false;
                    }));

                acryWrite("Updating the database/dataset");

                if (add)
                {
                    using (System.IO.StreamWriter sw = System.IO.File.AppendText(Properties.Settings.Default.databaseFile))
                    {
                        sw.WriteLine(acry);
                        sw.WriteLine(def);
                    }
                }

                System.IO.File.Delete(filePath);

                acryWrite("Got it, thanks!");

                Thread.Sleep(1000);

                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.AddBttn.IsEnabled = true;
                    this.CancelBttn.IsEnabled = true;
                }));

                return 1;
            }
            catch
            {
                acryWrite("Something went wrong!", true);
                return -1;
            }
        }

        private void CancelBttn_Click(object sender, RoutedEventArgs e)
        {
            bool done = false;
            bool haveLock = false;

            while(!done)
            {
                usrLock.Enter(ref haveLock);

                if(haveLock)
                {
                    accepted = false;
                    userClicked = true;
                    done = true;
                    haveLock = false;
                    usrLock.Exit();
                }
            }
        }

        private void AddBttn_Click(object sender, RoutedEventArgs e)
        {
            bool done = false;
            bool haveLock = false;

            while (!done)
            {
                usrLock.Enter(ref haveLock);

                if (haveLock)
                {
                    accepted = true;
                    userClicked = true;
                    done = true;
                    haveLock = false;
                    usrLock.Exit();
                }
            }
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
                    this.SystemMessage.Text = newText;
                    this.SystemMessage.ClipToBounds = true;
                }

                ));
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cuteThread.Abort();
            cuteThread = null;
        }
    }
}
