using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ElevatorMechanism : Mechanism {

    public enum ELEVATOR_CURRENT_STATE {
        MOVING_TO_END,
        MOVING_TO_START,
    }

    public bool moveOnlyOnce = false;
    protected bool movedOnce = false;

    public GameObject thingToMove;
    public Transform startPosition;
    public Transform endPosition;

    public OnSceneEvent onMovingSound = new OnSceneEvent(SoundKeys.PLACEHOLDER_LOOP, null, false, true);
    public List<OnSceneEvent> onEndSoundList = new List<OnSceneEvent>();

    [SerializeField, HideInInspector]
    protected AnimationCurve transition = new AnimationCurve();

    protected ELEVATOR_CURRENT_STATE state = ELEVATOR_CURRENT_STATE.MOVING_TO_END;

    protected List<WorldObject> transportedObjects = new List<WorldObject>();

    /// <summary>
    /// The global number of turns needed to activate.
    /// </summary>
    public int cyclesNeeded = 0;

    /// <summary>
    /// The time needed (in seconds) to go to the end, at full speed.
    /// </summary>
    protected float turnValue = 1.0f;

    /// <summary>
    /// Current time value.
    /// </summary>
    protected float currentValue = 0.0f;
    
    public float getCompletionState() {
        if (turnValue != 0.0f) {
            return currentValue / turnValue;
        } else {
            return 0.0f;
        }
    }

    public AnimationCurve transitionCurve {
        get { return transition; }
        set { transition = value; }
    }

    public override void Awake() {
        base.Awake();
        interactionMechanism = new interaction.InteractionElevatorMechanism(this);
        initModifiers();
        if (transitionCurve.length == 0) {
            transitionCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
        }
        turnValue = cyclesNeeded * BotdanovActionnerAnimationController.ACTIONNER_CYCLE_TIME;
        if(thingToMove == null) {
            thingToMove = this.gameObject;
        }
		Debug.Assert(startPosition != null, "The elevatorMechanism has no point to start from.", this.gameObject);
		Debug.Assert(endPosition != null, "The elevatorMechanism has no endpoint to go to.", this.gameObject);
        if (onMovingSound.gameObjectAttached == null)
        {
            onMovingSound.gameObjectAttached = this.gameObject;
        }
    }

    public override void init()
    {
		base.init ();
        thingToMove.transform.position = startPosition.position;
    }

    void FixedUpdate() {
        if (currentValue != 0f) {
            float percentage = getCompletionState();
            switch (state) {
                case ELEVATOR_CURRENT_STATE.MOVING_TO_END: move(startPosition, endPosition, percentage); break;
                case ELEVATOR_CURRENT_STATE.MOVING_TO_START: move(endPosition, startPosition, percentage); break;
                default: Debug.LogError("Elevator is using an unkown state.", this); break;
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        WorldObject wo = other.gameObject.GetComponentInChildren<WorldObject>();
        if (wo == null) {
            return;
        }
        transportedObjects.Add(wo);
    }

    void OnTriggerExit(Collider other) {
        WorldObject wo = other.gameObject.GetComponentInChildren<WorldObject>();
        if (wo == null) {
            return;
        }
        transportedObjects.Remove(wo);
    }

    public override bool onCommandProcess(MechanismCommand _command) {
        return updatePosition(_command.value);
    }

    public bool updatePosition(float amount) {
        if (!moveOnlyOnce || (!movedOnce && moveOnlyOnce)) {
            currentValue += amount;
            manageMovingSound(amount);
            if (currentValue >= turnValue) {
                movedOnce = true;
                OnSceneEvent.unprocess(onMovingSound, true);
                for (int i = 0; i < onEndSoundList.Count; ++i)
                {
                    OnSceneEvent.process(onEndSoundList[i], onEndSoundList[i].delaySound);
                }
                switch (state) {
                    case ELEVATOR_CURRENT_STATE.MOVING_TO_END: state = ELEVATOR_CURRENT_STATE.MOVING_TO_START; break;
                    case ELEVATOR_CURRENT_STATE.MOVING_TO_START: state = ELEVATOR_CURRENT_STATE.MOVING_TO_END; break;
                    default: Debug.LogError("Elevator tries to finish moving while not moving!", this); break;
                }
                currentValue = 0.0f;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Move the platform and all WorldObjects on it.
    /// </summary>
    /// <param name="from">Where the platform started.</param>
    /// <param name="to">Where the platform should go</param>
    /// <param name="state">The value, between 0 and 1, the platform should be. 0 means it is starting at "from", 1 at "to", and 0.5 halfwayThrough.</param>
    protected void move(Transform from, Transform to, float state) {
        Vector3 newPosition = Vector3.Lerp(from.position, to.position, transitionCurve.Evaluate(state));
        Vector3 oldPosition = thingToMove.transform.position;
        thingToMove.transform.position = newPosition;
        thingToMove.transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, transitionCurve.Evaluate(state));
        for (int i = 0; i < transportedObjects.Count; ++i) {
            transportedObjects[i].Translate(newPosition - oldPosition);
        }
    }

    private void manageMovingSound(float _value)
    {
        if (onMovingSound.soundId == string.Empty)
        {
            return;
        }
        if (_value > 0f && !onMovingSound.isStarted)
        {
            OnSceneEvent.process(onMovingSound, onMovingSound.delaySound);
        }
        else if (_value == 0f && onMovingSound.isStarted)
        {
            OnSceneEvent.unprocess(onMovingSound, true);
        }
        if (onMovingSound.isStarted)
        {
            App.Sound.EventMgr.Enqueue(new Evt.OnUpdateSoundParameter("speed", _value / Time.deltaTime, onMovingSound.soundId, onMovingSound.gameObjectAttached));
        }
    }
}