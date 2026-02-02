using System;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;

public class BudgetAllowanceLog : IHaveLongIdentifier
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public BudgetAllowanceLogDiscriminator Discriminator { get; set; }
    
    public long ProjectId { get; set; }
    public decimal Amount { get; set; }

    public long BudgetAllowanceId { get; set; }
    public long? OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public long? SubscriptionId { get; set; }
    public string SubscriptionName { get; set; }

    public long TargetBudgetAllowanceId { get; set; }
    public long? TargetOrganizationId { get; set; }
    public string TargetOrganizationName { get; set; }
    public long? TargetSubscriptionId { get; set; }
    public string TargetSubscriptionName { get; set; }

    public string InitiatorId { get; set; }
    public string InitiatorFirstname { get; set; }
    public string InitiatorLastname { get; set; }
    public string InitiatorEmail { get; set; }
}