using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace interaction {

    public class InteractionActionner : InteractionMechanism {

        protected List<ActionnerUser> currentActionnerUser = new List<ActionnerUser>();

        public CommandData begin(CommandData cmdData) {
            if (isPowered()) {
				spreadMessage(new CommandMessage(cmdData, this, CommandMessage.MessageType.start));
            }
            return cmdData;
        }

        public CommandData begin(CommandData cmdData, ActionnerUser inter) {
            if (isPowered()) {
                currentActionnerUser.Add(inter);
				spreadMessage(new CommandMessage(cmdData, this, CommandMessage.MessageType.start));
            }
            return cmdData;
        }

        public CommandData update(CommandData cmdData) {
            if (isPowered()) {
                spreadMessage(new CommandMessage(cmdData, this));
            }
            return cmdData;
        }
 
        public CommandData end(CommandData cmdData) {
            if (isPowered()) {
				spreadMessage(new CommandMessage(cmdData, this, CommandMessage.MessageType.end));
            }
            return cmdData;
        }

        public CommandData end(CommandData cmdData, ActionnerUser inter) {
            if (isPowered()) {
                currentActionnerUser.Remove(inter);
				spreadMessage(new CommandMessage(cmdData, this, CommandMessage.MessageType.end));
            }
            return cmdData;
        }

        public void receiveFeedbackMessage(CommandMessage cmdMessage) {
            //If an actionner receives a feedBackmessage, it means that a process finished.
            for (int i = 0; i < currentActionnerUser.Count; ++i) {
                cmdMessage = applyModifiers(INMessageModifiers, cmdMessage);
                currentActionnerUser[i].onActionnerEndedMessageReceived(cmdMessage.cmdData);
            }
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {
            return cmdMessage;
        }

		public void registerUser(ActionnerUser _user) {
			currentActionnerUser.Add (_user);
		}
		public void unregisterUser(ActionnerUser _user) {
			currentActionnerUser.Remove (_user);
		}

    }
}