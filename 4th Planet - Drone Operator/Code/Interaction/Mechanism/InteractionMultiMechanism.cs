using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionMultiMechanism : InteractionMechanism {

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {

            spreadMessage(cmdMessage);
            return cmdMessage;
        }
    }

}