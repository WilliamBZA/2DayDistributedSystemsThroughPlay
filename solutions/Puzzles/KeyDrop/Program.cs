namespace KeyDrop;

using Iot.Device.Uln2003;
using KeyDrop.Messages;
using nanoFramework.Networking;
using SimonSays;
using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.IO;
using System.Threading;

public class Program
{
    public static void Main()
    {
        var connectionString = LoadConnectionString();

        //if (!ConnectToWiFi("dropitlikeaSquat", "DaisyToddAndButt"))
        //{
        //    return;
        //}

        //var bus = new MessageBus(connectionString, "keydrop_dispenser");
        //bus.On(typeof(DropKey), () =>
        //{

        //});
        while (true)
        {
            TestMotorPins(1, 2, 7, 6);
        }
        //TestMotorPins(5,6,8,7);
        //TestMotorPins(5,7,6,8);
        //TestMotorPins(5,7,8,6);
        //TestMotorPins(5,8,6,7);
        //TestMotorPins(5,8,7,6);
        //TestMotorPins(6,5,7,8);
        //TestMotorPins(6,5,8,7);
        //TestMotorPins(6,7,5,8);
        //TestMotorPins(6,7,8,5);
        //TestMotorPins(6,8,5,7);
        //TestMotorPins(6,8,7,5);
        //TestMotorPins(7,5,6,8);
        //TestMotorPins(7,5,8,6);
        //TestMotorPins(7,6,5,8);
        //TestMotorPins(7,6,8,5);
        //TestMotorPins(7,8,5,6);
        //TestMotorPins(7,8,6,5);
        //TestMotorPins(8,5,6,7);
        //TestMotorPins(8,5,7,6);
        //TestMotorPins(8,6,5,7);
        //TestMotorPins(8,6,7,5);
        //TestMotorPins(8,7,5,6);
        //TestMotorPins(8, 7, 6, 5);
    }

    private static void TestMotorPins(int first, int second, int third, int fourth)
    {
        Console.WriteLine($"Trying pins: {first}, {second}, {third}, {fourth}");
        using (Uln2003 motor = new Uln2003(first, second, third, fourth, new GpioController()))
        {
            motor.Mode = StepperMode.FullStepSinglePhase;
            motor.RPM = 15;
            // The motor rotate 2048 steps counterclockwise (360 degrees for FullStepDualPhase mode).
            Console.WriteLine($"{motor.RPM} {motor.Mode} 2048");
            motor.Step(2048);
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