using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.Beings.Biology;
using UnityIsekaiGame.CharacterSystem;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Combat.Execution;
using UnityIsekaiGame.Combat.OngoingEffects;
using UnityIsekaiGame.Economy;
using UnityIsekaiGame.Economy.Businesses;
using UnityIsekaiGame.Economy.Markets;
using UnityIsekaiGame.Economy.Payroll;
using UnityIsekaiGame.Economy.Properties;
using UnityIsekaiGame.Economy.RegionalFlow;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Inventory.Crafting;
using UnityIsekaiGame.Inventory.Composition;
using UnityIsekaiGame.Inventory.Durability;
using UnityIsekaiGame.Inventory.Experimentation;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Inventory.Production;
using UnityIsekaiGame.Inventory.Quality;
using UnityIsekaiGame.Inventory.Recipes;
using UnityIsekaiGame.Knowledge;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Knowledge.History;
using UnityIsekaiGame.Knowledge.Records;
using UnityIsekaiGame.Knowledge.Sharing;
using UnityIsekaiGame.Knowledge.Sources;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.Organizations;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Professions;
using UnityIsekaiGame.Progression;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.Social.Decisions;
using UnityIsekaiGame.Social.Attitudes;
using UnityIsekaiGame.Social.Emotions;
using UnityIsekaiGame.Social.Family;
using UnityIsekaiGame.Social.Influence;
using UnityIsekaiGame.Social.Interactions;
using UnityIsekaiGame.Social.Networks;
using UnityIsekaiGame.Social.Norms;
using UnityIsekaiGame.Social.Reputation;
using UnityIsekaiGame.Social.Relationships;
using UnityIsekaiGame.Social.Rumors;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Traits;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Economy.InstitutionalRevenue;
using UnityIsekaiGame.Economy.Trading;
using UnityIsekaiGame.WorldEntities;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PrototypePersistenceServiceBehaviour : MonoBehaviour, IItemDurabilityRuntimeProvider
    {
        [SerializeField] private PrototypePersistenceState prototypeState;
        [SerializeField] private DefinitionCatalog definitionCatalog;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerMana playerMana;
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private CharacterAttributes playerAttributes;
        [SerializeField] private CalculatedStatCollection playerCalculatedStats;
        [SerializeField] private CharacterResourceCollection playerResources;
        [SerializeField] private ActorLifecycleController playerActorLifecycle;
        [SerializeField] private OngoingEffectService playerOngoingEffects;
        [SerializeField] private CharacterSkillCollection playerSkills;
        [SerializeField] private CharacterTraitCollection playerTraits;
        [SerializeField] private ActorBodyRuntime playerBody;
        [SerializeField] private PersonKnowledgeRuntime playerKnowledge;
        [SerializeField] private PlayerSkillActionEventSource playerSkillActionEventSource;
        [SerializeField] private StatusEffectController statusEffectController;
        [SerializeField] private PlayerIdentityProgression playerIdentityProgression;
        [SerializeField] private OverallLevelConfiguration overallLevelConfiguration;
        [SerializeField] private PlayerQuestLog playerQuestLog;
        [SerializeField] private PlayerContractJournal playerContractJournal;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private MonoBehaviour inventoryScreenController;
        [SerializeField] private CurrentPlaceTracker currentPlaceTracker;
        [SerializeField] private string sceneKey = "scene.prototype";
        [SerializeField] private string defaultSpawnPointId = "spawn.prototype.default";
        [SerializeField] private string defaultPlayerSpeciesId = "species.human";
        [SerializeField] private bool registerPlayerInventoryEquipment = true;
        [SerializeField] private bool registerPlayerItemIdentities = true;
        [SerializeField] private bool registerWorldEconomy = true;
        [SerializeField] private bool registerWorldMarkets = true;
        [SerializeField] private bool registerWorldTrades = true;
        [SerializeField] private bool registerWorldPayroll = true;
        [SerializeField] private bool registerWorldBusinesses = true;
        [SerializeField] private bool registerWorldProperties = true;
        [SerializeField] private bool registerWorldContracts = true;
        [SerializeField] private bool registerWorldInstitutionalRevenue = true;
        [SerializeField] private bool registerWorldRegionalFlow = true;
        [SerializeField] private bool registerWorldOrganizations = true;
        [SerializeField] private bool registerWorldOrganizationMemberships = true;
        [SerializeField] private bool registerWorldOrganizationAuthority = true;
        [SerializeField] private bool registerWorldOrganizationResources = true;
        [SerializeField] private bool registerWorldOrganizationDecisions = true;
        [SerializeField] private bool registerPlayerItemCompositions = true;
        [SerializeField] private bool registerPlayerItemQualityAffixes = true;
        [SerializeField] private bool registerPlayerItemDurability = true;
        [SerializeField] private bool registerPlayerProductionRequirements = true;
        [SerializeField] private bool registerPlayerRecipeKnowledge = true;
        [SerializeField] private bool registerPlayerCraftingExecution = true;
        [SerializeField] private bool registerPlayerProductionWorkflow = true;
        [SerializeField] private bool registerPlayerExperimentation = true;
        [SerializeField] private bool registerPlayerIdentityProgression = true;
        [SerializeField] private bool registerPlayerAttributes = true;
        [SerializeField] private bool registerPlayerSkills = true;
        [SerializeField] private bool registerPlayerTraits = true;
        [SerializeField] private bool registerPlayerBody = true;
        [SerializeField] private bool registerPlayerKnowledge = true;
        [SerializeField] private bool registerPlayerProfessions = true;
        [SerializeField] private bool registerPlayerProfessionEntries = true;
        [SerializeField] private bool registerPlayerTraining = true;
        [SerializeField] private bool registerPlayerProfessionalActivities = true;
        [SerializeField] private bool registerPlayerCredentials = true;
        [SerializeField] private bool registerPlayerProfessionalRanks = true;
        [SerializeField] private bool registerPlayerPositionEmployment = true;
        [SerializeField] private bool registerPlayerCareerHistory = true;
        [SerializeField] private bool registerPlayerLifePaths = true;
        [SerializeField] private bool registerPlayerRelationships = true;
        [SerializeField] private bool registerPlayerInterpersonalAttitudes = true;
        [SerializeField] private bool registerWorldReputation = true;
        [SerializeField] private bool registerWorldRumors = true;
        [SerializeField] private bool registerWorldSocialInteractions = true;
        [SerializeField] private bool registerWorldSocialNorms = true;
        [SerializeField] private bool registerWorldSocialNetworks = true;
        [SerializeField] private bool registerWorldSocialDecisions = true;
        [SerializeField] private bool registerWorldSocialInfluence = true;
        [SerializeField] private bool registerWorldSocialEmotions = true;
        [SerializeField] private bool registerWorldFamilyRelationships = true;
        [SerializeField] private bool registerPlayerInformationSources = true;
        [SerializeField] private bool registerPlayerInformationTransfers = true;
        [SerializeField] private bool registerPlayerInformationAccess = true;
        [SerializeField] private bool registerPlayerKnowledgeRecords = true;
        [SerializeField] private bool registerPlayerStatsVitalsStatus = true;
        [SerializeField] private bool registerPlayerResources = true;
        [SerializeField] private bool registerPlayerCombatExecution = true;
        [SerializeField] private bool registerPlayerActorLifecycle = true;
        [SerializeField] private bool registerPlayerOngoingEffects = true;
        [SerializeField] private bool registerPlayerQuestContract = true;
        [SerializeField] private bool registerPlayerLocation = true;
        [SerializeField] private string prototypeSlotId = PersistenceService.PrototypeSlotId;
        [Header("Save Slots")]
        [SerializeField, Min(1)] private int manualSlotCount = PrototypeSaveSlotCatalog.DefaultManualSlotCount;
        [SerializeField, Min(1)] private int autosaveSlotCount = PrototypeSaveSlotCatalog.DefaultAutosaveSlotCount;
        [SerializeField, Min(5f)] private float autosaveIntervalSeconds = 300f;
        [SerializeField] private PlayTimeTracker playTimeTracker;
        [SerializeField] private GameSaveDirtyTracker dirtyTracker;
        [SerializeField] private AutosaveCoordinator autosaveCoordinator;

        private PersistenceService service;
        private PrototypePersistenceStateParticipant participant;
        private PlayerIdentityProgressionPersistenceParticipant identityProgressionParticipant;
        private PlayerAttributesPersistenceParticipant playerAttributesParticipant;
        private PlayerSkillsPersistenceParticipant playerSkillsParticipant;
        private PlayerTraitsPersistenceParticipant playerTraitsParticipant;
        private PlayerBodyPersistenceParticipant playerBodyParticipant;
        private PersonKnowledgePersistenceParticipant playerKnowledgeParticipant;
        private InformationSourcePersistenceParticipant playerInformationSourceParticipant;
        private InformationTransferPersistenceParticipant playerInformationTransferParticipant;
        private InformationAccessPersistenceParticipant playerInformationAccessParticipant;
        private KnowledgeRecordPersistenceParticipant playerKnowledgeRecordParticipant;
        private PersonProfessionPersistenceParticipant playerProfessionParticipant;
        private ProfessionEntryPersistenceParticipant playerProfessionEntryParticipant;
        private TrainingPersistenceParticipant playerTrainingParticipant;
        private ProfessionalActivityPersistenceParticipant playerProfessionalActivityParticipant;
        private CredentialPersistenceParticipant playerCredentialParticipant;
        private ProfessionalRankPersistenceParticipant playerProfessionalRankParticipant;
        private PositionEmploymentPersistenceParticipant playerPositionEmploymentParticipant;
        private CareerHistoryPersistenceParticipant playerCareerHistoryParticipant;
        private LifePathPersistenceParticipant playerLifePathParticipant;
        private RelationshipPersistenceParticipant playerRelationshipParticipant;
        private InterpersonalAttitudePersistenceParticipant playerInterpersonalAttitudeParticipant;
        private ReputationPersistenceParticipant worldReputationParticipant;
        private RumorPersistenceParticipant worldRumorParticipant;
        private SocialInteractionPersistenceParticipant worldSocialInteractionParticipant;
        private SocialNormPersistenceParticipant worldSocialNormParticipant;
        private SocialNetworkPersistenceParticipant worldSocialNetworkParticipant;
        private SocialDecisionPersistenceParticipant worldSocialDecisionParticipant;
        private SocialInfluencePersistenceParticipant worldSocialInfluenceParticipant;
        private SocialEmotionPersistenceParticipant worldSocialEmotionParticipant;
        private FamilyRelationshipPersistenceParticipant worldFamilyRelationshipParticipant;
        private PlayerInventoryEquipmentPersistenceParticipant inventoryEquipmentParticipant;
        private ItemInstanceIdentityPersistenceParticipant itemIdentityParticipant;
        private EconomyPersistenceParticipant economyParticipant;
        private MarketPersistenceParticipant marketParticipant;
        private TradePersistenceParticipant tradeParticipant;
        private PayrollPersistenceParticipant payrollParticipant;
        private BusinessPersistenceParticipant businessParticipant;
        private PropertyPersistenceParticipant propertyParticipant;
        private ContractEconomyPersistenceParticipant contractEconomyParticipant;
        private InstitutionalRevenuePersistenceParticipant institutionalRevenueParticipant;
        private RegionalFlowPersistenceParticipant regionalFlowParticipant;
        private OrganizationPersistenceParticipant organizationParticipant;
        private OrganizationMembershipPersistenceParticipant organizationMembershipParticipant;
        private OrganizationAuthorityPersistenceParticipant organizationAuthorityParticipant;
        private OrganizationResourcePersistenceParticipant organizationResourceParticipant;
        private OrganizationDecisionPersistenceParticipant organizationDecisionParticipant;
        private ItemCompositionPersistenceParticipant itemCompositionParticipant;
        private ItemQualityAffixPersistenceParticipant itemQualityAffixParticipant;
        private ItemDurabilityPersistenceParticipant itemDurabilityParticipant;
        private ProductionRequirementPersistenceParticipant productionRequirementParticipant;
        private RecipeKnowledgePersistenceParticipant recipeKnowledgeParticipant;
        private CraftingExecutionPersistenceParticipant craftingExecutionParticipant;
        private ProductionWorkflowPersistenceParticipant productionWorkflowParticipant;
        private ExperimentationPersistenceParticipant experimentationParticipant;
        private PlayerStatsVitalsStatusPersistenceParticipant statsVitalsStatusParticipant;
        private PlayerResourcesPersistenceParticipant playerResourcesParticipant;
        private PlayerCombatExecutionPersistenceParticipant playerCombatExecutionParticipant;
        private PlayerActorLifecyclePersistenceParticipant playerActorLifecycleParticipant;
        private PlayerOngoingEffectsPersistenceParticipant playerOngoingEffectsParticipant;
        private PlayerQuestContractPersistenceParticipant questContractParticipant;
        private PlayerLocationPersistenceParticipant playerLocationParticipant;
        private DefinitionRegistry definitionRegistry;
        private CombatExecutionService combatExecutionService;
        private InformationSourceRuntime playerInformationSources;
        private InformationTransferRuntime playerInformationTransfers;
        private InformationAccessRuntime playerInformationAccess;
        private KnowledgeRecordRuntime playerKnowledgeRecords;
        private PersonProfessionRuntime playerProfessions;
        private ProfessionEntryRuntime playerProfessionEntries;
        private TrainingRuntime playerTraining;
        private ProfessionalActivityRuntime playerProfessionalActivities;
        private CredentialRuntime playerCredentials;
        private ProfessionalRankRuntime playerProfessionalRanks;
        private PositionEmploymentRuntime playerPositionEmployment;
        private CareerHistoryRuntime playerCareerHistory;
        private LifePathRuntime playerLifePaths;
        private RelationshipRuntime playerRelationships;
        private InterpersonalAttitudeRuntime playerInterpersonalAttitudes;
        private ReputationRuntime worldReputation;
        private RumorRuntime worldRumors;
        private SocialInteractionRuntime worldSocialInteractions;
        private SocialNormRuntime worldSocialNorms;
        private SocialNetworkRuntime worldSocialNetworks;
        private SocialDecisionRuntime worldSocialDecisions;
        private SocialInfluenceRuntime worldSocialInfluence;
        private SocialEmotionRuntime worldSocialEmotions;
        private FamilyRelationshipRuntime worldFamilyRelationships;
        private ItemInstanceIdentityRuntime playerItemIdentities;
        private EconomyRuntime worldEconomy;
        private MarketRuntime worldMarkets;
        private TradeRuntime worldTrades;
        private PayrollRuntime worldPayroll;
        private BusinessRuntime worldBusinesses;
        private PropertyRuntime worldProperties;
        private ContractEconomyRuntime worldContracts;
        private InstitutionalRevenueRuntime worldInstitutionalRevenue;
        private RegionalFlowRuntime worldRegionalFlow;
        private OrganizationRuntime worldOrganizations;
        private OrganizationMembershipRuntime worldOrganizationMemberships;
        private OrganizationAuthorityRuntime worldOrganizationAuthority;
        private OrganizationResourceRuntime worldOrganizationResources;
        private OrganizationDecisionRuntime worldOrganizationDecisions;
        private ItemCompositionRuntime playerItemCompositions;
        private ItemQualityAffixRuntime playerItemQualityAffixes;
        private ItemDurabilityRuntime playerItemDurability;
        private ProductionRequirementRuntime playerProductionRequirements;
        private RecipeKnowledgeRuntime playerRecipeKnowledge;
        private CraftingExecutionRuntime playerCraftingExecution;
        private ProductionWorkflowRuntime playerProductionWorkflow;
        private ExperimentationRuntime playerExperimentation;
        private PlayerItemIdentitySynchronizer playerItemIdentitySynchronizer;
        private bool dirtyEventsSubscribed;

        public PersistenceService Service => service;
        public PrototypePersistenceState PrototypeState => prototypeState;
        public string PrototypeSlotId => string.IsNullOrWhiteSpace(prototypeSlotId) ? PersistenceService.PrototypeSlotId : prototypeSlotId;
        public int ManualSlotCount => Mathf.Max(1, manualSlotCount);
        public int AutosaveSlotCount => Mathf.Max(1, autosaveSlotCount);
        public PlayTimeTracker PlayTime => playTimeTracker;
        public GameSaveDirtyTracker DirtyTracker => dirtyTracker;
        public AutosaveCoordinator Autosave => autosaveCoordinator;
        public DefinitionCatalog DefinitionCatalog => definitionCatalog;
        public CombatExecutionService CombatExecution => combatExecutionService ??= new CombatExecutionService();
        public InformationSourceRuntime InformationSources => playerInformationSources ??= new InformationSourceRuntime();
        public InformationTransferRuntime InformationTransfers => playerInformationTransfers ??= new InformationTransferRuntime();
        public InformationAccessRuntime InformationAccess => playerInformationAccess ??= new InformationAccessRuntime();
        public KnowledgeRecordRuntime KnowledgeRecords => playerKnowledgeRecords ??= new KnowledgeRecordRuntime();
        public RelationshipRuntime Relationships
        {
            get
            {
                if (playerRelationships == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    playerRelationships = new RelationshipRuntime();
                    playerRelationships.Configure(GetDefinitionRegistry(), new[] { personId, service == null ? PersistenceService.LocalPlayerId : service.PlayerId });
                }

                return playerRelationships;
            }
        }
        public InterpersonalAttitudeRuntime InterpersonalAttitudes
        {
            get
            {
                if (playerInterpersonalAttitudes == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    playerInterpersonalAttitudes = new InterpersonalAttitudeRuntime();
                    playerInterpersonalAttitudes.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId));
                }

                return playerInterpersonalAttitudes;
            }
        }
        public ReputationRuntime Reputation
        {
            get
            {
                if (worldReputation == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldReputation = new ReputationRuntime();
                    worldReputation.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId));
                }

                return worldReputation;
            }
        }
        public RumorRuntime Rumors
        {
            get
            {
                if (worldRumors == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldRumors = new RumorRuntime();
                    worldRumors.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), ResolveKnowledgeRuntimeForPerson, ResolveMemoryRuntimeForPerson);
                }

                return worldRumors;
            }
        }
        public SocialInteractionRuntime SocialInteractions
        {
            get
            {
                if (worldSocialInteractions == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldSocialInteractions = new SocialInteractionRuntime();
                    worldSocialInteractions.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), Relationships, InterpersonalAttitudes, Reputation, Rumors);
                }

                return worldSocialInteractions;
            }
        }
        public SocialNormRuntime SocialNorms
        {
            get
            {
                if (worldSocialNorms == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldSocialNorms = new SocialNormRuntime();
                    worldSocialNorms.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialInteractions);
                }

                return worldSocialNorms;
            }
        }
        public SocialNetworkRuntime SocialNetworks
        {
            get
            {
                if (worldSocialNetworks == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldSocialNetworks = new SocialNetworkRuntime();
                    worldSocialNetworks.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialInteractions, SocialNorms);
                }

                return worldSocialNetworks;
            }
        }

        public SocialDecisionRuntime SocialDecisions
        {
            get
            {
                if (worldSocialDecisions == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldSocialDecisions = new SocialDecisionRuntime();
                    worldSocialDecisions.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), SocialInteractions, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialNorms, SocialNetworks, SocialDecisionModifierSourceCollection.Compose(SocialInfluence, SocialEmotions));
                }

                return worldSocialDecisions;
            }
        }

        public SocialInfluenceRuntime SocialInfluence
        {
            get
            {
                if (worldSocialInfluence == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldSocialInfluence = new SocialInfluenceRuntime();
                    worldSocialInfluence.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), InterpersonalAttitudes, Reputation, SocialInteractions, new[] { playerKnowledge });
                }

                return worldSocialInfluence;
            }
        }
        public SocialEmotionRuntime SocialEmotions
        {
            get
            {
                if (worldSocialEmotions == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    worldSocialEmotions = new SocialEmotionRuntime();
                    worldSocialEmotions.Configure(GetDefinitionRegistry(), GetPrototypeSocialPersonIds(personId), Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialInteractions, SocialNorms, SocialNetworks, SocialInfluence);
                }

                return worldSocialEmotions;
            }
        }
        public FamilyRelationshipRuntime FamilyRelationships
        {
            get
            {
                if (worldFamilyRelationships == null)
                {
                    string personId = playerIdentityProgression == null ? PersistenceService.LocalPlayerId : playerIdentityProgression.PersonId;
                    string[] knownPersons = GetPrototypeSocialPersonIds(personId);
                    worldFamilyRelationships = new FamilyRelationshipRuntime();
                    worldFamilyRelationships.Configure(GetDefinitionRegistry(), knownPersons, Relationships, InterpersonalAttitudes, SocialInteractions, service == null ? PersistenceService.LocalWorldId : service.WorldId, GetPrototypeAdultPersonIds(personId));
                }

                return worldFamilyRelationships;
            }
        }
        public PersonProfessionRuntime Professions
        {
            get
            {
                if (playerProfessions == null)
                {
                    playerProfessions = new PersonProfessionRuntime();
                    playerProfessions.Configure(GetDefinitionRegistry(), new[] { service == null ? PersistenceService.LocalPlayerId : service.PlayerId });
                }

                return playerProfessions;
            }
        }
        public ProfessionEntryRuntime ProfessionEntries
        {
            get
            {
                if (playerProfessionEntries == null)
                {
                    playerProfessionEntries = new ProfessionEntryRuntime();
                    playerProfessionEntries.Configure(GetDefinitionRegistry(), Professions, new[] { service == null ? PersistenceService.LocalPlayerId : service.PlayerId });
                }

                return playerProfessionEntries;
            }
        }
        public TrainingRuntime Training
        {
            get
            {
                if (playerTraining == null)
                {
                    playerTraining = new TrainingRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerTraining.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, new[] { personId });
                }

                return playerTraining;
            }
        }
        public ProfessionalActivityRuntime ProfessionalActivities
        {
            get
            {
                if (playerProfessionalActivities == null)
                {
                    playerProfessionalActivities = new ProfessionalActivityRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, new[] { personId });
                }

                return playerProfessionalActivities;
            }
        }
        public CredentialRuntime Credentials
        {
            get
            {
                if (playerCredentials == null)
                {
                    playerCredentials = new CredentialRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerCredentials.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, new[] { personId }, GetPrototypeCredentialAuthorities());
                }

                return playerCredentials;
            }
        }
        public ProfessionalRankRuntime ProfessionalRanks
        {
            get
            {
                if (playerProfessionalRanks == null)
                {
                    playerProfessionalRanks = new ProfessionalRankRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerProfessionalRanks.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, new[] { personId }, GetPrototypeCredentialAuthorities());
                }

                return playerProfessionalRanks;
            }
        }
        public PositionEmploymentRuntime PositionEmployment
        {
            get
            {
                if (playerPositionEmployment == null)
                {
                    playerPositionEmployment = new PositionEmploymentRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerPositionEmployment.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, new[] { personId }, GetPrototypeOrganizations(), GetPrototypeCredentialAuthorities());
                }

                return playerPositionEmployment;
            }
        }
        public CareerHistoryRuntime CareerHistory
        {
            get
            {
                if (playerCareerHistory == null)
                {
                    playerCareerHistory = new CareerHistoryRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerCareerHistory.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, PositionEmployment, new[] { personId }, GetPrototypeOrganizations(), GetPrototypeCredentialAuthorities());
                }

                return playerCareerHistory;
            }
        }
        public LifePathRuntime LifePaths
        {
            get
            {
                if (playerLifePaths == null)
                {
                    playerLifePaths = new LifePathRuntime();
                    string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                    playerLifePaths.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, PositionEmployment, CareerHistory, new[] { personId }, GetPrototypeOrganizations());
                }

                return playerLifePaths;
            }
        }
        public ItemInstanceIdentityRuntime ItemIdentities => playerItemIdentities ??= new ItemInstanceIdentityRuntime();
        public EconomyRuntime Economy
        {
            get
            {
                if (worldEconomy == null)
                {
                    worldEconomy = new EconomyRuntime();
                }

                worldEconomy.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldEconomy;
            }
        }
        public MarketRuntime Markets
        {
            get
            {
                if (worldMarkets == null)
                {
                    worldMarkets = new MarketRuntime();
                }

                worldMarkets.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldMarkets;
            }
        }
        public TradeRuntime Trades
        {
            get
            {
                if (worldTrades == null)
                {
                    worldTrades = new TradeRuntime();
                }

                worldTrades.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldTrades;
            }
        }
        public PayrollRuntime Payroll
        {
            get
            {
                if (worldPayroll == null)
                {
                    worldPayroll = new PayrollRuntime();
                }

                worldPayroll.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldPayroll;
            }
        }
        public BusinessRuntime Businesses
        {
            get
            {
                if (worldBusinesses == null)
                {
                    worldBusinesses = new BusinessRuntime();
                }

                worldBusinesses.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldBusinesses;
            }
        }
        public PropertyRuntime Properties
        {
            get
            {
                if (worldProperties == null)
                {
                    worldProperties = new PropertyRuntime();
                }

                worldProperties.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldProperties;
            }
        }
        public ContractEconomyRuntime ContractEconomy
        {
            get
            {
                if (worldContracts == null)
                {
                    worldContracts = new ContractEconomyRuntime();
                }

                worldContracts.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldContracts;
            }
        }
        public InstitutionalRevenueRuntime InstitutionalRevenue
        {
            get
            {
                if (worldInstitutionalRevenue == null)
                {
                    worldInstitutionalRevenue = new InstitutionalRevenueRuntime();
                }

                worldInstitutionalRevenue.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldInstitutionalRevenue;
            }
        }
        public RegionalFlowRuntime RegionalFlow
        {
            get
            {
                if (worldRegionalFlow == null)
                {
                    worldRegionalFlow = new RegionalFlowRuntime();
                }

                worldRegionalFlow.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                return worldRegionalFlow;
            }
        }
        public OrganizationRuntime Organizations
        {
            get
            {
                if (worldOrganizations == null)
                {
                    worldOrganizations = new OrganizationRuntime();
                    PrototypeOrganizationDefinitionFactory.SeedPrototypeOrganizations(worldOrganizations, GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId);
                }

                string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                worldOrganizations.Configure(GetDefinitionRegistry(), service == null ? PersistenceService.LocalWorldId : service.WorldId, GetPrototypeSocialPersonIds(personId), Array.Empty<string>());
                return worldOrganizations;
            }
        }
        public OrganizationMembershipRuntime OrganizationMemberships
        {
            get
            {
                if (worldOrganizationMemberships == null)
                {
                    worldOrganizationMemberships = new OrganizationMembershipRuntime();
                }

                string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                worldOrganizationMemberships.Configure(GetDefinitionRegistry(), Organizations, service == null ? PersistenceService.LocalWorldId : service.WorldId, GetPrototypeSocialPersonIds(personId), GetPrototypeOrganizations());
                return worldOrganizationMemberships;
            }
        }
        public OrganizationAuthorityRuntime OrganizationAuthority
        {
            get
            {
                if (worldOrganizationAuthority == null)
                {
                    worldOrganizationAuthority = new OrganizationAuthorityRuntime();
                }

                string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                worldOrganizationAuthority.Configure(GetDefinitionRegistry(), Organizations, OrganizationMemberships, service == null ? PersistenceService.LocalWorldId : service.WorldId, GetPrototypeSocialPersonIds(personId), GetPrototypeOrganizations());
                return worldOrganizationAuthority;
            }
        }
        public OrganizationResourceRuntime OrganizationResources
        {
            get
            {
                if (worldOrganizationResources == null)
                {
                    worldOrganizationResources = new OrganizationResourceRuntime();
                }

                worldOrganizationResources.Configure(GetDefinitionRegistry(), Organizations, OrganizationAuthority, Economy, service == null ? PersistenceService.LocalWorldId : service.WorldId, Properties, Businesses, ItemIdentities, ContractEconomy, Payroll);
                return worldOrganizationResources;
            }
        }
        public OrganizationDecisionRuntime OrganizationDecisions
        {
            get
            {
                if (worldOrganizationDecisions == null)
                {
                    worldOrganizationDecisions = new OrganizationDecisionRuntime();
                }

                string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
                worldOrganizationDecisions.Configure(GetDefinitionRegistry(), Organizations, OrganizationMemberships, OrganizationAuthority, OrganizationResources, service == null ? PersistenceService.LocalWorldId : service.WorldId, GetPrototypeSocialPersonIds(personId), Economy);
                return worldOrganizationDecisions;
            }
        }
        public ItemCompositionRuntime ItemCompositions => playerItemCompositions ??= new ItemCompositionRuntime();
        public ItemQualityAffixRuntime ItemQualityAffixes => playerItemQualityAffixes ??= new ItemQualityAffixRuntime();
        public ItemDurabilityRuntime ItemDurability => playerItemDurability ??= new ItemDurabilityRuntime();
        public ProductionRequirementRuntime ProductionRequirements => playerProductionRequirements ??= new ProductionRequirementRuntime();
        public RecipeKnowledgeRuntime RecipeKnowledge => playerRecipeKnowledge ??= new RecipeKnowledgeRuntime();
        public CraftingExecutionRuntime CraftingExecution => playerCraftingExecution ??= new CraftingExecutionRuntime();
        public ProductionWorkflowRuntime ProductionWorkflow => playerProductionWorkflow ??= new ProductionWorkflowRuntime();
        public ExperimentationRuntime Experimentation => playerExperimentation ??= new ExperimentationRuntime();
        public DefinitionRegistry ItemQualityDefinitionRegistry => GetDefinitionRegistry();
        public DefinitionRegistry ItemDurabilityDefinitionRegistry => GetDefinitionRegistry();

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (service != null && participant != null)
            {
                service.UnregisterParticipant(participant);
                participant = null;
            }

            if (service != null && inventoryEquipmentParticipant != null)
            {
                service.UnregisterParticipant(inventoryEquipmentParticipant);
                inventoryEquipmentParticipant = null;
            }

            if (service != null && itemIdentityParticipant != null)
            {
                service.UnregisterParticipant(itemIdentityParticipant);
                itemIdentityParticipant = null;
            }

            if (service != null && economyParticipant != null)
            {
                service.UnregisterParticipant(economyParticipant);
                economyParticipant = null;
            }

            if (service != null && marketParticipant != null)
            {
                service.UnregisterParticipant(marketParticipant);
                marketParticipant = null;
            }

            if (service != null && tradeParticipant != null)
            {
                service.UnregisterParticipant(tradeParticipant);
                tradeParticipant = null;
            }

            if (service != null && payrollParticipant != null)
            {
                service.UnregisterParticipant(payrollParticipant);
                payrollParticipant = null;
            }

            if (service != null && businessParticipant != null)
            {
                service.UnregisterParticipant(businessParticipant);
                businessParticipant = null;
            }

            if (service != null && propertyParticipant != null)
            {
                service.UnregisterParticipant(propertyParticipant);
                propertyParticipant = null;
            }

            if (service != null && contractEconomyParticipant != null)
            {
                service.UnregisterParticipant(contractEconomyParticipant);
                contractEconomyParticipant = null;
            }

            if (service != null && institutionalRevenueParticipant != null)
            {
                service.UnregisterParticipant(institutionalRevenueParticipant);
                institutionalRevenueParticipant = null;
            }

            if (service != null && regionalFlowParticipant != null)
            {
                service.UnregisterParticipant(regionalFlowParticipant);
                regionalFlowParticipant = null;
            }

            if (service != null && organizationParticipant != null)
            {
                service.UnregisterParticipant(organizationParticipant);
                organizationParticipant = null;
            }

            if (service != null && organizationMembershipParticipant != null)
            {
                service.UnregisterParticipant(organizationMembershipParticipant);
                organizationMembershipParticipant = null;
            }

            if (service != null && organizationAuthorityParticipant != null)
            {
                service.UnregisterParticipant(organizationAuthorityParticipant);
                organizationAuthorityParticipant = null;
            }

            if (service != null && organizationResourceParticipant != null)
            {
                service.UnregisterParticipant(organizationResourceParticipant);
                organizationResourceParticipant = null;
            }

            if (service != null && organizationDecisionParticipant != null)
            {
                service.UnregisterParticipant(organizationDecisionParticipant);
                organizationDecisionParticipant = null;
            }

            if (service != null && itemCompositionParticipant != null)
            {
                service.UnregisterParticipant(itemCompositionParticipant);
                itemCompositionParticipant = null;
            }

            if (service != null && itemQualityAffixParticipant != null)
            {
                service.UnregisterParticipant(itemQualityAffixParticipant);
                itemQualityAffixParticipant = null;
            }

            if (service != null && itemDurabilityParticipant != null)
            {
                service.UnregisterParticipant(itemDurabilityParticipant);
                itemDurabilityParticipant = null;
            }

            if (service != null && productionRequirementParticipant != null)
            {
                service.UnregisterParticipant(productionRequirementParticipant);
                productionRequirementParticipant = null;
            }

            if (service != null && recipeKnowledgeParticipant != null)
            {
                service.UnregisterParticipant(recipeKnowledgeParticipant);
                recipeKnowledgeParticipant = null;
            }

            if (service != null && craftingExecutionParticipant != null)
            {
                service.UnregisterParticipant(craftingExecutionParticipant);
                craftingExecutionParticipant = null;
            }

            if (service != null && productionWorkflowParticipant != null)
            {
                service.UnregisterParticipant(productionWorkflowParticipant);
                productionWorkflowParticipant = null;
            }

            if (service != null && experimentationParticipant != null)
            {
                service.UnregisterParticipant(experimentationParticipant);
                experimentationParticipant = null;
            }

            if (service != null && identityProgressionParticipant != null)
            {
                service.UnregisterParticipant(identityProgressionParticipant);
                identityProgressionParticipant = null;
            }

            if (service != null && playerAttributesParticipant != null)
            {
                service.UnregisterParticipant(playerAttributesParticipant);
                playerAttributesParticipant = null;
            }

            if (service != null && playerSkillsParticipant != null)
            {
                service.UnregisterParticipant(playerSkillsParticipant);
                playerSkillsParticipant = null;
            }

            if (service != null && playerTraitsParticipant != null)
            {
                service.UnregisterParticipant(playerTraitsParticipant);
                playerTraitsParticipant = null;
            }

            if (service != null && playerBodyParticipant != null)
            {
                service.UnregisterParticipant(playerBodyParticipant);
                playerBodyParticipant = null;
            }

            if (service != null && playerKnowledgeParticipant != null)
            {
                service.UnregisterParticipant(playerKnowledgeParticipant);
                playerKnowledgeParticipant = null;
            }

            if (service != null && playerProfessionParticipant != null)
            {
                service.UnregisterParticipant(playerProfessionParticipant);
                playerProfessionParticipant = null;
            }

            if (service != null && playerProfessionEntryParticipant != null)
            {
                service.UnregisterParticipant(playerProfessionEntryParticipant);
                playerProfessionEntryParticipant = null;
            }

            if (service != null && playerTrainingParticipant != null)
            {
                service.UnregisterParticipant(playerTrainingParticipant);
                playerTrainingParticipant = null;
            }

            if (service != null && playerProfessionalActivityParticipant != null)
            {
                service.UnregisterParticipant(playerProfessionalActivityParticipant);
                playerProfessionalActivityParticipant = null;
            }

            if (service != null && playerCredentialParticipant != null)
            {
                service.UnregisterParticipant(playerCredentialParticipant);
                playerCredentialParticipant = null;
            }

            if (service != null && playerProfessionalRankParticipant != null)
            {
                service.UnregisterParticipant(playerProfessionalRankParticipant);
                playerProfessionalRankParticipant = null;
            }

            if (service != null && playerPositionEmploymentParticipant != null)
            {
                service.UnregisterParticipant(playerPositionEmploymentParticipant);
                playerPositionEmploymentParticipant = null;
            }

            if (service != null && playerCareerHistoryParticipant != null)
            {
                service.UnregisterParticipant(playerCareerHistoryParticipant);
                playerCareerHistoryParticipant = null;
            }

            if (service != null && playerLifePathParticipant != null)
            {
                service.UnregisterParticipant(playerLifePathParticipant);
                playerLifePathParticipant = null;
            }

            if (service != null && playerRelationshipParticipant != null)
            {
                service.UnregisterParticipant(playerRelationshipParticipant);
                playerRelationshipParticipant = null;
            }

            if (service != null && playerInterpersonalAttitudeParticipant != null)
            {
                service.UnregisterParticipant(playerInterpersonalAttitudeParticipant);
                playerInterpersonalAttitudeParticipant = null;
            }

            if (service != null && worldReputationParticipant != null)
            {
                service.UnregisterParticipant(worldReputationParticipant);
                worldReputationParticipant = null;
            }

            if (service != null && worldRumorParticipant != null)
            {
                service.UnregisterParticipant(worldRumorParticipant);
                worldRumorParticipant = null;
            }

            if (service != null && worldSocialInteractionParticipant != null)
            {
                service.UnregisterParticipant(worldSocialInteractionParticipant);
                worldSocialInteractionParticipant = null;
            }

            if (service != null && worldSocialNormParticipant != null)
            {
                service.UnregisterParticipant(worldSocialNormParticipant);
                worldSocialNormParticipant = null;
            }

            if (service != null && worldSocialNetworkParticipant != null)
            {
                service.UnregisterParticipant(worldSocialNetworkParticipant);
                worldSocialNetworkParticipant = null;
            }

            if (service != null && worldSocialDecisionParticipant != null)
            {
                service.UnregisterParticipant(worldSocialDecisionParticipant);
                worldSocialDecisionParticipant = null;
            }

            if (service != null && worldSocialInfluenceParticipant != null)
            {
                service.UnregisterParticipant(worldSocialInfluenceParticipant);
                worldSocialInfluenceParticipant = null;
            }

            if (service != null && worldSocialEmotionParticipant != null)
            {
                service.UnregisterParticipant(worldSocialEmotionParticipant);
                worldSocialEmotionParticipant = null;
            }

            if (service != null && worldFamilyRelationshipParticipant != null)
            {
                service.UnregisterParticipant(worldFamilyRelationshipParticipant);
                worldFamilyRelationshipParticipant = null;
            }

            if (service != null && statsVitalsStatusParticipant != null)
            {
                service.UnregisterParticipant(statsVitalsStatusParticipant);
                statsVitalsStatusParticipant = null;
            }

            if (service != null && playerResourcesParticipant != null)
            {
                service.UnregisterParticipant(playerResourcesParticipant);
                playerResourcesParticipant = null;
            }

            if (service != null && playerActorLifecycleParticipant != null)
            {
                service.UnregisterParticipant(playerActorLifecycleParticipant);
                playerActorLifecycleParticipant = null;
            }

            if (service != null && playerCombatExecutionParticipant != null)
            {
                service.UnregisterParticipant(playerCombatExecutionParticipant);
                playerCombatExecutionParticipant = null;
            }

            if (service != null && playerOngoingEffectsParticipant != null)
            {
                service.UnregisterParticipant(playerOngoingEffectsParticipant);
                playerOngoingEffectsParticipant = null;
            }

            if (service != null && questContractParticipant != null)
            {
                service.UnregisterParticipant(questContractParticipant);
                questContractParticipant = null;
            }

            if (service != null && playerLocationParticipant != null)
            {
                service.UnregisterParticipant(playerLocationParticipant);
                playerLocationParticipant = null;
            }

            if (service != null && playerKnowledgeRecordParticipant != null)
            {
                service.UnregisterParticipant(playerKnowledgeRecordParticipant);
                playerKnowledgeRecordParticipant = null;
            }

            UnsubscribeDirtyEvents();
        }

        public void ConfigurePlayerPersistence(
            DefinitionCatalog catalog,
            PlayerInventory inventory,
            PlayerEquipment equipment,
            PlayerStats stats,
            PlayerHealth health,
            PlayerMana mana,
            PlayerStamina stamina,
            StatusEffectController statusController,
            PlayerIdentityProgression identityProgression,
            PlayerQuestLog questLog,
            PlayerContractJournal contractJournal)
        {
            if (catalog != null && definitionCatalog != catalog)
            {
                definitionCatalog = catalog;
                definitionRegistry = null;
            }

            playerInventory = inventory;
            playerEquipment = equipment;
            playerStats = stats;
            playerHealth = health;
            playerMana = mana;
            playerStamina = stamina;
            statusEffectController = statusController;
            playerIdentityProgression = identityProgression;
            playerQuestLog = questLog;
            playerContractJournal = contractJournal;
            playerRoot = inventory == null ? playerRoot : inventory.transform;
        }

        public void EnsureInitialized()
        {
            if (prototypeState == null)
            {
                prototypeState = GetComponent<PrototypePersistenceState>();
            }

            if (prototypeState == null)
            {
                prototypeState = gameObject.AddComponent<PrototypePersistenceState>();
            }

            EnsureRuntimeHelpers();
            service ??= new PersistenceService();
            service.PlaytimeSecondsProvider = () => playTimeTracker == null ? 0d : playTimeTracker.CumulativeSeconds;
            if (participant == null)
            {
                participant = new PrototypePersistenceStateParticipant(prototypeState);
                service.RegisterParticipant(participant, out string failureReason);
                if (!string.IsNullOrWhiteSpace(failureReason))
                {
                    Debug.LogWarning(failureReason);
                    participant = null;
                }
            }

            EnsurePlayerIdentityProgressionParticipant();
            EnsurePlayerAttributesParticipant();
            EnsurePlayerSkillsParticipant();
            EnsurePlayerTraitsParticipant();
            EnsurePlayerBodyParticipant();
            EnsurePlayerKnowledgeParticipant();
            EnsurePlayerProfessionParticipant();
            EnsurePlayerProfessionEntryParticipant();
            EnsurePlayerInformationSourceParticipant();
            EnsurePlayerInformationTransferParticipant();
            EnsurePlayerTrainingParticipant();
            EnsurePlayerProfessionalActivityParticipant();
            EnsurePlayerCredentialParticipant();
            EnsurePlayerProfessionalRankParticipant();
            EnsurePlayerPositionEmploymentParticipant();
            EnsurePlayerCareerHistoryParticipant();
            EnsurePlayerLifePathParticipant();
            EnsurePlayerRelationshipParticipant();
            EnsurePlayerInterpersonalAttitudeParticipant();
            EnsureWorldReputationParticipant();
            EnsureWorldRumorParticipant();
            EnsureWorldSocialInteractionParticipant();
            EnsureWorldSocialNormParticipant();
            EnsureWorldSocialNetworkParticipant();
            EnsureWorldSocialInfluenceParticipant();
            EnsureWorldSocialEmotionParticipant();
            EnsureWorldFamilyRelationshipParticipant();
            EnsureWorldSocialDecisionParticipant();
            EnsurePlayerInformationAccessParticipant();
            EnsurePlayerKnowledgeRecordParticipant();
            EnsurePlayerItemIdentityParticipant();
            EnsureWorldEconomyParticipant();
            EnsureWorldMarketParticipant();
            EnsureWorldTradeParticipant();
            EnsureWorldPayrollParticipant();
            EnsureWorldBusinessParticipant();
            EnsureWorldPropertyParticipant();
            EnsureWorldContractEconomyParticipant();
            EnsureWorldInstitutionalRevenueParticipant();
            EnsureWorldRegionalFlowParticipant();
            EnsureWorldOrganizationParticipant();
            EnsureWorldOrganizationMembershipParticipant();
            EnsureWorldOrganizationAuthorityParticipant();
            EnsureWorldOrganizationResourceParticipant();
            EnsureWorldOrganizationDecisionParticipant();
            EnsurePlayerItemCompositionParticipant();
            EnsurePlayerItemQualityAffixParticipant();
            EnsurePlayerItemDurabilityParticipant();
            EnsurePlayerProductionRequirementParticipant();
            EnsurePlayerRecipeKnowledgeParticipant();
            EnsurePlayerCraftingExecutionParticipant();
            EnsurePlayerProductionWorkflowParticipant();
            EnsurePlayerExperimentationParticipant();
            EnsurePlayerInventoryEquipmentParticipant();
            EnsurePlayerStatsVitalsStatusParticipant();
            EnsurePlayerResourcesParticipant();
            EnsurePlayerActorLifecycleParticipant();
            EnsurePlayerCombatExecutionParticipant();
            EnsurePlayerOngoingEffectsParticipant();
            EnsurePlayerQuestContractParticipant();
            EnsurePlayerLocationParticipant();
            SubscribeDirtyEvents();
        }

        public PersistenceSaveResult SavePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.Save(PrototypeSlotId, "Prototype Slot");
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceLoadResult LoadPrototypeSlot(bool expectedFailureAsInfo = false)
        {
            EnsureInitialized();
            PersistenceLoadResult result = service.Load(PrototypeSlotId);
            Report(result.Succeeded, result.Message, expectedFailureAsInfo);
            return result;
        }

        public PersistenceLoadResult LoadPrototypeBackup()
        {
            EnsureInitialized();
            PersistenceLoadResult result = service.Load(PrototypeSlotId, loadBackup: true);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceValidationResult ValidatePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceValidationResult result = service.ValidateSlot(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult DeletePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.DeleteSlot(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public IReadOnlyList<SaveSlotMetadata> ListSaveSlots()
        {
            EnsureInitialized();
            return service.ListSaveSlots();
        }

        public IReadOnlyList<SaveSlotDescriptor> BuildSaveSlotDescriptors()
        {
            EnsureInitialized();
            return PrototypeSaveSlotCatalog.BuildDescriptors(service, ManualSlotCount, AutosaveSlotCount);
        }

        public SaveEligibilityResult CheckSaveEligibility(bool showDetailedPlayerMessage)
        {
            EnsureInitialized();
            if (service.OperationInProgress)
            {
                return SaveEligibilityResult.Block(SaveEligibilityStatus.OperationInProgress, "A persistence operation is already running.");
            }

            ResolvePlayerPersistenceReferences();
            if (playerRoot == null)
            {
                return SaveEligibilityResult.Block(SaveEligibilityStatus.NoActivePlayer, "No active player root is available.");
            }

            if (playerHealth != null && playerHealth.IsDefeated)
            {
                return SaveEligibilityResult.Block(SaveEligibilityStatus.InvalidPlayerState, "Cannot save while the player is defeated.");
            }

            return SaveEligibilityResult.Allow(showDetailedPlayerMessage ? "Saving is available." : "Allowed");
        }

        public PersistenceSaveResult SaveManualSlot(int zeroBasedIndex)
        {
            string slotId = PrototypeSaveSlotCatalog.ManualSlotId(zeroBasedIndex);
            return SaveNamedSlot(slotId, PrototypeSaveSlotCatalog.ManualDisplayName(zeroBasedIndex), markClean: true);
        }

        public PersistenceSaveResult SaveNamedSlot(string slotId, string displayName, bool markClean)
        {
            EnsureInitialized();
            SaveEligibilityResult eligibility = CheckSaveEligibility(showDetailedPlayerMessage: true);
            if (!eligibility.Allowed)
            {
                return PersistenceSaveResult.Failure(PersistenceSaveStatus.ParticipantCaptureFailed, slotId, string.Empty, eligibility.Message);
            }

            PersistenceSaveResult result = service.Save(slotId, displayName);
            Report(result.Succeeded, result.Message);
            if (result.Succeeded && markClean)
            {
                dirtyTracker?.MarkClean($"Saved {displayName}.");
                autosaveCoordinator?.ResetTimer();
            }

            return result;
        }

        public PersistenceSaveResult SaveAutosave(string reason)
        {
            EnsureInitialized();
            string staging = PrototypeSaveSlotCatalog.AutosaveStagingSlotId;
            PersistenceSaveResult saveResult = SaveNamedSlot(staging, $"Autosave ({reason})", markClean: false);
            if (!saveResult.Succeeded)
            {
                return saveResult;
            }

            PersistenceSaveResult rotate = service.RotateAutosaveSlots(staging, PrototypeSaveSlotCatalog.BuildAutosaveSlotIds(AutosaveSlotCount));
            Report(rotate.Succeeded, rotate.Message);
            if (rotate.Succeeded)
            {
                dirtyTracker?.MarkClean($"Autosaved: {reason}.");
            }

            return rotate;
        }

        public PersistenceSaveResult ForceAutosave(string reason = "DevelopmentCommand")
        {
            EnsureInitialized();
            return autosaveCoordinator == null ? SaveAutosave(reason) : autosaveCoordinator.ForceAutosave(reason);
        }

        public PersistenceLoadResult LoadSaveSlot(string slotId, bool loadBackup = false)
        {
            EnsureInitialized();
            PersistenceValidationResult preValidation = service.ValidateSlot(slotId, loadBackup);
            PersistenceLoadResult result = service.Load(slotId, loadBackup);
            Report(result.Succeeded, result.Message);
            if (result.Succeeded)
            {
                playTimeTracker?.Restore(preValidation.Envelope == null ? 0d : preValidation.Envelope.playtimeSeconds);
                dirtyTracker?.MarkClean(loadBackup ? "Loaded backup save." : "Loaded save.");
                autosaveCoordinator?.ResetTimer();
            }

            return result;
        }

        public PersistenceValidationResult ValidateSaveSlot(string slotId, bool validateBackup = false)
        {
            EnsureInitialized();
            PersistenceValidationResult result = service.ValidateSlot(slotId, validateBackup);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult DeleteSaveSlot(string slotId)
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.DeleteSlot(slotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public void SetAutosaveIntervalForTesting(float seconds)
        {
            autosaveIntervalSeconds = Mathf.Max(5f, seconds);
            autosaveCoordinator?.SetIntervalForTesting(autosaveIntervalSeconds);
        }

        public string BuildSaveSlotDiagnosticSummary()
        {
            EnsureInitialized();
            PersistenceTransactionDiagnostics diagnostics = service.BuildTransactionDiagnostics();
            return $"Operation={service.OperationState} Phase={diagnostics.phase} Safety={diagnostics.runtimeSafety} Dirty={dirtyTracker != null && dirtyTracker.IsDirty} PlayTime={PrototypeSaveSlotCatalog.FormatPlayTime(playTimeTracker == null ? 0d : playTimeTracker.CumulativeSeconds)} Autosave={autosaveCoordinator?.LastResult ?? "None"}";
        }

        public string BuildPersistenceIntegrationDiagnosticSummary()
        {
            EnsureInitialized();
            PersistenceDependencyReport dependencies = service.BuildParticipantDependencyReport();
            PersistenceTransactionDiagnostics diagnostics = service.BuildTransactionDiagnostics();
            string order = dependencies.orderedParticipantKeys == null || dependencies.orderedParticipantKeys.Length == 0
                ? "None"
                : string.Join(" -> ", dependencies.orderedParticipantKeys);
            return string.Join("\n", new[]
            {
                "Persistence Integration",
                $"Transaction: {diagnostics.transactionId}",
                $"Phase: {diagnostics.phase}",
                $"Operation: {diagnostics.operationState}",
                $"Safety: {diagnostics.runtimeSafety}",
                $"Guard Active: {PersistenceRestorationGuard.IsActive}",
                $"Participant Dependencies: {(dependencies.succeeded ? "Valid" : "Invalid")}",
                $"Participant Order: {order}",
                $"Dependency Detail: {dependencies.message}",
                $"Fingerprint: {BuildRuntimeStateFingerprint()}",
                $"Last Audit: {diagnostics.lastConsistencyAudit}",
                $"Last Recovery: {diagnostics.lastRecoveryRecommendation}"
            });
        }

        public string BuildRuntimeStateFingerprint()
        {
            EnsureInitialized();
            return service.BuildRuntimeStateFingerprint();
        }

        public SaveRecoveryScanReport RunRecoveryScan()
        {
            EnsureInitialized();
            SaveRecoveryScanReport report = service.ScanRecoverySources();
            Report(true, report.recommendation);
            return report;
        }

        public PersistenceSaveResult PromoteBackup(string slotId)
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.PromoteBackup(slotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceSaveResult QuarantinePrimary(string slotId)
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.QuarantinePrimary(slotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult CleanupStaleTemporaryFiles()
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.CleanupStaleTemporaryFiles();
            Report(result.Succeeded, result.Message);
            return result;
        }

        public void InjectNextPersistenceFault(PersistenceFaultInjectionPoint point)
        {
            EnsureInitialized();
            service.FaultInjection.nextFailurePoint = point;
            service.FaultInjection.message = $"Injected {point} fault.";
            Report(true, $"Next persistence fault: {point}");
        }

        private static void Report(bool succeeded, string message, bool failureAsInfo = false)
        {
            if (succeeded || failureAsInfo)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }

            PrototypeHudMessageBus.Show(message);
        }

        private void EnsurePlayerInventoryEquipmentParticipant()
        {
            if (!registerPlayerInventoryEquipment || inventoryEquipmentParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerInventory == null || playerEquipment == null)
            {
                Debug.LogWarning("Player inventory/equipment persistence participant was not registered because the prototype player inventory or equipment component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player inventory/equipment persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            inventoryEquipmentParticipant = new PlayerInventoryEquipmentPersistenceParticipant(
                playerInventory,
                playerEquipment,
                GetDefinitionRegistry,
                service.PlayerId,
                registerPlayerItemIdentities ? ItemIdentities : null,
                "prototype.player.inventory-equipment");

            service.RegisterParticipant(inventoryEquipmentParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                inventoryEquipmentParticipant = null;
            }
        }

        private void EnsurePlayerItemIdentityParticipant()
        {
            if (!registerPlayerItemIdentities || itemIdentityParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player item identity persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            EnsurePlayerItemIdentitySynchronizer();
            itemIdentityParticipant = new ItemInstanceIdentityPersistenceParticipant(
                ItemIdentities,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(itemIdentityParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                itemIdentityParticipant = null;
            }
        }

        private void EnsureWorldEconomyParticipant()
        {
            if (!registerWorldEconomy || economyParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World economy persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            economyParticipant = new EconomyPersistenceParticipant(
                Economy,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(economyParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                economyParticipant = null;
            }
        }

        private void EnsureWorldMarketParticipant()
        {
            if (!registerWorldMarkets || marketParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World market persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            marketParticipant = new MarketPersistenceParticipant(
                Markets,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(marketParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                marketParticipant = null;
            }
        }

        private void EnsureWorldTradeParticipant()
        {
            if (!registerWorldTrades || tradeParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World trade persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            tradeParticipant = new TradePersistenceParticipant(
                Trades,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(tradeParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                tradeParticipant = null;
            }
        }

        private void EnsureWorldPayrollParticipant()
        {
            if (!registerWorldPayroll || payrollParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World payroll persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            payrollParticipant = new PayrollPersistenceParticipant(
                Payroll,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(payrollParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                payrollParticipant = null;
            }
        }

        private void EnsureWorldBusinessParticipant()
        {
            if (!registerWorldBusinesses || businessParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World business persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            businessParticipant = new BusinessPersistenceParticipant(
                Businesses,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(businessParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                businessParticipant = null;
            }
        }

        private void EnsureWorldPropertyParticipant()
        {
            if (!registerWorldProperties || propertyParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World property persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            propertyParticipant = new PropertyPersistenceParticipant(
                Properties,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(propertyParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                propertyParticipant = null;
            }
        }

        private void EnsureWorldContractEconomyParticipant()
        {
            if (!registerWorldContracts || contractEconomyParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World contract economy persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            contractEconomyParticipant = new ContractEconomyPersistenceParticipant(
                ContractEconomy,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(contractEconomyParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                contractEconomyParticipant = null;
            }
        }

        private void EnsureWorldInstitutionalRevenueParticipant()
        {
            if (!registerWorldInstitutionalRevenue || institutionalRevenueParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World institutional revenue persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            institutionalRevenueParticipant = new InstitutionalRevenuePersistenceParticipant(
                InstitutionalRevenue,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(institutionalRevenueParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                institutionalRevenueParticipant = null;
            }
        }

        private void EnsureWorldRegionalFlowParticipant()
        {
            if (!registerWorldRegionalFlow || regionalFlowParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World regional flow persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            regionalFlowParticipant = new RegionalFlowPersistenceParticipant(
                RegionalFlow,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(regionalFlowParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                regionalFlowParticipant = null;
            }
        }

        private void EnsureWorldOrganizationParticipant()
        {
            if (!registerWorldOrganizations || organizationParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World organization persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            organizationParticipant = new OrganizationPersistenceParticipant(
                Organizations,
                GetDefinitionRegistry,
                service.WorldId,
                () => GetPrototypeSocialPersonIds(service.PlayerId),
                () => Array.Empty<string>());

            service.RegisterParticipant(organizationParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                organizationParticipant = null;
            }
        }

        private void EnsureWorldOrganizationMembershipParticipant()
        {
            if (!registerWorldOrganizationMemberships || organizationMembershipParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World organization membership persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            organizationMembershipParticipant = new OrganizationMembershipPersistenceParticipant(
                OrganizationMemberships,
                GetDefinitionRegistry,
                () => Organizations,
                service.WorldId,
                () => GetPrototypeSocialPersonIds(service.PlayerId),
                GetPrototypeOrganizations);

            service.RegisterParticipant(organizationMembershipParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                organizationMembershipParticipant = null;
            }
        }

        private void EnsureWorldOrganizationAuthorityParticipant()
        {
            if (!registerWorldOrganizationAuthority || organizationAuthorityParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World organization authority persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            organizationAuthorityParticipant = new OrganizationAuthorityPersistenceParticipant(
                OrganizationAuthority,
                GetDefinitionRegistry,
                () => Organizations,
                () => OrganizationMemberships,
                service.WorldId,
                () => GetPrototypeSocialPersonIds(service.PlayerId),
                GetPrototypeOrganizations);

            service.RegisterParticipant(organizationAuthorityParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                organizationAuthorityParticipant = null;
            }
        }

        private void EnsureWorldOrganizationResourceParticipant()
        {
            if (!registerWorldOrganizationResources || organizationResourceParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World organization resource persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            organizationResourceParticipant = new OrganizationResourcePersistenceParticipant(
                OrganizationResources,
                GetDefinitionRegistry,
                () => Organizations,
                () => OrganizationAuthority,
                () => Economy,
                service.WorldId,
                () => Properties,
                () => Businesses,
                () => ItemIdentities,
                () => ContractEconomy,
                () => Payroll);

            service.RegisterParticipant(organizationResourceParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                organizationResourceParticipant = null;
            }
        }

        private void EnsureWorldOrganizationDecisionParticipant()
        {
            if (!registerWorldOrganizationDecisions || organizationDecisionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("World organization decision persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = service == null ? PersistenceService.LocalPlayerId : service.PlayerId;
            organizationDecisionParticipant = new OrganizationDecisionPersistenceParticipant(
                OrganizationDecisions,
                GetDefinitionRegistry,
                () => Organizations,
                () => OrganizationMemberships,
                () => OrganizationAuthority,
                () => OrganizationResources,
                service.WorldId,
                () => GetPrototypeSocialPersonIds(personId));

            service.RegisterParticipant(organizationDecisionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                organizationDecisionParticipant = null;
            }
        }

        private void EnsurePlayerItemCompositionParticipant()
        {
            if (!registerPlayerItemCompositions || itemCompositionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (!registerPlayerItemIdentities)
            {
                Debug.LogWarning("Player item composition persistence participant was not registered because item identity persistence is disabled.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player item composition persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            itemCompositionParticipant = new ItemCompositionPersistenceParticipant(
                ItemCompositions,
                ItemIdentities,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(itemCompositionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                itemCompositionParticipant = null;
            }
        }

        private void EnsurePlayerItemQualityAffixParticipant()
        {
            if (!registerPlayerItemQualityAffixes || itemQualityAffixParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (!registerPlayerItemIdentities)
            {
                Debug.LogWarning("Player item quality persistence participant was not registered because item identity persistence is disabled.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player item quality persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            itemQualityAffixParticipant = new ItemQualityAffixPersistenceParticipant(
                ItemQualityAffixes,
                ItemIdentities,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(itemQualityAffixParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                itemQualityAffixParticipant = null;
            }
        }

        private void EnsurePlayerItemDurabilityParticipant()
        {
            if (!registerPlayerItemDurability || itemDurabilityParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (!registerPlayerItemIdentities)
            {
                Debug.LogWarning("Player item durability persistence participant was not registered because item identity persistence is disabled.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player item durability persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            itemDurabilityParticipant = new ItemDurabilityPersistenceParticipant(
                ItemDurability,
                ItemIdentities,
                registerPlayerItemCompositions ? ItemCompositions : null,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(itemDurabilityParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                itemDurabilityParticipant = null;
            }
        }

        private void EnsurePlayerRecipeKnowledgeParticipant()
        {
            if (!registerPlayerRecipeKnowledge || recipeKnowledgeParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player recipe knowledge persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            recipeKnowledgeParticipant = new RecipeKnowledgePersistenceParticipant(
                RecipeKnowledge,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(recipeKnowledgeParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                recipeKnowledgeParticipant = null;
            }
        }

        private void EnsurePlayerProductionRequirementParticipant()
        {
            if (!registerPlayerProductionRequirements || productionRequirementParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player production requirement persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            productionRequirementParticipant = new ProductionRequirementPersistenceParticipant(
                ProductionRequirements,
                service.WorldId);

            service.RegisterParticipant(productionRequirementParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                productionRequirementParticipant = null;
            }
        }

        private void EnsurePlayerCraftingExecutionParticipant()
        {
            if (!registerPlayerCraftingExecution || craftingExecutionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (!registerPlayerItemIdentities || !registerPlayerRecipeKnowledge)
            {
                Debug.LogWarning("Player crafting execution persistence participant was not registered because item identity or recipe persistence is disabled.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player crafting execution persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            craftingExecutionParticipant = new CraftingExecutionPersistenceParticipant(
                CraftingExecution,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(craftingExecutionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                craftingExecutionParticipant = null;
            }
        }

        private void EnsurePlayerProductionWorkflowParticipant()
        {
            if (!registerPlayerProductionWorkflow || productionWorkflowParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (!registerPlayerItemIdentities || !registerPlayerProductionRequirements || !registerPlayerCraftingExecution)
            {
                Debug.LogWarning("Player production workflow persistence participant was not registered because item identity, production requirement, or crafting execution persistence is disabled.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player production workflow persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            productionWorkflowParticipant = new ProductionWorkflowPersistenceParticipant(
                ProductionWorkflow,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(productionWorkflowParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                productionWorkflowParticipant = null;
            }
        }

        private void EnsurePlayerExperimentationParticipant()
        {
            if (!registerPlayerExperimentation || experimentationParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (!registerPlayerItemIdentities || !registerPlayerProductionRequirements || !registerPlayerCraftingExecution)
            {
                Debug.LogWarning("Player experimentation persistence participant was not registered because item identity, production requirement, or crafting execution persistence is disabled.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player experimentation persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            experimentationParticipant = new ExperimentationPersistenceParticipant(
                Experimentation,
                GetDefinitionRegistry,
                service.WorldId);

            service.RegisterParticipant(experimentationParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                experimentationParticipant = null;
            }
        }

        private void EnsurePlayerItemIdentitySynchronizer()
        {
            ResolvePlayerPersistenceReferences();
            if (playerInventory == null || playerEquipment == null)
            {
                return;
            }

            playerItemIdentitySynchronizer = playerInventory.GetComponent<PlayerItemIdentitySynchronizer>();
            if (playerItemIdentitySynchronizer == null)
            {
                playerItemIdentitySynchronizer = playerInventory.gameObject.AddComponent<PlayerItemIdentitySynchronizer>();
            }

            playerItemIdentitySynchronizer.Configure(
                playerInventory,
                playerEquipment,
                ItemIdentities,
                GetDefinitionRegistry,
                service.PlayerId,
                "prototype.player.inventory-equipment");
            ItemIdentityInventoryBridgeResult synchronization = playerItemIdentitySynchronizer.SynchronizeNow();
            if (!synchronization.Succeeded)
            {
                Debug.LogWarning($"Player item identity synchronization failed: {synchronization.Message}");
            }
        }

        private void EnsurePlayerIdentityProgressionParticipant()
        {
            if (!registerPlayerIdentityProgression || identityProgressionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Player identity/progression persistence participant was not registered because the prototype player identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player identity/progression persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            DefinitionRegistry registry = GetDefinitionRegistry();
            playerIdentityProgression.ConfigureIdentity(service.AccountId, service.PlayerId);
            playerIdentityProgression.RegisterDefinitionCache(registry);

            identityProgressionParticipant = new PlayerIdentityProgressionPersistenceParticipant(
                playerIdentityProgression,
                GetDefinitionRegistry,
                service.PlayerId,
                service.AccountId);

            service.RegisterParticipant(identityProgressionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                identityProgressionParticipant = null;
            }
        }

        private void EnsureRuntimeHelpers()
        {
            if (playTimeTracker == null)
            {
                playTimeTracker = GetComponent<PlayTimeTracker>();
                if (playTimeTracker == null)
                {
                    playTimeTracker = gameObject.AddComponent<PlayTimeTracker>();
                }
            }

            if (dirtyTracker == null)
            {
                dirtyTracker = GetComponent<GameSaveDirtyTracker>();
                if (dirtyTracker == null)
                {
                    dirtyTracker = gameObject.AddComponent<GameSaveDirtyTracker>();
                }
            }

            if (autosaveCoordinator == null)
            {
                autosaveCoordinator = GetComponent<AutosaveCoordinator>();
                if (autosaveCoordinator == null)
                {
                    autosaveCoordinator = gameObject.AddComponent<AutosaveCoordinator>();
                }
            }

            autosaveCoordinator.Configure(this, autosaveIntervalSeconds);
        }

        private void EnsurePlayerAttributesParticipant()
        {
            if (!registerPlayerAttributes || playerAttributesParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerAttributes == null || playerIdentityProgression == null)
            {
                Debug.LogWarning("Player attributes persistence participant was not registered because the prototype player attributes or identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player attributes persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerAttributes.Configure(GetDefinitionRegistry());
            playerAttributesParticipant = new PlayerAttributesPersistenceParticipant(
                playerAttributes,
                playerIdentityProgression,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerAttributesParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerAttributesParticipant = null;
            }
        }

        private void EnsurePlayerSkillsParticipant()
        {
            if (!registerPlayerSkills || playerSkillsParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerSkills == null || playerIdentityProgression == null)
            {
                Debug.LogWarning("Player Skills persistence participant was not registered because the prototype player Skill collection or identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player Skills persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerSkills.Configure(GetDefinitionRegistry(), playerCalculatedStats, playerRoot == null ? null : playerRoot.GetComponent<PlayerSpellLoadout>());
            playerSkillsParticipant = new PlayerSkillsPersistenceParticipant(
                playerSkills,
                playerIdentityProgression,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerSkillsParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerSkillsParticipant = null;
            }
        }

        private void EnsurePlayerTraitsParticipant()
        {
            if (!registerPlayerTraits || playerTraitsParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerTraits == null || playerCalculatedStats == null)
            {
                Debug.LogWarning("Player Traits persistence participant was not registered because the prototype player Trait collection or calculated stats are missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player Traits persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerTraits.Configure(GetDefinitionRegistry(), playerCalculatedStats, playerSkills, service.PlayerId);
            playerTraitsParticipant = new PlayerTraitsPersistenceParticipant(
                playerTraits,
                playerIdentityProgression,
                playerCalculatedStats,
                playerSkills,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerTraitsParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerTraitsParticipant = null;
            }
        }

        private void EnsurePlayerBodyParticipant()
        {
            if (!registerPlayerBody || playerBodyParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerBody == null || playerTraits == null || playerCalculatedStats == null)
            {
                Debug.LogWarning("Player body persistence participant was not registered because the body runtime, Trait collection, or calculated stats are missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player body persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerBody.Configure(GetDefinitionRegistry(), ResolvePlayerActorId(), playerIdentityProgression == null ? string.Empty : playerIdentityProgression.PersonId, playerTraits, playerCalculatedStats);
            if (!playerBody.IsReady && !string.IsNullOrWhiteSpace(defaultPlayerSpeciesId))
            {
                BodyOperationResult defaultAssignment = playerBody.AssignSpecies(defaultPlayerSpeciesId, restoring: false, "Prototype player default Species");
                if (!defaultAssignment.Succeeded)
                {
                    Debug.LogWarning(defaultAssignment.Message);
                    return;
                }
            }

            playerBodyParticipant = new PlayerBodyPersistenceParticipant(
                playerBody,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerBodyParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerBodyParticipant = null;
            }
        }

        private void EnsurePlayerKnowledgeParticipant()
        {
            if (!registerPlayerKnowledge || playerKnowledgeParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Person Knowledge persistence participant was not registered because the prototype Person identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Person Knowledge persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            GameObject owner = playerBody == null ? playerIdentityProgression.gameObject : playerBody.gameObject;
            if (playerKnowledge == null)
            {
                playerKnowledge = owner.GetComponent<PersonKnowledgeRuntime>();
            }

            if (playerKnowledge == null)
            {
                playerKnowledge = owner.AddComponent<PersonKnowledgeRuntime>();
            }

            playerKnowledge.Configure(
                GetDefinitionRegistry(),
                playerIdentityProgression.PersonId,
                ResolvePlayerActorId(),
                playerBody == null ? string.Empty : playerBody.ActorBodyId);

            playerKnowledgeParticipant = new PersonKnowledgePersistenceParticipant(
                playerKnowledge,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerKnowledgeParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerKnowledgeParticipant = null;
            }
        }

        private void EnsurePlayerProfessionParticipant()
        {
            if (!registerPlayerProfessions || playerProfessionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Person Profession persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            Professions.Configure(GetDefinitionRegistry(), new[] { personId, service.PlayerId });
            playerProfessionParticipant = new PersonProfessionPersistenceParticipant(
                Professions,
                GetDefinitionRegistry,
                () => new[] { personId, service.PlayerId },
                service.PlayerId);

            service.RegisterParticipant(playerProfessionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerProfessionParticipant = null;
            }
        }

        private void EnsurePlayerProfessionEntryParticipant()
        {
            if (!registerPlayerProfessionEntries || playerProfessionEntryParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Profession Entry persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            Professions.Configure(GetDefinitionRegistry(), new[] { personId, service.PlayerId });
            ProfessionEntries.Configure(GetDefinitionRegistry(), Professions, new[] { personId, service.PlayerId });
            playerProfessionEntryParticipant = new ProfessionEntryPersistenceParticipant(
                ProfessionEntries,
                GetDefinitionRegistry,
                () => Professions,
                () => new[] { personId, service.PlayerId },
                service.PlayerId);

            service.RegisterParticipant(playerProfessionEntryParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerProfessionEntryParticipant = null;
            }
        }

        private void EnsurePlayerInformationSourceParticipant()
        {
            if (!registerPlayerInformationSources || playerInformationSourceParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Information Source persistence participant was not registered because the prototype Person identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Information Source persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            InformationSources.Configure(GetDefinitionRegistry(), playerIdentityProgression.PersonId);
            playerInformationSourceParticipant = new InformationSourcePersistenceParticipant(
                InformationSources,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerInformationSourceParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerInformationSourceParticipant = null;
            }
        }

        private void EnsurePlayerInformationTransferParticipant()
        {
            if (!registerPlayerInformationTransfers || playerInformationTransferParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Information Transfer persistence participant was not registered because the prototype Person identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Information Transfer persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            InformationTransfers.Configure(GetDefinitionRegistry(), playerIdentityProgression.PersonId);
            playerInformationTransferParticipant = new InformationTransferPersistenceParticipant(
                InformationTransfers,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerInformationTransferParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerInformationTransferParticipant = null;
            }
        }

        private void EnsurePlayerTrainingParticipant()
        {
            if (!registerPlayerTraining || playerTrainingParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Training persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            ProfessionEntries.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            InformationTransfers.Configure(GetDefinitionRegistry(), personId);
            Training.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, knownPersons);
            playerTrainingParticipant = new TrainingPersistenceParticipant(
                Training,
                GetDefinitionRegistry,
                () => Professions,
                () => InformationTransfers,
                () => knownPersons,
                service.PlayerId);

            service.RegisterParticipant(playerTrainingParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerTrainingParticipant = null;
            }
        }

        private void EnsurePlayerProfessionalActivityParticipant()
        {
            if (!registerPlayerProfessionalActivities || playerProfessionalActivityParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Professional Activity persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            ProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            playerProfessionalActivityParticipant = new ProfessionalActivityPersistenceParticipant(
                ProfessionalActivities,
                GetDefinitionRegistry,
                () => Professions,
                () => knownPersons,
                service.PlayerId);

            service.RegisterParticipant(playerProfessionalActivityParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerProfessionalActivityParticipant = null;
            }
        }

        private void EnsurePlayerCredentialParticipant()
        {
            if (!registerPlayerCredentials || playerCredentialParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Credential persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            string[] authorities = GetPrototypeCredentialAuthorities();
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            Training.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, knownPersons);
            ProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            Credentials.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, knownPersons, authorities);
            playerCredentialParticipant = new CredentialPersistenceParticipant(
                Credentials,
                GetDefinitionRegistry,
                () => Professions,
                () => Training,
                () => ProfessionalActivities,
                () => knownPersons,
                () => authorities,
                service.PlayerId);

            service.RegisterParticipant(playerCredentialParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerCredentialParticipant = null;
            }
        }

        private void EnsurePlayerProfessionalRankParticipant()
        {
            if (!registerPlayerProfessionalRanks || playerProfessionalRankParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Professional rank persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            string[] authorities = GetPrototypeCredentialAuthorities();
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            Training.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, knownPersons);
            ProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            Credentials.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, knownPersons, authorities);
            ProfessionalRanks.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, knownPersons, authorities);
            playerProfessionalRankParticipant = new ProfessionalRankPersistenceParticipant(
                ProfessionalRanks,
                GetDefinitionRegistry,
                () => Professions,
                () => Training,
                () => ProfessionalActivities,
                () => Credentials,
                () => knownPersons,
                () => authorities,
                service.PlayerId);

            service.RegisterParticipant(playerProfessionalRankParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerProfessionalRankParticipant = null;
            }
        }

        private void EnsurePlayerPositionEmploymentParticipant()
        {
            if (!registerPlayerPositionEmployment || playerPositionEmploymentParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Position employment persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            string[] authorities = GetPrototypeCredentialAuthorities();
            string[] organizations = GetPrototypeOrganizations();
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            Training.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, knownPersons);
            ProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            Credentials.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, knownPersons, authorities);
            ProfessionalRanks.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, knownPersons, authorities);
            PositionEmployment.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, knownPersons, organizations, authorities);
            playerPositionEmploymentParticipant = new PositionEmploymentPersistenceParticipant(
                PositionEmployment,
                GetDefinitionRegistry,
                () => Professions,
                () => Training,
                () => ProfessionalActivities,
                () => Credentials,
                () => ProfessionalRanks,
                () => knownPersons,
                () => organizations,
                () => authorities,
                service.PlayerId);

            service.RegisterParticipant(playerPositionEmploymentParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerPositionEmploymentParticipant = null;
            }
        }

        private void EnsurePlayerCareerHistoryParticipant()
        {
            if (!registerPlayerCareerHistory || playerCareerHistoryParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Career history persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            string[] authorities = GetPrototypeCredentialAuthorities();
            string[] organizations = GetPrototypeOrganizations();
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            Training.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, knownPersons);
            ProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            Credentials.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, knownPersons, authorities);
            ProfessionalRanks.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, knownPersons, authorities);
            PositionEmployment.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, knownPersons, organizations, authorities);
            CareerHistory.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, PositionEmployment, knownPersons, organizations, authorities);
            playerCareerHistoryParticipant = new CareerHistoryPersistenceParticipant(
                CareerHistory,
                GetDefinitionRegistry,
                () => Professions,
                () => Training,
                () => ProfessionalActivities,
                () => Credentials,
                () => ProfessionalRanks,
                () => PositionEmployment,
                () => knownPersons,
                () => organizations,
                () => authorities,
                service.PlayerId);

            service.RegisterParticipant(playerCareerHistoryParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerCareerHistoryParticipant = null;
            }
        }

        private void EnsurePlayerLifePathParticipant()
        {
            if (!registerPlayerLifePaths || playerLifePathParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Life-path persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = new[] { personId, service.PlayerId };
            string[] authorities = GetPrototypeCredentialAuthorities();
            string[] organizations = GetPrototypeOrganizations();
            Professions.Configure(GetDefinitionRegistry(), knownPersons);
            Training.Configure(GetDefinitionRegistry(), Professions, InformationTransfers, knownPersons);
            ProfessionalActivities.Configure(GetDefinitionRegistry(), Professions, knownPersons);
            Credentials.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, knownPersons, authorities);
            ProfessionalRanks.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, knownPersons, authorities);
            PositionEmployment.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, knownPersons, organizations, authorities);
            CareerHistory.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, PositionEmployment, knownPersons, organizations, authorities);
            LifePaths.Configure(GetDefinitionRegistry(), Professions, Training, ProfessionalActivities, Credentials, ProfessionalRanks, PositionEmployment, CareerHistory, knownPersons, organizations);
            playerLifePathParticipant = new LifePathPersistenceParticipant(
                LifePaths,
                GetDefinitionRegistry,
                () => Professions,
                () => Training,
                () => ProfessionalActivities,
                () => Credentials,
                () => ProfessionalRanks,
                () => PositionEmployment,
                () => CareerHistory,
                () => knownPersons,
                () => organizations,
                service.PlayerId);

            service.RegisterParticipant(playerLifePathParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerLifePathParticipant = null;
            }
        }

        private void EnsurePlayerRelationshipParticipant()
        {
            if (!registerPlayerRelationships || playerRelationshipParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Relationship persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            Relationships.Configure(GetDefinitionRegistry(), knownPersons);
            playerRelationshipParticipant = new RelationshipPersistenceParticipant(
                Relationships,
                GetDefinitionRegistry,
                () => knownPersons,
                service.PlayerId);

            service.RegisterParticipant(playerRelationshipParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerRelationshipParticipant = null;
            }
        }

        private void EnsurePlayerInterpersonalAttitudeParticipant()
        {
            if (!registerPlayerInterpersonalAttitudes || playerInterpersonalAttitudeParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Interpersonal attitude persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            InterpersonalAttitudes.Configure(GetDefinitionRegistry(), knownPersons);
            playerInterpersonalAttitudeParticipant = new InterpersonalAttitudePersistenceParticipant(
                InterpersonalAttitudes,
                GetDefinitionRegistry,
                () => knownPersons,
                service.PlayerId);

            service.RegisterParticipant(playerInterpersonalAttitudeParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerInterpersonalAttitudeParticipant = null;
            }
        }

        private void EnsureWorldReputationParticipant()
        {
            if (!registerWorldReputation || worldReputationParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Reputation persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            Reputation.Configure(GetDefinitionRegistry(), knownPersons);
            worldReputationParticipant = new ReputationPersistenceParticipant(
                Reputation,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldReputationParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldReputationParticipant = null;
            }
        }

        private void EnsureWorldRumorParticipant()
        {
            if (!registerWorldRumors || worldRumorParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Rumor persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            Rumors.Configure(GetDefinitionRegistry(), knownPersons, ResolveKnowledgeRuntimeForPerson, ResolveMemoryRuntimeForPerson);
            worldRumorParticipant = new RumorPersistenceParticipant(
                Rumors,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldRumorParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldRumorParticipant = null;
            }
        }

        private void EnsureWorldSocialInteractionParticipant()
        {
            if (!registerWorldSocialInteractions || worldSocialInteractionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Social Interaction persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            SocialInteractions.Configure(GetDefinitionRegistry(), knownPersons, Relationships, InterpersonalAttitudes, Reputation, Rumors);
            worldSocialInteractionParticipant = new SocialInteractionPersistenceParticipant(
                SocialInteractions,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldSocialInteractionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldSocialInteractionParticipant = null;
            }
        }

        private void EnsureWorldSocialNormParticipant()
        {
            if (!registerWorldSocialNorms || worldSocialNormParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Social Norm persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            SocialNorms.Configure(GetDefinitionRegistry(), knownPersons, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialInteractions);
            worldSocialNormParticipant = new SocialNormPersistenceParticipant(
                SocialNorms,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldSocialNormParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldSocialNormParticipant = null;
            }
        }

        private void EnsureWorldSocialNetworkParticipant()
        {
            if (!registerWorldSocialNetworks || worldSocialNetworkParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Social Network persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            SocialNetworks.Configure(GetDefinitionRegistry(), knownPersons, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialInteractions, SocialNorms);
            worldSocialNetworkParticipant = new SocialNetworkPersistenceParticipant(
                SocialNetworks,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldSocialNetworkParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldSocialNetworkParticipant = null;
            }
        }

        private void EnsureWorldSocialDecisionParticipant()
        {
            if (!registerWorldSocialDecisions || worldSocialDecisionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Social Decision persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            SocialDecisions.Configure(GetDefinitionRegistry(), knownPersons, SocialInteractions, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialNorms, SocialNetworks, SocialDecisionModifierSourceCollection.Compose(SocialInfluence, SocialEmotions));
            worldSocialDecisionParticipant = new SocialDecisionPersistenceParticipant(
                SocialDecisions,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldSocialDecisionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldSocialDecisionParticipant = null;
            }
        }

        private void EnsureWorldSocialInfluenceParticipant()
        {
            if (!registerWorldSocialInfluence || worldSocialInfluenceParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Social Influence persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            SocialInfluence.Configure(GetDefinitionRegistry(), knownPersons, InterpersonalAttitudes, Reputation, SocialInteractions, new[] { playerKnowledge });
            SocialDecisions.Configure(GetDefinitionRegistry(), knownPersons, SocialInteractions, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialNorms, SocialNetworks, SocialDecisionModifierSourceCollection.Compose(SocialInfluence, SocialEmotions));
            worldSocialInfluenceParticipant = new SocialInfluencePersistenceParticipant(
                SocialInfluence,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldSocialInfluenceParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldSocialInfluenceParticipant = null;
            }
        }

        private void EnsureWorldSocialEmotionParticipant()
        {
            if (!registerWorldSocialEmotions || worldSocialEmotionParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Social Emotion persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            SocialInfluence.Configure(GetDefinitionRegistry(), knownPersons, InterpersonalAttitudes, Reputation, SocialInteractions, new[] { playerKnowledge });
            SocialEmotions.Configure(GetDefinitionRegistry(), knownPersons, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialInteractions, SocialNorms, SocialNetworks, SocialInfluence);
            SocialDecisions.Configure(GetDefinitionRegistry(), knownPersons, SocialInteractions, Relationships, InterpersonalAttitudes, Reputation, Rumors, SocialNorms, SocialNetworks, SocialDecisionModifierSourceCollection.Compose(SocialInfluence, SocialEmotions));
            worldSocialEmotionParticipant = new SocialEmotionPersistenceParticipant(
                SocialEmotions,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldSocialEmotionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldSocialEmotionParticipant = null;
            }
        }

        private void EnsureWorldFamilyRelationshipParticipant()
        {
            if (!registerWorldFamilyRelationships || worldFamilyRelationshipParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (definitionCatalog == null)
            {
                Debug.LogWarning("Family Relationship persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            string personId = playerIdentityProgression == null || string.IsNullOrWhiteSpace(playerIdentityProgression.PersonId)
                ? service.PlayerId
                : playerIdentityProgression.PersonId;
            string[] knownPersons = GetPrototypeSocialPersonIds(personId);
            FamilyRelationships.Configure(GetDefinitionRegistry(), knownPersons, Relationships, InterpersonalAttitudes, SocialInteractions, service.WorldId, GetPrototypeAdultPersonIds(personId));
            worldFamilyRelationshipParticipant = new FamilyRelationshipPersistenceParticipant(
                FamilyRelationships,
                GetDefinitionRegistry,
                () => knownPersons,
                service.WorldId);

            service.RegisterParticipant(worldFamilyRelationshipParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                worldFamilyRelationshipParticipant = null;
            }
        }

        private void EnsurePlayerInformationAccessParticipant()
        {
            if (!registerPlayerInformationAccess || playerInformationAccessParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Information Access persistence participant was not registered because the prototype Person identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Information Access persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            InformationAccess.Configure(GetDefinitionRegistry(), playerIdentityProgression.PersonId);
            playerInformationAccessParticipant = new InformationAccessPersistenceParticipant(
                InformationAccess,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerInformationAccessParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerInformationAccessParticipant = null;
            }
        }

        private void EnsurePlayerKnowledgeRecordParticipant()
        {
            if (!registerPlayerKnowledgeRecords || playerKnowledgeRecordParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerIdentityProgression == null)
            {
                Debug.LogWarning("Knowledge Record persistence participant was not registered because the prototype Person identity/progression component is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Knowledge Record persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            KnowledgeRecords.Configure(GetDefinitionRegistry(), playerIdentityProgression.PersonId);
            playerKnowledgeRecordParticipant = new KnowledgeRecordPersistenceParticipant(
                KnowledgeRecords,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerKnowledgeRecordParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerKnowledgeRecordParticipant = null;
            }
        }

        private void ResolvePlayerPersistenceReferences()
        {
            if (playerInventory == null)
            {
                playerInventory = Object.FindAnyObjectByType<PlayerInventory>();
            }

            if (playerEquipment == null && playerInventory != null)
            {
                playerEquipment = playerInventory.GetComponent<PlayerEquipment>();
            }

            if (playerEquipment == null)
            {
                playerEquipment = Object.FindAnyObjectByType<PlayerEquipment>();
            }

            if (playerInventory == null && playerEquipment != null)
            {
                playerInventory = playerEquipment.GetComponent<PlayerInventory>();
            }

            GameObject playerObject = playerInventory == null ? playerEquipment == null ? null : playerEquipment.gameObject : playerInventory.gameObject;
            if (playerObject == null && playerStats != null)
            {
                playerObject = playerStats.gameObject;
            }

            if (playerStats == null)
            {
                playerStats = playerObject == null ? Object.FindAnyObjectByType<PlayerStats>() : playerObject.GetComponent<PlayerStats>();
            }

            if (playerObject == null && playerStats != null)
            {
                playerObject = playerStats.gameObject;
            }

            if (playerAttributes == null)
            {
                playerAttributes = playerObject == null ? Object.FindAnyObjectByType<CharacterAttributes>() : playerObject.GetComponent<CharacterAttributes>();
            }

            if (playerAttributes == null && playerObject != null)
            {
                playerAttributes = playerObject.AddComponent<CharacterAttributes>();
            }

            if (playerCalculatedStats == null)
            {
                playerCalculatedStats = playerObject == null ? Object.FindAnyObjectByType<CalculatedStatCollection>() : playerObject.GetComponent<CalculatedStatCollection>();
            }

            if (playerCalculatedStats == null && playerObject != null)
            {
                playerCalculatedStats = playerObject.AddComponent<CalculatedStatCollection>();
            }

            if (playerSkills == null)
            {
                playerSkills = playerObject == null ? Object.FindAnyObjectByType<CharacterSkillCollection>() : playerObject.GetComponent<CharacterSkillCollection>();
            }

            if (playerSkills == null && playerObject != null)
            {
                playerSkills = playerObject.AddComponent<CharacterSkillCollection>();
            }

            if (playerTraits == null)
            {
                playerTraits = playerObject == null ? Object.FindAnyObjectByType<CharacterTraitCollection>() : playerObject.GetComponent<CharacterTraitCollection>();
            }

            if (playerTraits == null && playerObject != null)
            {
                playerTraits = playerObject.AddComponent<CharacterTraitCollection>();
            }

            if (playerBody == null)
            {
                playerBody = playerObject == null ? Object.FindAnyObjectByType<ActorBodyRuntime>() : playerObject.GetComponent<ActorBodyRuntime>();
            }

            if (playerBody == null && playerObject != null)
            {
                playerBody = playerObject.AddComponent<ActorBodyRuntime>();
            }

            if (playerKnowledge == null)
            {
                playerKnowledge = playerObject == null ? Object.FindAnyObjectByType<PersonKnowledgeRuntime>() : playerObject.GetComponent<PersonKnowledgeRuntime>();
            }

            if (playerResources == null)
            {
                playerResources = playerObject == null ? Object.FindAnyObjectByType<CharacterResourceCollection>() : playerObject.GetComponent<CharacterResourceCollection>();
            }

            if (playerResources == null && playerObject != null)
            {
                playerResources = playerObject.AddComponent<CharacterResourceCollection>();
            }

            if (playerActorLifecycle == null)
            {
                playerActorLifecycle = playerObject == null ? Object.FindAnyObjectByType<ActorLifecycleController>() : playerObject.GetComponent<ActorLifecycleController>();
            }

            if (playerActorLifecycle == null && playerObject != null)
            {
                playerActorLifecycle = playerObject.AddComponent<ActorLifecycleController>();
            }

            if (playerOngoingEffects == null)
            {
                playerOngoingEffects = playerObject == null ? Object.FindAnyObjectByType<OngoingEffectService>() : playerObject.GetComponent<OngoingEffectService>();
            }

            if (playerOngoingEffects == null && playerObject != null)
            {
                playerOngoingEffects = playerObject.AddComponent<OngoingEffectService>();
            }

            if (definitionCatalog != null)
            {
                DefinitionRegistry registry = GetDefinitionRegistry();
                playerAttributes?.Configure(registry);
                playerCalculatedStats?.Configure(registry, playerAttributes);
                playerResources?.Configure(registry, playerCalculatedStats, service == null ? PersistenceService.LocalPlayerId : service.PlayerId);
                playerSkills?.Configure(registry, playerCalculatedStats, playerObject == null ? null : playerObject.GetComponent<PlayerSpellLoadout>());
                playerTraits?.Configure(registry, playerCalculatedStats, playerSkills, service == null ? PersistenceService.LocalPlayerId : service.PlayerId);
                playerActorLifecycle?.Configure(null, playerResources, playerObject == null ? null : playerObject.GetComponent<CharacterSystemCoordinator>(), playerTraits);
                playerOngoingEffects?.Configure(playerObject == null ? null : playerObject.GetComponent<CharacterSystemCoordinator>());
                playerStats?.ConfigureDerivedStats(registry);
                playerStats?.RefreshEquipmentModifiers();
                if (playerKnowledge != null && playerIdentityProgression != null)
                {
                    playerKnowledge.Configure(registry, playerIdentityProgression.PersonId, ResolvePlayerActorId(), playerBody == null ? string.Empty : playerBody.ActorBodyId);
                }
            }

            if (playerHealth == null)
            {
                playerHealth = playerObject == null ? Object.FindAnyObjectByType<PlayerHealth>() : playerObject.GetComponent<PlayerHealth>();
            }

            if (playerMana == null)
            {
                playerMana = playerObject == null ? Object.FindAnyObjectByType<PlayerMana>() : playerObject.GetComponent<PlayerMana>();
            }

            if (playerStamina == null)
            {
                playerStamina = playerObject == null ? Object.FindAnyObjectByType<PlayerStamina>() : playerObject.GetComponent<PlayerStamina>();
            }

            if (statusEffectController == null)
            {
                statusEffectController = playerObject == null ? Object.FindAnyObjectByType<StatusEffectController>() : playerObject.GetComponent<StatusEffectController>();
            }

            if (playerQuestLog == null)
            {
                playerQuestLog = playerObject == null ? Object.FindAnyObjectByType<PlayerQuestLog>() : playerObject.GetComponent<PlayerQuestLog>();
            }

            if (playerContractJournal == null)
            {
                playerContractJournal = playerObject == null ? Object.FindAnyObjectByType<PlayerContractJournal>() : playerObject.GetComponent<PlayerContractJournal>();
            }

            if (playerIdentityProgression == null)
            {
                playerIdentityProgression = playerObject == null ? Object.FindAnyObjectByType<PlayerIdentityProgression>() : playerObject.GetComponent<PlayerIdentityProgression>();
            }

            if (playerIdentityProgression == null && playerObject != null)
            {
                playerIdentityProgression = playerObject.AddComponent<PlayerIdentityProgression>();
            }

            if (playerRoot == null)
            {
                playerRoot = playerObject == null ? null : playerObject.transform;
            }

            if (playerIdentityProgression != null)
            {
                WorldEntityIdentity worldEntityIdentity = playerRoot == null ? null : playerRoot.GetComponent<WorldEntityIdentity>();
                playerIdentityProgression.ConfigureRuntimeReferences(playerStats, worldEntityIdentity, playTimeTracker, overallLevelConfiguration);
                if (definitionCatalog != null)
                {
                    playerIdentityProgression.RegisterDefinitionCache(GetDefinitionRegistry());
                }
            }

            if (playerSkillActionEventSource == null)
            {
                playerSkillActionEventSource = playerObject == null ? Object.FindAnyObjectByType<PlayerSkillActionEventSource>() : playerObject.GetComponent<PlayerSkillActionEventSource>();
            }

            if (playerSkillActionEventSource == null && playerObject != null)
            {
                playerSkillActionEventSource = playerObject.AddComponent<PlayerSkillActionEventSource>();
            }

            if (playerSkillActionEventSource != null && playerObject != null)
            {
                playerSkillActionEventSource.Configure(
                    playerSkills,
                    playerIdentityProgression,
                    playerObject.GetComponent<PlayerMeleeCombat>(),
                    playerObject.GetComponent<PlayerSpellcaster>(),
                    playerEquipment,
                    playTimeTracker);
            }

            if (playerInput == null)
            {
                playerInput = playerRoot == null ? Object.FindAnyObjectByType<PlayerInputReader>() : playerRoot.GetComponentInChildren<PlayerInputReader>();
            }

            if (inventoryScreenController == null)
            {
                inventoryScreenController = FindMenuController();
            }

            if (currentPlaceTracker == null && playerRoot != null)
            {
                currentPlaceTracker = playerRoot.GetComponent<CurrentPlaceTracker>();
                if (currentPlaceTracker == null)
                {
                    currentPlaceTracker = playerRoot.gameObject.AddComponent<CurrentPlaceTracker>();
                }
            }
        }

        private void EnsurePlayerResourcesParticipant()
        {
            if (!registerPlayerResources || playerResourcesParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerResources == null || playerCalculatedStats == null)
            {
                Debug.LogWarning("Player resources persistence participant was not registered because the prototype player resource collection or calculated stats are missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player resources persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerResources.Configure(GetDefinitionRegistry(), playerCalculatedStats, service.PlayerId);
            playerResourcesParticipant = new PlayerResourcesPersistenceParticipant(
                playerResources,
                playerIdentityProgression,
                playerCalculatedStats,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(playerResourcesParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerResourcesParticipant = null;
            }
        }

        private void EnsurePlayerActorLifecycleParticipant()
        {
            if (!registerPlayerActorLifecycle || playerActorLifecycleParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerActorLifecycle == null || playerResources == null)
            {
                Debug.LogWarning("Player actor lifecycle persistence participant was not registered because the lifecycle controller or player resources are missing.");
                return;
            }

            playerActorLifecycle.Configure(null, playerResources, playerRoot == null ? null : playerRoot.GetComponent<CharacterSystemCoordinator>(), playerTraits);
            playerActorLifecycleParticipant = new PlayerActorLifecyclePersistenceParticipant(
                playerActorLifecycle,
                playerIdentityProgression,
                service.PlayerId);

            service.RegisterParticipant(playerActorLifecycleParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerActorLifecycleParticipant = null;
            }
        }

        private void EnsurePlayerCombatExecutionParticipant()
        {
            if (!registerPlayerCombatExecution || playerCombatExecutionParticipant != null)
            {
                return;
            }

            playerCombatExecutionParticipant = new PlayerCombatExecutionPersistenceParticipant(
                CombatExecution,
                service.PlayerId,
                () => playerIdentityProgression == null ? string.Empty : playerIdentityProgression.PersonId);

            service.RegisterParticipant(playerCombatExecutionParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerCombatExecutionParticipant = null;
            }
        }

        private void EnsurePlayerOngoingEffectsParticipant()
        {
            if (!registerPlayerOngoingEffects || playerOngoingEffectsParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerOngoingEffects == null || playerRoot == null)
            {
                Debug.LogWarning("Player ongoing effects persistence participant was not registered because the ongoing effects service or player root is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player ongoing effects persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            CharacterSystemCoordinator character = playerRoot.GetComponent<CharacterSystemCoordinator>();
            playerOngoingEffects.Configure(character);
            playerOngoingEffectsParticipant = new PlayerOngoingEffectsPersistenceParticipant(
                playerOngoingEffects,
                playerRoot.gameObject,
                GetDefinitionRegistry,
                ResolvePlayerActorId,
                service.PlayerId);

            service.RegisterParticipant(playerOngoingEffectsParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerOngoingEffectsParticipant = null;
            }
        }

        private string ResolvePlayerActorId()
        {
            if (playerRoot == null)
            {
                return string.Empty;
            }

            CharacterSystemCoordinator character = playerRoot.GetComponent<CharacterSystemCoordinator>();
            if (character != null && !string.IsNullOrWhiteSpace(character.ActorId))
            {
                return character.ActorId;
            }

            WorldEntityIdentity identity = playerRoot.GetComponent<WorldEntityIdentity>();
            return identity == null ? string.Empty : identity.EntityId;
        }

        private void EnsurePlayerStatsVitalsStatusParticipant()
        {
            if (!registerPlayerStatsVitalsStatus || statsVitalsStatusParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerStats == null || playerHealth == null || playerMana == null || playerStamina == null || statusEffectController == null)
            {
                Debug.LogWarning("Player stats/vitals/status persistence participant was not registered because one or more prototype player runtime components are missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player stats/vitals/status persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            statsVitalsStatusParticipant = new PlayerStatsVitalsStatusPersistenceParticipant(
                playerStats,
                playerHealth,
                playerMana,
                playerStamina,
                statusEffectController,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(statsVitalsStatusParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                statsVitalsStatusParticipant = null;
            }
        }

        private void EnsurePlayerQuestContractParticipant()
        {
            if (!registerPlayerQuestContract || questContractParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerQuestLog == null || playerContractJournal == null || playerInventory == null)
            {
                Debug.LogWarning("Player quest/contract persistence participant was not registered because the prototype player quest log, contract journal, or inventory is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player quest/contract persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            questContractParticipant = new PlayerQuestContractPersistenceParticipant(
                playerQuestLog,
                playerContractJournal,
                playerInventory,
                GetDefinitionRegistry,
                service.PlayerId);

            service.RegisterParticipant(questContractParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                questContractParticipant = null;
            }
        }

        private void EnsurePlayerLocationParticipant()
        {
            if (!registerPlayerLocation || playerLocationParticipant != null)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerRoot == null)
            {
                Debug.LogWarning("Player location persistence participant was not registered because the prototype player root is missing.");
                return;
            }

            if (definitionCatalog == null)
            {
                Debug.LogWarning("Player location persistence participant was not registered because no definition catalog is assigned.");
                return;
            }

            playerLocationParticipant = new PlayerLocationPersistenceParticipant(
                playerRoot,
                GetDefinitionRegistry,
                service.PlayerId,
                ResolveSceneKey(),
                defaultSpawnPointId,
                playerInput,
                inventoryScreenController as IPlayerMenuController,
                currentPlaceTracker);

            playerLocationParticipant.LocationFallbackUsed += OnLocationFallbackUsed;
            service.RegisterParticipant(playerLocationParticipant, out string failureReason);
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogWarning(failureReason);
                playerLocationParticipant.LocationFallbackUsed -= OnLocationFallbackUsed;
                playerLocationParticipant = null;
            }
        }

        public string BuildPlayerLocationDiagnosticSummary()
        {
            EnsureInitialized();
            return playerLocationParticipant == null
                ? $"Scene: {ResolveSceneKey()}\nPlayer location participant: not registered"
                : playerLocationParticipant.BuildDiagnosticSummary();
        }

        private void OnLocationFallbackUsed(LocationFallbackEventArgs args)
        {
            string message = args == null ? "Player location fallback was used." : args.Message;
            Debug.LogWarning(message);
            PrototypeHudMessageBus.Show(message);
        }

        private static MonoBehaviour FindMenuController()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPlayerMenuController)
                {
                    return behaviours[i];
                }
            }

            return null;
        }

        private void SubscribeDirtyEvents()
        {
            if (dirtyEventsSubscribed)
            {
                return;
            }

            ResolvePlayerPersistenceReferences();
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += OnMeaningfulRuntimeStateChanged;
            }

            if (playerEquipment != null)
            {
                playerEquipment.EquipmentChanged += OnMeaningfulRuntimeStateChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged += OnVitalsChanged;
            }

            if (playerMana != null)
            {
                playerMana.ManaChanged += OnResourceChanged;
            }

            if (playerStamina != null)
            {
                playerStamina.StaminaChanged += OnResourceChanged;
            }

            if (playerResources != null)
            {
                playerResources.ResourceChanged += OnPlayerResourceChanged;
                playerResources.ResourceMaximumChanged += OnPlayerResourceMaximumChanged;
                playerResources.ResourcesRestored += OnPlayerResourcesRestored;
            }

            if (playerActorLifecycle != null)
            {
                playerActorLifecycle.DefeatProcessed += OnActorLifecycleChanged;
                playerActorLifecycle.ActorRecovered += OnActorLifecycleChanged;
                playerActorLifecycle.ActorDied += OnActorLifecycleChanged;
                playerActorLifecycle.ActorRevived += OnActorLifecycleChanged;
            }

            if (combatExecutionService != null)
            {
                combatExecutionService.CombatExecutionCommitted += OnCombatExecutionCommitted;
                combatExecutionService.CooldownChanged += OnCombatExecutionChanged;
                combatExecutionService.ExecutionCompleted += OnCombatExecutionChanged;
            }

            if (playerOngoingEffects != null)
            {
                playerOngoingEffects.OngoingEffectApplied += OnOngoingEffectApplicationChanged;
                playerOngoingEffects.OngoingEffectRefreshed += OnOngoingEffectApplicationChanged;
                playerOngoingEffects.OngoingEffectStackChanged += OnOngoingEffectApplicationChanged;
                playerOngoingEffects.OngoingEffectTickProcessed += OnOngoingEffectTickChanged;
                playerOngoingEffects.OngoingEffectTickSkipped += OnOngoingEffectTickChanged;
                playerOngoingEffects.OngoingEffectCancelled += OnOngoingEffectCancellationChanged;
                playerOngoingEffects.OngoingEffectCompleted += OnOngoingEffectCompleted;
            }

            if (statusEffectController != null)
            {
                statusEffectController.StatusAdded += OnStatusChanged;
                statusEffectController.StatusChanged += OnStatusChanged;
                statusEffectController.StatusRemoved += OnStatusChanged;
                statusEffectController.StatusExpired += OnStatusChanged;
            }

            if (playerQuestLog != null)
            {
                playerQuestLog.QuestLogChanged += OnQuestContractChanged;
            }

            if (playerContractJournal != null)
            {
                playerContractJournal.JournalChanged += OnQuestContractChanged;
            }

            if (currentPlaceTracker != null)
            {
                currentPlaceTracker.CurrentPlaceChanged += OnPlaceChanged;
            }

            if (playerIdentityProgression != null)
            {
                playerIdentityProgression.ProgressionChanged += OnIdentityProgressionChanged;
            }

            if (playerAttributes != null)
            {
                playerAttributes.AttributesChanged += OnAttributesChanged;
            }

            if (playerCalculatedStats != null)
            {
                playerCalculatedStats.CalculatedStatsChanged += OnCalculatedStatsChanged;
            }

            if (playerSkills != null)
            {
                playerSkills.SkillsChanged += OnSkillsChanged;
                playerSkills.HiddenProgressChanged += OnSkillHiddenProgressChanged;
            }

            if (playerTraits != null)
            {
                playerTraits.TraitsChanged += OnTraitsChanged;
                playerTraits.TraitRecordChanged += OnTraitRecordChanged;
            }

            if (playerBody != null)
            {
                playerBody.BodyChanged += OnBodyChanged;
            }

            if (playerKnowledge != null)
            {
                playerKnowledge.KnowledgeChanged += OnKnowledgeChanged;
            }

            dirtyEventsSubscribed = true;
        }

        private void UnsubscribeDirtyEvents()
        {
            if (!dirtyEventsSubscribed)
            {
                return;
            }

            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= OnMeaningfulRuntimeStateChanged;
            }

            if (playerEquipment != null)
            {
                playerEquipment.EquipmentChanged -= OnMeaningfulRuntimeStateChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnVitalsChanged;
            }

            if (playerMana != null)
            {
                playerMana.ManaChanged -= OnResourceChanged;
            }

            if (playerStamina != null)
            {
                playerStamina.StaminaChanged -= OnResourceChanged;
            }

            if (playerResources != null)
            {
                playerResources.ResourceChanged -= OnPlayerResourceChanged;
                playerResources.ResourceMaximumChanged -= OnPlayerResourceMaximumChanged;
                playerResources.ResourcesRestored -= OnPlayerResourcesRestored;
            }

            if (playerActorLifecycle != null)
            {
                playerActorLifecycle.DefeatProcessed -= OnActorLifecycleChanged;
                playerActorLifecycle.ActorRecovered -= OnActorLifecycleChanged;
                playerActorLifecycle.ActorDied -= OnActorLifecycleChanged;
                playerActorLifecycle.ActorRevived -= OnActorLifecycleChanged;
            }

            if (combatExecutionService != null)
            {
                combatExecutionService.CombatExecutionCommitted -= OnCombatExecutionCommitted;
                combatExecutionService.CooldownChanged -= OnCombatExecutionChanged;
                combatExecutionService.ExecutionCompleted -= OnCombatExecutionChanged;
            }

            if (playerOngoingEffects != null)
            {
                playerOngoingEffects.OngoingEffectApplied -= OnOngoingEffectApplicationChanged;
                playerOngoingEffects.OngoingEffectRefreshed -= OnOngoingEffectApplicationChanged;
                playerOngoingEffects.OngoingEffectStackChanged -= OnOngoingEffectApplicationChanged;
                playerOngoingEffects.OngoingEffectTickProcessed -= OnOngoingEffectTickChanged;
                playerOngoingEffects.OngoingEffectTickSkipped -= OnOngoingEffectTickChanged;
                playerOngoingEffects.OngoingEffectCancelled -= OnOngoingEffectCancellationChanged;
                playerOngoingEffects.OngoingEffectCompleted -= OnOngoingEffectCompleted;
            }

            if (statusEffectController != null)
            {
                statusEffectController.StatusAdded -= OnStatusChanged;
                statusEffectController.StatusChanged -= OnStatusChanged;
                statusEffectController.StatusRemoved -= OnStatusChanged;
                statusEffectController.StatusExpired -= OnStatusChanged;
            }

            if (playerIdentityProgression != null)
            {
                playerIdentityProgression.ProgressionChanged -= OnIdentityProgressionChanged;
            }

            if (playerAttributes != null)
            {
                playerAttributes.AttributesChanged -= OnAttributesChanged;
            }

            if (playerCalculatedStats != null)
            {
                playerCalculatedStats.CalculatedStatsChanged -= OnCalculatedStatsChanged;
            }

            if (playerSkills != null)
            {
                playerSkills.SkillsChanged -= OnSkillsChanged;
                playerSkills.HiddenProgressChanged -= OnSkillHiddenProgressChanged;
            }

            if (playerTraits != null)
            {
                playerTraits.TraitsChanged -= OnTraitsChanged;
                playerTraits.TraitRecordChanged -= OnTraitRecordChanged;
            }

            if (playerBody != null)
            {
                playerBody.BodyChanged -= OnBodyChanged;
            }

            if (playerKnowledge != null)
            {
                playerKnowledge.KnowledgeChanged -= OnKnowledgeChanged;
            }

            if (playerQuestLog != null)
            {
                playerQuestLog.QuestLogChanged -= OnQuestContractChanged;
            }

            if (playerContractJournal != null)
            {
                playerContractJournal.JournalChanged -= OnQuestContractChanged;
            }

            if (currentPlaceTracker != null)
            {
                currentPlaceTracker.CurrentPlaceChanged -= OnPlaceChanged;
            }

            dirtyEventsSubscribed = false;
        }

        private void OnMeaningfulRuntimeStateChanged()
        {
            dirtyTracker?.MarkDirty("Player state changed.");
        }

        private void OnQuestContractChanged()
        {
            dirtyTracker?.MarkDirty("Quest or contract state changed.");
            autosaveCoordinator?.RequestAutosave("Progression");
        }

        private void OnVitalsChanged(int current, int maximum)
        {
            dirtyTracker?.MarkDirty("Player vitals changed.");
        }

        private void OnResourceChanged(float current, float maximum)
        {
            dirtyTracker?.MarkDirty("Player resource changed.");
        }

        private void OnPlayerResourceChanged(CharacterResourceCollection resources, ResourceChangeResult result)
        {
            if (result == null || result.Request.Restoration || result.Request.Migration)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player resource changed.");
        }

        private void OnActorLifecycleChanged(ActorLifecycleResult result)
        {
            if (result == null || result.Preview || result.Duplicate)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player actor lifecycle changed.");
        }

        private void OnCombatExecutionCommitted(CombatExecutionCommitted committed)
        {
            if (committed == null)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player combat execution changed.");
        }

        private void OnCombatExecutionChanged(CombatExecutionResult result)
        {
            if (result == null || result.Preview || result.Duplicate)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player combat execution changed.");
        }

        private void OnOngoingEffectApplicationChanged(OngoingEffectApplicationResult result)
        {
            if (result == null || result.Preview || result.Duplicate)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player ongoing effect changed.");
        }

        private void OnOngoingEffectTickChanged(OngoingEffectTickResult result)
        {
            if (result == null || string.Equals(result.Code, OngoingEffectResultCode.DuplicateTick, System.StringComparison.Ordinal))
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player ongoing effect tick processed.");
        }

        private void OnOngoingEffectCancellationChanged(OngoingEffectCancellationResult result)
        {
            if (result == null || result.Preview || result.Duplicate)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player ongoing effect cancelled.");
        }

        private void OnOngoingEffectCompleted(RuntimeOngoingEffectInstance instance)
        {
            dirtyTracker?.MarkDirty("Player ongoing effect completed.");
        }

        private void OnPlayerResourceMaximumChanged(CharacterResourceCollection resources, ResourceSnapshot snapshot, float oldMaximum, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player resource maximum changed.");
        }

        private void OnPlayerResourcesRestored(CharacterResourceCollection resources, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player resources changed.");
        }

        private void OnStatusChanged(RuntimeStatusEffect status)
        {
            dirtyTracker?.MarkDirty("Status effect state changed.");
        }

        private void OnPlaceChanged(PlaceDefinition place, bool entered)
        {
            dirtyTracker?.MarkDirty("Player location changed.");
        }

        private void OnIdentityProgressionChanged(PlayerIdentityProgression progression, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player identity/progression changed.");
        }

        private void OnAttributesChanged(CharacterAttributes attributes, IReadOnlyList<string> attributeIds, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player attributes changed.");
        }

        private void OnCalculatedStatsChanged(CalculatedStatCollection stats, IReadOnlyList<string> statIds, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player calculated stats changed.");
        }

        private void OnSkillsChanged(CharacterSkillCollection skills, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player Skills changed.");
        }

        private void OnSkillHiddenProgressChanged(CharacterSkillCollection skills, SkillLearningProgressRecord progress, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player hidden Skill learning progress changed.");
        }

        private void OnTraitsChanged(CharacterTraitCollection traits, TraitOperationResult result, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player Traits changed.");
        }

        private void OnTraitRecordChanged(CharacterTraitCollection traits, RuntimeTraitRecord record, bool restoring)
        {
            if (restoring)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player Trait record changed.");
        }

        private void OnBodyChanged(ActorBodyRuntime bodyRuntime, BodyOperationResult result, bool restoring)
        {
            if (restoring || result == null || result.Preview || result.Duplicate)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player body Species changed.");
            if (playerKnowledge != null && result.Snapshot != null)
            {
                foreach (KnowledgeBeliefRecord belief in playerKnowledge.CreateSnapshot(bodyId: result.Snapshot.ActorBodyId).Beliefs)
                {
                    playerKnowledge.MarkStale(belief.BeliefId, $"knowledge.body-stale.{result.Snapshot.ActorBodyId}.{result.Snapshot.BodyRevision}.{belief.BeliefId}", "Body-specific Knowledge marked stale after body change.");
                }
            }
        }

        private void OnKnowledgeChanged(PersonKnowledgeRuntime runtime, KnowledgeOperationResult result)
        {
            if (result == null || result.Preview || result.Duplicate)
            {
                return;
            }

            dirtyTracker?.MarkDirty("Player Knowledge changed.");
        }

        private string ResolveSceneKey()
        {
            if (!string.IsNullOrWhiteSpace(sceneKey))
            {
                return sceneKey;
            }

            SceneKeyIdentity identity = Object.FindAnyObjectByType<SceneKeyIdentity>();
            return identity == null || string.IsNullOrWhiteSpace(identity.SceneKey) ? "scene.prototype" : identity.SceneKey;
        }

        private DefinitionRegistry GetDefinitionRegistry()
        {
            if (definitionCatalog == null)
            {
                return null;
            }

            if (definitionRegistry != null)
            {
                return definitionRegistry;
            }

            DefinitionRegistry catalogRegistry = definitionCatalog.CreateRegistry();
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            HashSet<string> definitionIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (IGameDefinition definition in catalogRegistry.DefinitionsById.Values)
            {
                if (definition != null && definitionIds.Add(definition.Id))
                {
                    definitions.Add(definition);
                }
            }

            foreach (KnowledgeRecordDefinition definition in PrototypeKnowledgeRecordDefinitionFactory.CreateMissingKnowledgeRecordDefinitions(definitionIds))
            {
                definitions.Add(definition);
            }

            definitionRegistry = PrototypeFamilyRelationshipDefinitionFactory.AddMissingPrototypeFamilyRelationshipDefinitions(
                PrototypeSocialEmotionDefinitionFactory.AddMissingPrototypeSocialEmotionDefinitions(
                    PrototypeSocialInfluenceDefinitionFactory.AddMissingPrototypeSocialInfluenceDefinitions(
                        PrototypeSocialDecisionDefinitionFactory.AddMissingPrototypeSocialDecisionDefinitions(
                            PrototypeSocialNetworkDefinitionFactory.AddMissingPrototypeSocialNetworkDefinitions(
                                PrototypeSocialNormDefinitionFactory.AddMissingPrototypeSocialNormDefinitions(
                                    PrototypeSocialInteractionDefinitionFactory.AddMissingPrototypeSocialInteractionDefinitions(
                                        PrototypeRumorDefinitionFactory.AddMissingPrototypeRumorDefinitions(
                                            PrototypeReputationDefinitionFactory.AddMissingPrototypeReputationDefinitions(
                                                PrototypeAttitudeDefinitionFactory.AddMissingPrototypeAttitudeDefinitions(
                                                    PrototypeRelationshipDefinitionFactory.AddMissingPrototypeRelationshipDefinitions(
                                                        PrototypeProfessionDefinitionFactory.AddMissingPrototypeProfessionDefinitions(
                                                            PrototypeOrganizationDecisionDefinitionFactory.AddMissingPrototypeOrganizationDecisionDefinitions(
                                                                PrototypeOrganizationResourceDefinitionFactory.AddMissingPrototypeOrganizationResourceDefinitions(
                                                                    PrototypeOrganizationAuthorityDefinitionFactory.AddMissingPrototypeOrganizationAuthorityDefinitions(
                                                                        PrototypeOrganizationMembershipDefinitionFactory.AddMissingPrototypeOrganizationMembershipDefinitions(
                                                                            PrototypeOrganizationDefinitionFactory.AddMissingPrototypeOrganizationDefinitions(new DefinitionRegistry(definitions))))))))))))))))));
            return definitionRegistry;
        }

        private PersonKnowledgeRuntime ResolveKnowledgeRuntimeForPerson(string personId)
        {
            if (playerKnowledge == null || playerIdentityProgression == null)
            {
                return null;
            }

            return string.Equals(playerIdentityProgression.PersonId, personId, System.StringComparison.Ordinal)
                ? playerKnowledge
                : null;
        }

        private PersonMemoryRuntime ResolveMemoryRuntimeForPerson(string personId)
        {
            return null;
        }

        private string[] GetPrototypeSocialPersonIds(string primaryPersonId)
        {
            return new[]
            {
                primaryPersonId,
                service == null ? PersistenceService.LocalPlayerId : service.PlayerId,
                "person.prototype.npc",
                "person.prototype.friend",
                "person.prototype.rival",
                "person.prototype.parent",
                "person.prototype.child",
                "person.prototype.dependent",
                "person.prototype.partner",
                "person.prototype.spouse",
                "person.prototype.sibling",
                "person.prototype.cousin",
                "person.prototype.mentor",
                "person.prototype.student"
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(System.StringComparer.Ordinal)
                .ToArray();
        }

        private string[] GetPrototypeAdultPersonIds(string primaryPersonId)
        {
            return GetPrototypeSocialPersonIds(primaryPersonId)
                .Where(value => !value.Contains(".child", System.StringComparison.Ordinal) && !value.Contains(".dependent", System.StringComparison.Ordinal))
                .ToArray();
        }

        private static string[] GetPrototypeCredentialAuthorities()
        {
            return new[]
            {
                "authority.guild.prototype",
                "authority.medical.prototype",
                "organization.prototype.guild",
                "authority.government.prototype",
                "authority.school.prototype",
                PrototypeProfessionDefinitionFactory.PositionAppointAuthorityId,
                PrototypeProfessionDefinitionFactory.PositionDutyAssignAuthorityId,
                PrototypeProfessionDefinitionFactory.PositionSuperviseAuthorityId,
                PrototypeProfessionDefinitionFactory.PositionRestrictedRecordsAuthorityId,
                PrototypeProfessionDefinitionFactory.BlacksmithTeachPermissionId,
                PrototypeProfessionDefinitionFactory.ForgeRestrictedStationPermissionId,
                "organization.prototype.royal-forge",
                "organization.prototype.temple",
                "organization.prototype.university",
                "organization.prototype.government",
                "organization.prototype.independent",
                PersistenceService.LocalPlayerId
            };
        }

        private static string[] GetPrototypeOrganizations()
        {
            return PrototypeOrganizationDefinitionFactory.PrototypeOrganizationIds
                .Concat(new[] { PersistenceService.LocalPlayerId })
                .ToArray();
        }
    }
}
