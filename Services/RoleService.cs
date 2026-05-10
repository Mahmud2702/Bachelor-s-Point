using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Models;

namespace Bachelor_s_Point.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _unitOfWork.RoleRepo.GetAllAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _unitOfWork.RoleRepo.GetByIdAsync(id);
        }

        public async Task<string> CreateRoleAsync(Role role)
        {
            var existing = await _unitOfWork.RoleRepo.GetRoleByNameAsync(role.RoleName!);

            if (existing != null)
            {
                return "Role already exists";
            }

            await _unitOfWork.RoleRepo.AddAsync(role);
            await _unitOfWork.SaveAsync();

            return "Success";
        }

        public async Task<string> UpdateRoleAsync(Role role)
        {
            var existing = await _unitOfWork.RoleRepo.GetByIdAsync(role.Id);

            if (existing == null)
            {
                return "Role not found";
            }

            existing.RoleName = role.RoleName;
            existing.RoleDescription = role.RoleDescription;

            _unitOfWork.RoleRepo.Update(existing);
            await _unitOfWork.SaveAsync();

            return "Success";
        }

        public async Task<string> DeleteRoleAsync(int id)
        {
            var role = await _unitOfWork.RoleRepo.GetByIdAsync(id);

            if (role == null)
            {
                return "Role not found";
            }

            bool hasUsers = await _unitOfWork.RoleRepo.HasUsersAsync(id);

            if (hasUsers)
            {
                return "This role cannot be deleted because users are assigned to it";
            }

            _unitOfWork.RoleRepo.Delete(role);
            await _unitOfWork.SaveAsync();

            return "Success";
        }
    }
}
