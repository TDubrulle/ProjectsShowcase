using UnityEngine;
using System.Collections;

namespace interaction {
	public class InteractionNoticeMechanism : InteractionMechanism {

		NoticeMechanism linkedMechanism;

		public InteractionNoticeMechanism(NoticeMechanism m) {
			linkedMechanism = m;
		}

		protected override CommandMessage processMessage(CommandMessage cmdMessage) {
			bool shouldSendFeedback = false;
			if (cmdMessage.cmdData.mainActivate.HasValue) {
				linkedMechanism.sendMainNotification (cmdMessage.cmdData.mainActivate.Value);
				shouldSendFeedback = true;
			}
			if (cmdMessage.cmdData.secondaryActivate.HasValue) {
				linkedMechanism.sendSecondaryNotification (cmdMessage.cmdData.secondaryActivate.Value);
				shouldSendFeedback = true;
			}
			if (shouldSendFeedback) {
				sendFeedbackMessage (cmdMessage);
			}
			return cmdMessage;
		}

	}
}