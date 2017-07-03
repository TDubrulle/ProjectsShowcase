using UnityEngine;
using System.Collections;


namespace interaction {

    public interface ActionnerUser {
        void onActionnerEndedMessageReceived(CommandData cmdData);
    }
}