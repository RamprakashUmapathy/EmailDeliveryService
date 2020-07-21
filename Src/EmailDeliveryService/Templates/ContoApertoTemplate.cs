using EmailDeliveryService.Infrastructure;
using EmailDeliveryService.Model;
using EmailDeliveryService.Properties;
using EmailDeliveryService.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EmailDeliveryService.Templates
{
    class ContoApertoTemplate : BaseTemplate
    {

        public ContoApertoTemplate()
        {
        }

        public override string TemplateName => this.GetType().Name;

        public override async Task<PaginationViewModel<Mail>> GetDataAsync(IDbConnection connection, int pageNumber, int pageSize)
        {
            if (connection is SqlConnection == false) throw new Exception("The connection string NewsLetters should be of type SqlConnection");
            var destination = (SqlConnection)connection;

            var optionsBuilder = new DbContextOptionsBuilder<NewsLettersContext>();
            optionsBuilder.UseSqlServer(destination.ConnectionString);

            using (NewsLettersContext context = new NewsLettersContext(optionsBuilder.Options))
            {
                Template template = context.Templates.Single(s => s.Name == TemplateName);

                var recordCount = await context.Mails
                                    .Where(m => m.TemplateId == template.Id && m.MailStatus == MailStatus.Prepared)
                                    .CountAsync();

                var pageData = await context.Mails
                                    .Where(m => m.TemplateId == template.Id && m.MailStatus == MailStatus.Prepared)
                                    .OrderBy(o => o.Id)
                                    .Skip(pageSize * pageNumber)
                                    .Take(pageSize)
                                    .ToListAsync();

                return base.PaginationViewModel<Mail>(pageSize, pageNumber, recordCount, pageData);
            }
        }

        public override async Task LoadDataAsync(Dictionary<string, IDbConnection> connenctionManagers)
        {
            if (!connenctionManagers.TryGetValue("sede", out IDbConnection sourceConnection)) throw new KeyNotFoundException("Connection string Sede not found");
            if (!connenctionManagers.TryGetValue("newsletters", out IDbConnection destinationConnection)) throw new KeyNotFoundException("Connection string NewsLetters not found");

            if (sourceConnection is SqlConnection == false) throw new Exception("The connection string Sede should be of type SqlConnection");
            if (destinationConnection is SqlConnection == false) throw new Exception("The connection string NewsLetters should be of type SqlConnection");

            var source = (SqlConnection)sourceConnection;
            var destination = (SqlConnection)destinationConnection;

            await BulkCopy(source, destination);

            await PrepareMailDataAsync(destination.ConnectionString);
        }

        private async Task BulkCopy(SqlConnection source, SqlConnection destination)
        {
            try
            {
                //EFcore
                var optionsBuilder = new DbContextOptionsBuilder<NewsLettersContext>();
                optionsBuilder.UseSqlServer(destination.ConnectionString);

                using (NewsLettersContext context = new NewsLettersContext(optionsBuilder.Options))
                {
                    Template template = context.Templates.Single(s => s.Name == TemplateName);

                    if (context.Mails.Any(m => m.TemplateId == template.Id && m.MailStatus == MailStatus.Prepared))
                    {
                        throw new ApplicationException($"Unable to proceed as there are some previous mails are in not sent condition for the template {template.Name}");
                    }
                }

                //Delete
                using SqlCommand tCmd = new SqlCommand("DELETE FROM [nl].[ContoApertoTemplateData]", destination);
                tCmd.CommandTimeout = 0;
                await tCmd.ExecuteNonQueryAsync();

                using SqlCommand cmd = new SqlCommand(Resources.ContoApertoTemplateLIST, source)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 0
                };
                cmd.Parameters.AddWithValue("@year", DateTime.Now.Date.Year);
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                using (var bulkcopy = new SqlBulkCopy(destination))
                {
                    bulkcopy.ColumnMappings.Add("CardId", "CardId");
                    bulkcopy.ColumnMappings.Add("ShopId", "ShopId");
                    bulkcopy.ColumnMappings.Add("ShopName", "ShopName");
                    bulkcopy.ColumnMappings.Add("Name", "Name");
                    bulkcopy.ColumnMappings.Add("Surname", "Surname");
                    bulkcopy.ColumnMappings.Add("Email", "Email");
                    bulkcopy.ColumnMappings.Add("BrandId", "BrandId");
                    bulkcopy.ColumnMappings.Add("LastPurchaseDate", "LastPurchaseDate");
                    bulkcopy.ColumnMappings.Add("TotalPurchase", "TotalPurchase");
                    bulkcopy.ColumnMappings.Add("DueAmount", "DueAmount");
                    bulkcopy.ColumnMappings.Add("Discount", "Discount");
                    bulkcopy.DestinationTableName = "[nl].[ContoApertoTemplateData]";
                    bulkcopy.WriteToServer(reader);
                    bulkcopy.Close();
                }
            }
            catch (Exception)
            {
                //await tr.RollbackAsync();
                throw;
            }
        }

        private async Task PrepareMailDataAsync(string destinationConnectionString)
        {
            //EFcore
            var optionsBuilder = new DbContextOptionsBuilder<NewsLettersContext>();
            optionsBuilder.UseSqlServer(destinationConnectionString);

            //using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            //{
            using (NewsLettersContext context = new NewsLettersContext(optionsBuilder.Options))
            {
                Template template = context.Templates.Single(s => s.Name == TemplateName);

                //Too slow removing by key
                //var deletedMails = context.Mails.Where(m => m.TemplateId == template.Id);
                //context.Mails.RemoveRange(deletedMails);

                var commandText = "DELETE FROM nl.Mails WHERE templateid = @templateid";
                var name = new Microsoft.Data.SqlClient.SqlParameter("@templateid", template.Id);

                await context.Database.ExecuteSqlRawAsync(commandText, name);

                //Paginate
                var culture = new CultureInfo("it-IT");
                TextInfo textInfo = culture.TextInfo;
                var page = 0;
                var pageSize = 20000;
                var recordCount = context.ContoApertoTemplateData.Count();
                var pageCount = (int)((recordCount + pageSize) / pageSize);

                if (recordCount < 1)
                {
                    return;
                }

                while (page < pageCount)
                {
                    var pageData = context.ContoApertoTemplateData
                                            .OrderBy(o => o.CardId)
                                            .ThenBy(o => o.ShopId)
                                            .Skip(pageSize * page).Take(pageSize).ToList();
                    var userItems = pageData
                                       .GroupBy(
                                          ca =>
                                             new
                                             {
                                                 CardId = ca.CardId,
                                                 Name = ca.Name,
                                                 Surname = ca.Surname,
                                                 Email = ca.Email
                                             }
                                       )
                                       .Select(
                                          g =>
                                             new BodyParameterData
                                             {
                                                 CardId = g.Key.CardId,
                                                 Name = textInfo.ToTitleCase(g.Key.Name.ToLower()),
                                                 Email = g.Key.Email,
                                                 TotalPurchase = g.Sum(g1 => g1.TotalPurchase).ToString("C2", culture),
                                                 Due = g.Sum(g1 => g1.DueAmount).ToString("C2", culture),
                                                 LastUpdatedDateTime = DateTime.Now.ToString("dd/MM/yyyy hh:mm"),
                                                 Purchases = g.GroupBy(g1 => g1.ShopName)
                                                   .Select(
                                                      g1 =>
                                                         new Purchase
                                                         {
                                                             ShopName = g1.Key,
                                                             LastPurchaseDate = g1.Max(g1 => g1.LastPurchaseDate).ToString("dd/MM/yyyy"),
                                                             TotalPurchase = g1.Sum(g1 => g1.TotalPurchase).ToString("C2", culture),
                                                             Due = g1.Sum(g1 => g1.DueAmount).ToString("C2", culture),
                                                             Discount = g1.Max(g1 => g1.Discount),
                                                             IsDiscountEligible = !string.IsNullOrEmpty(g1.Max(g1 => g1.Discount))
                                                         }
                                                   ).ToList()
                                             }
                                       ).ToList();

                    List<Mail> mails = new List<Mail>();
                    userItems.ForEach(i =>
                    {
                        string data = JsonSerializer.Serialize<BodyParameterData>(i);
                        var mail = new Mail()
                        {
                            BodyParametersData = data,
                            CardId = i.CardId,
                            Date = DateTime.Now,
                            EmailId = i.Email,
                            EmailIdBcc = null,
                            EmailIdCc = null,
                            MailStatus = MailStatus.Prepared,
                            TemplateId = template.Id
                        };
                        mail.MailStatusNavigation.Add(new MailStatus() { Date = DateTime.Now, MailStatus1 = MailStatus.Prepared, LineNumber = 1 });

                        mails.Add(mail);
                    });
                    context.Mails.AddRange(mails);
                    context.SaveChanges();
                    page++;
                }
                //    scope.Complete();
                //}
            }
        }
    }
    class BodyParameterData
    {
        public BodyParameterData()
        {
            Purchases = new List<Purchase>();
        }

        [JsonIgnore()]
        public string CardId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("purchases")]
        public List<Purchase> Purchases { get; set; }
        [JsonPropertyName("totalPurchase")]
        public string TotalPurchase { get; set; }
        [JsonPropertyName("dueAmount")]
        public string Due { get; set; }
        [JsonPropertyName("lastUpdatedDateTime")]
        public string LastUpdatedDateTime { get; set; }

    }

    class Purchase
    {
        [JsonPropertyName("shopName")]
        public string ShopName { get; set; }
        [JsonPropertyName("lastPurchaseDate")]
        public string LastPurchaseDate { get; set; }
        [JsonPropertyName("totalPurchase")]
        public string TotalPurchase { get; set; }
        [JsonPropertyName("dueAmount")]
        public string Due { get; set; }
        [JsonPropertyName("extraDiscount")]
        public string Discount { get; set; }
        [JsonPropertyName("isDiscountEligible")]
        public bool IsDiscountEligible { get; set; }

    }
}
