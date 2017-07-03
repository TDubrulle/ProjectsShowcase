using UnityEngine;
using System.Collections;
using interaction;


namespace interaction {
	
	public class InteractionBlockableDoorMechanism : InteractionMechanism {

		protected BlockableDoorMechanism linkedMechanism;

		public InteractionBlockableDoorMechanism(BlockableDoorMechanism m) {
			linkedMechanism = m;
		}
			
		protected override CommandMessage processMessage(CommandMessage cmdMessage) {
			if (cmdMessage.cmdData.mainActivate.HasValue) {
				if (linkedMechanism.changeDirection (cmdMessage.cmdData.mainActivate.Value)) {
					sendFeedbackMessage (cmdMessage);
				}
			}
			return cmdMessage;
		}
	}
}
