using Dapper;
using EmailDeliveryService.Infrastructure;
using EmailDeliveryService.Model;
using EmailDeliveryService.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmailDeliveryService.Templates
{
    public abstract class BaseTemplate : ITemplate<MailData>
    {
        public BaseTemplate()
        {

        }

        public abstract string TemplateName { get; }

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

        //protected async Task<Template> ReadAsync(NewsLettersContext dataConext, string templateName)
        //{
        //    return await dataConext.Templates.SingleAsync(t => t.Name == templateName);
        //}

        [Obsolete("Don't use. Will be removed in future versions")]
        protected async Task<TemplateOld> ReadAsync(IDbConnection connection, string templateName)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            var result = await connection.QuerySingleAsync<TemplateOld>($"nl.TemplateRead", new { templateName }, commandTimeout: 0, commandType: CommandType.StoredProcedure);
            return result;
        }

        //protected async Task UpdateAsync(IDbConnection connection, string templateName, bool IsUpdating)
        //{
        //    if (connection.State != ConnectionState.Open)
        //    {
        //        connection.Open();
        //    }

        //    _ = await connection.ExecuteAsync($"nl.TemplateUpdate", new { templateName, IsUpdating }, commandTimeout: 0, commandType: CommandType.StoredProcedure);
        //}



    }
}
