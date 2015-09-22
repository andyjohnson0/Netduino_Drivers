using System;
using System.Text;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
#if NETDUINO_1
using SecretLabs.NETMF.Hardware.Netduino;
#elif NETDUINO_MINI
using SecretLabs.NETMF.Hardware.NetduinoMini;
#endif


using System.IO;
using System.IO.Ports;


namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    /// <summary>
    /// A simple event driven NMEA message extracter for use with GPS receovers that provide a serial communication interface.
    /// Uses a Netduino serial port.
    /// </summary>
    public class GenericSerialGps
    {
        /// <summary>
        /// Constructor. Initialise a GenericSerialGps object.
        /// Serial connection to the GPS hardware uses 4800 baud, no parity, data data bits, no stop bit.
        /// </summary>
        /// <param name="serialPortName"></param>
        public GenericSerialGps(string serialPortName)
        {
            this.serialPort = new SerialPort(serialPortName, 4800, Parity.None, 8, StopBits.One);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
        }


        /// <summary>
        /// Constructor. Initialise a GenericSerialGps object.
        /// Serial connection to the GPS hardware uses specified baud rate, no parity, data data bits, no stop bit.
        /// </summary>
        /// <param name="serialPortName"></param>
        /// <param name="baudrate"></param>
        public GenericSerialGps(string serialPortName, int baudrate)
        {
            this.serialPort = new SerialPort(serialPortName, baudrate, Parity.None, 8, StopBits.One);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
        }


        private const int maxNmeaSentenceLen = 90; // Actually 80 but ...
        private const char nmeaStartChar = '$';
        private /*const*/ char[] nmeaEndChars = new char[2] { (char)0x0D, (char)0x0A };  // CR,LF
        private /*const*/ char[] nmeaSeperator = new char[1] { ',' };

        private SerialPort serialPort;

        private bool inNmeaSentence = false;
        private byte[] sentenceBuff = new byte[maxNmeaSentenceLen];
        private int sentenceBuffLen = 0;



        #region Events

        /// <summary>
        /// Event class for the MessageReceived event.
        /// </summary>
        public class MessageReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// An  NMEA message string.
            /// </summary>
            public string Message { get; internal set; }
        }

        public delegate void MessageReceivedHandler(object sender, MessageReceivedEventArgs e);

        /// <summary>
        /// Event fired when a complete NMEA message has been received.
        /// </summary>
        public event MessageReceivedHandler MessageReceived;

        #endregion Events


        /// <summary>
        /// Start receiving NMEA messages on the serial port and firing the MessageReceived event when a complete message has been received.
        /// </summary>
        public void Start()
        {
            if (!serialPort.IsOpen)
                serialPort.Open();
        }


        /// <summary>
        /// Stop receiving NMEA messages and firing the MessageReceived event.
        /// </summary>
        public void Stop()
        {
            if (serialPort.IsOpen)
                serialPort.Close();
        }


        /// <summary>
        /// Time since last full and valid message was received.
        /// If no messages have yet been received then equals TimeSpan.MaxValue.
        /// </summary>
        public TimeSpan LastMessageTimeOffset
        {
            get
            {
                if (lastMessageTime != DateTime.MinValue)
                    return DateTime.UtcNow - lastMessageTime;
                else
                    return TimeSpan.MaxValue;
            }
        }
        private DateTime lastMessageTime = DateTime.MinValue;



        #region Message Handling

        // See http://www.gpsinformation.org/dale/nmea.htm#nmea
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] b = new byte[1];

            while (serialPort.BytesToRead > 0)
            {
                serialPort.Read(b, 0, b.Length);

                if (b[0] == nmeaStartChar)
                {
                    // Restart reading sentence.
                    inNmeaSentence = true;
                    sentenceBuffLen = 0;
                    continue;
                }

                if (inNmeaSentence)
                {
                    if (sentenceBuffLen >= maxNmeaSentenceLen)
                    {
                        // Buffer overflow. Wait for a new sentence to start.
                        sentenceBuffLen = 0;
                        inNmeaSentence = false;
                        return;
                    }

                    if ((b[0] == nmeaEndChars[1]) && (sentenceBuffLen >= 1) && (sentenceBuff[sentenceBuffLen - 1] == nmeaEndChars[0]))
                    {
                        // End of sentence
                        lastMessageTime = DateTime.UtcNow;
                        sentenceBuffLen--;
                        inNmeaSentence = false;
                        ProcessNmeaSentence(sentenceBuff, sentenceBuffLen);
                    }
                    else
                    {
                        // Read a byte.
                        sentenceBuff[sentenceBuffLen] = b[0];
                        sentenceBuffLen++;
                    }
                }
            }
        }



        private void ProcessNmeaSentence(
            byte[] sentence,
            int sentenceLen)
        {
            byte[] tmp = new byte[sentenceLen];
            for (int i = 0; i < sentenceLen; i++)
            {
                tmp[i] = sentence[i];
            }
            string sentenceStr = new string(Encoding.UTF8.GetChars(tmp));
            if (sentenceStr == null)
                return;
            sentenceStr = sentenceStr.Trim();

            if (MessageReceived != null)
            {
                MessageReceivedEventArgs e = new MessageReceivedEventArgs();
                e.Message = sentenceStr;
                MessageReceived(this, e);
            }
        }

        #endregion Message Handling

    }
}
