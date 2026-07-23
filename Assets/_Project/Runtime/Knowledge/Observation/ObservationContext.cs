using System;
using System.Linq;

namespace UnityIsekaiGame.Knowledge.Observation
{
    public sealed class ObservationContext
    {
        public ObservationContext(
            string observerPersonId,
            string transactionId,
            string methodId,
            SensoryChannel sensoryChannel,
            ObservationTargetType targetType,
            string targetSubjectId,
            string observerActorId = "",
            string observerBodyId = "",
            string targetPersonId = "",
            string targetActorId = "",
            string targetBodyId = "",
            string targetItemId = "",
            string targetLocationId = "",
            string targetEventId = "",
            int distanceQuality = KnowledgeConfidence.Maximum,
            ObservationVisibilityState visibility = ObservationVisibilityState.Clear,
            ConcealmentState concealment = ConcealmentState.None,
            ObservationAccessLevel accessLevel = ObservationAccessLevel.Public,
            ObservationConsentState consent = ObservationConsentState.NotRequired,
            int environmentalQuality = KnowledgeConfidence.Maximum,
            int lightingQuality = KnowledgeConfidence.Maximum,
            int noiseQuality = KnowledgeConfidence.Maximum,
            int obstructionQuality = KnowledgeConfidence.Maximum,
            int expertiseQuality = KnowledgeConfidence.DefaultObservation,
            int toolQuality = KnowledgeConfidence.DefaultObservation,
            double gameTimeSeconds = 0d,
            KnowledgeTrackingPolicy trackingPolicy = KnowledgeTrackingPolicy.PlayerMechanicalOnly,
            bool mechanicallyRelevant = true,
            bool privateAccessAuthorized = false,
            KnowledgeTruthAuthorization truthAuthorization = null,
            long expectedBodyRevision = 0L,
            long expectedConditionRevision = 0L,
            string authorityContext = "",
            string[] tags = null)
        {
            ObserverPersonId = observerPersonId ?? string.Empty;
            TransactionId = transactionId ?? string.Empty;
            MethodId = methodId ?? string.Empty;
            SensoryChannel = sensoryChannel;
            TargetType = targetType;
            TargetSubjectId = targetSubjectId ?? string.Empty;
            ObserverActorId = observerActorId ?? string.Empty;
            ObserverBodyId = observerBodyId ?? string.Empty;
            TargetPersonId = targetPersonId ?? string.Empty;
            TargetActorId = targetActorId ?? string.Empty;
            TargetBodyId = targetBodyId ?? string.Empty;
            TargetItemId = targetItemId ?? string.Empty;
            TargetLocationId = targetLocationId ?? string.Empty;
            TargetEventId = targetEventId ?? string.Empty;
            DistanceQuality = KnowledgeConfidence.Clamp(distanceQuality);
            Visibility = visibility;
            Concealment = concealment;
            AccessLevel = accessLevel;
            Consent = consent;
            EnvironmentalQuality = KnowledgeConfidence.Clamp(environmentalQuality);
            LightingQuality = KnowledgeConfidence.Clamp(lightingQuality);
            NoiseQuality = KnowledgeConfidence.Clamp(noiseQuality);
            ObstructionQuality = KnowledgeConfidence.Clamp(obstructionQuality);
            ExpertiseQuality = KnowledgeConfidence.Clamp(expertiseQuality);
            ToolQuality = KnowledgeConfidence.Clamp(toolQuality);
            GameTimeSeconds = Math.Max(0d, gameTimeSeconds);
            TrackingPolicy = trackingPolicy;
            MechanicallyRelevant = mechanicallyRelevant;
            PrivateAccessAuthorized = privateAccessAuthorized;
            TruthAuthorization = truthAuthorization;
            ExpectedBodyRevision = Math.Max(0L, expectedBodyRevision);
            ExpectedConditionRevision = Math.Max(0L, expectedConditionRevision);
            AuthorityContext = authorityContext ?? string.Empty;
            Tags = (tags ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        public string ObserverPersonId { get; }
        public string TransactionId { get; }
        public string MethodId { get; }
        public SensoryChannel SensoryChannel { get; }
        public ObservationTargetType TargetType { get; }
        public string TargetSubjectId { get; }
        public string ObserverActorId { get; }
        public string ObserverBodyId { get; }
        public string TargetPersonId { get; }
        public string TargetActorId { get; }
        public string TargetBodyId { get; }
        public string TargetItemId { get; }
        public string TargetLocationId { get; }
        public string TargetEventId { get; }
        public int DistanceQuality { get; }
        public ObservationVisibilityState Visibility { get; }
        public ConcealmentState Concealment { get; }
        public ObservationAccessLevel AccessLevel { get; }
        public ObservationConsentState Consent { get; }
        public int EnvironmentalQuality { get; }
        public int LightingQuality { get; }
        public int NoiseQuality { get; }
        public int ObstructionQuality { get; }
        public int ExpertiseQuality { get; }
        public int ToolQuality { get; }
        public double GameTimeSeconds { get; }
        public KnowledgeTrackingPolicy TrackingPolicy { get; }
        public bool MechanicallyRelevant { get; }
        public bool PrivateAccessAuthorized { get; }
        public KnowledgeTruthAuthorization TruthAuthorization { get; }
        public long ExpectedBodyRevision { get; }
        public long ExpectedConditionRevision { get; }
        public string AuthorityContext { get; }
        public string[] Tags { get; }
    }
}
