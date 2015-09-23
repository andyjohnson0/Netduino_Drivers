# Netduino Drivers Library

## Contents

1. [Synopsis](#Synopsis)
2. [Motivation](#Motivation)
3. [Installation and Use](#Installation and Use)
  * [uk.andyjohnson0.Netduino.Drivers.Display.Lcd Class](uk.andyjohnson0.Netduino.Drivers.Display.Lcd Class)
  * [uk.andyjohnson0.Netduino.Drivers.Motor.HBridgeSN754410 Class](uk.andyjohnson0.Netduino.Drivers.Motor.HBridgeSN754410 Class)
  * [uk.andyjohnson0.Netduino.Drivers.Gps.GenericSerialGps Class](uk.andyjohnson0.Netduino.Drivers.Sensor.GenericSerialGps Class)
4. [Author](#Author)
5. [Licence](#Licence)


## Synopsis

This is a C# class library for the Netduino class of microcontrollers. It provides various driver classes that simplfy the
task of dealing with various physical hardward devices.

Build configurations for provided for the Netduino 1 and Netduino Mini targets.


## Motivation

I wrote these drivers for use in various projects using a Netduino 1 and Netduino Minis. I intend to add more classes to this library.


# Installation and Use

This library depends upon the .net Micro Framework and my Netduino Utilities library.

Build the required configuration and add a reference in your project to uk.andyjohnson0.Netduino.Drivers.dll in the appropriate
build directory. Configurations are:
* Debug_Netduino1
* Release_Netduino1
* Debug_NetduinoMini
* Release_NetduinoMini

Namespace names and class names (in **bold**) are:

* uk.andyjohnson0.Netduino.Drivers

  * uk.andyjohnson0.Netduino.Drivers.Display
    * uk.andyjohnson0.Netduino.Drivers.Display.**Lcd**

  * uk.andyjohnson0.Netduino.Drivers.Motor
    * uk.andyjohnson0.Netduino.Drivers.Motor.**HBridgeSN754410**

  * uk.andyjohnson0.Netduino.Drivers.Gps
    * uk.andyjohnson0.Netduino.Drivers.Spg.**GenericSerialGps**
    * uk.andyjohnson0.Netduino.Drivers.Spg.**NmeaMonitor**
    * uk.andyjohnson0.Netduino.Drivers.Spg.**NmeaDecoder**
    * uk.andyjohnson0.Netduino.Drivers.Spg.**NmeaFixMessage**
    * uk.andyjohnson0.Netduino.Drivers.Spg.**NmeaRecommendedMinimumMessage**
    * uk.andyjohnson0.Netduino.Drivers.Spg.**LatLonCoord**


## uk.andyjohnson0.Netduino.Drivers.Display.Lcd Class

First add a using directive to your code:

	using uk.andyjohnson0.Netduino.Drivers.Display;

Then create an instance of the Lcd class. For example, for the Netduino 1:

	// HD44780 in 8-bit mode
	var lcd = new Lcd(Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5, Pins.GPIO_PIN_D6, Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D9, Pins.GPIO_PIN_D10, Pins.GPIO_PIN_D11, Pins.GPIO_PIN_D12, Pins.GPIO_PIN_D13);

or

	// HD44780 in 4-bit mode
	var lcd = new Lcd(Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5, Pins.GPIO_PIN_D6, Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D9);

or

	// EA DOG using SPI:
	var config = new SPI.Configuration(Pins.GPIO_PIN_D10, false, 5, 5, false, false, 48, SPI_Devices.SPI1);
	var lcd = new Lcd(Lcd.Device.DOGM162_33V, config, Pins.GPIO_PIN_D9);

For the Netduino Mini, simply change the pin IDs as appropriate.

Then initialise the display:

	lcd.Reset();
	lcd.SetDisplay(true, true, true);
	lcd.SetFunction(Lcd.LineMode.TwoLine, Lcd.Font.SingleHeight);

To write to the display:

	var msg1 = "ABCDEFGHIJKLMNOP";
	var msg2 = "0123456789<>*&+-";
	for(int i = 0; i < msg1.Length; i++)
	{
		lcd.SetCursorAddress(0x00 + (uint)i);
		lcd.Write(msg1[i]);
		lcd.SetCursorAddress(0X40 + (uint)i);
		lcd.Write(msg2[i]);
		Thread.Sleep(250);
	}

To define and display user-defined characters:

	var udc1 = new byte[] { 0x0E, 0x11, 0x0E, 0x04, 0x1F, 0x04, 0x0A, 0x11 };  // figure
	var udc2 = new byte[] { 0x1F, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1F };  // box
	var udc3 = new byte[] { 0x15, 0x0A, 0x15, 0x0A, 0x15, 0x0A, 0x15, 0x0A };  // checkerboard
	lcd.SetUserdefinedChar(0, udc1);
	lcd.SetUserdefinedChar(1, udc2);
	lcd.SetUserdefinedChar(2, udc3);
	lcd.SetCursorAddress(0);
	lcd.Write(0);
	lcd.Write(1);
	lcd.Write(2);



## uk.andyjohnson0.Netduino.Drivers.Motor.HBridgeSN754410 Class

(To be done)


## uk.andyjohnson0.Netduino.Drivers.Gps namespace

The classes in this namespace implement an event-based pipeline for extracting NMEA message frames
from a GPS device's serial data stream and parsing a subset of those messages into an object
representation. Classes are provided for representing terrestrial locations as latitude/longitude
coordinates or UK Ordnance Survey gride references.

First add the following using directive to your code:

    using uk.andyjohnson0.Netduino.Drivers.Gps;

Create an NmeaMessageDecoder and an NmeaMessageMonitor:

	var messageDecoder = new NmeaDecoder();
    var messageMonitor = new NmeaMonitor();

Create serial connection to the GPS hardware. Typically use 4800 for EM406a and 9600 for Sparkfun breakout board.

    GenericSerialGps gps = new GenericSerialGps(SerialPorts.COM1, 4800);
    gps.MessageReceived += (sender, e) => { messageMonitor.OnMessage(messageDecoder.Decode(e.Message)); };
	
Periodically sample the values cached by messageMonitor:

	while(true)
	{
		if ((messageMonitor.CurrentGpsTime != DateTime.MinValue) && (messageMonitor.LastFixCoord != null))
		{
			var gridRef = new OSGBGridRef(messageMonitor.LastFixCoord);
			Debug.WriteLine("At {0} {1} LatLon={2} GridRef={3}",
			                messageMonitor.CurrentGpsTime.ToShortDateString(), messageMonitor.CurrentGpsTime.ShortTimeString(),
                            messageMonitor.LastFixCoord.ToString(), gridRef.ToString());
		}
		Thread.Sleep(100);
	}


## Author

Andrew Johnson | http://uk.andyjohnson0.me.uk | andyjohnson0@gmail.com | @andyjohnson0


## License

The MIT License (MIT)

Copyright (c) 2015 Andrew Johnson
except for the contents of OSGBGridRef.cs which are derived from work by
Roger Muggleton at http://www.dorcus.co.uk/carabus/ll_ngr.html

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
