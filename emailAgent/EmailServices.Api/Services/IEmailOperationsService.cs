namespace EmailServices.Api.Services;

public interface IEmailOperationsService
{
    Task<bool> DeleteEmailAsync(string emailId, string service, string userEmail);

    Task<bool> MoveEmailAsync(string emailId, string service, string userEmail, string destinationFolder);
}
