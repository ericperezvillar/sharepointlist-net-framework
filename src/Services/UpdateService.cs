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
    public class UpdateService
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger _logger;

        public UpdateService(AsyncRetryPolicy retryPolicy, ILogger logger)
        {
            _retryPolicy = retryPolicy;
            _logger = logger;
        }

        public async Task BatchUpdateAsync(ClientContext context, List list, List<TaskInfo> itemsToUpdate)
        {
            const int batchSize = 100;

            _logger.Information($"Number of items to update {itemsToUpdate.Count}");

            try
            {
                foreach (var batch in itemsToUpdate.BatchSharepointData(batchSize))
                {
                    foreach (var taskInfo in batch)
                    {
                        var query = new CamlQuery
                        {
                            ViewXml = $@"<View><Query>
                                    <Where><And><Eq><FieldRef Name='TaskCode'/><Value Type='Text'>{taskInfo.Code}</Value>
                                        </Eq><And><Eq><FieldRef Name='Name'/><Value Type='Text'>{taskInfo.Name}</Value>
                                        </Eq><And><Eq><FieldRef Name='Title'/><Value Type='Text'>{taskInfo.Title}</Value>
                                        </Eq><And><Eq><FieldRef Name='Start_x0020_Date'/><Value Type='DateTime'>{taskInfo.StartDate.ToString("yyyy-MM-dd")}</Value></Eq>
                                        <Eq><FieldRef Name='End_x0020_Date'/><Value Type='DateTime'>{taskInfo.EndDate.ToString("yyyy-MM-dd")}</Value></Eq></And></And></And>
                                    </Where></Query>
                                    <RowLimit>1</RowLimit></View>"
                        };

                        var items = list.GetItems(query);
                        context.Load(items);
                        await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());

                        if (items.Count > 0)
                        {
                            var item = items[0];
                            item["Rate"] = taskInfo.Rate;
                            item.Update();
                        }
                    }

                    await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());
                    _logger.Information($"Updated batch of {batchSize} items.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while updating existing records from Sharepoint.");
                throw ex;
            }
        }
    }
}
