using System;
using System.Configuration;
using System.Threading.Tasks;
using ListViewSharepoint.Access;
using ListViewSharepoint.Services;
using Serilog;

namespace ListViewSharepoint
{
    public class Program
    {        
        public static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("Logs/logfile.txt", rollingInterval: RollingInterval.Day)
                //.ReadFrom.AppSettings()
                .CreateLogger();

            // Read configuration settings
            string sqlConnectionString = ConfigurationManager.ConnectionStrings["SqlDatabase"].ConnectionString;
            string sharePointSiteUrl = ConfigurationManager.AppSettings["SharePoint:SiteUrl"];
            string sharePointUsername = ConfigurationManager.AppSettings["SharePoint:Username"];
            string sharePointPassword = ConfigurationManager.AppSettings["SharePoint:Password"];
            string sharePointListName = ConfigurationManager.AppSettings["SharePoint:ListName"];

            // Initialize services
            DatabaseService databaseService = new DatabaseService(sqlConnectionString);
            SharePointService sharePointService = new SharePointService(sharePointSiteUrl, sharePointUsername, sharePointPassword);


            try
            {
                Log.Information("Data transfer started. Date: {DateTime}", DateTime.Now);

                // Retrieve data from database
                var taskInfoList = await databaseService.GetTaskInfoAsync();

                // Update SharePoint list
                await sharePointService.UpdateSharePointListAsync(taskInfoList, sharePointListName);

                Log.Information("Data transfer completed successfully. Date: {DateTime}", DateTime.Now);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred: {ErrorMessage}", ex.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }       
        
}
