using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionDummyMechanism : InteractionMechanism {

        protected DummyMechanism linkedMechanism;

        public InteractionDummyMechanism(DummyMechanism m) {
            linkedMechanism = m;
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {
			
			if (cmdMessage.cmdData.mainActivate.GetValueOrDefault ()) {
				if (linkedMechanism.fill ((cmdMessage.cmdData.floatL.GetValueOrDefault () + cmdMessage.cmdData.floatR.GetValueOrDefault ()) * 0.5f * Time.deltaTime)) {
					//We finished activating the mechanism : we stop.
					spreadMessage (cmdMessage);
					if (destMechanisms.Count == 0) {
						sendFeedbackMessage (cmdMessage);
					}
				}
			}
			if (cmdMessage.isStartingMessage ()) {
				linkedMechanism.start ();
			} else if (cmdMessage.isEndingMessage ()) {
				linkedMechanism.end ();
			}
			return cmdMessage;
        }
    }

}