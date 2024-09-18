using ListViewSharepoint.Utility;
using Microsoft.SharePoint.Client;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListViewSharepoint.Services
{
    public class DeleteService
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger _logger;

        public DeleteService(AsyncRetryPolicy retryPolicy, ILogger logger)
        {
            _retryPolicy = retryPolicy;
            _logger = logger;
        }

        public async Task BatchDeleteAsync(ClientContext context, List<ListItem> itemsToDelete)
        {
            const int batchSize = 100;

            _logger.Information($"Number of items to delete {itemsToDelete.Count}");

            try
            {
                foreach (var batch in itemsToDelete.BatchSharepointData(batchSize))
                {
                    foreach (var item in batch)
                    {
                        item.DeleteObject();
                    }

                    await _retryPolicy.ExecuteAsync(async () => await context.ExecuteQueryAsync());
                    _logger.Information($"Deleted batch of {batchSize} items.");
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
