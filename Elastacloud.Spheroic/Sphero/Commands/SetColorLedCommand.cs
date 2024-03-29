﻿// <copyright file="SetColorLedCommand.cs" company="Cirrious">
// (c) Copyright Cirrious. http://www.cirrious.com
// This source is subject to the Microsoft Public License (Ms-PL)
// Please see license.txt on http://opensource.org/licenses/ms-pl.html
// All other rights reserved.
// </copyright>
//  
// Project Lead - Stuart Lodge, Cirrious. http://www.cirrious.com - Hire me - I'm worth it!


namespace Cirrious.MvvmCross.Plugins.Sphero.Commands
{
    public class SetColorLedCommand : BaseSpheroCommand
    {
        public SetColorLedCommand(MvxColor color)
            : base(DeviceSphero, CommandSetRGBLed)
        {
            Color = color;
        }

        public MvxColor Color { get; set; }

        protected override sealed byte[] GetPayloadBytes()
        {
            var data = new byte[3];

            data[0] = (byte) this.Color.R;
            data[1] = (byte) this.Color.G;
            data[2] = (byte) this.Color.B;

            return data;
        }
    }

    public class MvxColor
    {
        public MvxColor(int r, int g, int b)
        {
            R = (byte) r;
            G = (byte) g;
            B = (byte) b;
        }

        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }
    }
}