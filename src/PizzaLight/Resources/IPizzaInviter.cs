using System.Collections.Generic;
using System.Threading.Tasks;
using PizzaLight.Infrastructure;
using PizzaLight.Models;

namespace PizzaLight.Resources
{
    public interface IPizzaInviter: IMessageHandler, IMustBeInitialized
    {
        event PizzaInviter.InvitationChangedEventHandler OnInvitationChanged;
        void Invite(IEnumerable<Invitation> newInvites);
        List<Invitation> OutstandingInvites { get; }
    }
}