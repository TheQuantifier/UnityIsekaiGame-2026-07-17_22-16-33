# Property, Land, and Buildings

Feature 11.6 adds the property authority layer for land parcels, buildings, units, deeds, title, occupancy, tenancy, rent, condition, inspection, and maintenance.

## Ownership Boundaries

`PropertyRuntime` owns property records and title state. A property is not a scene transform, not a world location, and not an inventory container. Spatial and scene references are opaque IDs that can later be resolved by world/location systems.

Ownership is represented by `PropertyOwnershipInterestData` and made authoritative by `PropertyTitleRecordData`. Possession, occupancy, access rights, and tenancy are separate records and never imply ownership.

## Transfers

Transfers are staged as property operations. Sales validate the property side first and move money only at the final commit point, so injected transfer failures do not leave partial title changes or balance changes. Gifts and inheritance use the same title/deed path without requiring payment.

Payment alone does not transfer property. A deed alone does not transfer property. The committed transfer record ties the deed, title, and optional payment transaction together.

## Tenancy And Rent

Tenancy creates explicit possession, occupancy, and access grants. Rent obligations remain payable after tenancy termination until separately paid, waived, or corrected by later systems.

Deposits are stored in rent terms as a foundation value. Generalized deposits, collateral, loans, penalties, eviction, legal disputes, and courts are deferred to later contract and legal features.

## Maintenance

Property condition, inspections, maintenance obligations, and maintenance records are explicit. Maintenance can validate declared tools and authorized workers without taking ownership of item instances, business expenses, or profession credentials.

## Integration

Business premises are linked by establishment references. Inventory remains owned by the item/inventory systems. Information access projections can redact property details without mutating the authoritative property record.

## Persistence

`PropertyPersistenceParticipant` stores the world property graph under `world.properties`. Prepare validation rejects unsupported schema versions, missing definitions when a registry is supplied, duplicate IDs, broken property references, and broken hierarchy references before commit.

## Deferred

Deferred work includes final scene/location resolution, legal enforcement, taxes, mortgages, collateral, eviction, courts, autonomous AI property decisions, and final UI presentation.
