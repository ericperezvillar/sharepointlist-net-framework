using ListViewSharepoint.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ListViewSharepoint.Access
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private ILogger _logger;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
            _logger = Log.ForContext<DatabaseService>();
        }

        public async Task<List<TaskInfo>> GetTaskInfoAsync()
        {
            var taskInfoList = new List<TaskInfo>();

            try
            {
                _logger.Information("Fetching task information from database...");

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT Code, Name, Title, StartDate, EndDate, Rate FROM Table";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 60; // Set timeout to 60 seconds

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var taskInfo = new TaskInfo
                                {
                                    Code = reader["Code"].ToString(),
                                    Title = reader["Title"].ToString(),
                                    StartDate = Convert.ToDateTime(reader["StartDate"]),
                                    EndDate = Convert.ToDateTime(reader["EndDate"]),
                                    Rate = Convert.ToDouble(reader["Rate"]),
                                    Name = reader["Name"].ToString()
                                };
                                taskInfoList.Add(taskInfo);
                            }
                        }
                    }
                }

                _logger.Information("Task information fetched successfully from database.");

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching task information from database.");
                throw;
            }
            

            return taskInfoList;
        }
    }
}
