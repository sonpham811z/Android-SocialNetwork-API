using System.Threading.Tasks;
using Identity.Domain.Events;

namespace Identity.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event) where T : UserEvent;
    }
}