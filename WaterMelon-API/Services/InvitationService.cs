using WaterMelon_API.Models;
using System.Linq;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace WaterMelon_API.Services
{
    public class InvitationService
    {
        private readonly IMongoCollection<Invitation> _invitations;
        private readonly IConfiguration _configuration;

        public InvitationService(IInvitationDatabaseSettings invSettings, IEventDatabaseSettings evSettings, IConfiguration config)
        {
            var client = new MongoClient(invSettings.ConnectionString);
            var invDatabase = client.GetDatabase(invSettings.DatabaseName);

            _configuration = config;
            _invitations = invDatabase.GetCollection<Invitation>(invSettings.InvitationsCollectionName);

        }

        public Invitation Create(Invitation invitation)
        {
            // You can only send an invite if it didn't exist before or if the guest previously refused 
            Invitation invitationLoaded = _invitations.Find<Invitation>(invitationQuery => invitationQuery.SenderId.Equals(invitation.SenderId) 
                                                                && invitation.GuestId.Equals(invitation.GuestId)
                                                                && invitation.Status.Equals(InvitationStatus.Accepted) || 
                                                                invitation.Status.Equals(InvitationStatus.Pending)).FirstOrDefault();
            if (invitationLoaded == null)
            {
                _invitations.InsertOne(invitation);
                return invitation;
            }
            if (invitationLoaded.Status == InvitationStatus.Refused)
            {
                invitationLoaded.Status = InvitationStatus.Pending;
                _invitations.ReplaceOne(i => i.Id == invitationLoaded.Id, invitationLoaded);
                return invitationLoaded;
            }
            return null;
        }

        public List<Invitation> GetAllInvitations() 
        {
            var result = _invitations.Find(invitations => true).ToList();
            return result;
        } 

        public Invitation GetFromInvitationId(string id) {
            var result = _invitations.Find<Invitation>(_invitation => _invitation.Id == id).FirstOrDefault();
            return result;
        } 

        public List<Invitation> GetFromSender(string id)
            => _invitations.Find(_event => _event.SenderId.Contains(id)).ToList();

        public List<Invitation> GetFromGuest(string id)
            => _invitations.Find(_event => _event.GuestId.Contains(id)).ToList();

        public Invitation UpdateInvitation(string id, InvitationRequest invitationRequest)
        {
            Invitation invitationReceived = new Invitation(id, invitationRequest);
            _invitations.ReplaceOne(i => i.Id == id, invitationReceived);
            return GetFromInvitationId(id);
        }

        public void RemoveInvitationWithId(string id)
        {
            _invitations.DeleteOne(i => i.Id == id);
        }

        public Invitation AcceptInvitation(string id) 
        { 
            Invitation invitationLoaded = GetFromInvitationId(id);
            if (invitationLoaded == null)
            {
                return null;
            }
            invitationLoaded.Status = InvitationStatus.Accepted;
            _invitations.ReplaceOne(i => i.Id == invitationLoaded.Id, invitationLoaded);
            return GetFromInvitationId(id); 
        }

        public Invitation RefuseInvitation(string id)
        {
            Invitation invitationLoaded = GetFromInvitationId(id);
            if (invitationLoaded == null)
                return null;
            if (invitationLoaded.Status == InvitationStatus.Pending) 
            {
                invitationLoaded.Status = InvitationStatus.Refused;
                _invitations.ReplaceOne(i => i.Id == id, invitationLoaded);
                return invitationLoaded;
            }
            return null;
        }
    }
}