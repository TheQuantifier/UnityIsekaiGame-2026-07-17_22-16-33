using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory.Composition;
using UnityIsekaiGame.Inventory.Durability;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Inventory.Production;
using UnityIsekaiGame.Inventory.Quality;
using UnityIsekaiGame.Inventory.Recipes;

namespace UnityIsekaiGame.Inventory.Crafting
{
    public sealed class CraftingExecutionRuntime
    {
        private readonly Dictionary<string, CraftingOperationRecordData> operationsById = new Dictionary<string, CraftingOperationRecordData>(StringComparer.Ordinal);
        private long revision;

        public long Revision => revision;
        public int OperationCount => operationsById.Count;
        public IReadOnlyList<CraftingOperationRecordData> Operations => operationsById.Values.OrderBy(entry => entry.operationId, StringComparer.Ordinal).Select(entry => entry.Clone()).ToArray();

        public bool TryGetOperation(string operationId, out CraftingOperationRecordData operation)
        {
            if (!string.IsNullOrWhiteSpace(operationId) && operationsById.TryGetValue(operationId, out CraftingOperationRecordData found))
            {
                operation = found.Clone();
                return true;
            }

            operation = null;
            return false;
        }

        public CraftingExecutionResult Preview(
            CraftingExecutionRequest request,
            DefinitionRegistry registry,
            RecipeRuntime recipeRuntime,
            ProductionRequirementRuntime productionRuntime,
            ItemInstanceIdentityRuntime itemRuntime,
            ItemDurabilityRuntime durabilityRuntime)
        {
            CraftingExecutionRequest preview = request?.Clone() ?? new CraftingExecutionRequest();
            preview.preview = true;
            return Execute(preview, registry, recipeRuntime, productionRuntime, itemRuntime, null, null, durabilityRuntime);
        }

