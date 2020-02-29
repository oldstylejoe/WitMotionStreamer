using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using LSL;

namespace WitMotionStreamer
{
    public partial class WitMotionForm : Form
    {
        List<System.IO.Ports.SerialPort> serialPorts = new List<System.IO.Ports.SerialPort>();
        Dictionary<string, liblsl.StreamOutlet> outlets = new Dictionary<string, liblsl.StreamOutlet>();

        string streamName = "BT";
        string streamType = "IMU";
        int streamChannels = 4;

        public WitMotionForm()
        {
            InitializeComponent();

            AddLine("Wit Motion LSL streamer v0.1");
            AddLine("Joe Snider - oldstyle_joe@yahoo.com");
            AddLine("Pair the Wit Motion devices and click scan below.");

            buttonScan.Click += ButtonScan_Click;

            buttonStart.Click += ButtonStart_Click;

            buttonStop.Click += ButtonStop_Click;

            listViewComPorts.CheckBoxes = true;
            listViewComPorts.FullRowSelect = true;
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            ClosePorts();
        }

        ~WitMotionForm()
        {
            ClosePorts();
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            AddLine("Starting selected ports.");
            AddLine("   *** wait at least 30 seconds before clicking anything ***");
            AddLine("TODO: bug Joe about these 30 seconds.");
            StartPorts();
            AddLine("Finished starting selected ports. Opened " + serialPorts.Count + " ports");
        }

        private void ButtonScan_Click(object sender, EventArgs e)
        {
            ScanComPorts();
        }

        /// <summary>
        /// start up the ports listed in the view box.
        /// </summary>
        private void StartPorts()
        {
            if(listViewComPorts.CheckedItems.Count == 0)
            {
                MessageBox.Show("Select at least one com port in the list.");
                return;
            }

            ClosePorts();

            foreach(int x in listViewComPorts.CheckedIndices)
            {
                string port = listViewComPorts.Items[x].Text;
                AddLine("Opening port " + port);
                OpenPort(port);
            }

            StartStreaming();
        }

        /// <summary>
        /// close any open serial ports.
        /// Safe to call if nothing is open.
        /// </summary>
        private void ClosePorts()
        {
            AddLine("Closing ports.");
            foreach(var x in serialPorts)
            {
                x.Close();
            }

            serialPorts.Clear();
            outlets.Clear();
        }

        /// <summary>
        /// Open the specified port and add to the list of ports.
        /// Safe to call multiple times, and ignores any ports that are already on the list.
        /// </summary>
        /// <param name="port">something like com1</param>
        private void OpenPort(string port)
        {
            if(!CheckPortOpen(port))
            {
                var p = new System.IO.Ports.SerialPort(port, 115200);
                p.Open();
                serialPorts.Add(p);

                p.DataReceived += _DataReceived;

                liblsl.StreamInfo info = new liblsl.StreamInfo(streamName, streamType, streamChannels, 0, 
                    liblsl.channel_format_t.cf_double64, port);
                outlets.Add(port, new liblsl.StreamOutlet(info));
            }
        }

        /// <summary>
        /// just do it here. The codes are from the WitMotion instructions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //11 is a magic number for each sample from the devices.
            byte[] data = new byte[11];
            System.IO.Ports.SerialPort sp = (System.IO.Ports.SerialPort)sender;

