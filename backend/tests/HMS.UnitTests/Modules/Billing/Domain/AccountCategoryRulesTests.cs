using FluentAssertions;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;
using Xunit;
using VisitType = HMS.Modules.Patients.Contracts.VisitType;

namespace HMS.UnitTests.Modules.Billing.Domain;

/// <summary>ADR-134: which bills each Accounts and Finance category lists.</summary>
public class AccountCategoryRulesTests
{
    private static Invoice Bill(VisitType? encounterType, params BillingType[] lineTypes) => Invoice.Create(
        "INV-2026-000001",
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Aravind Nadar",
        "NH20260001",
        lineTypes.Select(type => new InvoiceLineItemSpec(type, null, null, null, null, 1, 100m, 0m, false, null)).ToList(),
        createdBy: null,
        encounterType);

    private static bool IsIn(AccountCategory category, Invoice bill) => AccountCategoryRules.Predicate(category).Compile()(bill);

    [Theory]
    [InlineData(VisitType.OP, AccountCategory.Op)]
    [InlineData(VisitType.Emergency, AccountCategory.Op)]
    [InlineData(VisitType.IP, AccountCategory.Ip)]
    [InlineData(VisitType.DayCare, AccountCategory.Ip)]
    [InlineData(VisitType.Observation, AccountCategory.Ip)]
    public void EveryEncounterType_IsInExactlyOneOfOpAndIp(VisitType encounterType, AccountCategory expected)
    {
        var bill = Bill(encounterType, BillingType.Consultation);
        var other = expected == AccountCategory.Op ? AccountCategory.Ip : AccountCategory.Op;

        IsIn(expected, bill).Should().BeTrue();
        IsIn(other, bill).Should().BeFalse();
    }

    [Fact]
    public void ABillWithNoEncounterType_IsCountedUnderOp()
    {
        var bill = Bill(null, BillingType.Consultation);

        IsIn(AccountCategory.Op, bill).Should().BeTrue();
        IsIn(AccountCategory.Ip, bill).Should().BeFalse();
    }

    [Fact]
    public void Lab_ListsEveryBillWithALaboratoryLine_WhateverItsEncounterType()
    {
        IsIn(AccountCategory.Lab, Bill(VisitType.OP, BillingType.Consultation, BillingType.Laboratory)).Should().BeTrue();
        IsIn(AccountCategory.Lab, Bill(VisitType.IP, BillingType.Laboratory)).Should().BeTrue();
        IsIn(AccountCategory.Lab, Bill(VisitType.OP, BillingType.Consultation, BillingType.Radiology)).Should().BeFalse();
    }

    [Fact]
    public void EncounterTypeFor_UsesTheVisitsType_ThenIpForAnIpdFinalBill_ElseNone()
    {
        AccountCategoryRules.EncounterTypeFor(VisitType.Emergency, [BillingType.InpatientCharge]).Should().Be(VisitType.Emergency);
        AccountCategoryRules.EncounterTypeFor(null, [BillingType.InpatientCharge]).Should().Be(VisitType.IP);
        AccountCategoryRules.EncounterTypeFor(null, [BillingType.Pharmacy]).Should().BeNull();
    }
}
