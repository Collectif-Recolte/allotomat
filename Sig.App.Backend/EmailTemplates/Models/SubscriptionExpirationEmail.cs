﻿using Sig.App.Backend.Services.Mailer;
using System;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class SubscriptionExpirationEmail : EmailModel
    {
        public string SubscriptionName { get; set; }
        public decimal TotalAmountLoadedOnCards { get; set; }
        public decimal AmountUsedForPurchases { get; set; }
        public decimal ExpiredAmount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public override string Subject => $"L'abonnement {SubscriptionName} vient d'expirer / Subscription {SubscriptionName} expired";

        public SubscriptionExpirationEmail(string to) : base(to)
        {
        }
    }
}