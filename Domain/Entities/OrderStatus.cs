namespace Domain.Entities
{
    public enum OrderStatus
    {
        Placed,
        PendingApproval,
        Rejected,
        Accepted,
        InPreparation,
        ReadyForPickup,
        AssignedToRider,
        PickedUp,
        OutForDelivery,
        Delivered,
        Cancelled
    }
}
