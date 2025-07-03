namespace KeyDrop;

using Iot.Device.Uln2003;
using KeyDrop.Messages;
using nanoFramework.Networking;
using SimonSays;
using System;
using System.Device.Wifi;
using System.Diagnostics;
using System.IO;
using System.Threading;

public class Program
{
    public static void Main()
    {
        var connectionString = LoadConnectionString();

        if (!ConnectToWiFi("dropitlikeaSquat", "DaisyToddAndButt"))
        {
            return;
        }

        var bus = new MessageBus(connectionString, "keydrop_dispenser");
        bus.On(typeof(DropKey), () =>
        {

        });

        const int bluePin = 4;
        const int pinkPin = 17;
        const int yellowPin = 27;
        const int orangePin = 22;

        using (Uln2003 motor = new Uln2003(bluePin, pinkPin, yellowPin, orangePin))
        {
            while (true)
            {
                // Set the motor speed to 15 revolutions per minute.
                motor.RPM = 15;
                // Set the motor mode.  
                motor.Mode = StepperMode.HalfStep;
                // The motor rotate 2048 steps clockwise (180 degrees for HalfStep mode).
                motor.Step(2048);

                motor.Mode = StepperMode.FullStepDualPhase;
                motor.RPM = 8;
                // The motor rotate 2048 steps counterclockwise (360 degrees for FullStepDualPhase mode).
                motor.Step(-2048);

                motor.Mode = StepperMode.HalfStep;
                motor.RPM = 1;
                motor.Step(4096);
            }
        }

    }

    private static bool ConnectToWiFi(string ssid, string password)
    {
        WifiAdapter wa = WifiAdapter.FindAllAdapters()[0];
        wa.Disconnect();

        CancellationTokenSource cs = new(30000);
        Console.WriteLine("ConnectDHCP");
        WifiNetworkHelper.Disconnect();
        bool success;

        success = WifiNetworkHelper.ConnectDhcp(ssid, password, WifiReconnectionKind.Automatic, true, token: cs.Token);

        if (!success)
        {
            wa.Disconnect();
            var res = wa.Connect(ssid, WifiReconnectionKind.Manual, password);
            success = res.ConnectionStatus == WifiConnectionStatus.Success;
        }

        Console.WriteLine($"ConnectDHCP exit {success}");
        return success;
    }

    private static string LoadConnectionString()
    {
        using (var file = File.OpenRead("I:\\connection.sys"))
        {
            using (var reader = new StreamReader(file))
            {
                return reader.ReadLine();
            }
        }
    }
}