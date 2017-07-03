using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PresenceActionner : Actionner
{
	/**The condition to send a message*/
	public enum PresenceActionnerCondition
	{
		None = 0,
		OnEnter = 1 << 0,
		OnExit  = 1 << 1,
        OnEnterAndOnExit = (1 << 1) | (1 << 0)
	}

	/**The type of message sent.*/
	public enum PresenceActionnerMessageType
	{
		//Messages that will send always true or false.
		sendAlwaysTrue,
		sendAlwaysFalse,
		//Messages that will send depending if the object enters or exits
		sendTrueOnEnterFalseOnExit,
		sendFalseOnEnterTrueOnExit
	}

	public PresenceActionnerMessageType type = PresenceActionnerMessageType.sendAlwaysFalse;
	public PresenceActionnerCondition condition = PresenceActionnerCondition.None;

	LayerMask detectionMask = 0;

	[SerializeField][EnumFlagAttribute]
	public RobotType robotType;

    /// <summary>The number of object triggers inside.</summary>
    protected int nbObjectsInActionner = 0;
    /// <summary>If the actionner should continue to activate while an object is inside it.</summary>
    public bool activateWhileObjectInside = false;

    
    #region Unity
    public void FixedUpdate() {
        //TODO: we can move this into a Process for performance optimization.
        if (nbObjectsInActionner > 0) {
            updateCollision(Time.fixedDeltaTime);
        }
    }

	void OnTriggerEnter(Collider other) {
		if (isConcerned (other.gameObject)) {
            nbObjectsInActionner++;
            beginCollision (other.gameObject);
		}
	}
    

	void OnCollisionEnter(Collision collision) {
		if (isConcerned (collision.gameObject)) {
            nbObjectsInActionner++;
            beginCollision (collision.gameObject);
		}
	}

	void OnTriggerExit(Collider other) {
		if (isConcerned (other.gameObject)) {
            nbObjectsInActionner--;
            endCollision (other.gameObject);
		}
	}

	void OnCollisionExit(Collision collision) {
		if (isConcerned (collision.gameObject)) {
            nbObjectsInActionner--;
            endCollision (collision.gameObject);
		}
	}
	#endregion

	#region triggerActions
	/**Actions made when an object enters the zone*/
	protected void beginCollision(GameObject other)
	{
		if ((condition & PresenceActionnerCondition.OnEnter) != PresenceActionnerCondition.None) {

			if (sendOnceCondition()) {
                bool messageType = triggerMessageType(true);
                if (isActivable()) {
                    Activate(messageType);
                }
				if (interactionActionner.isPowered ()) {
					if (condition != PresenceActionnerCondition.OnEnterAndOnExit) {
						sentOnce = true;
					}
					interactionActionner.begin(new interaction.CommandData(messageType, messageType, null, 1.0f, 1.0f));
					startONSound ();
					startONNotice ();
				}
            }
        }
    }

    protected void updateCollision(float deltaT) {
        if (activateWhileObjectInside) {
            if (sendOnceCondition()) {
                bool messageType = triggerMessageType(true);
                if (isActivable()) {
                    Activate(messageType);
                }
				if (interactionActionner.isPowered ()) {
					sentOnce = true;
					interactionActionner.update (new interaction.CommandData (messageType, messageType, null, 1.0f, 1.0f));
				}
            }
        }
    }

	/**Actions made when an object leaves the zone*/
	protected void endCollision(GameObject other)
	{
		if ((condition & PresenceActionnerCondition.OnExit) != PresenceActionnerCondition.None) {
            bool messageType = triggerMessageType(false);
			if (sendOnceCondition ()) {
				if (isActivable ()) {
					Activate (messageType);
				}
				if (interactionActionner.isPowered ()) {
					sentOnce = true;
					startOFFSound ();
					startOFFNotice ();
					interactionActionner.end (new interaction.CommandData (messageType, messageType, null, 0.0f, 0.0f));
				}
			}
       }
    }
	#endregion

    private bool triggerMessageType(bool isEntering) {
            switch (type) {
                case PresenceActionnerMessageType.sendAlwaysTrue:
                    return true;
                case PresenceActionnerMessageType.sendAlwaysFalse:
                    return false;
                case PresenceActionnerMessageType.sendFalseOnEnterTrueOnExit:
                    return !isEntering;
                case PresenceActionnerMessageType.sendTrueOnEnterFalseOnExit:
                    return isEntering;
                default:
                    Debug.LogError("A send message could not be handled because it is of unknown kind!");
                return false;
            }

    }

	bool isConcerned(GameObject other) {
		return isConcernedRobot (other);
	}

	bool isConcernedMask(GameObject other)
	{
		if (((other.layer) & detectionMask) == 0)
			return true;
		return false;
	}

	bool isConcernedRobot(GameObject other)
	{
		ControllableRobot cr = other.GetComponent<ControllableRobot> ();

		if (cr == null) 
			return false;
		if (((cr.getRobotType()) & robotType) != 0)
			return true;

		return false;
	}

}