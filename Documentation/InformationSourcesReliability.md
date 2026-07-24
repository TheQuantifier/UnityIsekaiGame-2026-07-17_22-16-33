# Feature 8.6 - Information Sources and Reliability

Feature 8.6 adds structured information-source identity and deterministic reliability evaluation to the existing Knowledge, Observation, Memory, and History foundations.

## Scope

The feature does not replace objective truth, Person Knowledge, Evidence, Memory, authoritative History, Life Events, relationships, reputation, dialogue, rumor, teaching, or organization-authority systems. It adds a production-owned source layer that those systems can reference.

The central distinction is:

- objective truth remains outside Person belief;
- evidence records keep raw strength and credibility;
- information sources describe where information came from;
- reliability evaluation calculates an effective contribution for a specific evaluator and context;
- belief confidence remains owned by `PersonKnowledgeRuntime`.

## Source Definitions

`InformationSourceDefinition` represents reusable authored source behavior. It has a canonical stable ID with the `information-source.` prefix, a concrete source category, a default reliability profile, supported domains/methods, authority classifications, risk defaults, staleness policy, transmission penalties, anonymity/copy/translation/summary flags, verification requirements, tags, and version.

Runtime source instances do not need to be ScriptableObjects. The prototype Test Lab uses runtime source instances for direct observation, expert testimony, ordinary testimony, anonymous testimony, official records, copies, translations, and summaries.

## Source Instances

`InformationSourceInstanceData` records a concrete source instance:

- source instance ID;
- optional source definition ID;
- source category and reference type;
- referenced Person, organization, item, document, location, body, method, memory, event, evidence, or custom ID;
- creator, observer, holder, and transmitter;
- creation, observation, and transmission world times;
- generation, parent source, and original source;
- verification and authenticity;
- domain, subject, method, authority, bias, error, deception, privacy, tags, revision, corrections, and supersession.

## Source Chains

Copies, translations, summaries, corrections, and supersessions are represented as transformations. A transformed source points to its immediate parent and retains the original source ID. Evidence payloads are not duplicated into each source-chain node.

`TraceSourceChain` returns the immediate source, parent chain, transformation records, transmission depth, original source, and whether the original was hidden by privacy.

## Reliability

Reliability is dimensional, not one global trust score. The supported dimensions are:

`GeneralDependability`, `DomainExpertise`, `FirsthandProximity`, `MethodQuality`, `Authenticity`, `IdentityCertainty`, `ObservationQuality`, `RecordIntegrity`, `Recency`, `TransmissionIntegrity`, `Independence`, `Corroboration`, `InternalConsistency`, `ErrorRisk`, `DeceptionRisk`, `BiasRisk`, `Completeness`, `Precision`, and `ContextFit`.

`SourceReliabilityResult.DerivedOverall` is a deterministic summary derived from these dimensions. It is used only when a caller needs a single scaling factor, such as effective evidence strength.

## Person-Relative Assessments

`PersonSourceAssessmentData` stores a Person-owned assessment of a source. Two Persons may assess the same source differently. Assessments can be scoped by domain, subject, and method, and carry authority, risk, familiarity, confidence, supporting evidence references, prior experiences, privacy, revision, and supersession data.

Assessments do not mutate the source record. Corrections create revised assessment data, while the source identity remains stable.

## Evidence Integration

`KnowledgeEvidenceRecordData` now has optional source-reliability fields:

- `informationSourceId`;
- `rawStrength`;
- `effectiveStrength`;
- `reliabilityPolicyId`;
- `reliabilityEvaluationId`.

Existing callers continue to work because `effectiveStrength` defaults to the old `strength` value. Source-aware callers may evaluate reliability, calculate an effective strength, and pass it through `KnowledgeObservationRequest.EffectiveStrengthOverride`.

Raw evidence strength is preserved for audit and debugging. Effective strength is what contributes to belief confidence.

## Persistence

`InformationSourcePersistenceParticipant` persists source instances, assessments, transformations, and processed transaction IDs as player-scoped data under `person.information-sources`.

Restore uses prepare/commit behavior. Corrupt payloads are rejected during prepare/restore validation and the live runtime remains unchanged. Restore is silent and does not emit source-change events.

## Test Lab

The Test Lab adds `Sources 8.6` under `Knowledge Step 8`.

Manual controls include source definition validation, registering source categories, copying/translating/summarizing, reliability evaluation, comparing two Persons' assessments, trust/untrust, authority/bias/error/deception risk, age/staleness, chain tracing, dependent/independent report checks, assessment correction, original-source hiding, raw/effective evidence comparison, save/restore, and Feature 8.6 automation.

## Deferred

The feature intentionally defers dialogue UI, rumor propagation, teaching/training systems, organization reputation, live NPC belief propagation, social consequences, and authored production source asset content beyond representative definitions and runtime behavior.
