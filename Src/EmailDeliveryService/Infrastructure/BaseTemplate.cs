using Dapper;
using EmailDeliveryService.Model;
using EmailDeliveryService.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailDeliveryService.Infrastructure
{
    public abstract class BaseTemplate : ITemplate<MailData>
    {
        public BaseTemplate()
        {

        }
        public abstract Task<PaginationViewModel<MailData>> GetDataAsync(IDbConnection connection, int pageNumber, int pageSize);

        public abstract Task LoadDataAsync(Dictionary<string, IDbConnection> connenctionManagers);

        protected PaginationViewModel<T> PaginationViewModel<T>(int pageSize, int pageNumber, int recordCount, IEnumerable<T> values)
        {
            var totalPages = Math.Ceiling(((double)recordCount / pageSize));
            return new PaginationViewModel<T>()
            {
                CurrentPage = pageNumber,
                Data = values,
                PageSize = pageSize > recordCount ? recordCount : pageSize,
                RecordCount = recordCount,
                TotalPages = (int)totalPages
            };
        }

        protected async Task<Template> TemplateReadAsync(IDbConnection connection, string templateName)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            var result = await connection.QuerySingleAsync<Template>($"nl.TemplateRead", new { templateName }, commandTimeout: 0, commandType: CommandType.StoredProcedure);
            return result;
        }

        protected async Task TemplateUpdateAsync(IDbConnection connection, string templateName, bool IsUpdating)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            _ = await connection.ExecuteAsync($"nl.TemplateUpdate", new { templateName, IsUpdating }, commandTimeout: 0, commandType: CommandType.StoredProcedure);
        }
    }
}
