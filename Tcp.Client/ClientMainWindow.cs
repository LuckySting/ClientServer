using System;
using System.Windows.Forms;
using SomeProject.Library.Client;
using SomeProject.Library;
using System.Threading.Tasks;

namespace SomeProject.TcpClient
{
    public partial class ClientMainWindow : Form
    {
        public ClientMainWindow()
        {
            InitializeComponent();
        }

        private void disable()
        {
            textBox.ReadOnly = true;
            sendMsgBtn.Enabled = false;
            button1.Enabled = false;
        }

        private void enable()
        {
            textBox.ReadOnly = false;
            sendMsgBtn.Enabled = true;
            button1.Enabled = true;
        }

        private string sendMsg()
        {
            if (textBox.Text != "")
            {
                Client client = new Client();
                var res = client.SendMessageToServer(textBox.Text);
                return "Ответ: " + res.Message;
            } else
            {
                return "Нельзя отправить пустое сообщение";
            }
            
        }

        private string sendFile(string fileName)
        {
            Client client = new Client();
            var fileSplit = fileName.Split('.');
            var extention = fileSplit[fileSplit.Length - 1];
            var res = client.SendFileToServer(fileName, extention);
            return "Ответ: " + res.Message;
        }

        private void OnMsgBtnClick(object sender, EventArgs e)
        {
            disable();
            backgroundWorker1.RunWorkerAsync();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            labelRes.Text = "";
            timer.Stop();
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var dialog = (FileDialog)sender;
            if (dialog.FileName != "")
            {
                disable();
                backgroundWorker2.RunWorkerAsync(dialog.FileName);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.ShowDialog();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            e.Result = sendMsg();
        }

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            e.Result = sendFile((string)e.Argument);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            enable();
            labelRes.Text = (string)e.Result;
            timer.Interval = 5000;
            timer.Start();
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            enable();
            labelRes.Text = (string)e.Result;
            timer.Interval = 5000;
            timer.Start();
        }
    }
}
