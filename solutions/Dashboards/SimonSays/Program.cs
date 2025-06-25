using Microsoft.AspNetCore.Mvc.ViewComponents;
using SimonSays.Hubs;
using SimonSays.Messages;
using SimonSays.Pages;

namespace SimonSays
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<MessageBusService>();

            builder.Services.AddHostedService<MessageBusService>(svc => svc.GetService<MessageBusService>());

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.MapHub<EventsHub>("/eventshub");

            var messageBus = app.Services.GetService<MessageBusService>();
            messageBus.On<CaptureInput>(message =>
            {
                Console.WriteLine($"Message {message.ButtonNumber} ");
            });

            messageBus.On<PuzzleSolved>(msg =>
            {
                Console.WriteLine("Puzzle solved");
            });

            messageBus.Route<ShowSequence>("Simonsays_puzzle");

            app.Run();
        }
    }
}
