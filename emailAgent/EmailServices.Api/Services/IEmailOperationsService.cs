using EmailAgent.Entities;

namespace EmailServices.Api.Services;

public interface IEmailOperationsService
{
    Task<GetEmailResponse> GetEmailsAsync(string service, string userEmail, int count = 10);

    Task<bool> DeleteEmailAsync(string emailId, string service, string userEmail);

    Task<bool> MoveEmailAsync(string emailId, string service, string userEmail, string destinationFolder);
}
