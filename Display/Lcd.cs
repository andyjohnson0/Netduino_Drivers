using System;
using System.Threading;
using System.Globalization;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;


namespace uk.andyjohnson0.Netduino.Drivers.Display
{
    /// <summary>
    /// Driver for seven-segment LCD displays.
    /// Currently supports generic HD44780 displays in 8-bit or 4-bit mode, or EA DOG series displays in 5V or 3.3V mode.
    /// </summary>
    public class Lcd
    {
        /// <summary>
        /// Constructor. Initailise an Lcd object to drive an HD44780 display in 8 bit mode.
        /// </summary>
        /// <param name="device">Type of device to be driven.</param>
        /// <param name="rs">GPIO pin connected to the LCD register select pin.</param>
        /// <param name="e">GPIO pin connected to the LCD enable pin.</param>
        /// <param name="d0">GPIO pin connected to the LCD D0 pin.</param>
        /// <param name="d1">GPIO pin connected to the LCD D1 pin.</param>
        /// <param name="d2">GPIO pin connected to the LCD D2 pin.</param>
        /// <param name="d3">GPIO pin connected to the LCD D3 pin.</param>
        /// <param name="d4">GPIO pin connected to the LCD D4 pin.</param>
        /// <param name="d5">GPIO pin connected to the LCD D5 pin.</param>
        /// <param name="d6">GPIO pin connected to the LCD D6 pin.</param>
        /// <param name="d7">GPIO pin connected to the LCD D7 pin.</param>
        public Lcd(
            Device device,
            Cpu.Pin rs, Cpu.Pin e,
            Cpu.Pin d0, Cpu.Pin d1, Cpu.Pin d2, Cpu.Pin d3, Cpu.Pin d4, Cpu.Pin d5, Cpu.Pin d6, Cpu.Pin d7)
        {
            this.device = device;
            this.rs = new OutputPort(rs, false);
            this.e = new OutputPort(e, false);
            this.d0 = new OutputPort(d0, false);
            this.d1 = new OutputPort(d1, false);
            this.d2 = new OutputPort(d2, false);
            this.d3 = new OutputPort(d3, false);
            this.d4 = new OutputPort(d4, false);
            this.d5 = new OutputPort(d5, false);
            this.d6 = new OutputPort(d6, false);
            this.d7 = new OutputPort(d7, false);
            dataMode = DataMode.EightBit;
        }


        /// <summary>
        /// Constructor. Initailise an Lcd object to drive an HD44780 display in 4 bit mode.
        /// </summary>
        /// <param name="device">Type of device to be driven.</param>
        /// <param name="rs">GPIO pin connected to the LCD register select pin.</param>
        /// <param name="e">GPIO pin connected to the LCD enable pin.</param>
        /// <param name="d4">GPIO pin connected to the LCD D4 pin.</param>
        /// <param name="d5">GPIO pin connected to the LCD D5 pin.</param>
        /// <param name="d6">GPIO pin connected to the LCD D6 pin.</param>
        /// <param name="d7">GPIO pin connected to the LCD D7 pin.</param>
        public Lcd(
            Device device,
            Cpu.Pin rs, Cpu.Pin e,
            Cpu.Pin d4, Cpu.Pin d5, Cpu.Pin d6, Cpu.Pin d7)
        {
            this.device = device;
            this.rs = new OutputPort(rs, false);
            this.e = new OutputPort(e, false);
            this.d4 = new OutputPort(d4, false);
            this.d5 = new OutputPort(d5, false);
            this.d6 = new OutputPort(d6, false);
            this.d7 = new OutputPort(d7, false);
            dataMode = DataMode.FourBit;
        }


        /// <summary>
        /// Constructor. Initialise an Lcd object to drive a DOGM162 display using the SPI interface.
        /// </summary>
        /// <param name="device">Type of device to be driven.</param>
        /// <param name="config">SPI interface settings.</param>
        /// <param name="rs">GPIO pin connected to the LCD register select pin.</param>
        public Lcd(
            Device device,
            SPI.Configuration config,
            Cpu.Pin rs)
        {
            if ((device != Device.DOGM162_33V) && (device != Device.DOGM162_5V))
            {
                throw new InvalidOperationException("Initialisation method not valid for specified device");
            }

            this.device = device;
            this.spi = new SPI(config);
            this.rs = new OutputPort(rs, false);
            this.spiBuffer = new byte[1];
            this.dataMode = DataMode.EightBit;
        }


