using Dapper;
using EmailDeliveryService.Extensions;
using EmailDeliveryService.Model;
using EmailDeliveryService.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EmailDeliveryService.Templates
{

    public class FirstTemplate : BaseTemplate
    {
        private class FirstTemplateParam
        {
            [JsonPropertyName("surname")]
            public string Surname { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("email")]
            public string Email { get; set; }

        }


        private readonly string sql = @"SELECT TOP 2000 CardId = t.Tessera,
                                             Name = a.Ragione_Sociale,
                                             SurName = a.Ragione_Sociale1,
                                             Email = N'umapathy.ramprakash@kasanova.it' --,--a.Email
                                        FROM Sede.dbo.TessereCRM AS t
                                        INNER JOIN[Storico].[dbo].[anagrafiche] AS a ON t.Tessera = a.Codice_Anagrafica
                                        WHERE(ISNULL(StatoTessera, 0) = 1) 
                                                    AND (Tessera LIKE 'KN02%')
                                                    AND ISNULL(A.Email, '') <> ''";

        private readonly string deletesql = @"DELETE ms FROM 
				                                nl.Mails AS m INNER JOIN
				                                nl.MailStatus AS ms ON m.Id = ms.MailId
		                                    WHERE M.TemplateId = @templateid
                                            private readonly string _name;
                                            DELETE FROM nl.Mails WHERE TemplateId = @templateid";

        public override string TemplateName => this.GetType().Name;

        //public override string TemplateName { return this.GetType().Name;}

        public FirstTemplate()
    {

    }


    public override async Task<PaginationViewModel<MailData>> GetDataAsync(IDbConnection connection, int pageNumber, int pageSize)
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
        var results = await connection.QueryMultipleAsync($"nl.{TemplateName}GetData", new { templatename = TemplateName, pageSize, pageNumber }, commandTimeout: 0, commandType: CommandType.StoredProcedure);
        var recordCount = results.Read<int>().First();
        var mails = await results.ReadAsync<MailData>();

        return base.PaginationViewModel<MailData>(pageSize, pageNumber, recordCount, mails);
    }

    public async override Task LoadDataAsync(Dictionary<string, IDbConnection> connenctionManagers)
    {

        if (!connenctionManagers.TryGetValue("newsletters", out IDbConnection nlConn)) throw new KeyNotFoundException("Connection string newsletters not found");
        if (!connenctionManagers.TryGetValue("sede", out IDbConnection sedeConn)) throw new KeyNotFoundException("Connection string sede not found");

        var template = await ReadAsync(nlConn, TemplateName);
        var reader = await sedeConn.ExecuteReaderAsync(sql, commandTimeout: 0, commandType: CommandType.Text);

        SqlConnection conn = null;
        if (nlConn is SqlConnection connection)
        {
            conn = connection;
        }

        using (var tr = conn.BeginTransaction())
        {
            try
            {
                //Delete all previous mails for this template
                await conn.ExecuteAsync("nl.MailsDelete", new { templateid = template.Id }, transaction: tr, commandTimeout: 0, commandType: CommandType.StoredProcedure);

                var jsonParam = JsonSerializer.Deserialize<FirstTemplateParam>(template.BodyParameters);

                while (reader.Read())
                {
                    string email = reader.GetValueOrDefault<string>("Email");
                    string cardId = reader.GetValueOrDefault<string>("CardId");
                    string name = reader.GetValueOrDefault<string>("Name");
                    string surName = reader.GetValueOrDefault<string>("SurName");

                    jsonParam.Email = email;
                    jsonParam.Name = name;
                    jsonParam.Surname = surName;

                    string jsonData = JsonSerializer.Serialize<FirstTemplateParam>(jsonParam);
                    var data = MailData.CreateInstance(cardId, email, "", "", jsonData);
                    var parameeters = new { date = DateTime.Now.Date, templateid = template.Id, cardId = data.CardId, emailid = data.EmailId, emailidcc = data.EmailIdCC, emailidbcc = data.EmailIdBCC, bodyparameters = data.BodyParametersData, mailstatus = MailStatus.Prepared };

                    await conn.ExecuteAsync("nl.MailsInsert", param: parameeters, transaction: tr, commandTimeout: 0, commandType: CommandType.StoredProcedure);
                }

                tr.Commit();
            }
            catch (Exception ex)
            {
                tr.Rollback();
                throw ex;
            }
        }
    }
}

}

