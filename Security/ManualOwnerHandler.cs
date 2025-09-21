using Microsoft.AspNetCore.Authorization;
using ManualApp.Models;
using ManualApp.Services;

namespace ManualApp.Security
{
    public class ManualOwnerHandler : AuthorizationHandler<ManualOwnerRequirement, Manual>
    {
        private readonly ICurrentUserService _current;

        public ManualOwnerHandler(ICurrentUserService current)
        {
            _current = current;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManualOwnerRequirement requirement, Manual resource)
        {
            if (_current.IsAdmin || resource.OwnerId == _current.UserId)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
