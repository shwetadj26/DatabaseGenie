
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamSaga.Services;

namespace TeamSaga.Controllers
{
    public class OAIController : Controller
    {
        private readonly QueryService _queryService;
        private readonly IConfiguration _config;
        private readonly ILogger<QueryService> _logger;

        public OAIController(IConfiguration configuration,QueryService queryService)
        {
            _queryService = queryService;
            _config = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSqlQuery(string userQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userQuery))
                {
                    return BadRequest("User query cannot be empty.");
                }

                var sqlQuery = await _queryService.GetSqlQuery(userQuery);
                return Json(new { sqlQuery });
            }
            catch (Exception)
            {

                throw;
            }
        }


        [HttpPost]
        //public async Task<IActionResult> ExecuteSqlQuery(string sqlQuery)
        //{
        //    var result = await _queryService.ExecuteSqlQuery(sqlQuery);
        //    return Json(new { result });
        //}

        public async Task<IActionResult> ExecuteSqlQuery(string sqlQuery)
        {
            try
            {
                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var command = new SqlCommand(sqlQuery, connection);
                using var reader = await command.ExecuteReaderAsync();

                var result = new Dictionary<string, List<string>>();
                var table = new DataTable();
                table.Load(reader);

                foreach (DataColumn column in table.Columns)
                {
                    result[column.ColumnName] = new List<string>();
                }

                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        result[column.ColumnName].Add(row[column].ToString());
                    }
                }

                return Json(result); // Return data as JSON
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
