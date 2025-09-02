namespace ChestOpener;

using nanoFramework.Networking;
using nanoFramework.WebServer;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

public class Program
{
    public static void Main()
    {
        WifiNetworkHelper.ConnectDhcp("dropitlikeaSquat", "DaisyToddAndButt", requiresDateTime: true);

        var ipAddress = IPGlobalProperties.GetIPAddress().ToString();
        Console.WriteLine($"IP Address: {ipAddress}");

        using (WebServer server = new WebServer(80, HttpProtocol.Http, new[] { typeof(Controller) } ))
        {
            // Start the server.
            server.Start();
            
            Thread.Sleep(Timeout.Infinite);
        }
    }
}

public class Controller
{
    public static Random random = new Random();

    [Route("api/openchest")]
    [Method("GET")]
    public void OpenChest(WebServerEventArgs e)
    {
        var randomNumber = random.Next(100);
        if (randomNumber <= 5)
        {
            WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            return;
        }
        else if (randomNumber <= 7)
        {
            Thread.Sleep(60000);
        }

        var response = "{\"IsOpening\": true}";
        e.Context.Response.ContentType = "text/json";
        WebServer.OutPutStream(e.Context.Response, response);
        WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
    }
}