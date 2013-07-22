using System;
using System.Collections;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sphero.Commands;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using NetduinoSerbRemote;
using RN42Bluetooth;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Math = System.Math;

namespace Elastacloud.Spheroic
{
    public class Program
    {
        private static WiiChuck wiiChuck;
        private static SerialPort bluetoothModule;
        /// <summary>
        /// Size of the buffer for the com port.
        /// </summary>
        private const int BUFFER_SIZE = 1024;
        
        /// <summary>
        /// Buffer for the data from the Bluetooth device.
        /// </summary>
        private static byte[] _buffer = new byte[BUFFER_SIZE];

        /// <summary>
        /// Amount of data in the buffer.
        /// </summary>
        private static int _currentBufferLength = 0;

        private static bool Discover = true;
        private static string sphero_id = "0006664830E4,Sphero-OOY,240704";

        public static void Main()
        {

            bluetoothModule = new SerialPort(Serial.COM1, 115200, Parity.None, 8, StopBits.One);
            bluetoothModule.DataReceived += new SerialDataReceivedEventHandler(bluetoothModule_DataReceived);
            bluetoothModule.Open();
            sendCmd("$$$");
            Debug.Print(ReadLine());
            sendCmd("SM,1");
            Debug.Print(ReadLine());
            sendCmd("---");
            Debug.Print(ReadLine());

            Debug.Print("\nEntering Command Mode");
            sendCmd("$$$");
            Debug.Print(ReadLine());

            Debug.Print("\nScanning for Spheros...");
            sendCmd("I,10");
            Debug.Print(ReadLine());

            while (Discover)
            {
                var spheroAck = ReadLine();
                if (spheroAck.IndexOf("Sphero") > -1)
                {
                    Debug.Print("Found a Sphero: " + spheroAck);
                }
                else
                {
                    if (spheroAck != "")
                        Debug.Print("Ignoring something that's not an ack or a sphero: " + spheroAck);
                }

                if (spheroAck.IndexOf("Sphero-OOY")>-1)
                {
                    sphero_id = spheroAck;
                }

                if (spheroAck.IndexOf("Inquiry Done") > -1) break;
            }

            if (sphero_id != "")
            {
                Debug.Print("Connecting to ");
                Debug.Print(sphero_id.Substring(13, sphero_id.Length - sphero_id.IndexOf(",", 13)));

                // Connect to Sphero Address
                sendCmd("C," + sphero_id.Substring(0, 12));
                Debug.Print("Connected.");

                // Save address
                sendCmd("SR," + sphero_id.Substring(0, 12));

                // We're done here!
                sendCmd("---");
                Debug.Print("\nConfiguration complete!\nYou're Sphero's address has been saved in the Bluetooth Module\n\nHave fun ^^;");

                
            }

            var currentDirection = 0;
            var randomiser = new Random((int) DateTime.Now.Ticks);
            //abandon the WiiChuck as it is solder fubar 

            try
            {


                while (true)
                {
                    var speed = randomiser.Next(255);
                    var heading = randomiser.Next(60);
                    currentDirection = (heading%2 == 0) ? currentDirection += heading : currentDirection -= heading;
                    if (currentDirection < 0) currentDirection *= -1;
                    BaseSpheroCommand command = new RollCommand(speed, currentDirection, false);
                    sendCommandBytes(command.GetBytes(0));

                    Thread.Sleep(randomiser.Next(500));

                    var color = new MvxColor(randomiser.Next(256), randomiser.Next(256), randomiser.Next(256));
                    command = new SetColorLedCommand(color);
                    sendCommandBytes(command.GetBytes(0));

                    Thread.Sleep(100);

                }
            }
            catch
            {
                RollCommand command = new RollCommand(0,0,false);
                sendCommandBytes(command.GetBytes(0));
            }

            // initialize wii chuck
            wiiChuck = new WiiChuck(true);

            Debug.Print("SERB WiiRemote - Ready");

            while (true)
            {
                // try to read the data from nunchucku
                if (wiiChuck.GetData() == false)
                {
                    continue;
                }

#if DEBUG
                WiiChuck.PrintData(wiiChuck);
#endif

                if (wiiChuck.ZButtonDown)
                {
                    //do lovely stuff
                    BaseSpheroCommand command = new BackLedCommand(10);
                    var bytes = command.GetBytes(0);
                    sendCommandBytes(bytes);

                    var random = new Random();

                    command = new SetColorLedCommand(new MvxColor(random.Next(256), random.Next(256), random.Next(256)));
                    bytes = command.GetBytes(0);
                    sendCommandBytes(bytes);
                }
                else if (wiiChuck.CButtonDown)
                {
                    var rollCommand = new RollCommand(255, readPitch(), false);
                    sendCommandBytes(rollCommand.GetBytes(0));
                }
                else if (wiiChuck.ZButtonDown && wiiChuck.CButtonDown)
                {
                    //uberleet stuff here
                    var stopCommand = new RollCommand(0,0,false);
                    sendCommandBytes(stopCommand.GetBytes(0));
                }
                else
                {
                    //drive the motor!
                    //var rollCommand = new RollCommand(255, ) 

                    int velocity = 100;// (int)(Math.Sqrt(Math.Pow(wiiChuck.RawAnalogX / 210, 2D) + Math.Pow(wiiChuck.RawAnalogY / 210, 2D)) * 255);
                    int heading = (int) Math.Atan2(wiiChuck.RawAnalogY, wiiChuck.RawAnalogX);

                    /*if (wiiChuck.RawAnalogX > 0 && wiiChuck.RawAnalogY < 0) heading += 360;//quad4
                    if (wiiChuck.RawAnalogY < 0 && wiiChuck.RawAnalogX < 0) heading += 180; //quad3 
                    if (wiiChuck.RawAnalogX < 0 && wiiChuck.RawAnalogY > 0) heading += 180; //quad2
                    */

                    var command = new RollCommand(velocity, heading, false);
                    sendCommandBytes(command.GetBytes(0));
                }
            }


        }

