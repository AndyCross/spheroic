/*
 * Nunchuck functions -- Talk to a Wii Nunchuck
 *
 * Copyright (c) 2010 Szymon Kobalczyk, http://geekswithblogs.net/kobush/
 *  
 * Adapted library from the Bionic Arduino course : 
 *  http://todbot.com/blog/bionicarduino/
 *
 * 2007 Tod E. Kurt, http://todbot.com/blog/
 *
 * The Wii Nunchuck reading code originally from Windmeadow Labs
 *   http://www.windmeadow.com/node/42
 *  
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 * Other resources:
 * http://todbot.com/blog/2007/10/25/boarduino-wii-nunchuck-servo/
 * http://oomlout.com/tmp/SERB_WiiNunChuckControl.txt
 * http://forums.nxtasy.org/index.php?showtopic=3075
 * http://wiibrew.org/wiki/Wiimote/Extension_Controllers
 * http://web.archive.org/web/20080627223429/http://www.jmaginary.com/wiiNunchuck.cpp.txt
 * http://randomhacksofboredom.blogspot.com/2009/06/wii-motion-plus-arduino-love.html
 * http://www.arduino.cc/cgi-bin/yabb2/YaBB.pl?num=1259091426

 * 
 */

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace NetduinoSerbRemote
{
    public class WiiChuck : IDisposable
    {
        private readonly bool _disableEncryption;
        // Wii Nunchuck scaling constants
        private const byte AnalogXLo = 0;
        private const byte AnalogXNu = 125;
        private const byte AnalogXHi = 255;

        private const byte AnalogYLo = 12;
        private const byte AnalogYNu = 125;
        private const byte AnalogYHi = 255;

        private const int AccelXLo = 490;
        private const int AccelXNu = 852;
        private const int AccelXHi = 890;

        private const int AccelYLo = 280;
        private const int AccelYNu = 488;
        private const int AccelYHi = 700;

        private const int AccelZLo = 296;
        private const int AccelZNu = 504;
        private const int AccelZHi = 708;

        /*
         * For scaling the raw values from the nunchuck into G values
         * Details on callibration and the maths can be found at
         *    http://web.archive.org/web/20080725180459/http://www.wiili.org/index.php/Motion_analysis
 
             Zero Points
               <math>x_0 = (x_1 + x_2) / 2\,</math>
               <math>y_0 = (y_1 + y_3) / 2\,</math>
               <math>z_0 = (z_2 + z_3) / 2\,</math>
     
             One G points
               <math>x = \frac{x_{raw} - x_0}{x_3 - x_0}</math>
               <math>y = \frac{y_{raw} - y_0}{y_2 - y_0}</math>
               <math>z = \frac{z_{raw} - z_0}{z_1 - z_0}</math>
       
         * Not all of these are used and could be deleted (kept to make interpretting math&apos;s easier 
         * 0-Zero G Value 
         * 1-Value when laying on table 
         * 2-Value when resting on nose
         * 3-Value when resting on side (left side up)
        */

        // Horizontal with the joystick facing up
        private const int X1 = 512;
        private const int Y1 = 512;
        private const int Z1 = 749;

        // Resting on nose with power cord facing up
        private const int X2 = 512;
        private const int Y2 = 727;
        private const int Z2 = 512;

        // Laying on its side, so the left side is facing up
        private const int X3 = 716;
        private const int Y3 = 512;
        private const int Z3 = 512;

        // zero G points
        private const float X0 = ((X1 + X2)/2f);
        private const float Y0 = ((Y1 + Y3)/2f);
        private const float Z0 = ((Z2 + Z3)/2f);

        // Device constants
        private const byte DeviceAddress = 0x52;    // slave address
        private const int ClockRateKHz = 400;       // 400kHz
        private const int TransactionTimeout = 100;
        private const int InitTimeout = 100;

        private readonly I2CDevice _device;
        private bool _isDisposed;
        private bool _isConnected;

        private bool zButtonDown;
        private bool cButtonDown;
        private byte analogX;
        private byte analogY;
        private int accelX;
        private int accelY;
        private int accelZ;

        public WiiChuck(bool disableEncryption) 
        {
            _disableEncryption = disableEncryption;

            var config = new I2CDevice.Configuration(DeviceAddress, ClockRateKHz);
            _device = new I2CDevice(config);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _device.Dispose();
            _isDisposed = true;
        }

        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public bool ZButtonDown
        {
            get { return zButtonDown; }
        }

        public bool CButtonDown
        {
            get { return cButtonDown; }
        }

        public byte RawAnalogX
        {
            get { return analogX; }
        }

        public byte RawAnalogY
        {
            get { return analogY; }
        }

        public int RawAccelerationX
        {
            get { return accelX; }
        }

        public int RawAccelerationY
        {
            get { return accelY; }
        }

        public int RawAccelerationZ
        {
            get { return accelZ; }
        }
            
        public float AnalogX
        {
            get { return Map(analogX, AnalogXLo, AnalogXNu, AnalogXHi); }
        }

        public float AnalogY
        {
            get { return Map(analogY, AnalogYLo, AnalogYNu, AnalogYHi); }
        }

        public float AccelerationX
        {
            get { return Map(accelX, AccelXLo, AccelXNu, AccelXHi); }
        }
        
        public float AccelerationY
        {
            get { return Map(accelY, AccelYLo, AccelYNu, AccelYHi); }
        }

        public float AccelerationZ
        {
            get { return Map(accelZ, AccelZLo, AccelZNu, AccelZHi); }
        }

        /// <summary>
        /// Calculates and returns the x-axis acceleration in Gs
        /// </summary>
        public float AccelerationXGs
        {
            get { return (accelX - X0) / (X3 - X0); }
        }

        /// <summary>
        /// Calculates and returns the y-axis acceleration in Gs
        /// </summary>
        public float AccelerationYGs
        {
            get { return (accelY - Y0) / (Y2 - Y0); }
        }

        /// <summary>
        /// Calculates and returns the z-axis acceleration in Gs
        /// </summary>
        public float AccelerationZGs
        {
            get { return (accelZ - Z0)/(Z1 - Z0); }
        }

        private void Init(int timeout)
        {
            if (_isDisposed)
                throw new ObjectDisposedException();

            // http://www.arduino.cc/cgi-bin/yabb2/YaBB.pl?num=1259091426
            if (_disableEncryption)
            {
                // disable encryption
                // look at <http://wiibrew.org/wiki/Wiimote#The_New_Way> at "The New Way"

                _isConnected = false;
                var startTime = Utility.GetMachineTime().Ticks;
                do
                {
                    bool ret = WriteToDevice(new byte[]
                                                 {
                                                     0xF0, // encryption register address
                                                     0x55 // disable encryption setup.
                                                 });
                    if (ret)
                    {
                        ret = WriteToDevice(
                            0xFB, // encryption register address
                            0x00 // disable encryption setup.
                            );

                        if (ret)
                            _isConnected = true;
                    }

                }
                while (!_isConnected && ((Utility.GetMachineTime().Ticks - startTime) < (timeout * 10)));
            }
            else
            {
                // init the old way 
                // look at <http://wiibrew.org/wiki/Wiimote#The_Old_Way> at "The Old Way"
                _isConnected = WriteToDevice(new byte[] {
                                             0x40, // sends memory address
                                             0x00 // sends sent a zero
                                         });
            }
        }

        /// <summary>
        /// Receive data back from the nunchuck, 
        /// returns 1 on successful read. returns 0 on failure
        /// </summary>
        public bool GetData()
        {
            if (_isDisposed)
                throw new ObjectDisposedException();

            if (!IsConnected)
            {
                Init(InitTimeout);
                return false;
            }

            // send request for data
            WriteToDevice(0x00);

            // get the data
            byte[] inputBuffer = new byte[6];
            var readTransaction = I2CDevice.CreateReadTransaction(inputBuffer);

            // execute both transactions
            int tranferred = _device.Execute(new I2CDevice.I2CTransaction[] { readTransaction}, TransactionTimeout);

            // less then 6 bytes read?
            if (tranferred != inputBuffer.Length)
            {
                // communication error, no need to reinitialize
                return false; 
            }

            if (!_disableEncryption)
            {
                // decrypt data in buffer
                for (int i = 0; i < 6; i++)
                    inputBuffer[i] = DecodeByte(inputBuffer[i]);
            }

            // check if all 0xff read? 
            byte cnt = 0;
            for (int i = 0; i < inputBuffer.Length; i++)
                if (inputBuffer[i] == 0xff) cnt++;

            if (cnt == inputBuffer.Length)
            {
                // this is connection error.
                _isConnected = false;
                return false;
            }

            ExtractData(inputBuffer);
            return true;
        }

        private bool WriteToDevice(params byte[] outBuffer)
        {
            // create transaction to sent to device
            var writeTransction = I2CDevice.CreateWriteTransaction(outBuffer);

            // data is sent to device
            var transferred = _device.Execute(new I2CDevice.I2CTransaction[] {writeTransction}, TransactionTimeout);

            // make sure data was sent
            return (transferred == outBuffer.Length);
        }

        private const int KEY_DECRYPT_XOR = 0x17;
        private const int KEY_DECRYPT_ADD = 0x17;

        // Encode data to format that most wiimote drivers except
        // only needed if you use one of the regular wiimote drivers
        static byte DecodeByte(byte x)
        {
            return (byte)((x ^ KEY_DECRYPT_XOR) + KEY_DECRYPT_ADD);
        }

        /// <summary>
        /// Extract data from data buffer.
        /// </summary>
        private void ExtractData(byte[] nunchuck_buf)
        {
            // Read analog data
            analogX = nunchuck_buf[0];
            analogY = nunchuck_buf[1];
            accelX = nunchuck_buf[2] << 2;
            accelY = nunchuck_buf[3] << 2;
            accelZ = nunchuck_buf[4] << 2;

            // byte nunchuck_buf[5] contains bits for z and c buttons
            // it also contains the least significant bits for the accelerometer data
            // so we have to check each bit of byte outbuf[5]

            // Read z button
            zButtonDown = ((nunchuck_buf[5] & 0x01) == 0);

            // Read c button
            cButtonDown = ((nunchuck_buf[5] & 0x02) == 0);

            // Add significant bits
            accelX |= (nunchuck_buf[5] >> 2) & 0x03;
            accelY |= (nunchuck_buf[5] >> 4) & 0x03;
            accelZ |= (nunchuck_buf[5] >> 6) & 0x03;
        }

        private static float Map(byte val, byte lo, byte nu, byte hi)
        {
            if (val <= nu)
                return ((float)val - nu) / (nu - lo);
            else
                return ((float)val - nu) / (hi - nu);
        }

        private static float Map(int val, int lo, int nu, int hi)
        {
            if (val <= nu)
                return ((float)val - nu) / (nu - lo);
            else
                return ((float)val - nu) / (hi - nu);
        }

        public static void PrintData(WiiChuck wii)
        {
            Debug.Print(
                "joy:" + wii.RawAnalogX + "," + wii.RawAnalogY +
                " -> " + wii.AnalogX + "," + wii.AnalogY + " \t" +
                "acc:" + wii.RawAccelerationX + "," + wii.RawAccelerationY + "," + wii.RawAccelerationZ +
                " -> " + wii.AccelerationXGs + "," + wii.AccelerationYGs + "," + wii.AccelerationZGs + " \t" +
                "but:" + wii.ZButtonDown + "," + wii.CButtonDown);
        }
    }
}