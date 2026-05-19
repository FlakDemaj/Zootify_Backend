using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    
    private readonly IMapper _mapper;

    public UserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<GetUserResponseDto> GetUserByGuidAsync(Guid guid)
    {
        var user = await _userRepository.GetUserByGuidWithAuditInformationAsync(guid);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        return _mapper.Map<GetUserResponseDto>(user);
    }
}