        private static void sendCmd(string arduino)
        {
            sendCommand(arduino);
        }

        private static void sendCommand(string command)
        {
            string line = "", commandLine = command;

            // Send command
            if (commandLine != "$$$")
                commandLine += "\n";

            try
            {
                var bytes = Encoding.UTF8.GetBytes(commandLine);
                sendCommandBytes(bytes);

                // Show which command is being sent
                Debug.Print("> " + commandLine);
            }
            catch(Exception ex)
            {
                Debug.Print(ex.ToString());
            }

            Thread.Sleep(100);
        }

        private static void sendCommandBytes(byte[] bytes)
        {
            bluetoothModule.Write(bytes, 0, bytes.Length);
        }

        static void bluetoothModule_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //process
            if (e.EventType == SerialData.Chars)
            {
                lock (_buffer)
                {
                    int amount;
                    byte[] buffer;

                    buffer = new byte[BUFFER_SIZE];
                    amount = ((SerialPort)sender).Read(buffer, 0, BUFFER_SIZE);
                    if (amount > 0)
                    {
                        for (int index = amount - 1; index >= 0; index--)
                        {
                            if (buffer[index] == '\r')
                            {
                                if (index != amount)
                                {
                                    Array.Copy(buffer, index + 1, buffer, index, amount - index - 1);
                                }
                                amount--;
                            }
                        }
                        if ((amount + _currentBufferLength) <= BUFFER_SIZE)
                        {
                            Array.Copy(buffer, 0, _buffer, _currentBufferLength, amount);
                            _currentBufferLength += amount;
                        }
                        else
                        {
                            throw new Exception("Buffer overflow");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read a line of text from the input buffer.
        /// </summary>
        /// <returns></returns>
        private static string ReadLine()
        {
            string result;

            result = "";

            for (int index = 0; index < _currentBufferLength; index++)
            {
                if (_buffer[index] == '\n')
                {
                    _buffer[index] = 0;
                    result = "" + new string(Encoding.UTF8.GetChars(_buffer));
                    _currentBufferLength = _currentBufferLength - index - 1;
                    Array.Copy(_buffer, index + 1, _buffer, 0, _currentBufferLength);
                    break;
                }
            }

            return (result);
        }

        private static int readRoll() {
        return (int)(Math.Atan2(wiiChuck.RawAnalogX,wiiChuck.RawAnalogY)/ Math.PI * 180.0);
    }

    // returns pitch in degrees
    private static int readPitch() {        
        return (int) (Math.Acos(wiiChuck.AccelerationY/210)/ Math.PI * 180.0);  // optionally swap 'RADIUS' for 'R()'
    }

    }
}