        private const int addressSetupTimeNs = 150;
        private const int enableHighTimeNs = 450;
        private const int dataSetupTimeNs = 200;
        private const int dataHoldTimeNs = 400;


        /// <summary>
        /// Types of LCD display devices that can be driven.
        /// </summary>
        public enum Device
        {
            /// <summary>Generic Hitachi HD44780 LCD display.</summary>
            HD44780,
            /// <summary>Electronic Assembly "DOG" series display in 5V mode.</summary>
            DOGM162_5V,
            /// <summary>Electronic Assembly "DOG" series display in 3.3V mode.</summary>
            DOGM162_33V
        }


        private Device device;
        private OutputPort rs;          // register select: 0 = write commands and read status, 1 = read/write character data
        private OutputPort e;           // enable: high->low = write data, low->high = ready to read.
        private OutputPort d0;
        private OutputPort d1;
        private OutputPort d2;
        private OutputPort d3;
        private OutputPort d4;
        private OutputPort d5;
        private OutputPort d6;
        private OutputPort d7;

        private SPI spi;
        private byte[] spiBuffer;

        protected DataMode dataMode;



        protected enum RegisterSelect
        {
            Command,
            Data
        }


        protected enum DataMode
        {
            EightBit,
            FourBit
        }


        /// <summary>
        /// Display line mode. Selects the number of lines that are displayed.
        /// </summary>
        public enum LineMode
        {
            /// <summary>Single-line (double height) mode.</summary>
            OneLine,
            /// <summary>Double-line (normal height) mode.</summary>
            TwoLine
        }


        /// <summary>
        /// Cursor direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>Move left.</summary>
            Left,
            /// <summary>Move right.</summary>
            Right
        }

        /// <summary>
        /// Cursor shift model for character entry.
        /// </summary>
        public enum CharacterEntryModeShift
        {
            /// <summary>Decrement cursor position.</summary>
            Decrement,
            /// <summary>Increment cursor position.</summary>
            Increment
        }

        public enum Font
        {
            /// <summary>5x8 character matrix. Valid for HD44780 only.</summary>
            FiveByEight,  
            /// <summary>5x10 character matrix. Valid for HD44780 only.</summary>
            FiveByTen,
            /// <summary>Single height character matrix. Valid for DOGM162 devicees only.</summary>
            SingleHeight,
            /// <summary>Double height character matrix. Valid for DOGM162 devicees only.</summary>
            DoubleHeight
        }


        /// <summary>
        /// Reset the device to an initial state.
        /// </summary>
        public void Reset()
        {
            switch (device)
            {
                case Device.HD44780:
                {
                    DataMode tmpMode = dataMode;  // Remember mode
                    dataMode = DataMode.EightBit;
                    WriteInternal(RegisterSelect.Command, 0x30);
                    WriteInternal(RegisterSelect.Command, 0x30);
                    WriteInternal(RegisterSelect.Command, 0x30);
                    WriteInternal(RegisterSelect.Command, 0x20);

                    dataMode = tmpMode;
                    switch (tmpMode)
                    {
                        case DataMode.EightBit:
                            WriteInternal(RegisterSelect.Command, 0x30);  // Set eight-bit mode
                            break;
                        case DataMode.FourBit:
                            WriteInternal(RegisterSelect.Command, 0x20);  // Set four-bit mode
                            break;
                    }
                    dataMode = tmpMode;  // Restore mode
                    WriteInternal(RegisterSelect.Command, 0x0C);  // Display on
                    break;
                }
                case Device.DOGM162_33V:
                {
                    WriteInternal(RegisterSelect.Command, 0x39);  // 8 bit data, two lines, instruction table 1
                    WriteInternal(RegisterSelect.Command, 0x14);  // Bias set 1/5, 2 Line Mode
                    WriteInternal(RegisterSelect.Command, 0x55);  // Booster on, contrast C5, set C4
                    WriteInternal(RegisterSelect.Command, 0x6D);  // Set voltage foller and gain
                    WriteInternal(RegisterSelect.Command, 0x78);  // Set contrast C3, C2, C1
                    WriteInternal(RegisterSelect.Command, 0x38);  // Instruction table 0
                    WriteInternal(RegisterSelect.Command, 0x0C);  // Display on
                    WriteInternal(RegisterSelect.Command, 0x01);  // Clear display, home
                    break;
                }
                case Device.DOGM162_5V:
                {
                    WriteInternal(RegisterSelect.Command, 0x39);  // 8 bit data, two lines, instruction table 1
                    WriteInternal(RegisterSelect.Command, 0x14);  // Bias set 1/5, 2 Line Mode
                    WriteInternal(RegisterSelect.Command, 0x51);  // Booster on, contrast C5, set C4
                    WriteInternal(RegisterSelect.Command, 0x6A);  // Set voltage foller and gain
                    WriteInternal(RegisterSelect.Command, 0x74);  // Set contrast C3, C2, C1
                    WriteInternal(RegisterSelect.Command, 0x38);  // Instruction table 0
                    WriteInternal(RegisterSelect.Command, 0x0C);  // Display on
                    WriteInternal(RegisterSelect.Command, 0x01);  // Clear display, home
                    break;
                }
            }
        }


