﻿using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.TicketModule
{
    [ModuleExport(typeof(TicketModule))]
    public class TicketModule : ModuleBase
    {
        readonly IRegionManager _regionManager;
        private readonly TicketEditorView _ticketEditorView;
        private readonly ICategoryCommand _navigateTicketCommand;

        [ImportingConstructor]
        public TicketModule(IRegionManager regionManager, TicketEditorView ticketEditorView)
        {
            _navigateTicketCommand = new CategoryCommand<string>("POS", Resources.Common, "Images/Network.png", OnNavigateTicketCommand, CanNavigateTicket);
            _regionManager = regionManager;
            _ticketEditorView = ticketEditorView;

            PermissionRegistry.RegisterPermission(PermissionNames.AddItemsToLockedTickets, PermissionCategories.Ticket, Resources.CanReleaseTicketLock);
            PermissionRegistry.RegisterPermission(PermissionNames.RemoveTicketTag, PermissionCategories.Ticket, Resources.CanRemoveTicketTag);
            PermissionRegistry.RegisterPermission(PermissionNames.GiftItems, PermissionCategories.Ticket, Resources.CanGiftItems);
            PermissionRegistry.RegisterPermission(PermissionNames.VoidItems, PermissionCategories.Ticket, Resources.CanVoidItems);
            PermissionRegistry.RegisterPermission(PermissionNames.MoveTicketItems, PermissionCategories.Ticket, Resources.CanMoveTicketLines);
            PermissionRegistry.RegisterPermission(PermissionNames.MergeTickets, PermissionCategories.Ticket, Resources.CanMergeTickets);
            PermissionRegistry.RegisterPermission(PermissionNames.DisplayOldTickets, PermissionCategories.Ticket, Resources.CanDisplayOldTickets);
            PermissionRegistry.RegisterPermission(PermissionNames.MoveUnlockedTicketItems, PermissionCategories.Ticket, Resources.CanMoveUnlockedTicketLines);
            PermissionRegistry.RegisterPermission(PermissionNames.ChangeExtraProperty, PermissionCategories.Ticket, Resources.CanUpdateExtraModifiers);

            PermissionRegistry.RegisterPermission(PermissionNames.MakePayment, PermissionCategories.Payment, Resources.CanGetPayment);
            PermissionRegistry.RegisterPermission(PermissionNames.MakeFastPayment, PermissionCategories.Payment, Resources.CanGetFastPayment);
            PermissionRegistry.RegisterPermission(PermissionNames.MakeDiscount, PermissionCategories.Payment, Resources.CanMakeDiscount);
            PermissionRegistry.RegisterPermission(PermissionNames.RoundPayment, PermissionCategories.Payment, Resources.CanRoundTicketTotal);
            PermissionRegistry.RegisterPermission(PermissionNames.FixPayment, PermissionCategories.Payment, Resources.CanFlattenTicketTotal);

            EventServiceFactory.EventService.GetEvent<GenericEvent<Customer>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.CustomerSelectedForTicket || x.Topic == EventTopicNames.PaymentRequestedForTicket)
                        ActivateTicketEditorView();
                }
                );

            EventServiceFactory.EventService.GetEvent<GenericEvent<Ticket>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.TicketSelectedFromTableList)
                    ActivateTicketEditorView();
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.ActivateTicketView || x.Topic == EventTopicNames.DisplayTicketView)
                        ActivateTicketEditorView();
                });


            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketCreated, "Ticket Created");
            RuleActionTypeRegistry.RegisterActionType("AddTicketDiscount", "Add Ticket Discount", new[] { "Discount Percentage" }, new[] { "" });
            EventServiceFactory.EventService.GetEvent<GenericEvent<ActionData>>().Subscribe(x =>
            {
                if (x.Value.Action.ActionType == "AddTicketDiscount")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var percent = x.Value.GetAsDecimal("Discount Percentage");
                        ticket.AddTicketDiscount(DiscountType.Percent, percent, AppServices.CurrentLoggedInUser.Id);
                    }
                }
            });
        }

        private static bool CanNavigateTicket(string arg)
        {
            return AppServices.MainDataContext.IsCurrentWorkPeriodOpen;
        }

        private static void OnNavigateTicketCommand(string obj)
        {
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateTicketView);
        }

        private void ActivateTicketEditorView()
        {
            _regionManager.Regions[RegionNames.MainRegion].Activate(_ticketEditorView);
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(TicketEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.UserRegion, typeof(DepartmentButtonView));
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishNavigationCommandEvent(_navigateTicketCommand);
        }
    }
}