        public CraftingExecutionResult Execute(
            CraftingExecutionRequest request,
            DefinitionRegistry registry,
            RecipeRuntime recipeRuntime,
            ProductionRequirementRuntime productionRuntime,
            ItemInstanceIdentityRuntime itemRuntime,
            ItemCompositionRuntime compositionRuntime,
            ItemQualityAffixRuntime qualityRuntime,
            ItemDurabilityRuntime durabilityRuntime)
        {
            CraftingExecutionRequest working = request?.Clone() ?? new CraftingExecutionRequest();
            NormalizeRequest(working);
            if (!ValidateExecutionInputs(working, registry, recipeRuntime, productionRuntime, itemRuntime, out string validationFailure))
            {
                return CraftingExecutionResult.Failure(CraftingExecutionStatus.InvalidRequest, validationFailure);
            }

            if (operationsById.TryGetValue(working.operationId, out CraftingOperationRecordData existing))
            {
                if (existing.state == CraftingOperationState.Completed)
                {
                    return CraftingExecutionResult.Success(existing, "Crafting operation was already completed.", duplicate: true);
                }

                return CraftingExecutionResult.Failure(CraftingExecutionStatus.InvalidRequest, $"Crafting operation '{working.operationId}' already exists in state {existing.state}.", existing);
            }

            RuntimeRollbackSnapshot rollback = working.preview
                ? null
                : RuntimeRollbackSnapshot.Capture(itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime, this);
            RecipeResolutionRequest recipeRequest = BuildRecipeRequest(working);
            RecipeResolutionResult recipeResult = recipeRuntime.Resolve(recipeRequest, registry, productionRuntime, itemRuntime, durabilityRuntime);
            CraftingOperationRecordData operation = CreateOperation(working, recipeResult);
            if (recipeResult == null || !recipeResult.Succeeded)
            {
                operation.state = CraftingOperationState.Failed;
                operation.status = recipeResult == null || recipeResult.Status == RecipeResolutionStatus.MissingRecipe ? CraftingExecutionStatus.MissingRecipe : CraftingExecutionStatus.RequirementFailed;
                operation.diagnostics = AddDiagnostics(operation.diagnostics, recipeResult?.Message ?? "Recipe resolution failed.");
                if (rollback != null && !rollback.Restore(registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime, this, out string rollbackFailure))
                {
                    operation.status = CraftingExecutionStatus.RollbackFailed;
                    operation.diagnostics = AddDiagnostics(operation.diagnostics, rollbackFailure);
                    return CraftingExecutionResult.Failure(CraftingExecutionStatus.RollbackFailed, rollbackFailure, operation, recipeResult);
                }

                return CraftingExecutionResult.Failure(operation.status, recipeResult?.Message ?? "Recipe resolution failed.", operation, recipeResult);
            }

            if (working.preview)
            {
                operation.state = CraftingOperationState.Prepared;
                operation.status = CraftingExecutionStatus.Preview;
                return CraftingExecutionResult.Success(operation, "Crafting execution preview prepared.", preview: true, recipeResult: recipeResult);
            }

            operation.state = CraftingOperationState.Executing;
            operation.status = CraftingExecutionStatus.Succeeded;

            try
            {
                ProductionRequirementEvaluationResult current = productionRuntime.ValidatePlanCurrent(recipeResult.RequirementResult?.Plan?.planId, itemRuntime, durabilityRuntime);
                if (current == null || !current.Succeeded)
                {
                    return FailAndRollback(CraftingExecutionStatus.StalePlan, current?.Message ?? "Crafting requirement plan is stale.", operation, recipeResult, rollback, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime);
                }

                if (!ConsumeInputs(operation, recipeResult.RequirementResult?.Plan, itemRuntime, out string consumeFailure))
                {
                    return FailAndRollback(CraftingExecutionStatus.InputConsumptionFailed, consumeFailure, operation, recipeResult, rollback, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime);
                }

                if (!CreateOutputs(operation, recipeResult.Snapshot, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, out string outputFailure))
                {
                    return FailAndRollback(CraftingExecutionStatus.OutputCreationFailed, outputFailure, operation, recipeResult, rollback, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime);
                }

                if (!ApplyToolWear(operation, recipeResult.RequirementResult?.Plan, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, registry, out string toolFailure))
                {
                    return FailAndRollback(CraftingExecutionStatus.ToolWearFailed, toolFailure, operation, recipeResult, rollback, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime);
                }

                ProductionReservationResult release = productionRuntime.ReleasePlanReservations(recipeResult.RequirementResult?.Plan?.planId);
                if (release == null || !release.Succeeded)
                {
                    return FailAndRollback(CraftingExecutionStatus.ReservationFailed, release?.Message ?? "Crafting reservation release failed.", operation, recipeResult, rollback, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime);
                }

                operation.state = CraftingOperationState.Completed;
                operation.status = CraftingExecutionStatus.Succeeded;
                operation.revision = 1L;
                operationsById.Add(operation.operationId, operation.Clone());
                revision++;
                return CraftingExecutionResult.Success(operation, "Crafting execution completed.", recipeResult: recipeResult);
            }
            catch (Exception ex)
            {
                return FailAndRollback(CraftingExecutionStatus.ValidationFailed, ex.Message, operation, recipeResult, rollback, registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime);
            }
        }

        public CraftingExecutionRuntimeSaveData CreateSaveData()
        {
            return new CraftingExecutionRuntimeSaveData
            {
                schemaVersion = CraftingExecutionRuntimeSaveData.CurrentSchemaVersion,
                revision = revision,
                operations = operationsById.Values.OrderBy(entry => entry.operationId, StringComparer.Ordinal).Select(entry => entry.Clone()).ToList()
            };
        }

        public CraftingExecutionResult RestoreFromSaveData(CraftingExecutionRuntimeSaveData saveData, DefinitionRegistry registry)
        {
            if (!ValidateSaveData(saveData, registry, out string failure))
            {
                return CraftingExecutionResult.Failure(CraftingExecutionStatus.RestoreFailed, failure);
            }

            operationsById.Clear();
            foreach (CraftingOperationRecordData operation in saveData.operations.Select(entry => entry.Clone()).OrderBy(entry => entry.operationId, StringComparer.Ordinal))
            {
                operationsById[operation.operationId] = operation;
            }

            revision = Math.Max(0L, saveData.revision);
            return CraftingExecutionResult.Success(null, "Crafting execution runtime restored.");
        }

