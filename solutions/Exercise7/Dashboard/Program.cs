using Azure.Messaging.ServiceBus;
using MessagingHelper;

namespace Dashboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var connectionString = "connectionstring";

            // Configure Azure Service Bus
            builder.Services.AddSingleton(sp =>
            {
                return new ServiceBusClient(connectionString);
            });

            var app = builder.Build();

            var serviceBusHelper = new ServiceBusHelper(connectionString);
            serviceBusHelper.EnsureQueueExists("williamdashboard").GetAwaiter().GetResult();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
