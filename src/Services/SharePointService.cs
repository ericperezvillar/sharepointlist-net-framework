using ListViewSharepoint.Utility;
using ListViewSharepoint.Models;
using Microsoft.SharePoint.Client;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using Serilog;

namespace ListViewSharepoint.Services
{
    public class SharePointService
    {
        private readonly string _siteUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly AsyncRetryPolicy _retryPolicy;
        private ILogger _logger;
        private DeleteService _deleteService;
        private UpdateService _updateService;
        private InsertService _insertService;

        public SharePointService(string siteUrl, string username, string password)
        {
            _siteUrl = siteUrl;
            _username = username;
            _password = password;

            _retryPolicy = Policy
               .Handle<WebException>(ex => (ex.Response as HttpWebResponse)?.StatusCode == (HttpStatusCode)429)
               .Or<ServerException>()
               .Or<WebException>()
               .Or<SocketException>()
               .WaitAndRetryAsync(
                   retryCount: 5,
                   sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(new Random().Next(0, 1000)),
                   onRetry: (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.Warning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                   });

            _logger = Log.ForContext<SharePointService>();
            _deleteService = new DeleteService(_retryPolicy, _logger);
            _insertService = new InsertService(_retryPolicy, _logger);
            _updateService = new UpdateService(_retryPolicy, _logger);
        }

        private ClientContext GetClientContext()
        {
            SecureString securePassword = new SecureString();
            foreach (char c in _password) securePassword.AppendChar(c);

            var credentials = new SharePointOnlineCredentials(_username, securePassword);
            ClientContext context = new ClientContext(_siteUrl) { Credentials = credentials };

            return context;
        }

        public async Task UpdateSharePointListAsync(List<TaskInfo> taskInfoList, string sharepointListName)
        {
            try
            {
                using (ClientContext context = GetClientContext())
                {
                    List list = context.Web.Lists.GetByTitle(sharepointListName);
                    context.Load(list);
                    await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());

                    // Fetch existing items from SharePoint
                    var existingItems = await FetchExistingItemsAsync(context, list);

                    // Determine changes
                    var itemsToInsert = new List<TaskInfo>();
                    var itemsToUpdate = new List<TaskInfo>();
                    var itemsToDelete = new List<ListItem>();

                    foreach (var taskInfo in taskInfoList)
                    {
                        var match = existingItems.FirstOrDefault(item =>
                            item["Title"].ToString() == taskInfo.Title &&
                            item["Code"].ToString() == taskInfo.Code &&
                            item["Name"].ToString() == taskInfo.Name &&
                            Convert.ToDateTime(item["Start_x0020_Date"].ToString()) == taskInfo.StartDate &&
                            Convert.ToDateTime(item["End_x0020_Date"].ToString()) == taskInfo.EndDate);

                        if (match == null)
                        {
                            itemsToInsert.Add(taskInfo);
                        }
                        else
                        {
                            // Check if update is needed
                            bool needsUpdate = match["Rate"].ToString() != taskInfo.Rate.ToString();

                            if (needsUpdate)
                            {
                                itemsToUpdate.Add(taskInfo);
                            }
                            existingItems.Remove(match);
                        }
                    }

                    itemsToDelete = existingItems.ToList();

                    // Batch process changes
                    await _insertService.BatchInsertAsync(context, list, itemsToInsert);
                    await _updateService.BatchUpdateAsync(context, list, itemsToUpdate);
                    await _deleteService.BatchDeleteAsync(context, itemsToDelete);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while updating SharePoint list.");
            }
        }

        private async Task<List<ListItem>> FetchExistingItemsAsync(ClientContext context, List list)
        {
            try
            {
                var existingItems = new List<ListItem>();
                CamlQuery query = new CamlQuery
                {
                    ViewXml = "<View><RowLimit>5000</RowLimit></View>"
                };

                ListItemCollection items = list.GetItems(query);
                context.Load(items);
                await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());

                while (items.Count > 0)
                {
                    existingItems.AddRange(items);
                    if (items.Count < 5000)
                        break;

                    query.ListItemCollectionPosition = items.ListItemCollectionPosition;
                    items = list.GetItems(query);
                    context.Load(items);
                    await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());
                }

                return existingItems;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while fetching existing records from Sharepoint.");
                throw ex;
            }
            
        }
        
    }
}
