using OnlineShopping.Models;

namespace OnlineShopping.Services
{
    public interface IOrderStatusTransitionValidator
    {
        bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus);
        string GetInvalidTransitionMessage(OrderStatus currentStatus, OrderStatus newStatus);
        IEnumerable<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus);
    }

    public class OrderStatusTransitionValidator : IOrderStatusTransitionValidator
    {
        private readonly Dictionary<OrderStatus, HashSet<OrderStatus>> _allowedTransitions = new()
        {
            [OrderStatus.Pending] = new HashSet<OrderStatus>
            {
                OrderStatus.Processing,
                OrderStatus.Cancelled
            },
            [OrderStatus.Processing] = new HashSet<OrderStatus>
            {
                OrderStatus.Shipped,
                OrderStatus.Cancelled
            },
            [OrderStatus.Shipped] = new HashSet<OrderStatus>
            {
                OrderStatus.Delivered,
                OrderStatus.Cancelled
            },
            [OrderStatus.Delivered] = new HashSet<OrderStatus>(), // No transitions allowed from Delivered
            [OrderStatus.Cancelled] = new HashSet<OrderStatus>()  // No transitions allowed from Cancelled
        };

        public bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            if (currentStatus == newStatus)
                return false; // No transition to the same status

            return _allowedTransitions.ContainsKey(currentStatus) &&
                   _allowedTransitions[currentStatus].Contains(newStatus);
        }

        public string GetInvalidTransitionMessage(OrderStatus currentStatus, OrderStatus newStatus)
        {
            if (currentStatus == newStatus)
                return $"Order is already in {currentStatus} status.";

            if (currentStatus == OrderStatus.Delivered)
                return "Cannot change status of a delivered order.";

            if (currentStatus == OrderStatus.Cancelled)
                return "Cannot change status of a cancelled order.";

            var allowed = GetAllowedTransitions(currentStatus);
            if (allowed.Any())
            {
                var allowedList = string.Join(", ", allowed);
                return $"Invalid status transition from {currentStatus} to {newStatus}. Allowed transitions: {allowedList}";
            }

            return $"No transitions allowed from {currentStatus} status.";
        }

        public IEnumerable<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus)
        {
            return _allowedTransitions.ContainsKey(currentStatus)
                ? _allowedTransitions[currentStatus].ToList()
                : Enumerable.Empty<OrderStatus>();
        }
    }
}