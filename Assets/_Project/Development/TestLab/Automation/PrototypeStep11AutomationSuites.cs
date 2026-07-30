#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityIsekaiGame.Economy;
using UnityIsekaiGame.Economy.Markets;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Inventory.Identity;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Progression;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(11, "Economy", 1100)]
    public static class PrototypeStep11AutomationSuites
    {
        private const string GoldCurrencyId = "currency.gold";
        private const string CoinCurrencyId = "currency.prototype.coin";
        private const string CoinItemId = "item.prototype-gold-coin";
        private const string PrototypeSwordItemId = "item.prototype-sword";

        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            registry?.TryRegister(Suite(
                "feature.11.1.currency-economic-transactions",
                "Currency and Transactions",
                "11.1",
                11010,
                new[] { "EconomyRuntime", "CurrencyDefinition", "ItemInstanceIdentityRuntime", "InformationAccessRuntime" },
                Scenario(
                    "amounts-and-accounts",
                    "Exact monetary amounts and accounts are authoritative",
                    10,
                    Step("step11-economy-amounts", "Create exact accounts", AmountsAndAccounts)),
                Scenario(
                    "payments-transfers-ledger",
                    "Payments and transfers commit atomically to a conserved ledger",
                    20,
                    Step("step11-economy-transfer", "Transfer and ledger", PaymentsTransfersAndLedger)),
                Scenario(
                    "reservations-refunds-reversals",
                    "Reservations, refunds, and reversals preserve balances",
                    30,
                    Step("step11-economy-reservations", "Reserve and refund", ReservationsRefundsAndReversals)),
                Scenario(
                    "physical-currency-price-snapshots",
                    "Physical currency conversion and fixed prices are explicit",
                    40,
                    Step("step11-economy-physical", "Convert physical currency", PhysicalCurrencyAndPrices)),
                Scenario(
                    "persistence-projections-validation",
                    "Persistence and access projections validate economy state",
                    50,
                    Step("step11-economy-persistence", "Persist and project", PersistenceProjectionAndValidation))), out _);

            registry?.TryRegister(Suite(
                "feature.11.2.markets-price-formation",
                "Markets and Price Formation",
                "11.2",
                11020,
                new[] { "MarketRuntime", "EconomyRuntime", "CurrencyDefinition", "ItemInstanceIdentityRuntime", "InformationAccessRuntime" },
                Scenario(
                    "supply-demand-scarcity",
                    "Supply and demand observations produce deterministic scarcity",
                    10,
                    Step("step11-markets-scarcity", "Evaluate scarcity", MarketSupplyDemandScarcity)),
                Scenario(
                    "reference-prices-regional-history",
                    "Reference prices use regional scarcity and immutable history",
                    20,
                    Step("step11-markets-prices", "Form regional prices", MarketReferencePrices)),
                Scenario(
                    "merchant-quotes-adjustments",
                    "Merchant quotes apply margins and item adjustments without trade mutation",
                    30,
                    Step("step11-markets-quotes", "Create merchant quotes", MarketMerchantQuotes)),
                Scenario(
                    "observations-persistence-projections",
                    "Transaction observations, persistence, and projections are explicit",
                    40,
                    Step("step11-markets-persistence", "Persist and project markets", MarketPersistenceAndProjection))), out _);
        }

        private static TestLabAutomationStepResult AmountsAndAccounts(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out EconomyRuntime economy, out CurrencyDefinition gold, out string failure))
            {
                return Fail("step11-economy-amounts", failure);
            }

            MoneyAmount first = new MoneyAmount(gold.Id, 100L);
            MoneyAmount total = first.Add(new MoneyAmount(gold.Id, 25L)).Subtract(new MoneyAmount(gold.Id, 5L));
            string accountId = Account(context, "player-wallet");
            EconomyOperationResult preview = economy.CreateAccount(accountId, gold, context.ScenarioContext.Runtimes.PersonId, EconomyAccountKind.PersonWallet, total.Units, preview: true);
            EconomyOperationResult create = economy.CreateAccount(accountId, gold, context.ScenarioContext.Runtimes.PersonId, EconomyAccountKind.PersonWallet, total.Units, transactionId: Tx(context, "opening"));

            bool valid = total.Units == 120L
                && preview.Succeeded
                && preview.Preview
                && create.Succeeded
                && economy.TryGetAccount(accountId, out EconomyAccountSnapshot account)
                && account.BalanceUnits == 120L
                && account.AvailableUnits == 120L
                && account.CurrencyId == GoldCurrencyId;
            return TestLabAssertions.True("step11-economy-amounts", "Exact monetary amounts and accounts are authoritative", valid, $"Preview={preview.Code} Create={create.Code} Balance={create.ToAccount?.BalanceUnits}");
        }

        private static TestLabAutomationStepResult PaymentsTransfersAndLedger(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out EconomyRuntime economy, out CurrencyDefinition gold, out string failure))
            {
                return Fail("step11-economy-transfer", failure);
            }

            string buyer = Account(context, "buyer");
            string seller = Account(context, "seller");
            economy.CreateAccount(buyer, gold, "person.buyer", EconomyAccountKind.PersonWallet, 100L, Tx(context, "buyer-open"));
            economy.CreateAccount(seller, gold, "person.seller", EconomyAccountKind.PersonWallet, 0L, Tx(context, "seller-open"));
            string transferId = Tx(context, "transfer");
            EconomyOperationResult preview = economy.Transfer(transferId, buyer, seller, new MoneyAmount(gold.Id, 35L), EconomyTransactionKind.Payment, preview: true);
            EconomyOperationResult execute = economy.Transfer(transferId, buyer, seller, new MoneyAmount(gold.Id, 35L), EconomyTransactionKind.Payment);
            EconomyOperationResult duplicate = economy.Transfer(transferId, buyer, seller, new MoneyAmount(gold.Id, 35L), EconomyTransactionKind.Payment);

            economy.TryGetAccount(buyer, out EconomyAccountSnapshot buyerSnapshot);
            economy.TryGetAccount(seller, out EconomyAccountSnapshot sellerSnapshot);
            bool conserved = buyerSnapshot.BalanceUnits + sellerSnapshot.BalanceUnits == 100L;
            bool valid = preview.Succeeded
                && preview.Preview
                && execute.Succeeded
                && duplicate.Succeeded
                && duplicate.Duplicate
                && buyerSnapshot.BalanceUnits == 65L
                && sellerSnapshot.BalanceUnits == 35L
                && execute.Transaction.LedgerEntries.Count == 2
                && conserved;
            return TestLabAssertions.True("step11-economy-transfer", "Payments and transfers commit atomically to a conserved ledger", valid, $"Preview={preview.Code} Execute={execute.Code} Duplicate={duplicate.Code} Buyer={buyerSnapshot?.BalanceUnits} Seller={sellerSnapshot?.BalanceUnits} Ledger={execute.Transaction?.LedgerEntries.Count}");
        }

        private static TestLabAutomationStepResult ReservationsRefundsAndReversals(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out EconomyRuntime economy, out CurrencyDefinition gold, out string failure))
            {
                return Fail("step11-economy-reservations", failure);
            }

            string buyer = Account(context, "reserve-buyer");
            string seller = Account(context, "reserve-seller");
            economy.CreateAccount(buyer, gold, "person.buyer", EconomyAccountKind.PersonWallet, 80L, Tx(context, "reserve-buyer-open"));
            economy.CreateAccount(seller, gold, "person.seller", EconomyAccountKind.PersonWallet, 0L, Tx(context, "reserve-seller-open"));
            string reservationId = context.ScenarioContext.ScopedId("economy-reservation", "deposit");
            EconomyOperationResult reserve = economy.Reserve(reservationId, buyer, new MoneyAmount(gold.Id, 40L), "shop.checkout");
            EconomyOperationResult pay = economy.Transfer(Tx(context, "reservation-pay"), buyer, seller, new MoneyAmount(gold.Id, 40L), EconomyTransactionKind.Payment, reservationId: reservationId);
            EconomyOperationResult refund = economy.Refund(Tx(context, "refund"), pay.Transaction?.TransactionId, actorId: "person.seller");
            EconomyOperationResult reverse = economy.Reverse(Tx(context, "reverse-invalid"), pay.Transaction?.TransactionId, actorId: "person.seller");

            economy.TryGetAccount(buyer, out EconomyAccountSnapshot buyerSnapshot);
            economy.TryGetAccount(seller, out EconomyAccountSnapshot sellerSnapshot);
            bool valid = reserve.Succeeded
                && pay.Succeeded
                && refund.Succeeded
                && !reverse.Succeeded
                && buyerSnapshot.BalanceUnits == 80L
                && sellerSnapshot.BalanceUnits == 0L
                && pay.Reservation?.state == EconomyReservationState.Committed
                && economy.TryGetTransaction(pay.Transaction.TransactionId, out EconomyTransactionSnapshot original)
                && original.State == EconomyTransactionState.Refunded;
            return TestLabAssertions.True("step11-economy-reservations", "Reservations, refunds, and reversals preserve balances", valid, $"Reserve={reserve.Code} Pay={pay.Code} Refund={refund.Code} Reverse={reverse.Code} Buyer={buyerSnapshot?.BalanceUnits} Seller={sellerSnapshot?.BalanceUnits}");
        }

        private static TestLabAutomationStepResult PhysicalCurrencyAndPrices(TestLabAutomationContext context)
        {
            if (!TryGetPhysicalRuntime(context, out EconomyRuntime economy, out CurrencyDefinition currency, out ItemDefinition coin, out string failure))
            {
                return Fail("step11-economy-physical", failure);
            }

            ItemInstanceIdentityRuntime items = context.ScenarioContext.Runtimes.ItemInstances;
            string wallet = Account(context, "physical-wallet");
            economy.CreateAccount(wallet, currency, context.ScenarioContext.Runtimes.PersonId, EconomyAccountKind.PersonWallet, 0L, Tx(context, "physical-open"));
            string coinInstance = RunGuid(context, "coin-input");
            ItemInstanceOperationResult createCoin = items.CreateItem(coin, ItemInstanceClassification.Fungible, coinInstance, ownerPersonId: context.ScenarioContext.Runtimes.PersonId, custodianPersonId: context.ScenarioContext.Runtimes.PersonId);
            EconomyOperationResult toAbstract = economy.ConvertPhysicalToAbstract(Tx(context, "to-abstract"), wallet, currency, items, coinInstance, 3, context.ScenarioContext.Runtimes.PersonId);
            EconomyOperationResult toPhysical = economy.ConvertAbstractToPhysical(Tx(context, "to-physical"), wallet, currency, items, 1, context.ScenarioContext.Runtimes.PersonId, RunGuid(context, "coin-output"));
            EconomyOperationResult price = economy.CaptureFixedPrice(context.ScenarioContext.ScopedId("fixed-price", "sword"), "item.prototype-sword", wallet, new MoneyAmount(currency.Id, 2L), "price-list.prototype.shop", context.ScenarioContext.Runtimes.PersonId, worldTime: 12d);

            items.TryGetSnapshot(coinInstance, out ItemInstanceSnapshot spentCoin);
            economy.TryGetAccount(wallet, out EconomyAccountSnapshot walletSnapshot);
            bool valid = createCoin.Succeeded
                && toAbstract.Succeeded
                && toPhysical.Succeeded
                && price.Succeeded
                && spentCoin.LifecycleState == ItemLifecycleState.Consumed
                && walletSnapshot.BalanceUnits == 2L
                && price.PriceSnapshot.currencyId == currency.Id
                && price.PriceSnapshot.units == 2L;
            return TestLabAssertions.True("step11-economy-physical", "Physical currency conversion and fixed prices are explicit", valid, $"Create={createCoin.Status} ToAbstract={toAbstract.Code} ToPhysical={toPhysical.Code} Price={price.Code} Wallet={walletSnapshot?.BalanceUnits}");
        }

        private static TestLabAutomationStepResult PersistenceProjectionAndValidation(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out EconomyRuntime economy, out CurrencyDefinition gold, out string failure))
            {
                return Fail("step11-economy-persistence", failure);
            }

            string wallet = Account(context, "private-wallet");
            EconomyOperationResult create = economy.CreateAccount(wallet, gold, "person.secret-holder", EconomyAccountKind.PersonWallet, 77L, Tx(context, "private-open"));
            economy.TryGetAccount(wallet, out EconomyAccountSnapshot account);
            InformationAccessRuntime access = context.ScenarioContext.Runtimes.Access;
            string policyId = context.ScenarioContext.ScopedId("information-access-policy", "economy-wallet");
            InformationAccessOperationResult policy = access.RegisterPolicy(new InformationAccessPolicyData
            {
                policyId = policyId,
                subject = account.CreateInformationSubject(),
                classification = InformationVisibilityClassification.Secret,
                detailVisibilityPolicy = InformationDetailVisibilityPolicy.Selected,
                defaultVisibleDetails = new[] { "detail.account", "detail.currency" },
                defaultRedactedDetails = new[] { "detail.owner", "detail.balance", "detail.reserved" },
                redactedAccessAcceptable = true
            }, Tx(context, "policy"));
            InformationAccessOperationResult grant = access.GrantAccess(new InformationAccessGrantData
            {
                grantId = context.ScenarioContext.ScopedId("information-access-grant", "economy-wallet"),
                policyId = policyId,
                subject = account.CreateInformationSubject(),
                granteeKind = InformationGranteeKind.Person,
                granteeId = "person.visitor",
                grantorId = "person.secret-holder",
                accessModes = new[] { InformationAccessMode.Inspect },
                detailIds = new[] { "detail.account", "detail.currency" }
            }, Tx(context, "grant"));
            InformationAccessProjection<EconomyAccountSnapshot> projection = economy.GetAccountProjection(wallet, access, new InformationAccessContext
            {
                RequestingPersonId = "person.visitor",
                HasDiscoveredSubject = true,
                RevealDenialReasons = true
            }, policyId);

            EconomyRuntimeSaveData save = economy.CreateSaveData();
            bool validSave = EconomyRuntime.ValidateSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry, out string validFailure);
            EconomyRuntime restored = new EconomyRuntime();
            EconomyOperationResult restore = restored.RestoreFromSaveData(save, context.ScenarioContext.Runtimes.DefinitionRegistry);
            EconomyRuntimeSaveData corrupt = save.Clone();
            corrupt.accounts[0].currencyId = "currency.missing";
            bool rejected = !EconomyRuntime.ValidateSaveData(corrupt, context.ScenarioContext.Runtimes.DefinitionRegistry, out string corruptFailure);

            bool valid = create.Succeeded
                && policy.Succeeded
                && grant.Succeeded
                && projection.Redacted
                && projection.Record.AccountId == wallet
                && projection.Record.BalanceUnits == 0L
                && validSave
                && restore.Succeeded
                && rejected
                && restored.TryGetAccount(wallet, out EconomyAccountSnapshot restoredWallet)
                && restoredWallet.BalanceUnits == 77L;
            return TestLabAssertions.True("step11-economy-persistence", "Persistence and access projections validate economy state", valid, $"Policy={policy.Code} Grant={grant.Code} Redacted={projection.Redacted} Save={validSave}:{validFailure} Restore={restore.Code} Rejected={rejected}:{corruptFailure}");
        }

        private static TestLabAutomationStepResult MarketSupplyDemandScarcity(TestLabAutomationContext context)
        {
            if (!TryGetMarketFixture(context, out MarketRuntime markets, out _, out MarketDefinition marketDefinition, out MarketSubjectDefinition subject, out _, out string failure))
            {
                return Fail("step11-markets-scarcity", failure);
            }

            string marketId = MarketId(context, "village");
            MarketOperationResult create = markets.CreateMarketInstance(marketDefinition, marketId, "region.prototype.village");
            MarketOperationResult supply = markets.RecordSupply(Supply(context, marketId, subject.Id, "granary", 120L, 40L, 90L, 1d));
            MarketOperationResult duplicateSource = markets.RecordSupply(Supply(context, marketId, subject.Id, "granary", 120L, 0L, 120L, 2d));
            MarketOperationResult expired = markets.RecordSupply(Supply(context, marketId, subject.Id, "old-cart", 30L, 0L, 30L, 0d, expires: 1d));
            MarketOperationResult demand = markets.RecordDemand(Demand(context, marketId, subject.Id, "villagers", 150L, 30L, 3d));
            MarketOperationResult scarcity = markets.EvaluateScarcity(Scoped(context, "market-scarcity", "main"), marketId, subject.Id, 3d);

            bool valid = create.Succeeded
                && supply.Succeeded
                && !duplicateSource.Succeeded
                && expired.Succeeded
                && demand.Succeeded
                && scarcity.Succeeded
                && scarcity.Scarcity.availableSupply == 90L
                && scarcity.Scarcity.currentDemand == 150L
                && scarcity.Scarcity.scarcityClass == MarketScarcityClass.Scarce;
            return TestLabAssertions.True("step11-markets-scarcity", "Supply and demand observations produce deterministic scarcity", valid, $"Create={create.Code} Supply={supply.Code} Duplicate={duplicateSource.Code} Expired={expired.Code} Demand={demand.Code} Scarcity={scarcity.Scarcity?.scarcityClass} Available={scarcity.Scarcity?.availableSupply} Demand={scarcity.Scarcity?.currentDemand}");
        }

        private static TestLabAutomationStepResult MarketReferencePrices(TestLabAutomationContext context)
        {
            if (!TryGetMarketFixture(context, out MarketRuntime markets, out _, out MarketDefinition marketDefinition, out MarketSubjectDefinition subject, out _, out string failure))
            {
                return Fail("step11-markets-prices", failure);
            }

            string scarceMarket = MarketId(context, "scarce");
            string abundantMarket = MarketId(context, "abundant");
            markets.CreateMarketInstance(marketDefinition, scarceMarket, "region.prototype.mountains");
            markets.CreateMarketInstance(marketDefinition, abundantMarket, "region.prototype.farms");
            markets.RecordSupply(Supply(context, scarceMarket, subject.Id, "scarce-source", 30L, 0L, 30L, 1d));
            markets.RecordDemand(Demand(context, scarceMarket, subject.Id, "scarce-demand", 120L, 0L, 1d));
            markets.RecordSupply(Supply(context, abundantMarket, subject.Id, "abundant-source", 200L, 0L, 200L, 1d));
            markets.RecordDemand(Demand(context, abundantMarket, subject.Id, "abundant-demand", 20L, 0L, 1d));

            MarketOperationResult scarcePrice = markets.UpdateMarketSubject(scarceMarket, subject.Id, 5d);
            MarketOperationResult duplicate = markets.UpdateMarketSubject(scarceMarket, subject.Id, 5d);
            MarketOperationResult abundantPrice = markets.UpdateMarketSubject(abundantMarket, subject.Id, 5d);
            IReadOnlyList<MarketPriceRecordData> history = markets.QueryPriceHistory(scarceMarket, subject.Id);

            bool valid = scarcePrice.Succeeded
                && duplicate.Succeeded
                && duplicate.Duplicate
                && abundantPrice.Succeeded
                && scarcePrice.Price.referenceAmountUnits > subject.BaselinePriceUnits
                && abundantPrice.Price.referenceAmountUnits < subject.BaselinePriceUnits
                && scarcePrice.Price.referenceAmountUnits > abundantPrice.Price.referenceAmountUnits
                && history.Count == 1;
            return TestLabAssertions.True("step11-markets-prices", "Reference prices use regional scarcity and immutable history", valid, $"Scarce={scarcePrice.Price?.referenceAmountUnits} Abundant={abundantPrice.Price?.referenceAmountUnits} Duplicate={duplicate.Duplicate} History={history.Count}");
        }

        private static TestLabAutomationStepResult MarketMerchantQuotes(TestLabAutomationContext context)
        {
            if (!TryGetMarketFixture(context, out MarketRuntime markets, out _, out MarketDefinition marketDefinition, out MarketSubjectDefinition subject, out _, out string failure))
            {
                return Fail("step11-markets-quotes", failure);
            }

            string marketId = MarketId(context, "quotes");
            markets.CreateMarketInstance(marketDefinition, marketId, "region.prototype.shop");
            markets.RecordSupply(Supply(context, marketId, subject.Id, "merchant-stock", 20L, 0L, 20L, 1d));
            markets.RecordDemand(Demand(context, marketId, subject.Id, "buyers", 20L, 0L, 1d));
            MarketOperationResult price = markets.UpdateMarketSubject(marketId, subject.Id, 2d);
            ItemInstanceSnapshot item = new ItemInstanceSnapshot(new ItemInstanceRecordData
            {
                itemInstanceId = Scoped(context, "item-instance", "quote-sword"),
                itemDefinitionId = PrototypeSwordItemId,
                condition = new ItemConditionStateData { state = ItemConditionState.Good, normalized = 0.5f },
                quality = new ItemQualityStateData { tier = ItemQualityTier.Fine, source = ItemQualitySource.Authored, assessed = true },
                labels = new ItemIdentityLabelData { makerMark = "maker.secret" },
                revision = 1L
            });

            MarketOperationResult preview = markets.CreateMerchantQuote(Scoped(context, "quote", "preview"), "merchant.prototype", marketId, subject.Id, MerchantQuoteDirection.MerchantSells, 1L, 3d, 7d, item: item, preview: true);
            MarketOperationResult sell = markets.CreateMerchantQuote(Scoped(context, "quote", "sell"), "merchant.prototype", marketId, subject.Id, MerchantQuoteDirection.MerchantSells, 1L, 3d, 7d, item: item);
            MarketOperationResult buy = markets.CreateMerchantQuote(Scoped(context, "quote", "buy"), "merchant.prototype", marketId, subject.Id, MerchantQuoteDirection.MerchantBuys, 1L, 3d, 7d, item: item);
            MarketOperationResult hidden = markets.CreateMerchantQuote(Scoped(context, "quote", "hidden"), "merchant.prototype", marketId, subject.Id, MerchantQuoteDirection.MerchantSells, 1L, 3d, 7d, item: item, privilegedHiddenFactors: true);
            bool validNow = markets.ValidateQuoteForExecution(sell.Quote.quoteId, 4d, out _);
            bool expired = !markets.ValidateQuoteForExecution(sell.Quote.quoteId, 8d, out string expiredReason);

            bool valid = price.Succeeded
                && preview.Succeeded
                && preview.Preview
                && sell.Succeeded
                && buy.Succeeded
                && hidden.Succeeded
                && buy.Quote.finalAmountUnits < sell.Quote.finalAmountUnits
                && hidden.Quote.finalAmountUnits > sell.Quote.finalAmountUnits
                && !sell.Quote.hiddenFactorsApplied
                && hidden.Quote.hiddenFactorsApplied
                && markets.QuoteCount == 3
                && validNow
                && expired;
            return TestLabAssertions.True("step11-markets-quotes", "Merchant quotes apply margins and item adjustments without trade mutation", valid, $"Price={price.Code} Preview={preview.Code} Sell={sell.Quote?.finalAmountUnits} Buy={buy.Quote?.finalAmountUnits} Hidden={hidden.Quote?.finalAmountUnits} Count={markets.QuoteCount} Expired={expired}:{expiredReason}");
        }

        private static TestLabAutomationStepResult MarketPersistenceAndProjection(TestLabAutomationContext context)
        {
            if (!TryGetMarketFixture(context, out MarketRuntime markets, out DefinitionRegistry registry, out MarketDefinition marketDefinition, out MarketSubjectDefinition subject, out CurrencyDefinition gold, out string failure))
            {
                return Fail("step11-markets-persistence", failure);
            }

            string marketId = MarketId(context, "persist");
            markets.CreateMarketInstance(marketDefinition, marketId, "region.prototype.private");
            markets.RecordSupply(Supply(context, marketId, subject.Id, "private-stock", 10L, 0L, 10L, 1d));
            markets.RecordDemand(Demand(context, marketId, subject.Id, "private-demand", 10L, 0L, 1d));
            MarketOperationResult price = markets.UpdateMarketSubject(marketId, subject.Id, 2d);

            EconomyRuntime economy = context.ScenarioContext.Runtimes.Economy;
            economy.Configure(registry, context.ScenarioContext.Runtimes.WorldId);
            string buyer = Account(context, "market-buyer");
            string seller = Account(context, "market-seller");
            economy.CreateAccount(buyer, gold, "person.buyer", EconomyAccountKind.PersonWallet, 500L, Tx(context, "market-buyer"));
            economy.CreateAccount(seller, gold, "person.seller", EconomyAccountKind.PersonWallet, 0L, Tx(context, "market-seller"));
            EconomyOperationResult transfer = economy.Transfer(Tx(context, "market-observed"), buyer, seller, new MoneyAmount(gold.Id, price.Price.referenceAmountUnits), EconomyTransactionKind.Payment);
            MarketOperationResult observation = markets.AddTransactionObservation(Scoped(context, "market-transaction-observation", "sale"), transfer.Transaction, marketId, subject.Id, MarketTransactionObservationPolicy.IncludeCommitted, publicObservation: true, worldTime: 3d);

            InformationAccessRuntime access = context.ScenarioContext.Runtimes.Access;
            string policyId = Scoped(context, "information-access-policy", "market-price");
            access.RegisterPolicy(new InformationAccessPolicyData
            {
                policyId = policyId,
                subject = price.Price.CreateInformationSubject(),
                classification = InformationVisibilityClassification.Secret,
                detailVisibilityPolicy = InformationDetailVisibilityPolicy.Selected,
                defaultVisibleDetails = new[] { "detail.market", "detail.subject", "detail.reference-price" },
                defaultRedactedDetails = new[] { "detail.supply", "detail.demand", "detail.scarcity", "detail.source" },
                redactedAccessAcceptable = true
            }, Tx(context, "market-policy"));
            access.GrantAccess(new InformationAccessGrantData
            {
                grantId = Scoped(context, "information-access-grant", "market-price"),
                policyId = policyId,
                subject = price.Price.CreateInformationSubject(),
                granteeKind = InformationGranteeKind.Person,
                granteeId = "person.viewer",
                grantorId = "merchant.prototype",
                accessModes = new[] { InformationAccessMode.Inspect },
                detailIds = new[] { "detail.market", "detail.subject", "detail.reference-price" }
            }, Tx(context, "market-grant"));
            MarketProjection<MarketPriceRecordData> projection = markets.GetPriceProjection(price.Price.marketPriceId, access, new InformationAccessContext
            {
                RequestingPersonId = "person.viewer",
                HasDiscoveredSubject = true,
                RevealDenialReasons = true
            }, policyId);

            MarketRuntimeSaveData save = markets.CreateSaveData();
            bool validSave = MarketRuntime.ValidateSaveData(save, registry, out string validFailure);
            MarketRuntime restored = new MarketRuntime();
            MarketOperationResult restore = restored.RestoreFromSaveData(save, registry);
            MarketRuntimeSaveData corrupt = save.Clone();
            corrupt.currentPrices[0].marketPriceId = "market-price.missing";
            bool rejected = !MarketRuntime.ValidateSaveData(corrupt, registry, out string corruptFailure);

            bool valid = price.Succeeded
                && transfer.Succeeded
                && observation.Succeeded
                && projection.Succeeded
                && projection.Redacted
                && projection.Record.supplyAvailable == 0L
                && validSave
                && restore.Succeeded
                && rejected
                && restored.TryGetCurrentPrice(marketId, subject.Id, out MarketPriceRecordData restoredPrice)
                && restoredPrice.referenceAmountUnits == price.Price.referenceAmountUnits;
            return TestLabAssertions.True("step11-markets-persistence", "Transaction observations, persistence, and projections are explicit", valid, $"Price={price.Code} Transfer={transfer.Code} Observation={observation.Code} Redacted={projection.Redacted} Save={validSave}:{validFailure} Restore={restore.Code} Rejected={rejected}:{corruptFailure}");
        }

        private static bool TryGetMarketFixture(
            TestLabAutomationContext context,
            out MarketRuntime markets,
            out DefinitionRegistry extendedRegistry,
            out MarketDefinition marketDefinition,
            out MarketSubjectDefinition subject,
            out CurrencyDefinition gold,
            out string failure)
        {
            markets = context?.ScenarioContext?.Runtimes?.Markets;
            extendedRegistry = null;
            marketDefinition = null;
            subject = null;
            gold = null;
            failure = string.Empty;
            DefinitionRegistry registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (markets == null)
            {
                failure = "Market runtime is missing from the Test Lab runtime bundle.";
                return false;
            }

            if (registry == null)
            {
                failure = "Definition registry is missing.";
                return false;
            }

            if (!registry.TryGet(GoldCurrencyId, out gold))
            {
                failure = $"Currency definition '{GoldCurrencyId}' is missing.";
                return false;
            }

            ItemDefinition sword = registry.TryGet(PrototypeSwordItemId, out ItemDefinition foundSword)
                ? foundSword
                : CreateItemDefinition(PrototypeSwordItemId, "Prototype Sword");
            marketDefinition = ScriptableObject.CreateInstance<MarketDefinition>();
            marketDefinition.Initialize(
                "market.prototype.local",
                "Prototype Local Market",
                gold,
                MarketCategory.LocalSettlement,
                MarketScopeType.Settlement,
                new[] { MarketSubjectKind.ItemDefinition });

            subject = ScriptableObject.CreateInstance<MarketSubjectDefinition>();
            subject.Initialize(
                "market-subject.prototype-sword",
                "Prototype Sword",
                MarketSubjectKind.ItemDefinition,
                sword.Id,
                gold,
                100L,
                MarketQuantityUnit.Each,
                1L);
            SetPrivate(subject, "minimumPriceUnits", 1L);
            SetPrivate(subject, "maximumPriceUnits", 1000L);

            List<IGameDefinition> definitions = registry.DefinitionsById.Values.ToList();
            if (!registry.Contains(sword.Id))
            {
                definitions.Add(sword);
            }

            definitions.Add(marketDefinition);
            definitions.Add(subject);
            extendedRegistry = new DefinitionRegistry(definitions);
            markets.Configure(extendedRegistry, context.ScenarioContext.Runtimes.WorldId);
            context.ScenarioContext.Runtimes.Economy?.Configure(extendedRegistry, context.ScenarioContext.Runtimes.WorldId);
            return true;
        }

        private static MarketObservationRecordData Supply(TestLabAutomationContext context, string marketId, string subjectId, string source, long quantity, long reserved, long available, double observed, double expires = -1d)
        {
            return new MarketObservationRecordData
            {
                observationId = Scoped(context, "market-supply", source),
                marketInstanceId = marketId,
                marketSubjectId = subjectId,
                unit = MarketQuantityUnit.Each,
                quantity = quantity,
                reservedQuantity = reserved,
                availableNowQuantity = available,
                supplySourceCategory = MarketSupplySourceCategory.MerchantInventory,
                sourceReferenceId = source,
                observedWorldTime = observed,
                expiresWorldTime = expires,
                reliability = 9000
            };
        }

        private static MarketObservationRecordData Demand(TestLabAutomationContext context, string marketId, string subjectId, string source, long quantity, long expected, double observed)
        {
            return new MarketObservationRecordData
            {
                observationId = Scoped(context, "market-demand", source),
                marketInstanceId = marketId,
                marketSubjectId = subjectId,
                unit = MarketQuantityUnit.Each,
                quantity = quantity,
                expectedFutureQuantity = expected,
                demandCategory = MarketDemandCategory.Consumer,
                sourceReferenceId = source,
                observedWorldTime = observed,
                reliability = 9000
            };
        }

        private static ItemDefinition CreateItemDefinition(string id, string display)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetPrivate(item, "itemId", id);
            SetPrivate(item, "displayName", display);
            SetPrivate(item, "stackable", false);
            SetPrivate(item, "maximumStackSize", 1);
            SetPrivate(item, "instanceMode", ItemInstanceMode.AlwaysInstanced);
            return item;
        }

        private static bool TryGetRuntime(TestLabAutomationContext context, out EconomyRuntime economy, out CurrencyDefinition gold, out string failure)
        {
            economy = context?.ScenarioContext?.Runtimes?.Economy;
            gold = null;
            failure = string.Empty;
            DefinitionRegistry registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (economy == null)
            {
                failure = "Economy runtime is missing from the Test Lab runtime bundle.";
                return false;
            }

            if (registry == null)
            {
                failure = "Definition registry is missing.";
                return false;
            }

            if (!registry.TryGet(GoldCurrencyId, out gold))
            {
                failure = $"Currency definition '{GoldCurrencyId}' is missing.";
                return false;
            }

            economy.Configure(registry, context.ScenarioContext.Runtimes.WorldId);
            return true;
        }

        private static bool TryGetPhysicalRuntime(TestLabAutomationContext context, out EconomyRuntime economy, out CurrencyDefinition currency, out ItemDefinition coin, out string failure)
        {
            economy = context?.ScenarioContext?.Runtimes?.Economy;
            currency = null;
            coin = null;
            failure = string.Empty;
            DefinitionRegistry registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (economy == null || registry == null)
            {
                failure = economy == null ? "Economy runtime is missing." : "Definition registry is missing.";
                return false;
            }

            coin = ScriptableObject.CreateInstance<ItemDefinition>();
            SetPrivate(coin, "itemId", CoinItemId);
            SetPrivate(coin, "displayName", "Prototype Gold Coin");
            SetPrivate(coin, "stackable", true);
            SetPrivate(coin, "maximumStackSize", 999);
            SetPrivate(coin, "instanceMode", ItemInstanceMode.AlwaysInstanced);

            currency = ScriptableObject.CreateInstance<CurrencyDefinition>();
            currency.Initialize(CoinCurrencyId, "Prototype Coin Currency", "G", physicalItem: coin, physicalUnits: 1L, issuer: "issuer.prototype");
            DefinitionRegistry extended = new DefinitionRegistry(registry.DefinitionsById.Values.Concat(new IGameDefinition[] { coin, currency }));
            economy.Configure(extended, context.ScenarioContext.Runtimes.WorldId);
            return true;
        }

        private static ITestLabAutomationSuite Suite(string suiteId, string displayName, string feature, int order, IReadOnlyList<string> required, params ITestLabAutomationScenario[] scenarios)
        {
            return new TestLabAutomationSuite(suiteId, displayName, feature, $"{displayName} runtime integration scenarios.", order, TestLabAutomationCategory.Standard, includeInRunAll: true, requiredServices: required, scenarios: scenarios);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Economy | TestLabRuntimeArea.Items | TestLabRuntimeArea.KnowledgeHistory,
                requiredDefinitionIds: new[] { GoldCurrencyId });
        }

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            return new TestLabScenarioStep(stepId, displayName, action);
        }

        private static TestLabAutomationStepResult Fail(string stepId, string diagnostics)
        {
            return TestLabAssertions.Fail(stepId, "Currency and Transactions", "OperationSucceeded", "Succeeded", "Failed", diagnostics);
        }

        private static string Account(TestLabAutomationContext context, string slug)
        {
            return context.ScenarioContext.ScopedId("economy-account", slug);
        }

        private static string Tx(TestLabAutomationContext context, string slug)
        {
            return context.ScenarioContext.ScopedId("economy-tx", slug);
        }

        private static string MarketId(TestLabAutomationContext context, string slug)
        {
            return Scoped(context, "market-instance", slug);
        }

        private static string Scoped(TestLabAutomationContext context, string prefix, string slug)
        {
            return context.ScenarioContext.ScopedId(prefix, slug);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            target?.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(target, value);
        }

        private static string RunGuid(TestLabAutomationContext context, string slug)
        {
            string seed = $"{context?.RunId}.{context?.CurrentSuiteId}.{context?.CurrentScenarioId}.{slug}";
            using MD5 md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(seed ?? string.Empty));
            return new Guid(bytes).ToString("D");
        }
    }
}
#endif
