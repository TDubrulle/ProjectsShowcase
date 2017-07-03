using UnityEngine;
using System.Collections;

namespace interaction {

    public class DelayedMessageProcess : WaitProcess {

        public InteractionMechanism src;
        public InteractionMechanism dest;
        public CommandMessage message;

        public DelayedMessageProcess(InteractionMechanism src, InteractionMechanism dest, CommandMessage message, float delay) : base(delay) {
            this.src = src;
            this.dest = dest;
            this.message = message;
        }

        public override void OnEnd() {
            base.OnEnd();
            dest.onMessageReceived(message);
        }

    }

}