using System;
using System.Collections.Generic;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.DbModel.Entities.TransactionLogs;

public class TransactionLog : IHaveLongIdentifier
{
    public long Id { get; set; }
    public string TransactionUniqueId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public TransactionLogDiscriminator Discriminator { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public long? CardProgramCardId { get; set; }
    public string CardNumber { get; set; }
    public long? FundTransferredFromProgramCardId { get; set; }
    public string FundTransferredFromCardNumber { get; set; }
    
    public long? BeneficiaryId { get; set; }
    public string BeneficiaryID1 { get; set; }
    public string BeneficiaryID2 { get; set; }
    public string BeneficiaryFirstname { get; set; }
    public string BeneficiaryLastname { get; set; }
    public string BeneficiaryEmail { get; set; }
    public string BeneficiaryPhone { get; set; }
    public bool BeneficiaryIsOffPlatform { get; set; }
    public long? BeneficiaryTypeId { get; set; }
    
    public string TransactionInitiatorId { get; set; }
    public string TransactionInitiatorFirstname { get; set; }
    public string TransactionInitiatorLastname { get; set; }
    public string TransactionInitiatorEmail { get; set; }
    
    public long? OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public long? MarketId { get; set; }
    public string MarketName { get; set; }
    public long? SubscriptionId { get; set; }
    public string SubscriptionName { get; set; }
    public long ProjectId { get; set; }
    public string ProjectName { get; set; }
    public bool InitiatedByProject { get; set; }
    public bool InitiatedByOrganization { get; set; }

    public long? CashRegisterId { get; set; }
    public string CashRegisterName { get; set; }

    public List<TransactionLogProductGroup> TransactionLogProductGroups { get; set; }
}