        public static bool ValidateSaveData(CraftingExecutionRuntimeSaveData saveData, DefinitionRegistry registry, out string failure)
        {
            failure = string.Empty;
            if (saveData == null)
            {
                failure = "Crafting execution save data is missing.";
                return false;
            }

            if (saveData.schemaVersion != CraftingExecutionRuntimeSaveData.CurrentSchemaVersion)
            {
                failure = $"Unsupported crafting execution schema version {saveData.schemaVersion}.";
                return false;
            }

            if (saveData.revision < 0L)
            {
                failure = "Crafting execution revision cannot be negative.";
                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (CraftingOperationRecordData operation in saveData.operations ?? new List<CraftingOperationRecordData>())
            {
                if (operation == null || string.IsNullOrWhiteSpace(operation.operationId))
                {
                    failure = "Crafting operation is missing an operation ID.";
                    return false;
                }

                if (!ids.Add(operation.operationId))
                {
                    failure = $"Duplicate crafting operation ID '{operation.operationId}'.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(operation.recipeId))
                {
                    failure = $"Crafting operation '{operation.operationId}' is missing a recipe ID.";
                    return false;
                }

                if (registry != null && !registry.TryGet(operation.recipeId, out RecipeDefinition _))
                {
                    failure = $"Crafting operation '{operation.operationId}' references missing recipe '{operation.recipeId}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateExecutionInputs(CraftingExecutionRequest request, DefinitionRegistry registry, RecipeRuntime recipeRuntime, ProductionRequirementRuntime productionRuntime, ItemInstanceIdentityRuntime itemRuntime, out string failure)
        {
            failure = string.Empty;
            if (request == null)
            {
                failure = "Crafting execution request is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.operationId))
            {
                failure = "Crafting execution requires an operation ID.";
                return false;
            }

            if (registry == null || recipeRuntime == null || productionRuntime == null || itemRuntime == null)
            {
                failure = "Crafting execution requires recipe, production, item, and definition runtimes.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.recipeId))
            {
                failure = "Crafting execution requires a recipe ID.";
                return false;
            }

            return true;
        }

        private static RecipeResolutionRequest BuildRecipeRequest(CraftingExecutionRequest request)
        {
            ProductionContextData context = request.productionContext?.Clone() ?? new ProductionContextData();
            if (string.IsNullOrWhiteSpace(context.actorPersonId))
            {
                context.actorPersonId = request.actorPersonId;
            }

            if (string.IsNullOrWhiteSpace(context.actorBodyId))
            {
                context.actorBodyId = request.actorBodyId;
            }

            if (string.IsNullOrWhiteSpace(context.locationId))
            {
                context.locationId = request.locationId;
            }

            if (string.IsNullOrWhiteSpace(context.worldTime))
            {
                context.worldTime = request.worldTime;
            }

            return new RecipeResolutionRequest
            {
                recipeId = request.recipeId,
                versionId = request.versionId,
                variantId = request.variantId,
                batchSize = request.batchSize,
                selectedOptionalInputIds = request.selectedOptionalInputIds,
                accessLevel = request.accessLevel,
                productionContext = context,
                buildRequirementPlan = true,
                reservePlan = !request.preview,
                productionJobId = request.operationId,
                planId = $"crafting-plan.{request.operationId}",
                reservationExpiresWorldTime = request.worldTime
            };
        }

        private static CraftingOperationRecordData CreateOperation(CraftingExecutionRequest request, RecipeResolutionResult recipeResult)
        {
            RecipeResolvedSnapshot snapshot = recipeResult?.Snapshot;
            return new CraftingOperationRecordData
            {
                operationId = request.operationId,
                recipeId = request.recipeId,
                versionId = snapshot?.VersionId ?? request.versionId,
                variantId = snapshot?.VariantId ?? request.variantId,
                actorPersonId = request.actorPersonId,
                actorBodyId = request.actorBodyId,
                ownerPersonId = string.IsNullOrWhiteSpace(request.ownerPersonId) ? request.actorPersonId : request.ownerPersonId,
                locationId = request.locationId,
                worldTime = request.worldTime,
                deterministicSeed = request.deterministicSeed,
                recipeSignature = snapshot?.Signature ?? string.Empty,
                requirementPlanId = recipeResult?.RequirementResult?.Plan?.planId ?? string.Empty,
                state = request.preview ? CraftingOperationState.Prepared : CraftingOperationState.Reserved,
                status = request.preview ? CraftingExecutionStatus.Preview : CraftingExecutionStatus.Succeeded,
                failurePolicy = request.failurePolicy
            };
        }

        private static bool ConsumeInputs(CraftingOperationRecordData operation, ProductionRequirementPlanData plan, ItemInstanceIdentityRuntime itemRuntime, out string failure)
        {
            failure = string.Empty;
            foreach (ProductionInputAllocationData allocation in Allocations(plan))
            {
                CraftingConsumedInputData consumed = new CraftingConsumedInputData
                {
                    allocationId = allocation.allocationId,
                    inputId = allocation.requirementId,
                    itemInstanceId = allocation.itemInstanceId,
                    definitionId = allocation.definitionId,
                    quantity = allocation.quantity,
                    unit = allocation.unit,
                    reusable = allocation.reusable,
                    consumed = false
                };

                if (!allocation.reusable && !string.IsNullOrWhiteSpace(allocation.itemInstanceId))
                {
                    ItemInstanceOperationResult destroy = itemRuntime.DestroyOrConsume(allocation.itemInstanceId, consumed: true);
                    if (!destroy.Succeeded)
                    {
                        failure = destroy.Message;
                        return false;
                    }

                    consumed.consumed = true;
                }

                operation.consumedInputs.Add(consumed);
            }

            return true;
        }

        private static bool CreateOutputs(
            CraftingOperationRecordData operation,
            RecipeResolvedSnapshot recipe,
            DefinitionRegistry registry,
            ItemInstanceIdentityRuntime itemRuntime,
            ItemCompositionRuntime compositionRuntime,
            ItemQualityAffixRuntime qualityRuntime,
            ItemDurabilityRuntime durabilityRuntime,
            out string failure)
        {
            failure = string.Empty;
            if (recipe == null)
            {
                failure = "Resolved recipe snapshot is missing.";
                return false;
            }

            foreach (RecipeOutputSpecificationData output in recipe.Outputs.OrderBy(entry => entry.outputId, StringComparer.Ordinal))
            {
                CraftingOutputItemData record = new CraftingOutputItemData
                {
                    outputId = output.outputId,
                    itemDefinitionId = output.itemDefinitionId,
                    materialDefinitionId = output.materialDefinitionId,
                    outputKind = ToOutputKind(output.role),
                    quantity = output.quantity,
                    unit = output.unit
                };

                if (!string.IsNullOrWhiteSpace(output.itemDefinitionId))
                {
                    if (!registry.TryGet(output.itemDefinitionId, out ItemDefinition itemDefinition))
                    {
                        failure = $"Crafting output '{output.outputId}' references missing item definition '{output.itemDefinitionId}'.";
                        return false;
                    }

                    string itemInstanceId = DeterministicGuid($"{operation.operationId}:{output.outputId}:0");
                    ItemInstanceOperationResult create = itemRuntime.CreateItem(
                        itemDefinition,
                        itemDefinition.Stackable ? ItemInstanceClassification.StackableWhileEquivalent : ItemInstanceClassification.IndividuallyTracked,
                        itemInstanceId,
                        creatorPersonId: operation.actorPersonId,
                        ownerPersonId: operation.ownerPersonId,
                        custodianPersonId: operation.ownerPersonId,
                        creationSourceId: operation.operationId);
                    if (!create.Succeeded)
                    {
                        failure = create.Message;
                        return false;
                    }

                    record.itemInstanceId = itemInstanceId;
                    record.createdItemInstance = true;
                    if (compositionRuntime != null)
                    {
                        ItemCompositionOperationResult composition = SetOutputComposition(recipe, output, itemInstanceId, itemDefinition.Id, registry, itemRuntime, compositionRuntime, operation.operationId);
                        if (!composition.Succeeded)
                        {
                            failure = composition.Message;
                            return false;
                        }
                    }

                    if (qualityRuntime != null)
                    {
                        ItemQualityAffixOperationResult quality = qualityRuntime.EnsureDefaultQuality(itemRuntime, compositionRuntime, registry, itemInstanceId);
                        if (!quality.Succeeded)
                        {
                            failure = quality.Message;
                            return false;
                        }

                        qualityRuntime.GenerateAffixes(itemRuntime, compositionRuntime, registry, new ItemAffixGenerationRequest
                        {
                            ItemInstanceId = itemInstanceId,
                            PolicyId = string.IsNullOrWhiteSpace(output.affixPolicyId) ? "affix-policy.crafting.default" : output.affixPolicyId,
                            Seed = string.IsNullOrWhiteSpace(operation.deterministicSeed) ? operation.operationId : operation.deterministicSeed,
                            RequestedAffixCount = output.affixPolicy == RecipeAffixPolicy.None ? 0 : 1,
                            Source = ItemAffixSource.Generated,
                            CorrelationId = operation.operationId
                        });
                    }

                    if (durabilityRuntime != null)
                    {
                        ItemDurabilityOperationResult durability = durabilityRuntime.EnsureDefaultDurability(itemRuntime, compositionRuntime, qualityRuntime, registry, itemInstanceId);
                        if (!durability.Succeeded)
                        {
                            failure = durability.Message;
                            return false;
                        }
                    }
                }

                operation.outputs.Add(record);
            }

            return operation.outputs.Count > 0;
        }

        private static ItemCompositionOperationResult SetOutputComposition(
            RecipeResolvedSnapshot recipe,
            RecipeOutputSpecificationData output,
            string itemInstanceId,
            string itemDefinitionId,
            DefinitionRegistry registry,
            ItemInstanceIdentityRuntime itemRuntime,
            ItemCompositionRuntime compositionRuntime,
            string operationId)
        {
            string materialId = !string.IsNullOrWhiteSpace(output.materialDefinitionId)
                ? output.materialDefinitionId
                : recipe.Inputs.FirstOrDefault(input => !string.IsNullOrWhiteSpace(input.materialDefinitionId))?.materialDefinitionId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(materialId))
            {
                return compositionRuntime.EnsureCompositionForItem(itemRuntime, registry, itemInstanceId);
            }

            ItemCompositionRecordData composition = new ItemCompositionRecordData
            {
                compositionId = $"item-composition.{itemInstanceId}",
                itemInstanceId = itemInstanceId,
                sourceItemDefinitionId = itemDefinitionId,
                completeness = ItemCompositionCompleteness.Complete,
                source = operationId,
                massAuthority = ItemCompositionMassAuthority.CompositionProjection,
                lastMutationPurpose = ItemCompositionMutationPurpose.CraftingProduction,
                materials =
                {
                    new ItemMaterialEntryData
                    {
                        entryId = $"material.{output.outputId}",
                        materialDefinitionId = materialId,
                        role = MaterialEntryRole.PrimaryStructure,
                        quantity = new MaterialQuantityData { value = Math.Max(0.001f, output.quantity), unit = ToMaterialUnit(output.unit) },
                        purity = 1f
                    }
                },
                components =
                {
                    new ItemComponentEntryData
                    {
                        componentEntryId = $"component.{output.outputId}",
                        kind = ItemComponentKind.AbstractComponent,
                        materialEntryIds = new[] { $"material.{output.outputId}" }
                    }
                },
                provenanceIds = new[] { operationId },
                tags = new[] { "item.composition", "composition.crafted" }
            };
            return compositionRuntime.SetComposition(itemRuntime, registry, composition, ItemCompositionMutationPurpose.CraftingProduction);
        }

        private static bool ApplyToolWear(
            CraftingOperationRecordData operation,
            ProductionRequirementPlanData plan,
            ItemInstanceIdentityRuntime itemRuntime,
            ItemCompositionRuntime compositionRuntime,
            ItemQualityAffixRuntime qualityRuntime,
            ItemDurabilityRuntime durabilityRuntime,
            DefinitionRegistry registry,
            out string failure)
        {
            failure = string.Empty;
            if (durabilityRuntime == null)
            {
                return true;
            }

            foreach (ProductionRequirementSelectionData selection in (plan?.selections ?? new List<ProductionRequirementSelectionData>()).OrderBy(entry => entry.requirementId, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(selection.selectedToolItemInstanceId) || selection.expectedToolWear <= 0f)
                {
                    continue;
                }

                ItemDurabilityOperationResult wear = durabilityRuntime.ApplyWear(itemRuntime, compositionRuntime, qualityRuntime, registry, selection.selectedToolItemInstanceId, selection.expectedToolWear, operation.operationId);
                if (!wear.Succeeded)
                {
                    failure = wear.Message;
                    return false;
                }

                operation.toolUses.Add(new CraftingToolUseData
                {
                    requirementId = selection.requirementId,
                    toolItemInstanceId = selection.selectedToolItemInstanceId,
                    toolDefinitionId = selection.selectedToolDefinitionId,
                    wearApplied = selection.expectedToolWear,
                    applied = true
                });
            }

            return true;
        }

        private CraftingExecutionResult FailAndRollback(
            CraftingExecutionStatus status,
            string message,
            CraftingOperationRecordData operation,
            RecipeResolutionResult recipeResult,
            RuntimeRollbackSnapshot rollback,
            DefinitionRegistry registry,
            ItemInstanceIdentityRuntime itemRuntime,
            ItemCompositionRuntime compositionRuntime,
            ItemQualityAffixRuntime qualityRuntime,
            ItemDurabilityRuntime durabilityRuntime,
            ProductionRequirementRuntime productionRuntime)
        {
            operation.state = CraftingOperationState.Failed;
            operation.status = status;
            operation.diagnostics = AddDiagnostics(operation.diagnostics, message);
            if (rollback == null)
            {
                return CraftingExecutionResult.Failure(status, message, operation, recipeResult);
            }

            if (!rollback.Restore(registry, itemRuntime, compositionRuntime, qualityRuntime, durabilityRuntime, productionRuntime, this, out string rollbackFailure))
            {
                operation.state = CraftingOperationState.Failed;
                operation.status = CraftingExecutionStatus.RollbackFailed;
                operation.diagnostics = AddDiagnostics(operation.diagnostics, rollbackFailure);
                return CraftingExecutionResult.Failure(CraftingExecutionStatus.RollbackFailed, $"{message} Rollback failed: {rollbackFailure}", operation, recipeResult);
            }

            operation.state = CraftingOperationState.RolledBack;
            return CraftingExecutionResult.Failure(status, message, operation, recipeResult);
        }

        private static IEnumerable<ProductionInputAllocationData> Allocations(ProductionRequirementPlanData plan)
        {
            return (plan?.selections ?? new List<ProductionRequirementSelectionData>())
                .SelectMany(selection => selection.allocations ?? new List<ProductionInputAllocationData>())
                .OrderBy(allocation => allocation.requirementId, StringComparer.Ordinal)
                .ThenBy(allocation => allocation.allocationId, StringComparer.Ordinal);
        }

        private static CraftingOutputKind ToOutputKind(RecipeOutputRole role)
        {
            return role switch
            {
                RecipeOutputRole.SecondaryOutput => CraftingOutputKind.Secondary,
                RecipeOutputRole.Byproduct => CraftingOutputKind.Byproduct,
                RecipeOutputRole.Waste => CraftingOutputKind.Waste,
                RecipeOutputRole.Scrap => CraftingOutputKind.Scrap,
                RecipeOutputRole.RecoveredInput => CraftingOutputKind.RecoveredInput,
                RecipeOutputRole.FailedResult or RecipeOutputRole.DamagedResult => CraftingOutputKind.FailureOutput,
                _ => CraftingOutputKind.Primary
            };
        }

        private static MaterialQuantityUnit ToMaterialUnit(ProductionQuantityUnit unit)
        {
            return unit switch
            {
                ProductionQuantityUnit.Kilogram => MaterialQuantityUnit.Kilogram,
                ProductionQuantityUnit.Liter => MaterialQuantityUnit.Liter,
                _ => MaterialQuantityUnit.Count
            };
        }

        private static string[] AddDiagnostics(IEnumerable<string> existing, string message)
        {
            return (existing ?? Array.Empty<string>())
                .Concat(new[] { message ?? string.Empty })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void NormalizeRequest(CraftingExecutionRequest request)
        {
            request.operationId = string.IsNullOrWhiteSpace(request.operationId) ? $"crafting-operation.{DeterministicGuid($"{request.recipeId}:{request.worldTime}:{request.deterministicSeed}")}" : request.operationId.Trim();
            request.batchSize = request.batchSize <= 0f ? 1f : request.batchSize;
            request.actorPersonId = request.actorPersonId ?? string.Empty;
            request.ownerPersonId = string.IsNullOrWhiteSpace(request.ownerPersonId) ? request.actorPersonId : request.ownerPersonId;
            request.custodianPersonId = string.IsNullOrWhiteSpace(request.custodianPersonId) ? request.ownerPersonId : request.custodianPersonId;
        }

        private static string DeterministicGuid(string seed)
        {
            using MD5 md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(seed ?? string.Empty));
            return new Guid(bytes).ToString("D");
        }

        private sealed class RuntimeRollbackSnapshot
        {
            private ItemInstanceRuntimeSaveData itemInstances;
            private ItemCompositionRuntimeSaveData itemCompositions;
            private ItemQualityAffixRuntimeSaveData itemQuality;
            private ItemDurabilityRuntimeSaveData itemDurability;
            private ProductionRequirementRuntimeSaveData production;
            private CraftingExecutionRuntimeSaveData crafting;

            public static RuntimeRollbackSnapshot Capture(
                ItemInstanceIdentityRuntime itemRuntime,
                ItemCompositionRuntime compositionRuntime,
                ItemQualityAffixRuntime qualityRuntime,
                ItemDurabilityRuntime durabilityRuntime,
                ProductionRequirementRuntime productionRuntime,
                CraftingExecutionRuntime craftingRuntime)
            {
                return new RuntimeRollbackSnapshot
                {
                    itemInstances = itemRuntime?.CreateSaveData(),
                    itemCompositions = compositionRuntime?.CreateSaveData(),
                    itemQuality = qualityRuntime?.CreateSaveData(),
                    itemDurability = durabilityRuntime?.CreateSaveData(),
                    production = productionRuntime?.CreateSaveData(),
                    crafting = craftingRuntime?.CreateSaveData()
                };
            }

            public bool Restore(
                DefinitionRegistry registry,
                ItemInstanceIdentityRuntime itemRuntime,
                ItemCompositionRuntime compositionRuntime,
                ItemQualityAffixRuntime qualityRuntime,
                ItemDurabilityRuntime durabilityRuntime,
                ProductionRequirementRuntime productionRuntime,
                CraftingExecutionRuntime craftingRuntime,
                out string failure)
            {
                failure = string.Empty;
                if (itemRuntime != null && itemInstances != null)
                {
                    ItemInstanceOperationResult result = itemRuntime.RestoreFromSaveData(itemInstances, registry);
                    if (!result.Succeeded)
                    {
                        failure = result.Message;
                        return false;
                    }
                }

                if (compositionRuntime != null && itemCompositions != null)
                {
                    ItemCompositionOperationResult result = compositionRuntime.RestoreFromSaveData(itemCompositions, registry, itemRuntime);
                    if (!result.Succeeded)
                    {
                        failure = result.Message;
                        return false;
                    }
                }

                if (qualityRuntime != null && itemQuality != null)
                {
                    ItemQualityAffixOperationResult result = qualityRuntime.RestoreFromSaveData(itemQuality, registry, itemRuntime);
                    if (!result.Succeeded)
                    {
                        failure = result.Message;
                        return false;
                    }
                }

                if (durabilityRuntime != null && itemDurability != null)
                {
                    ItemDurabilityOperationResult result = durabilityRuntime.RestoreFromSaveData(itemDurability, registry, itemRuntime, compositionRuntime);
                    if (!result.Succeeded)
                    {
                        failure = result.Message;
                        return false;
                    }
                }

                if (productionRuntime != null && production != null)
                {
                    ProductionRequirementEvaluationResult result = productionRuntime.RestoreFromSaveData(production);
                    if (!result.Succeeded)
                    {
                        failure = result.Message;
                        return false;
                    }
                }

                if (craftingRuntime != null && crafting != null)
                {
                    CraftingExecutionResult result = craftingRuntime.RestoreFromSaveData(crafting, registry);
                    if (!result.Succeeded)
                    {
                        failure = result.Message;
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
