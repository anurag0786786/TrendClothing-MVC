using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Utility
{
    public class SD
    {
        public const string sp_ProductTypes = "GetProductTypes";
        public const string Sp_GetProductTypes = "GetProductTypes";
        public const string Sp_CreateProductTypes = "CreateProductTypes";
        public const string Sp_UpdateProductTypes = "UpdateProductTypes";
        public const string Sp_DeleteProductTypes = "DeleteProductTypes";
        public const string Role_Admin = "Admin User";
        public const string Role_Employee = "Employee User";
        public const string Role_Company = "Company User";
        public const string Role_Idividual = "Individual User";

        public const string Ss_cartSessionCount = "Cart Count Session";
        public const string SessionCart = "SessionShoppingCart";

        public static double GetPriceBasedQuantity(double quantity, double price,
             double price50, double price100)
        {
            if (quantity < 50)
                return price;
            else if (quantity < 100)
                return price50;
            return price100;
        }
        public const string OrderStatusPending = "Pending";
        public const string OrderStatusApproved = "Approved";
        public const string OrderStatusInProcess = "Processing";
        public const string OrderStatusShipped = "Shipped";
        public const string OrderStatusCancelled = "Cancelled";
        public const string OrderStatusRefunded = "Refunded";

        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayedPayment = "Delayed Payment";
        public const string PaymentStatusRejected = "Rejected";

    }


}

