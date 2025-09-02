using Amqp;
using nanoFramework.Networking;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace MaglockSubscriber
{
    public class Program
    {
        private static GpioPin led;

        public static void Main()
        {
            ConfigurePins();
            ConnectToWiFi();

            StartServiceBusReceiver();

            Thread.Sleep(Timeout.Infinite);
        }

        private static void StartServiceBusReceiver()
        {
            var connection = new Connection(new Address("amqp connection string"));
            var session = new Session(connection);
            var receiverLink = new ReceiverLink(session, "Esp32 receiver Link", "williamcluedispenser");

            receiverLink.Start(5, (receiver, message) =>
            {
                // Extraxt actual message from message body
                var bytes = (byte[])message.Body;
                var bodyText = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                Console.WriteLine($"Message text: {bodyText}");

                led.Write(PinValue.Low);

                // Accept the message, telling Azure it can be removed from the queue
                receiver.Accept(message);
            });
        }

        private static void ConfigurePins()
        {
            var gpioController = new GpioController();
            led = gpioController.OpenPin(5, PinMode.Output);

            led.Write(PinValue.High);
        }

        private static void ConnectToWiFi()
        {
            var connected = WifiNetworkHelper.ConnectDhcp("dropitlikeaSquat", "DaisyToddAndButt", requiresDateTime: true);
            if (connected)
            {
                var ipAddress = IPGlobalProperties.GetIPAddress().ToString();
                Console.WriteLine($"Connected to WiFi. IP Address {ipAddress}");
            }
        }
    }
}
