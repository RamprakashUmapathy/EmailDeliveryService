using System;
using System.Collections.Generic;

namespace EmailDeliveryService.Model
{
    public partial class ContoApertoTemplateData
    {
        public long Id { get; set; }
        public string CardId { get; set; }
        public string ShopId { get; set; }
        public string ShopName { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string BrandId { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public decimal TotalPurchase { get; set; }
        public decimal DueAmount { get; set; }
        public string Discount { get; set; }
    }
}
