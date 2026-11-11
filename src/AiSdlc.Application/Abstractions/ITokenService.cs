namespace AiSdlc.Application.Abstractions;

/// <summary>Issues signed access tokens for authenticated users.</summary>
public interface ITokenService
{
    AccessToken Issue(UserDto user);
}
