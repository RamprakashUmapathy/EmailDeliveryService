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

        public override Task<PaginationViewModel<MailData>> GetDataAsync(IDbConnection connection, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
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
                //Delete
                using SqlCommand tCmd = new SqlCommand("DELETE FROM [nl].[ContoApertoTemplateData]", destination);
                tCmd.CommandTimeout = 0;
                await tCmd.ExecuteNonQueryAsync();

                using SqlCommand cmd = new SqlCommand(Resources.ContoApertoTemplateLIST, source)
                {
                    CommandType = CommandType.Text
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
                    await bulkcopy.WriteToServerAsync(reader);
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

                    //Process Shop by shop with in memory options (otherwise takes too much time)
                    var shops = context.ContoApertoTemplateData.Select(c => c.ShopId).Distinct().ToList();
                    TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
                    foreach (string shopId in shops)
                    {
                        var shopItems = await context.ContoApertoTemplateData
                                            .Where(f => f.ShopId == shopId)
                                            .ToListAsync();

                        var userItems = shopItems
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
                                                     TotalPurchase = g.Sum(g1 => g1.TotalPurchase),
                                                     Due = g.Sum(g1 => g1.DueAmount),
                                                     Purchases = g.GroupBy(g1 => g1.ShopName)
                                                       .Select(
                                                          g1 =>
                                                             new Purchase
                                                             {
                                                                 ShopName = g1.Key,
                                                                 TotalPurchase = g1.Sum(g1 => g1.TotalPurchase),
                                                                 Due = g1.Sum(g1 => g1.DueAmount),
                                                                 Discount = g1.Max(g1 => g1.Discount)
                                                             }
                                                       ).ToList()
                                                 }
                                           ).ToList();

                        List<Mail> mails = new List<Mail>();
                        userItems.ForEach(i =>
                        {
                            string data = JsonSerializer.Serialize<BodyParameterData>(i);
                            mails.Add(new Mail()
                            {
                                BodyParametersData = data,
                                CardId = i.CardId,
                                Date = DateTime.Now,
                                EmailId = i.Email,
                                EmailIdBcc = null,
                                EmailIdCc = null,
                                MailStatus = MailStatus.Prepared,
                                TemplateId = template.Id,
                            //MailStatusNavigation = new MailStatus() { Date = DateTime.Now}
                        });
                        });
                        context.Mails.AddRange(mails);
                        context.SaveChanges();

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
        public decimal TotalPurchase { get; set; }
        [JsonPropertyName("dueAmount")]
        public decimal Due { get; set; }

    }

    class Purchase
    {
        [JsonPropertyName("shopName")]
        public string ShopName { get; set; }
        [JsonPropertyName("lastPurchaseDate")]
        public decimal TotalPurchase { get; set; }
        [JsonPropertyName("dueAmount")]
        public decimal Due { get; set; }
        [JsonPropertyName("extraDiscount")]
        public string Discount { get; set; }

    }
}