        #region Interface


        /// <summary>
        /// Clear the display.
        /// </summary>
        public void Clear()
        {
            WriteInternal(RegisterSelect.Command, 0x01);
        }


        /// <summary>
        /// Move the cursor to the home (top left) position.
        /// </summary>
        public void Home()
        {
            WriteInternal(RegisterSelect.Command, 0x02);
        }


        /// <summary>
        /// Set character entry mode.
        /// </summary>
        /// <param name="shift">Character entry mode.</param>
        /// <param name="displayShiftEnable">Enable/disable display shift.</param>
        public void SetCharacterEntryMode(
            CharacterEntryModeShift shift,
            bool displayShiftEnable)
        {
            uint cmd = 0x04;
            if (shift == CharacterEntryModeShift.Increment)
                cmd |= 0x02;
            if (displayShiftEnable)
                cmd |= 0x01;
            WriteInternal(RegisterSelect.Command, cmd);
        }


        /// <summary>
        /// Set the display properties.
        /// </summary>
        /// <param name="isOn">Display on (true) or off (false).</param>
        /// <param name="cursorVisible">Cursor visible (true) or invisible (false).</param>
        /// <param name="cursorBlink">Cursor blink (true) or steady (false).</param>
        public void SetDisplay(bool isOn, bool cursorVisible, bool cursorBlink)
        {
            uint cmd = 0x08;
            if (isOn)
                cmd |= 0x04;
            if (cursorVisible)
                cmd |= 0x02;
            if (cursorBlink)
                cmd |= 0x01;
            WriteInternal(RegisterSelect.Command, cmd);
        }


        /// <summary>
        /// Shift the cursor.
        /// </summary>
        /// <param name="direction">Direction to shift cursor.</param>
        public void ShiftCursor(Direction direction)
        {
            uint cmd = 0x10;
            if (direction == Direction.Right)
                cmd |= 0x04;
            WriteInternal(RegisterSelect.Command, cmd);
        }


        /// <summary>
        /// Shift the display contents.
        /// </summary>
        /// <param name="direction">Direction to shift screen contents.</param>
        public void ShiftScreen(Direction direction)
        {
            uint cmd = 0x18;
            if (direction == Direction.Right)
                cmd |= 0x04;
            WriteInternal(RegisterSelect.Command, cmd);
        }


        /// <summary>
        /// Set display function.
        /// </summary>
        /// <param name="lineMode">Display line mode.</param>
        /// <param name="font">Display font.</param>
        public void SetFunction(LineMode lineMode, Font font)
        {

            uint cmd = 0x20;
            if (dataMode == DataMode.EightBit)
                cmd |= 0x10;
            if (lineMode == LineMode.TwoLine)
                cmd |= 0x08;
            switch (font)
            {
                case Font.FiveByEight:
                    if (device == Device.HD44780)
                        cmd |= 0x00;
                    else
                        throw new InvalidOperationException("Initialisation method not valid for specified device");
                    break;
                case Font.FiveByTen:
                    if (device == Device.HD44780)
                        cmd |= 0x04;
                    else
                        throw new InvalidOperationException("Initialisation method not valid for specified device");
                    break;
                case Font.SingleHeight:
                    if ((device == Device.DOGM162_33V) || (device == Device.DOGM162_5V))
                        cmd |= 0x00;
                    else
                        throw new InvalidOperationException("Font not valid for specified device");
                    break;
                case Font.DoubleHeight:
                    if ((device == Device.DOGM162_33V) || (device == Device.DOGM162_5V))
                        cmd |= 0x04;
                    else
                        throw new InvalidOperationException("Font not valid for specified device");
                    break;
            }
            WriteInternal(RegisterSelect.Command, cmd);
        }


