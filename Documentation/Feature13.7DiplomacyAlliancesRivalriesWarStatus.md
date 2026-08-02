# Feature 13.7 - Diplomacy, Alliances, Rivalries, and War Status

Feature 13.7 adds authoritative diplomacy records for formal intergroup state. Organizations and eligible factions remain owned by their existing runtimes; the diplomacy runtime stores only diplomatic actors, relations, agreements, clauses, breaches, war status, sides, participation, incidents, visibility, revisions, and transaction history.

## Runtime Ownership

`DiplomacyRuntime` is the single owner of diplomacy state. It validates typed `DiplomaticActorReferenceData` values against `OrganizationRuntime` and `FactionRuntime`, keeps formal diplomacy separate from faction dispositions and social attitudes, and exposes immutable save/projection data rather than scene-object references.

Organizations, memberships, offices, authority grants, resources, decisions, factions, contracts, social state, and knowledge remain authoritative in their own systems. Diplomacy references those records by stable ID and never duplicates their core behavior.

## Formal Relations

Diplomatic relations are definition-backed records with stable IDs, source and target actors, category, lifecycle state, start/end time, visibility, provenance IDs, and revisions. Directional relations are preserved. Reciprocal behavior happens only when the relation definition explicitly requests mirror creation.

The alpha prototype definitions include recognition, neutral, cooperative, alliance, rivalry, hostile, ceasefire, and war-style relations.

## Agreements and Clauses

Diplomatic agreements are stable records separate from clauses. Agreement definitions control category, party limits, supported actor types, secret-clause policy, and allowed clause definitions. Party records preserve actor references, role, authority/decision sources, representative person IDs, entry and withdrawal times.

Clauses are stable records with definition IDs, category, lifecycle state, visibility, typed parameter slots, optional contract/resource references, effective/expiration times, and independent revision state. Breach records distinguish alleged, disputed, confirmed, waived, cured, and false breach states. A confirmed breach marks the referenced clause as breached without executing punishment or economic transfer logic.

## Signature, Ratification, and Authority

Signatures and ratifications are explicit records. Signature requests can reference authority grants, and diplomacy revalidates the grant against Feature 13.3 when a grant is provided. Ratification records can preserve source decision IDs from Feature 13.5. Agreement activation is an explicit operation and does not infer automatic internal approval.

## Alliances, Rivalries, and Hostility

Alliances are explicit diplomatic relations or agreements with scoped terms. A mutual-defense agreement does not merge organizations, share treasuries, bypass authority, or automatically enter war. Rivalry and hostility are formal diplomatic states but remain separate from war status and from Step 12 interpersonal hostility.

## War Status

War records have stable IDs, definition IDs, lifecycle state, declaration time, sides, participation IDs, declaration/provenance references, ceasefire agreement IDs, peace agreement IDs, visibility, and revisions. Sides and participant records are separately identifiable. War declaration creates formal war status only; combat encounters, armies, territorial control, occupation, fronts, law, courts, and governments are deferred.

Ceasefire and peace are lifecycle transitions. Peace can end a war while preserving the war, sides, participation, incidents, and related agreement history.

## Visibility and Projections

`DiplomacyRuntime.GetProjection` returns privileged, public, redacted, or denied views. Secret and hidden records do not leak full payloads to ordinary projections. This is the diplomacy-local view boundary and can be routed through Step 8 information access when UI and gameplay disclosure layers are expanded.

## Persistence

`DiplomacyPersistenceParticipant` captures shared-world diplomacy state, prepares and validates payloads before commit, rejects corrupt references before mutation, supports rollback, and depends on organization persistence. Faction, organization authority, organization resources, organization decisions, and information access are optional dependency declarations so restore ordering remains explicit as those systems participate.

## Test Lab

Feature 13.7 registers `feature.13.7.diplomacy-alliances-rivalries-war-status` with command-line capable scenarios for:

- runtime definition readiness;
- actor eligibility, directional/reciprocal relations, and internal-faction rejection;
- agreements, parties, clauses, signatures, ratification, activation, and breaches;
- war declaration, sides, participation, incidents, ceasefire, and peace;
- projections, save/restore, and corrupt-payload rejection.

The suite uses fresh isolated runtimes and the shared automation catalog so command-line and in-game runs use the same scenario definitions.

## Deferred Scope

Feature 13.7 deliberately does not implement governments, sovereignty, territory, borders, jurisdiction, laws, crimes, courts, military units, campaigns, tactical combat, strategic war AI, autonomous diplomacy, embassies, sanctions execution, espionage, treaty-law enforcement, final diplomacy UI, final war UI, multiplayer replication, or server networking.

