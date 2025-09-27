using MediatR;
using TodoApi.Domain.Events;

namespace TodoApi.Application.Events
{
    public class MediatRDomainEvent : DomainEvent, INotification
    {
        public DomainEvent DomainEvent { get; }

        public MediatRDomainEvent(DomainEvent domainEvent)
        {
            DomainEvent = domainEvent;
        }
    }
}