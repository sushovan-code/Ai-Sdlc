using AiSdlc.Application.Auth.ChangePassword;
using AiSdlc.Application.Auth.GetCurrentUser;
using AiSdlc.Application.Auth.Login;
using AiSdlc.Application.Auth.Register;
using AiSdlc.Application.Files.DeleteProjectFile;
using AiSdlc.Application.Files.DownloadProjectFile;
using AiSdlc.Application.Files.UploadProjectFile;
using AiSdlc.Application.Projects.AddProjectMember;
using AiSdlc.Application.Projects.CreateProject;
using AiSdlc.Application.Projects.GetProject;
using AiSdlc.Application.Projects.ListProjects;
using AiSdlc.Application.Projects.RemoveProjectMember;
using AiSdlc.Application.UserDirectory.SearchUsers;
using AiSdlc.Application.Users.ListUsers;
using AiSdlc.Application.Users.SetUserActive;
using AiSdlc.Application.Users.SetUserRole;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AiSdlc.Application;

public static class DependencyInjection
{
    /// <summary>Registers use-case handlers and validators. Adapters are registered by the infrastructure layer.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginValidator>(ServiceLifetime.Singleton);

        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ListUsersHandler>();
        services.AddScoped<SetUserRoleHandler>();
        services.AddScoped<SetUserActiveHandler>();

        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<ListProjectsHandler>();
        services.AddScoped<GetProjectHandler>();
        services.AddScoped<AddProjectMemberHandler>();
        services.AddScoped<RemoveProjectMemberHandler>();
        services.AddScoped<UploadProjectFileHandler>();
        services.AddScoped<DownloadProjectFileHandler>();
        services.AddScoped<DeleteProjectFileHandler>();
        services.AddScoped<SearchUsersHandler>();

        return services;
    }
}
