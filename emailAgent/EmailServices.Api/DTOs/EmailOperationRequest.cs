namespace EmailServices.Api.DTOs;

public record EmailDeleteRequest(string EmailId, string Service, string UserEmail);

public record EmailMoveRequest(string EmailId, string Service, string UserEmail, string DestinationFolder);

public record ApiResponse<T>(bool Success, T? Data = default, string? Error = null);

public record ErrorResponse(string Message, string? Details = null, string? CorrelationId = null);
