using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.ComponentModel;

namespace Chat_Room
{
    public partial class MainWindow : Window
    {
        public string name = "";

        Thread thread;

        UdpClient udpResponse = new UdpClient(42424, AddressFamily.InterNetwork);

        public MainWindow()
        {
            InitializeComponent();

            // get a non-empty name
            while (name.Length == 0)
                name = Interaction.InputBox("What is your name?").Trim();

            // so we can press enter in the textbox
            textBox.KeyDown += new KeyEventHandler(textBox_KeyDown);
            textBox.Focus();

            // we need to do a few things when the program ends
            Closing += EndReceive;

            // receiving messages on a different thread
            thread = new Thread(new ThreadStart(ReceiveMessage));
            thread.Start();

            // send a connection message
            var message = new Message { Time = DateTime.Now.ToLongTimeString(), Name = "", Text = string.Format("{0} connected", name) };
            SendMessage(message.ToString());
        }

        private void EndReceive(object sender, CancelEventArgs e)
        {
            // send a disconnection message
            var message = new Message { Time = DateTime.Now.ToLongTimeString(), Name = "", Text = string.Format("{0} disconnected", name) };           
            SendMessage(message.ToString());

            // stop receiving
            udpResponse.Close();
        }

        private void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    IPEndPoint recvEp = new IPEndPoint(IPAddress.Any, 0);

                    var text = Encoding.ASCII.GetString(udpResponse.Receive(ref recvEp));
                    
                    var message = new Message(text);

                    // using the UI thread, add the message to the listview
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        listView.Items.Add(message);
                        listView.SelectedIndex = listView.Items.Count - 1;
                        listView.ScrollIntoView(listView.SelectedItem);
                        listView.SelectedIndex = -1;
                    }));
                }
            }
            catch (Exception ex)
            {
                // did we force close the connection?
                if (ex.Message != "A blocking operation was interrupted by a call to WSACancelBlockingCall")
                    throw ex;
            }
        }

        private void SendMessage(string message)
        {
            // send via UDP
            UdpClient client = new UdpClient(24242, AddressFamily.InterNetwork);
            client.EnableBroadcast = true;

            IPEndPoint groupEp = new IPEndPoint(IPAddress.Broadcast, 42424);
            client.Connect(groupEp);

            var data = Encoding.ASCII.GetBytes(message);

            client.Send(data, data.Length);
            client.Close();
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                button_Click(null, null);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Text.Length > 0)
            {
                var message = new Message { Time = DateTime.Now.ToLongTimeString(), Name = name, Text = textBox.Text };

                // reset the textbox
                textBox.Text = "";

                SendMessage(message.ToString());                
            }
        }
    }

    public class Message
    {
        public string Time { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }

        public Message()
        {

        }

        public Message(string data)
        {
            var values = data.Split('|');

            Time = values[0];
            Name = values[1];
            Text = values[2];
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}", Time, Name, Text);
        }
    }
}
