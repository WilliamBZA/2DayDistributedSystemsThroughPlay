namespace SimonSays;

using Iot.Device.Mcp23xxx;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Networking;
using SimonSays.Messages;
using System;
using System.Collections;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Wifi;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

public class Program
{
    public static void Main()
    {
        Configuration.SetPinFunction(Gpio.IO08, DeviceFunction.I2C1_DATA);
        Configuration.SetPinFunction(Gpio.IO09, DeviceFunction.I2C1_CLOCK);

        var connectionSettings = new I2cConnectionSettings(1, 0x20, I2cBusSpeed.StandardMode);
        var i2cDevice = I2cDevice.Create(connectionSettings);
        var mcp23017 = new Mcp23017(i2cDevice);

        var ledController = new LedController(mcp23017);
        ledController.TurnAllLedsOff();

        ledController.SetLed(14, true);
        if (!ConnectToWiFi("dropitlikeaSquat", "DaisyToddAndButt"))
        {
            ledController.FlashAllLeds(5, 100, 13);
            return;
        }

        var connectionString = LoadConnectionString();
        var bus = new MessageBus(connectionString, "Simonsays_puzzle");

        var buttonPins = new[] { 4, 5, 13, 7, 6, 10, 3, 2, 19, 18, 12, 1 };
        var gpioController = new GpioController();

        var game = new SimonSaysGame(ledController, gpioController, buttonPins, difficulty: 5, bus);
        
        bus.On(typeof(ShowSequence), game.ShowSequence);
        bus.On(typeof(ShowSolved), game.ShowSolved);
        bus.On(typeof(ResetPattern), game.ResetPattern);
        bus.On(typeof(ShowFailed), game.ShowFailed);
        bus.On(typeof(CaptureInput), (message) =>
        {
            var msg = message as CaptureInput;
            game.CaptureInput(msg.ButtonNumber);
        });

        bus.Route(typeof(InputCaptured), "Puzzle_progress");
        bus.Route(typeof(PuzzleSolved), "Puzzle_progress");
        bus.Route(typeof(PuzzleFailed), "Puzzle_progress");
        bus.Route(typeof(PatternReset), "Puzzle_progress");

        bus.Start();

        game.ChangeDifficulty(5);
        game.Run();
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