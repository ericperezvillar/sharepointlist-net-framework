using ListViewSharepoint.Utility;
using ListViewSharepoint.Models;
using Microsoft.SharePoint.Client;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListViewSharepoint.Services
{
    public class InsertService
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger _logger;

        public InsertService(AsyncRetryPolicy retryPolicy, ILogger logger)
        {
            _retryPolicy = retryPolicy;
            _logger = logger;
        }

        public async Task BatchInsertAsync(ClientContext context, List list, List<TaskInfo> itemsToInsert)
        {
            const int batchSize = 100;

            _logger.Information($"Number of items to insert {itemsToInsert.Count}");

            try
            {
                foreach (var batch in itemsToInsert.BatchSharepointData(batchSize))
                {
                    foreach (var taskInfo in batch)
                    {
                        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                        ListItem newItem = list.AddItem(itemCreateInfo);

                        newItem["Code"] = taskInfo.Code;
                        newItem["Start_x0020_Date"] = taskInfo.StartDate.ToString("yyyy-MM-dd");
                        newItem["End_x0020_Date"] = taskInfo.EndDate.ToString("yyyy-MM-dd");
                        newItem["Rate"] = taskInfo.Rate;
                        newItem["Name"] = taskInfo.Name;
                        newItem["Title"] = taskInfo.Title;

                        newItem.Update();
                    }

                    await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());
                    _logger.Information($"Inserted batch of {batchSize} items.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while inserting new records to Sharepoint.");
                throw ex;
            }

        }
    }
}
