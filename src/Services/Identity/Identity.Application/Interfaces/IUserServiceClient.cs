using System;
using System.Threading.Tasks;

namespace Identity.Application.Interfaces
{
    public interface IUserServiceClient
    {
        Task EnsureProfileCreatedAsync(
            Guid userId,
            string email,
            string firstName,
            string lastName,
            DateTime? dateOfBirth,
            string? gender);
    }
}
