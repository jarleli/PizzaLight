using System.Collections.Generic;
using System.Threading.Tasks;
using PizzaLight.Models;

namespace PizzaLight.Resources
{
    public interface IPizzaInviter: IMessageHandler, IMustBeInitialized
    {
        event PizzaInviter.InvitationChangedEventHandler OnInvitationChanged;

        Task Invite(IEnumerable<Invitation> newInvites);
    }
}