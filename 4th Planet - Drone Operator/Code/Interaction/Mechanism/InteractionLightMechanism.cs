using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionLightMechanism : InteractionMechanism {

        protected LightMechanism linkedMechanism;

        public InteractionLightMechanism(LightMechanism m) {
            linkedMechanism = m;
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {

            if(cmdMessage.cmdData.mainActivate.HasValue) {
                if (linkedMechanism.startTransition(cmdMessage.cmdData.mainActivate.GetValueOrDefault())) {
                    sendFeedbackMessage(cmdMessage);
                };
            }
            return cmdMessage;
        }

    }

}