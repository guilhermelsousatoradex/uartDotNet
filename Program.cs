using System;
using System.IO.Ports;
using System.Threading;
using NmeaParser; // Needed for NmeaMessage.Parse(...)
// No "using NmeaParser.Messages;" to avoid conflicts with the library's NmeaMessageReceivedEventArgs

namespace gps
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort serialDev;
            SerialPortDevice gps;

            // serial configs
            serialDev = new SerialPort();
            serialDev.PortName = Environment.GetEnvironmentVariable("GPS_SERIAL_PORT");
            serialDev.BaudRate = 9600;

            // Wrap the SerialPort in our custom SerialPortDevice
            gps = new SerialPortDevice(serialDev);

            // set listener
            gps.MessageReceived += OnNmeaMessageReceived;
            gps.OpenAsync();

            // on ctrl+c close connection
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
            {
                gps.CloseAsync();
            };

            // wait for gps inputs
            Thread.Sleep(Timeout.Infinite);
        }

        static void OnNmeaMessageReceived(object sender, NmeaMessageReceivedEventArgs args)
        {
            // Fully qualify the Rmc type from NmeaParser.Messages
            if (args.Message is NmeaParser.Messages.Rmc rmc)
            {
                Console.WriteLine($"Latitude::{rmc.Latitude}\tLongitude::{rmc.Longitude}");
            }
        }
    }

    /// <summary>
    /// A custom EventArgs class that holds the parsed NmeaMessage.
    /// We name it "NmeaMessageReceivedEventArgs" (same as older NmeaParser code),
    /// but it's our own definition to avoid conflicts with the library's version.
    /// </summary>
    public class NmeaMessageReceivedEventArgs : EventArgs
    {
        public NmeaMessage Message { get; }

        public NmeaMessageReceivedEventArgs(NmeaMessage message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// A custom "SerialPortDevice" class that opens a SerialPort, reads lines,
    /// parses them via NmeaParser, and raises MessageReceived.
    /// </summary>
    public class SerialPortDevice
    {
        private readonly SerialPort _port;

        // Our event uses the custom NmeaMessageReceivedEventArgs defined above
        public event EventHandler<NmeaMessageReceivedEventArgs> MessageReceived;

        public SerialPortDevice(SerialPort port)
        {
            _port = port;
        }

        public void OpenAsync()
        {
            // Subscribe to the serial port's DataReceived event
            _port.DataReceived += OnDataReceived;
            _port.Open();
        }

        public void CloseAsync()
        {
            if (_port.IsOpen)
                _port.Close();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Read one line from the serial port
                string line = _port.ReadLine();

                // Parse the NMEA sentence (e.g. $GPRMC)
                var message = NmeaMessage.Parse(line);

                // Raise our event with a new NmeaMessageReceivedEventArgs
                MessageReceived?.Invoke(this, new NmeaMessageReceivedEventArgs(message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading/parsing: {ex.Message}");
            }
        }
    }
}
