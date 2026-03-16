namespace TrendClothing.Utility
{
    public static class SD
    {
        // ─── Roles ───────────────────────────────────────────────────────────────
        public const string Role_Admin = "Admin User";
        public const string Role_Employee = "Employee User";
        public const string Role_Company = "Company User";

        // ✅ FIX: Typo was "Role_Idividual" (missing 'n') → fixed
        public const string Role_Individual = "Individual User";

        // ─── Session Keys ─────────────────────────────────────────────────────────
        public const string Ss_cartSessionCount = "Cart Count Session";
        public const string SessionCart = "SessionShoppingCart";

        // ─── Order Status ─────────────────────────────────────────────────────────
        public const string OrderStatusPending = "Pending";
        public const string OrderStatusApproved = "Approved";
        public const string OrderStatusInProcess = "Processing";
        public const string OrderStatusShipped = "Shipped";
        public const string OrderStatusDelivered = "Delivered";
        public const string OrderStatusCancelled = "Cancelled";
        public const string OrderStatusRefunded = "Refunded";

        // ─── Payment Status ───────────────────────────────────────────────────────
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayedPayment = "Delayed Payment";
        public const string PaymentStatusRejected = "Rejected";
        public const string PaymentStatusRefunded = "Refunded";

        // ─── Admin Email ───────────────────────────────────────────────────────────
        // ✅ NEW: Move to appsettings.json in production (never hardcode real email)
        public const string AdminEmail = "admin@trendclothing.com";

        // ✅ REMOVED: Unused stored-procedure constants (sp_ProductTypes, Sp_GetProductTypes, etc.)
        // ✅ REMOVED: GetPriceBasedQuantity() — unused method, clutter removed
        // ✅ REMOVED: Role_Idividual typo kept as alias below for backward compat during migration
        [Obsolete("Use Role_Individual instead — this was a typo")]
        public const string Role_Idividual = Role_Individual;
    }
}