            while (sp.BytesToRead > 10)
            {
                sp.Read(data, 0, 11);

                if (data[0] != 0x55)
                {
                    //there was an error. Clear the buffer. This could be a bad sign.
                    sp.ReadExisting();
                    Console.WriteLine("Error: parse error from a device ... if this happens a lot, then something is wrong.");
                }

                switch (data[1])
                {
                    case 0x51:
                        double x, y, z;
                        x = ((data[3] << 8) | data[2]) / 32768.0 * 16;
                        y = ((data[5] << 8) | data[4]) / 32768.0 * 16;
                        z = ((data[7] << 8) | data[6]) / 32768.0 * 16;
                        SendData(sp.PortName, "a", x, y, z);
                        break;

                    case 0x52:
                        x = ((data[3] << 8) | data[2]) / 32768.0 * 16;
                        y = ((data[5] << 8) | data[4]) / 32768.0 * 16;
                        z = ((data[7] << 8) | data[6]) / 32768.0 * 16;
                        SendData(sp.PortName, "w", x, y, z);
                        break;

                    case 0x53:
                        x = ((data[3] << 8) | data[2]) / 32768.0 * 180;
                        y = ((data[5] << 8) | data[4]) / 32768.0 * 180;
                        z = ((data[7] << 8) | data[6]) / 32768.0 * 180;
                        SendData(sp.PortName, "e", x, y, z);
                        break;
                }
            }

            Thread.Sleep(10);

        }

        /// <summary>
        /// Check if the port is already on the list of open ports.
        /// Does not check if the port is open in the serial port sense, just that it's on the list.
        /// </summary>
        /// <param name="port">something like com1</param>
        /// <returns>bool</returns>
        private bool CheckPortOpen(string port)
        {
            //probably some lynq-o-magic I could do here instead.
            foreach(var x in serialPorts) {
                if (x.PortName == port) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Use this to insert strings in the status display.
        /// Probably not too much.
        /// </summary>
        /// <param name="line">Insert me</param>
        public void AddLine(string line)
        {
            textBoxStatus.Text += line + Environment.NewLine;
        }

        /// <summary>
        /// Scan the com ports and update the display.
        /// TODO: should allow for hotplugging.
        /// </summary>
        private void ScanComPorts()
        {
            AddLine("Scanning com ports.");
            listViewComPorts.Clear();
            foreach (string portName in System.IO.Ports.SerialPort.GetPortNames())
            {
                listViewComPorts.Items.Add(portName);
            }
            AddLine("Done scanning.");
            AddLine("Compare the list below to the one in windows bluetooth/settings/comports and select the ones corresponding to the WitMotion devices and marked as output");
            AddLine("    TODO: this is awkward ... bug Joe to fix it.");
        }

        /// <summary>
        /// Send to the paired device ports to start streaming.
        /// Uses magic commands from the WitMotion example c# app (AT commands)
        /// </summary>
        private void StartStreaming()
        {
            Byte[] byteSend = new Byte[9];
            byteSend[0] = (byte)'A';
            byteSend[1] = (byte)'T';
            byteSend[2] = (byte)'+';
            byteSend[3] = (byte)'R';
            byteSend[4] = (byte)'O';
            byteSend[5] = (byte)'L';
            byteSend[6] = (byte)'E';
            byteSend[7] = (byte)'=';
            byteSend[8] = (byte)'M';
            if (BroadcastMessage(byteSend) != 0) return;
            Thread.Sleep(1500);
            byteSend[8] = (byte)'S';
            if (BroadcastMessage(byteSend) != 0) return;
        }

        /// <summary>
        /// Send a message to all paired devices.
        /// No real checking.
        /// </summary>
        /// <param name="byteSend"></param>
        /// <returns></returns>
        private sbyte BroadcastMessage(Byte[] byteSend)
        {
            try {
                foreach (var p in serialPorts) {
                    p.Write(byteSend, 0, byteSend.Length);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Send the data on to LSL
        /// </summary>
        /// <param name="sender">the port, must be on the outlets list</param>
        /// <param name="measure">hacked in a, w, e</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        private void SendData(string sender, string measure, double x, double y, double z)
        {
            //Console.WriteLine(sender + " " + measure + " " + x + " " + y + " " + z);
            double[] data = new double[4];
            if(measure == "a") {
                data[0] = 0;
            } else if (measure == "w")
            {
                data[0] = 1;
            }
            else if (measure == "e")
            {
                data[0] = 2;
            } else
            {
                data[0] = -1;
            }
            data[1] = x;
            data[2] = y;
            data[3] = z;
            outlets[sender].push_sample(data);
        }
    }
}
