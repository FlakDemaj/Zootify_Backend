using Zootify_Backend.Controllers.System;
using Zootify_Backend.Model.Services.Auth.Classes;
using Zootify_Backend.Model.Services.Auth.Interfaces;

namespace Zootify_Backend.Controllers.Base;

public class BaseRestUserController : BaseRestController
{
    private protected IUserService  UserService { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/> with the required user service.
    /// </summary>
    public BaseRestUserController(IUserService userService)
    {
        UserService = userService;
    }
}