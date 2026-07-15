using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Enums
{
    public enum RoleEnum
    {
        Student,
        Lecture,
        Admin
    }

    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        Cancelled,
        Failed
    }
}
