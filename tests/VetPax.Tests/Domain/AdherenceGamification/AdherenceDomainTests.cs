
using VetPax.Domain.Modules.AdherenceGamification.Aggregates;
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.Events;
using VetPax.Domain.Modules.AdherenceGamification.Exceptions;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;
using Xunit;

namespace VetPax.Tests.Domain.AdherenceGamification;

public sealed class AdherenceDomainTests
{
    private static readonly DateTime Now =
        new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    // Test thresholds, not official business rules.
    private static readonly AdherencePolicy Policy =
        new(2, 2m, 4m, 8m, false);

    [Fact]
    public void NewProfile_HasNoEvidenceOrLevel()
    {
        var profile = Adherence.Create(Guid.NewGuid(), Now);

        Assert.Equal(0, profile.EvidenceCount);
        Assert.Null(profile.Score);
        Assert.Null(profile.Level);
        Assert.Empty(profile.Recognitions);
    }

    [Fact]
    public void InsufficientEvidence_DoesNotAssignLevel()
    {
        var profile = Adherence.Create(Guid.NewGuid(), Now);

        profile.Recalculate(
            1, new AdherenceScore(8),
            Policy, Now.AddSeconds(1));

        Assert.Null(profile.Level);
    }

    [Fact]
    public void InitialBronze_DoesNotGeneratePromotion()
    {
        var profile = Adherence.Create(Guid.NewGuid(), Now);

        profile.Recalculate(
            2, new AdherenceScore(2),
            Policy, Now.AddSeconds(1));

        Assert.Equal(ConstancyLevel.Bronze, profile.Level);
        Assert.Empty(profile.Recognitions);

        Assert.DoesNotContain(
            profile.PullDomainEvents(),
            e => e is OwnerPromotedEvent);
    }

    [Fact]
    public void BronzeToSilver_GeneratesRecognition()
    {
        var profile = Adherence.Create(Guid.NewGuid(), Now);

        profile.Recalculate(
            2, new AdherenceScore(2),
            Policy, Now.AddSeconds(1));

        profile.PullDomainEvents();

        profile.Recalculate(
            4, new AdherenceScore(4),
            Policy, Now.AddSeconds(2));

        Assert.Equal(ConstancyLevel.Silver, profile.Level);

        var recognition = Assert.Single(profile.Recognitions);

        Assert.Equal(ConstancyLevel.Bronze,
            recognition.FromLevel);

        Assert.Equal(ConstancyLevel.Silver,
            recognition.ToLevel);

        Assert.Contains(
            profile.PullDomainEvents(),
            e => e is OwnerPromotedEvent);
    }

    [Fact]
    public void SameLevel_DoesNotGenerateRecognition()
    {
        var profile = Adherence.Create(Guid.NewGuid(), Now);

        profile.Recalculate(
            2, new AdherenceScore(2),
            Policy, Now.AddSeconds(1));

        profile.Recalculate(
            3, new AdherenceScore(3),
            Policy, Now.AddSeconds(2));

        Assert.Empty(profile.Recognitions);
    }

    [Fact]
    public void DemotionDisabled_RetainsEarnedLevel()
    {
        var profile = Adherence.Create(Guid.NewGuid(), Now);

        profile.Recalculate(
            4, new AdherenceScore(4),
            Policy, Now.AddSeconds(1));

        profile.Recalculate(
            5, new AdherenceScore(2),
            Policy, Now.AddSeconds(2));

        Assert.Equal(ConstancyLevel.Silver, profile.Level);
    }

    [Fact]
    public void DemotionEnabled_AllowsLowerLevel()
    {
        var policy = new AdherencePolicy(
            2, 2, 4, 8, true);

        var profile = Adherence.Create(Guid.NewGuid(), Now);

        profile.Recalculate(
            4, new AdherenceScore(4),
            policy, Now.AddSeconds(1));

        profile.Recalculate(
            5, new AdherenceScore(2),
            policy, Now.AddSeconds(2));

        Assert.Equal(ConstancyLevel.Bronze, profile.Level);
        Assert.Empty(profile.Recognitions);
    }

    [Fact]
    public void InvalidThresholds_AreRejected()
    {
        Assert.Throws<AdherenceDomainException>(() =>
            new AdherencePolicy(2, 4, 2, 8, false));

        Assert.Throws<AdherenceDomainException>(() =>
            new AdherencePolicy(0, 2, 4, 8, false));
    }

    [Fact]
    public void NegativeScore_IsRejected()
    {
        Assert.Throws<AdherenceDomainException>(() =>
            new AdherenceScore(-1));
    }

    [Fact]
    public void Rehydrate_DoesNotEmitEvents()
    {
        var profile = Adherence.Rehydrate(
            Guid.NewGuid(),
            4,
            new AdherenceScore(4),
            ConstancyLevel.Silver,
            Now,
            Array.Empty<VetPax.Domain.Modules.AdherenceGamification.Entities.Recognition>(),
            5);

        Assert.Equal(5, profile.PersistenceVersion);
        Assert.Empty(profile.PullDomainEvents());
    }
}
