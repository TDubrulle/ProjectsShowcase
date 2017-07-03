using UnityEngine;
using System.Collections;

public class ActionnerMechanism : Mechanism {

    public Actionner actionner;

	    public override void Awake() {
        base.Awake();
		Debug.Assert (actionner != null, this.gameObject.name + " has no actionner linked to it.",this.gameObject);
        this.interactionMechanism = new interaction.InteractionActionnerMechanism(this);
        initModifiers();
    }

    public override bool onCommandProcess(MechanismCommand _command) {
		return powerActionner (_command.activated);
    }

	public bool powerActionner(bool newState) {
		if (actionner) {
			actionner.onPoweredFeedback (newState);
			actionner.activable = newState;
		} else {
			Debug.LogError("No actionner is attached to this Actionner Mechanism.", this);
			return false;
		}
		return true;
	}

}