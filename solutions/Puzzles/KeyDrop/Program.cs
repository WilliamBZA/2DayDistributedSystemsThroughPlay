namespace KeyDrop;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

public class Program
{
    public static void Main()
    {
        var connectionString = LoadConnectionString();

        Thread.Sleep(Timeout.Infinite);
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