using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using TrendClothing;
namespace TrendClothing.Utility
{
    public class SmsSender : ISmsSender
    {
        private readonly TwilioSettings _settings;

        public SmsSender(IOptions<TwilioSettings> settings)
        {
            _settings = settings.Value;

            TwilioClient.Init(
                _settings.AccountSid,
                _settings.AuthToken
            );
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhone),
                from: new PhoneNumber(_settings.FromNumber), // ✅ THIS FIXES ERROR
                body: message
            );
        }
    }
}
