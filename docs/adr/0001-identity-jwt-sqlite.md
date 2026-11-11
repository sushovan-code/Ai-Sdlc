# 0001. ASP.NET Core Identity, JWT access tokens and SQLite

Status: accepted (slice 1)

## Context
The product needs role-based access, account management (registration, password change, lockout, deactivation) and a local run that works without Docker or an external identity provider, so a reviewer can clone and run it in minutes.

## Decision
- **Identity**: ASP.NET Core Identity on EF Core, behind the `IUserAccounts` port in Application. Application code never sees Identity types.
- **Tokens**: short-lived (60 minutes) HS256 JWT access tokens, issued by `ITokenService`. There is no refresh token yet (YAGNI); the user signs in again after expiry. The signing key comes from user secrets or the environment; in Development a random key is generated at startup if none is configured (tokens then do not survive a restart).
- **Roles**: one role per user (ProjectManager, Architect, Developer, QA, Admin). Self-registration always yields Developer; only an Admin can change roles.
- **Freshness**: after the token is validated, the account is loaded from the database. A deactivated account is rejected at once and the role claim is replaced by the stored role, so deactivation and role changes apply on the next request instead of after token expiry.
- **Database**: SQLite through EF Core with committed migrations, applied on start by a hosted service. The provider is swappable through the connection string and one registration line.
- **Browser storage**: the token is kept in `sessionStorage` (cleared when the tab closes, not shared between tabs).
- **Lockout and abuse**: 5 failed attempts lock the account for 15 minutes; the auth endpoints are rate limited per client; unknown user, wrong password and disabled account return the same response.

## Consequences
- No external dependency is needed to run locally. Moving to Keycloak, Auth0 or Entra ID later means replacing the adapter and the JWT bearer configuration; use cases and endpoints stay as they are.
- A bearer token in `sessionStorage` is readable by script, so the web app must keep a strict content policy and avoid unsafe HTML. A cookie-based or backend-for-frontend flow is the alternative if the threat model requires it.
- Every authenticated request does one extra indexed lookup. SQLite makes this cheap; revisit with caching if the database changes.
- Email confirmation and password reset need an `IEmailSender` adapter and are not part of this slice. Accounts are created as confirmed.
