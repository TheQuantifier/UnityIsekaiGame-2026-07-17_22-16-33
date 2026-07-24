# Memory Recall, Reinforcement, Forgetting, and Alteration

Feature 8.4 extends the Person-owned memory foundation from Feature 8.3. It does not create a second history, knowledge, evidence, or belief system.

## Ownership

`PersonMemoryRuntime` remains the owner of memory records for one persistent Person. Memories may reference bodies, historical events, facts, evidence, beliefs, tags, and structured details, but they are not owned by Actors, bodies, UI, cameras, scenes, or player controllers.

## Memory Versus Truth

Authoritative facts and history remain owned by their existing systems. A memory is a retained recollection or learned representation of something a Person may recall. A memory can be accurate, incomplete, false, altered, suppressed, inaccessible, forgotten, or recovered.

Forgetting a memory does not change authoritative history. Suppression does not delete a memory. Reinforcement is not proof. Confidence is not accuracy. A belief may persist after a supporting memory becomes inaccessible.

## Accessibility

Memory accessibility uses `MemoryState`:

- `Accessible`: ordinary recall can return the memory.
- `Difficult`: recall may require an explicit attempt or contextual cue.
- `Inaccessible`: the memory exists but ordinary recall cannot access it.
- `Suppressed`: active suppression blocks ordinary recall.
- `Forgotten`: ordinary recall cannot access the memory, but the record remains.
- `Dormant`: retained but normally hidden from ordinary recall.
- `Altered`: current representation has been materially changed.
- `Recovered`: previously unavailable content has been explicitly recovered.
- Existing `Uncertain`, `Disputed`, and `Corrected` states remain available for subjective or revised memories.

## Recall

`MemoryRecallRequest` describes a deterministic recall attempt. It can target a memory, event, subject, body, location, organization, tags, and typed contextual cues. Ordinary recall must be requested by the owning Person. Privileged inspection uses explicit `MemoryAccessContext` values such as Debug, Validation, or Persistence.

`MemoryRecallResult` is immutable and reports outcomes such as fully recalled, partially recalled, uncertain, altered, conflicting, blocked by suppression, inaccessible, forgotten, no match, cue-assisted, recovered, or access denied.

Read-only debug inspection sets `MutateMetadata = false` and does not count as recall. Restore also does not update recall metadata.

## Contextual Cues

Recall cues are typed references such as Person, body, location, item, historical event, fact, knowledge domain, tag, or another memory. Cues are only matching data in this feature. They do not implement sensory simulation, emotion, psychology, dialogue, or AI.

## Reinforcement

`ReinforceMemory` can increase confidence, clarity, salience, and reinforcement metadata. Reinforcement may make a memory easier to recall, including a false or distorted memory. It does not prove the memory true and does not compare against authoritative history unless a later explicit correction/evidence operation does so.

## Degradation And Forgetting

`ApplyDegradation` evaluates explicit world-time intervals. It does not run per frame and does not require an active GameObject. Degradation can reduce confidence, clarity, salience, and accessibility. Normal forgetting is represented as state/detail changes, not deletion.

Each memory persists `lastDegradationEvaluatedWorldTime`. Re-evaluating degradation at the same world-time boundary is idempotent and does not apply another loss step. Advancing the world-time boundary applies only the unevaluated interval.

## Partial Forgetting

Memory details are structured `MemoryDetailData` records. A Person can retain the general event while selected details, such as participants, time, location, body, organization, source, or note, become unavailable, uncertain, altered, suppressed, or recovered. This avoids parsing prose and does not duplicate authoritative event ownership.

## Suppression And Recovery

Suppression is an explicit `MemorySuppressionData` record with source, reason, start time, optional end time, and cue-bypass policy. Multiple active suppression sources combine by blocking recall until all blocking sources are removed or expired. Expiration is evaluated from world time and does not require scene objects.

The memory records its pre-suppression state before the first active suppression. Removing or expiring the last blocking source restores that underlying state rather than blindly forcing `Accessible`. Permanent suppressions do not expire through the expiration path; they require explicit removal.

Recovery is explicit through `RecoverMemory`, cue-assisted recall, or alteration requests. Recovery preserves auditability through revisions.

## Alteration And Revisions

`AlterMemory` supports correction, reconstruction, natural degradation, detail loss, detail addition, distortion, deliberate manipulation, new-evidence revision, source attribution change, identity association change, recovery, suppression, suppression removal, and reinforcement records.

Material changes append `MemoryRevisionData`. Revisions preserve prior state, metrics, body association, and detail snapshots. Validation rejects missing, duplicate, self-referencing, and circular revision chains.

## Conflicting Memories

Multiple memories can reference the same event and disagree. Recall may return multiple entries with `Conflicting` outcome. Feature 8.4 does not silently choose the true memory, rewrite beliefs, or resolve contradictions.

## Knowledge Integration

Feature 8.1 remains responsible for evidence and beliefs. Feature 8.4 can expose memory-derived evidence references already stored on the memory, but it does not create a parallel belief system. Forgotten or suppressed memory does not automatically delete beliefs.

## Previous-Body Memory

Previous-body association is still Person-owned. Feature 8.4 can make the body detail unavailable or recover it without changing identity continuity or revealing continuity to another Person.

## Persistence

`PersonMemorySaveData` schema version is now 2. Version 1 memory data from Feature 8.3 is accepted and normalized in memory validation/restore by initializing details, recall metadata, suppression arrays, and revision history. Restore suppresses memory events and does not replay recall, reinforcement, forgetting, suppression, recovery, or alteration side effects.

## Test Lab

The Test Lab adds `Memory 8.4` under Knowledge Step 8. It demonstrates validation, inspection, recall, subject recall, cue recall, reinforcement, false-memory reinforcement, clarity/confidence reduction, difficult/inaccessible/forgotten states, partial forgetting, suppression, suppression removal/expiration, recovery, alteration, correction, revision history, conflicting memories, previous-body suppression/recovery, view comparison, and save/restore.

## Deferred Systems

Dialogue, teaching, books, journals, codex UI, relationships, trauma, psychology, dreams, magic memory manipulation, toxins/drugs affecting memory, reincarnation gameplay, NPC AI, networking, and replication remain deferred. Feature 8.4 provides contracts for those systems to call later.
