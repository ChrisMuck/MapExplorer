#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public enum DeliveredScoutStatus
{
    Unhurt,
    Injured,
    Exhausted,
    Overdue,
    Missing
}

public sealed class DeliveredMissionParticipantOutcome
{
    public DeliveredMissionParticipantOutcome(string memberId, bool returned, DeliveredScoutStatus deliveredStatus)
    {
        MemberId = RequireText(memberId, nameof(memberId));
        Returned = returned;
        DeliveredStatus = deliveredStatus;
    }

    public string MemberId { get; }
    public bool Returned { get; }
    public DeliveredScoutStatus DeliveredStatus { get; }

    private static string RequireText(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Value must not be empty.", name)
        : value.Trim();
}

/// <summary>An immutable snapshot of what one mission delivery established at that moment.</summary>
public sealed class DeliveredMissionOutcomeState
{
    private readonly IReadOnlyList<DeliveredMissionParticipantOutcome> participantOutcomes;
    private readonly IReadOnlyList<string> lostEquipmentIds;
    private readonly IReadOnlyList<string> deliveredReportIds;

    public DeliveredMissionOutcomeState(string deliveryId, string missionId, ScoutMissionStatus missionStatus,
        int expectedReturnWorldDay, int deliveredWorldDay, int? actualReturnWorldDay, bool wasOverdue,
        IEnumerable<DeliveredMissionParticipantOutcome> participantOutcomes,
        IEnumerable<string>? lostEquipmentIds = null, IEnumerable<string>? deliveredReportIds = null)
    {
        DeliveryId = RequireText(deliveryId, nameof(deliveryId));
        MissionId = RequireText(missionId, nameof(missionId));
        if (expectedReturnWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(expectedReturnWorldDay));
        if (deliveredWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(deliveredWorldDay));
        if (actualReturnWorldDay is < 1) throw new ArgumentOutOfRangeException(nameof(actualReturnWorldDay));
        ExpectedReturnWorldDay = expectedReturnWorldDay;
        DeliveredWorldDay = deliveredWorldDay;
        ActualReturnWorldDay = actualReturnWorldDay;
        WasOverdue = wasOverdue;
        MissionStatus = missionStatus;
        this.participantOutcomes = (participantOutcomes ?? throw new ArgumentNullException(nameof(participantOutcomes))).ToList();
        if (this.participantOutcomes.Count == 0) throw new ArgumentException("A delivered mission outcome needs participants.", nameof(participantOutcomes));
        if (this.participantOutcomes.Select(item => item.MemberId).Distinct(StringComparer.Ordinal).Count() != this.participantOutcomes.Count)
            throw new ArgumentException("Delivered mission participants must be unique.", nameof(participantOutcomes));
        this.lostEquipmentIds = Normalize(lostEquipmentIds);
        this.deliveredReportIds = Normalize(deliveredReportIds);
    }

    public string DeliveryId { get; }
    public string MissionId { get; }
    public ScoutMissionStatus MissionStatus { get; }
    public int ExpectedReturnWorldDay { get; }
    public int DeliveredWorldDay { get; }
    public int? ActualReturnWorldDay { get; }
    public bool WasOverdue { get; }
    public IReadOnlyList<DeliveredMissionParticipantOutcome> ParticipantOutcomes => participantOutcomes;
    public IReadOnlyList<string> LostEquipmentIds => lostEquipmentIds;
    public IReadOnlyList<string> DeliveredReportIds => deliveredReportIds;

    private static IReadOnlyList<string> Normalize(IEnumerable<string>? values) => (values ?? Enumerable.Empty<string>())
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
    private static string RequireText(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Value must not be empty.", name)
        : value.Trim();
}

}
