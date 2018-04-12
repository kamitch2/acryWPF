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
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    public partial class AddWindow : Window
    {
        private SpinLock usrLock;
        private string userAcryInput = "KyleWroteThis";
        private string prevAcry = "KyleWroteThis";
        private string userDefInput = "InIndia,2017";
        private string prevDef = "InIndia,2017";
        private System.Threading.Thread cuteThread;

        public AddWindow()
        {
            InitializeComponent();

            FocusManager.SetFocusedElement(this, UsrInptAcrTxtBox);

            Keyboard.Focus(UsrInptAcrTxtBox);

            cuteThread = new System.Threading.Thread(doStuff);
            cuteThread.IsBackground = true;
            cuteThread.Start();
            usrLock = new SpinLock();
        }

        private void doStuff()
        {
            acryWrite();

            bool haveLock = false;

            while (true)
            {
                System.Threading.Thread.Sleep(100);

                haveLock = false;

                usrLock.Enter(ref haveLock);

                if (haveLock && (!prevAcry.Equals(userAcryInput) || !prevDef.Equals(userDefInput)))
                {
                    prevAcry = userAcryInput;
                    prevDef = userDefInput;

                    if (userAcryInput.Length == 0 || userDefInput.Length == 0)
                    {
                        acryWrite("Only non-empty acronyms and defintiions, please.");
                    }
                    else
                    {
                        acryWrite("Writing new acronym to file");

                        int err = writeFile(userAcryInput, userDefInput);

                        if (err > 0)
                        {
                            acryWrite("Your ancronym has been noted.  An admin will review it ASAP.  Thanks!");

                            this.Dispatcher.BeginInvoke((Action)(() =>
                            {
                                this.UsrInptAcrTxtBox.Text = "";
                                this.UsrInptDefTxtBox.Text = "";
                            }
                                ));
                        }
                        else
                        {
                            acryWrite("We couldn't record your submission right now due to an error.  Sorry!", true);
                        }
                    }

                    usrLock.Exit();
                }
                else if(haveLock)
                {
                    usrLock.Exit();
                }
            }
        }

        private int writeFile(string acry, string def)
        {
            try
            {
                string dataFile = Properties.Settings.Default.databaseFile;
                string dataDir = "";
                System.Random rnd = new Random((int)DateTime.Now.ToBinary());

                for (int i = 0; i < dataFile.Split('\\').Length - 1; i++)
                {
                    dataDir += dataFile.Split('\\')[i] + "\\";
                }

                dataDir += Properties.Settings.Default.newAcryDirName;

                using(System.IO.StreamWriter sw = new System.IO.StreamWriter(dataDir + "\\" + DateTime.UtcNow.ToFileTime().ToString()+"-"+ rnd.Next(1000) + ".txt"))
                {
                    sw.WriteLine(acry);
                    sw.WriteLine(def);
                }

                return 1;
            }
            catch
            {
                return -1;
            }
        }



        private void AddBttn_Click(object sender, RoutedEventArgs e)
        {
            bool changed = false;
            bool haveLock = false;
            do
            {
                usrLock.Enter(ref haveLock);
                if (haveLock)
                {

                    userAcryInput = this.UsrInptAcrTxtBox.Text;
                    userDefInput = this.UsrInptDefTxtBox.Text;
                    changed = true;

                    haveLock = false;
                    usrLock.Exit();
                }
                

            } while (!changed);
        }

        private void CancelBttn_Click(object sender, RoutedEventArgs e)
        {
            this.UsrInptAcrTxtBox.Text = "";
            this.UsrInptDefTxtBox.Text = "";
            this.Close();
        }

        private void UsrInptAcrTxtBox_KeyUp(object sender, KeyEventArgs e)
        {
            acryWrite();
        }

        private void UsrInptDefTxtBox_KeyUp(object sender, KeyEventArgs e)
        {
            acryWrite();
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