        /// <summary>
        /// Set cursor position.
        /// </summary>
        /// <param name="address">
        /// Cursor position address.
        /// The first line of the display is an offset from 0x00, and the second line is an offset from 0x40.
        /// Value must be between 0 and 0x7F.
        /// </param>
        public void SetCursorAddress(uint address)
        {
            WriteInternal(RegisterSelect.Command, 0x80 | (address & 0x7F));
        }


        /// <summary>
        /// Write a string to the display at the current cursor position.
        /// </summary>
        /// <param name="str">String to display.</param>
        public void Write(string str)
        {
            foreach (char ch in str)
            {
                Write(ch);
            }
        }


        /// <summary>
        /// Write a character to the display at the current cursor position.
        /// </summary>
        /// <param name="ch">Charcter to display.</param>
        public void Write(char ch)
        {
            uint charCode = ((ch >= ' ') && (ch <= '}')) ? (uint)ch : 0xFF;
            WriteInternal(RegisterSelect.Data, charCode);
        }


        /// <summary>
        /// Write a character to the display at the current cursor position.
        /// </summary>
        /// <param name="charIdx">Index of character to display.</param>
        public void Write(uint charIdx)
        {
            WriteInternal(RegisterSelect.Data, charIdx);
        }


        #endregion Interface



        /// <summary>Minimum user-defined character index.</summary>
        public const uint MinUserDefinedChar = 0x00;
        /// <summary>Maximum user-defined character index.</summary>
        public const uint MaxuserDefinedChar = 0x07;


        /// <summary>
        /// Defne a user-defined character.
        /// </summary>
        /// <param name="charIdx">Index of character to define.</param>
        /// <param name="charData">Bitmap data defining character.</param>
        public void SetUserdefinedChar(uint charIdx, byte[] charData)
        {
            const uint bytesPerChar = 8;

            if ((charIdx < MinUserDefinedChar) || (charIdx > MaxuserDefinedChar))
            {
                return;
            }
            if ((charData == null) || (charData.Length != bytesPerChar))
            {
                return;
            }

            uint baseAddr = charIdx * bytesPerChar;
            WriteInternal(RegisterSelect.Command, 0x40 | baseAddr);
            for (uint i = 0; i < bytesPerChar; i++)
            {
                WriteInternal(RegisterSelect.Data, charData[i]);
            }
        }







        protected virtual void WriteInternal(RegisterSelect register, uint data)
        {
            rs.Write(register == RegisterSelect.Data);  // Data => rs=high
            WaitAtLeast(addressSetupTimeNs);

            if (spi != null)
            {
                spiBuffer[0] = (byte)data;
                spi.Write(spiBuffer);
                WaitAtLeast(dataHoldTimeNs);
            }
            else
            {
                switch (dataMode)
                {
                    case DataMode.EightBit:
                        d7.Write((data & 0x80) != 0);
                        d6.Write((data & 0x40) != 0);
                        d5.Write((data & 0x20) != 0);
                        d4.Write((data & 0x10) != 0);
                        if ((d3 != null) && (d2 != null) && (d1 != null) && (d0 != null))
                        {
                            d3.Write((data & 0x08) != 0);
                            d2.Write((data & 0x04) != 0);
                            d1.Write((data & 0x02) != 0);
                            d0.Write((data & 0x01) != 0);
                        }
                        WaitAtLeast(dataSetupTimeNs);
                        e.Write(true);
                        WaitAtLeast(enableHighTimeNs);
                        e.Write(false);
                        WaitAtLeast(dataHoldTimeNs);
                        break;
                    case DataMode.FourBit:
                        // Send left-hand four bits
                        d4.Write((data & 0x10) != 0);
                        d5.Write((data & 0x20) != 0);
                        d6.Write((data & 0x40) != 0);
                        d7.Write((data & 0x80) != 0);
                        WaitAtLeast(dataSetupTimeNs);
                        e.Write(true);
                        WaitAtLeast(enableHighTimeNs);
                        e.Write(false);
                        WaitAtLeast(dataHoldTimeNs);
                        // Send right-hand four bits
                        d4.Write((data & 0x01) != 0);
                        d5.Write((data & 0x02) != 0);
                        d6.Write((data & 0x04) != 0);
                        d7.Write((data & 0x08) != 0);
                        WaitAtLeast(dataSetupTimeNs);
                        e.Write(true);
                        WaitAtLeast(enableHighTimeNs);
                        e.Write(false);
                        WaitAtLeast(dataHoldTimeNs);
                        break;
                }
            }
        }



        private static void WaitAtLeast(int ns)
        {
            if (ns <= 1000000)
                Thread.Sleep(1);
            else
                Thread.Sleep(ns / 1000000);
        }
    }
}
