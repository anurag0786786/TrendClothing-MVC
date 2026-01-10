using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Utility
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string toPhone, string message);
    }
}
