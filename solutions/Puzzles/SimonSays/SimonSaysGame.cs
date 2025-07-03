namespace SimonSays;

using SimonSays.Messages;
using System;
using System.Device.Gpio;
using System.Threading;

public class SimonSaysGame
{
    private readonly LedController ledController;
    private readonly int[] buttonPins;
    private int difficulty;
    private int[] sequence;
    private int userStep;
    private readonly AutoResetEvent inputEvent = new AutoResetEvent(false);
    private int lastButtonPressed = -1;
    private MessageBus bus;

    public SimonSaysGame(LedController ledController, GpioController gpioController, int[] buttonPins, int difficulty, MessageBus bus)
    {
        this.ledController = ledController;
        this.buttonPins = buttonPins;
        this.difficulty = difficulty;
        this.bus = bus;

        for (var i = 0; i < this.buttonPins.Length; i++)
        {
            var button = gpioController.OpenPin(this.buttonPins[i], PinMode.InputPullDown);
            button.DebounceTimeout = TimeSpan.FromMilliseconds(100);
            var buttonNumber = i + 1;

            button.ValueChanged += (s, e) =>
            {
                if (e.ChangeType == PinEventTypes.Rising)
                {
                    CaptureInput(buttonNumber);
                }
            };
        }
    }

    public void Run()
    {
        while (true)
        {
            userStep = 0;

            // Show the sequence
            ShowSequence();

            // Wait for user input
            bool success = true;
            for (userStep = 0; userStep < sequence.Length; userStep++)
            {
                lastButtonPressed = -1;
                inputEvent.WaitOne();

                if (lastButtonPressed == sequence[userStep])
                {
                    // Flash the LED for feedback
                    ledController.SetLed(lastButtonPressed, true);
                    ledController.SetLed(15, true);
                    Thread.Sleep(200);

                    ledController.SetLed(lastButtonPressed, false);
                    ledController.SetLed(15, false);
                }
                else
                {
                    // Wrong button, restart
                    ShowFailed();
                    success = false;

                    bus.Publish(new PuzzleFailed());
                    break;
                }
            }

            if (success)
            {
                // Success feedback
                ShowSolved();

                bus.Publish(new PuzzleSolved());
                return;
            }

            Thread.Sleep(1000);
        }
    }

    public void ChangeDifficulty(int newDifficulty)
    {
        difficulty = newDifficulty;
        sequence = GenerateRandomSequence(difficulty * 2, buttonPins.Length);
    }

    public void ShowSolved()
    {
        ledController.FlashAllLeds(2, 400, 15);
        ledController.SetLed(15, true);
    }

    public void ShowSequence()
    {
        ledController.SetLed(14, true);

        for (int i = 0; i < difficulty * 2; i++)
        {
            ledController.SetLed(sequence[i], true);
            Thread.Sleep(600);
            ledController.SetLed(sequence[i], false);
            if (i < difficulty - 1)
            {
                Thread.Sleep(150);
            }
        }

        ledController.SetLed(14, false);
    }

    public void ResetPattern()
    {
        sequence = GenerateRandomSequence(difficulty * 2, buttonPins.Length);
        ShowSequence();

        bus.Publish(new PatternReset { NewPattern = sequence });
    }

    public void ShowFailed()
    {
        ledController.FlashAllLeds(3, 150, 13);
    }

    public void CaptureInput(int buttonNumber)
    {
        lastButtonPressed = buttonNumber;
        inputEvent.Set();

        bus.Publish(new InputCaptured { ButtonNumber = buttonNumber });
    }

    private int[] GenerateRandomSequence(int length, int maxButton)
    {
        var rand = new Random();
        var seq = new int[length];
        for (int i = 0; i < length; i++)
        {
            seq[i] = rand.Next(maxButton) + 1;
            Console.Write($"{seq[i]}, ");
        }
        Console.WriteLine();
        return seq;
    }
}