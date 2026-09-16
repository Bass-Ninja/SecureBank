using Mediator;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Behaviors;
using SecureBank.Application.Exceptions;

namespace SecureBank.UnitTests.Application.Behaviors;

public sealed class AuthorizationBehaviorTests
{
    private sealed record StaffOnlyRequest : IRequireRole, IRequest<string>
    {
        public IReadOnlyCollection<string> AllowedRoles => BankRoles.Staff;
    }

    private sealed record UnrestrictedRequest : IRequest<string>;

    private sealed class FakeUserContext(params string[] roles) : IUserContext
    {
        public Guid UserId => Guid.NewGuid();

        public bool IsInRole(string role) => roles.Contains(role);
    }

    [Fact]
    public async Task Handle_WhenRequestRequiresRoleAndUserLacksIt_ThrowsForbidden()
    {
        var behavior = new AuthorizationBehavior<StaffOnlyRequest, string>(
            new FakeUserContext(BankRoles.Customer));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(
                    new StaffOnlyRequest(),
                    (_, _) => new ValueTask<string>("handled"),
                    CancellationToken.None)
                .AsTask());
    }

    [Fact]
    public async Task Handle_WhenRequestRequiresRoleAndUserHasIt_CallsNext()
    {
        var behavior = new AuthorizationBehavior<StaffOnlyRequest, string>(
            new FakeUserContext(BankRoles.Support));

        var result = await behavior.Handle(
            new StaffOnlyRequest(),
            (_, _) => new ValueTask<string>("handled"),
            CancellationToken.None);

        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_WhenRequestHasNoRoleRequirement_CallsNext()
    {
        var behavior = new AuthorizationBehavior<UnrestrictedRequest, string>(
            new FakeUserContext());

        var result = await behavior.Handle(
            new UnrestrictedRequest(),
            (_, _) => new ValueTask<string>("handled"),
            CancellationToken.None);

        Assert.Equal("handled", result);
    }
}
