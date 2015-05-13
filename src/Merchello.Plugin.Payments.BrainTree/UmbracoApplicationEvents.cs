﻿namespace Merchello.Plugin.Payments.Braintree
{
    using System;
    using System.Linq;

    using Braintree;

    using Core.Services;

    using global::Braintree;

    using Merchello.Core.Models;
    using Merchello.Plugin.Payments.Braintree.Models;
    using Merchello.Plugin.Payments.Braintree.Services;

    using Umbraco.Core;
    using Umbraco.Core.Events;
    using Umbraco.Core.Logging;


    /// <summary>
    /// Handles Umbraco application events.
    /// </summary>
    public class UmbracoApplicationEvents : ApplicationEventHandler
    {
        /// <summary>
        /// The application started.
        /// </summary>
        /// <param name="umbracoApplication">
        /// The umbraco application.
        /// </param>
        /// <param name="applicationContext">
        /// The application context.
        /// </param>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            base.ApplicationStarted(umbracoApplication, applicationContext);

            LogHelper.Info<UmbracoApplicationEvents>("Initializing BrainTree Payment provider events");

            GatewayProviderService.Saving += GatewayProviderServiceOnSaving;

            AutoMapperMappings.CreateMappings();

            //// Clear cache for customer
            //// TODO this is sort of punting to blanket clear all cached braintree requests
            //// but in some cases a customer needs to be cleared when the id is not available
            //// likewise a subscription when the payment method changes, etc.
            //// Deadlines -
            BraintreeSubscriptionApiService.Created += BraintreeSubscriptionApiServiceOnCreated;
            BraintreeSubscriptionApiService.Updated += BraintreeSubscriptionApiServiceOnUpdated;
            BraintreePaymentMethodApiService.Created += BraintreePaymentMethodApiServiceOnCreated;
            BraintreePaymentMethodApiService.Updating += BraintreePaymentMethodApiServiceOnUpdating;
        }

        /// <summary>
        /// The clear braintree cache.
        /// </summary>
        private static void ClearBraintreeCache()
        {
            ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheByKeySearch("braintree.");
        }

        /// <summary>
        /// Clears the cache when a payment method is updated
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="saveEventArgs">
        /// The save event args.
        /// </param>
        private void BraintreePaymentMethodApiServiceOnUpdating(BraintreePaymentMethodApiService sender, SaveEventArgs<PaymentMethodRequest> saveEventArgs)
        {
            ClearBraintreeCache();
        }

        /// <summary>
        /// Clears the cache when a payment method is created
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="newEventArgs">
        /// The new event args.
        /// </param>
        private void BraintreePaymentMethodApiServiceOnCreated(BraintreePaymentMethodApiService sender, Core.Events.NewEventArgs<PaymentMethod> newEventArgs)
        {
            ClearBraintreeCache();
        }

        /// <summary>
        /// Clears the cache with a subscription is updated
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="saveEventArgs">
        /// The save event args.
        /// </param>
        private void BraintreeSubscriptionApiServiceOnUpdated(BraintreeSubscriptionApiService sender, SaveEventArgs<Subscription> saveEventArgs)
        {
            ClearBraintreeCache();
        }

        /// <summary>
        /// The braintree subscription api service on created.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="newEventArgs">
        /// The new event args.
        /// </param>
        private void BraintreeSubscriptionApiServiceOnCreated(BraintreeSubscriptionApiService sender, Core.Events.NewEventArgs<Subscription> newEventArgs)
        {
            ClearBraintreeCache();
        }

        /// <summary>
        /// The gateway provider service on saving.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="saveEventArgs">
        /// The save event args.
        /// </param>
        private void GatewayProviderServiceOnSaving(IGatewayProviderService sender, SaveEventArgs<IGatewayProviderSettings> saveEventArgs)
        {
            var key = new Guid("D143E0F6-98BB-4E0A-8B8C-CE9AD91B0969");
            var provider = saveEventArgs.SavedEntities.FirstOrDefault(x => key == x.Key && !x.HasIdentity);

            if (provider == null) return;

            var settings = new BraintreeProviderSettings()
                               {
                                   PrivateKey = string.Empty,
                                   PublicKey = string.Empty,
                                   DefaultTransactionOption = TransactionOption.SubmitForSettlement,
                                   MerchantDescriptor = new MerchantDescriptor()
                                                            {
                                                                Name = string.Empty,
                                                                Phone = string.Empty,
                                                                Url = string.Empty
                                                            },
                                   MerchantId = string.Empty,
                                   Environment = EnvironmentType.Sandbox
                               };

            provider.ExtendedData.SaveProviderSettings(settings);
        }
    }
}