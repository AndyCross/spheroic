//=======================================================================================
//
//  Purpose: Povide a method of talking to a Roving Networks RN-42 Bluetooth module in
//           command mode.
//
//
//  Copyright (C) 2011 Mark Stevens
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software") to use
//  the Software in a non-commercial environment subject to the following conditions:
//
//  1.  The above copyright notice and this permission notice shall be included in
//      all copies or substantial portions of the Software.
//  2.  The original author shall be acknowledged in any software and documentation
//      including (but not restricted to) books, articles and online posts.
//
//  Use in a commercial environment is subject to the written consent of the author
//  and/or copyright holder.
//
//  The Software, documentation, instructions for use and associated materials
//  are provided "as is" without warranty of any kind express or implied.  The
//  author and copyright holders shall in no way be held liable for any claim
//  or damages arising as a result of the use of the software and/or documentation.
//
//=======================================================================================
using System;
using System.Collections;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace RN42Bluetooth
{
    class RovingNetworks : IDisposable
    {
        #region Constants

        /// <summary>
        /// Size of the buffer for the com port.
        /// </summary>
        private const int BUFFER_SIZE = 1024;

        #endregion

        #region Enums

        /// <summary>
        /// Determine which mode the module is in.
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>
            /// We have not yet made a connection to the bluetooth device.
            /// </summary>
            Disconnected,
            /// <summary>
            /// Bluetooth module is in Command mode.
            /// </summary>
            Command,
            /// <summary>
            /// Bluetooth module is in data mode (default).
            /// </summary>
            Data
        }

        #endregion

        #region Private variables

        /// <summary>
        /// Serial port used to communicate with the bluetooth module.
        /// </summary>
        private SerialPort _bluetoothModule;

        /// <summary>
        /// Buffer for the data from the Bluetooth device.
        /// </summary>
        private byte[] _buffer = new byte[BUFFER_SIZE];

        /// <summary>
        /// Amount of data in the buffer.
        /// </summary>
        private int _currentBufferLength = 0;

        /// <summary>
        /// Used to track when Dispose is called.
        /// </summary>
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Current connection type.
        /// </summary>
        public ConnectionState ConnectionStatus { get; private set; }

        #endregion

        #region Constructor(s) and Dispose

        /// <summary>
        /// Default constructor.
        /// </summary>
        private RovingNetworks()
        {
            ConnectionStatus = ConnectionState.Disconnected;
        }

        /// <summary>
        /// Create a new instance of the RovingNetworks RN-42 class nad open a connection
        /// using the specified connection setting.
        /// </summary>
        /// <param name="port">Serial port to use to talk to the Bluetooth device.</param>
        /// <param name="baudRate">Baud rate for the device.</param>
        /// <param name="parity">Parity settings for the device.</param>
        /// <param name="dataBits">Number of data bits for the device.</param>
        /// <param name="stopBits">Number of stop bits for the device.</param>
        public RovingNetworks(string port, BaudRate baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            int rate;

            switch (baudRate)
            {
                case BaudRate.Baudrate1200:
                    rate = 1200;
                    break;
                case BaudRate.Baudrate2400:
                    rate = 2400;
                    break;
                case BaudRate.Baudrate4800:
                    rate = 4800;
                    break;
                case BaudRate.Baudrate9600:
                    rate = 9600;
                    break;
                case BaudRate.Baudrate19200:
                    rate = 19200;
                    break;
                case BaudRate.Baudrate38400:
                    rate = 38400;
                    break;
                case BaudRate.Baudrate57600:
                    rate = 57600;
                    break;
                case BaudRate.Baudrate115200:
                    rate = 115200;
                    break;
                case BaudRate.Baudrate230400:
                    rate = 230400;
                    break;
                default:
                    rate = -1;
                    break;
            }
            _bluetoothModule = new SerialPort(port, rate, parity, dataBits, stopBits);
            ConnectionStatus = ConnectionState.Data;
            _bluetoothModule.Open();
            _bluetoothModule.DataReceived += new SerialDataReceivedEventHandler(_bluetoothModule_DataReceived);
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // Call to GC.SupressFinalize will take this object
            // off the finalization queue and prevent multiple
            // executions.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initiate object disposal.
        /// </summary>
        /// <param name="disposing">Flag used to determine if the method is being called by the runtime (false) or programmatically (true).</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _bluetoothModule.Dispose();
                }
                _disposed = true;   // Done - prevent accidental or intentional Dispose calls.
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clear the buffer holding the responses from the bluetooth module.
        /// </summary>
        private void ClearBuffer()
        {
            lock (_buffer)
            {
                _buffer[0] = 0;
                _currentBufferLength = 0;
            }
        }

        /// <summary>
        /// Send a command to the bluetooth module and return the single line result
        /// from the module.
        /// </summary>
        /// <param name="command">Cammnd to send ot the module.</param>
        /// <exception cref="InvalidOperationException">This exception is thrown when the device is not in command mode.</exception>
        /// <returns>Result text from the bluetooth module.</returns>
        public string SendCommand(string command)
        {
            string result;

            ClearBuffer();
            _bluetoothModule.Write(Encoding.UTF8.GetBytes(command), 0, command.Length);
            result = ReadLine();
            while (result == "")
            {
                result = ReadLine();
            }
            return (result);
        }

        /// <summary>
        /// Send a command to the bluetooth module where a multi-line response is expected.
        /// </summary>
        /// <remarks>
        /// This method assumes that all of the responses from a command will be returned within the 
        /// specified timeout period.  Any responses after the timeout will not be returned.
        /// </remarks>
        /// <param name="command">Command to send to the bluetooth module.</param>
        /// <param name="timeout">Timeout (in milliseconds) between sending the command and expecting the result.</param>
        /// <returns>ArrayList of strings containing the response from the bluetooth module.</returns>
        public ArrayList SendCommand(string command, int timeout)
        {
            ArrayList result;
            string line;

            ClearBuffer();
            result = new ArrayList();
            _bluetoothModule.Write(Encoding.UTF8.GetBytes(command), 0, command.Length);
            Thread.Sleep(timeout);
            line = ReadLine();
            while (line != "")
            {
                result.Add(line);
                line = ReadLine();
            }
            return (result);
        }

        /// <summary>
        /// Put the bluetooth device into command mode.
        /// </summary>
        /// <returns>True if the connection is establised, false otherwise.</returns>
        public bool EnterCommandMode()
        {
            if (SendCommand("$$$") == "CMD")
            {
                ConnectionStatus = ConnectionState.Command;
                return (true);
            }
            return (false);
        }

        /// <summary>
        /// Exit command mode.
        /// </summary>
        /// <returns>True if command mode was exited successfully, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system is not in command mode.</exception>
        public bool ExitCommandMode()
        {
            if (SendCommand("---") == "END")
            {
                ConnectionStatus = ConnectionState.Data;
                return (true);
            }
            return (false);
        }

        /// <summary>
        /// Read a line of text from the input buffer.
        /// </summary>
        /// <returns></returns>
        private string ReadLine()
        {
            string result;

            result = "";
            lock (_buffer)
            {
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
            }

            return (result);
        }

        #endregion

        #region Events.

        /// <summary>
        /// Receive data from the bluetooth module and place into the data buffer.
        /// </summary>
        /// <param name="sender">Serial port generating the event.</param>
        /// <param name="e">Type of data received over the serial port.</param>
        private void _bluetoothModule_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                lock (_buffer)
                {
                    int amount;
                    byte[] buffer;

                    buffer = new byte[BUFFER_SIZE];
                    amount = ((SerialPort) sender).Read(buffer, 0, BUFFER_SIZE);
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

        #endregion
    }
}