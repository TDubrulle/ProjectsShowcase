using UnityEngine;
using System.Collections;


namespace interaction {

    public class InteractionGlobalConveyorMechanism : InteractionMechanism {

        protected GlobalConveyor linkedMechanism;

        public InteractionGlobalConveyorMechanism(GlobalConveyor m) {
            linkedMechanism = m;
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {

            bool shouldFeedback = false;

            if(cmdMessage.cmdData.mainActivate.HasValue) {
                linkedMechanism.setActive(cmdMessage.cmdData.mainActivate.GetValueOrDefault());
                shouldFeedback = true;
            }
            if(cmdMessage.cmdData.secondaryActivate.HasValue) {
                linkedMechanism.changeSens(cmdMessage.cmdData.secondaryActivate.GetValueOrDefault());
                shouldFeedback = true;
            }
            if(shouldFeedback) {
                sendFeedbackMessage(cmdMessage);
            }
            return cmdMessage;
        }
    }

}