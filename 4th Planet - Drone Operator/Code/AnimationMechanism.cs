using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AnimMechanismParams
{
    public float animSpeed = 1f;
}

public class AnimationMechanism : Mechanism
{
    public Animator animator = null;

    public const string ANIM_BOOL_PARAM = "onoff";
	public const string ANIM_SECONDARY_BOOL_PARAM = "secondary_onoff";

    [Tooltip("Sound launched at the activation of the Actionner")]
    public List<OnSceneEvent> onONSoundList = new List<OnSceneEvent>();
    [Tooltip("Sound launched at the deactivation of the Actionner")]
    public List<OnSceneEvent> onOFFSoundList = new List<OnSceneEvent>();

	[Tooltip("Sound launched at the activation of the Actionner (secondary activation)")]
	public List<OnSceneEvent> secondaryOnONSoundList = new List<OnSceneEvent>();
	[Tooltip("Sound launched at the deactivation of the Actionner (secondary activation)")]
	public List<OnSceneEvent> secondaryOnOFFSoundList = new List<OnSceneEvent>();

    [Tooltip("Notification launched at the activation of the Actionner")]
    public GuiHud.Notice onONNotification = new GuiHud.Notice("", 1, GuiHud.Notice.Type.None);
    [Tooltip("Notification launched at the deactivation of the Actionner")]
    public GuiHud.Notice onOFFNotification = new GuiHud.Notice("", 1, GuiHud.Notice.Type.None);

	[Tooltip("Notification launched at the activation of the Actionner (secondary activation)")]
	public GuiHud.Notice secondaryOnONNotification = new GuiHud.Notice("", 1, GuiHud.Notice.Type.None);
	[Tooltip("Notification launched at the deactivation of the Actionner (secondary activation)")]
	public GuiHud.Notice secondaryOnOFFNotification = new GuiHud.Notice("", 1, GuiHud.Notice.Type.None);

    public AnimMechanismParams animParam = new AnimMechanismParams();

	public ObjectMover linkedObjectMover = null;
	public bool sendFeedbackOnAnimationEnd = false;

	private bool onOff = false;
	private bool secondaryOnOff = false;

    public override void Awake()
    {
        base.Awake();
		//We don't need to have the objectMover active at the start.
		activateObjectMover(false);
        interactionMechanism = new interaction.InteractionAnimMechanism(this);
        initModifiers();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
		Debug.Assert (animator != null, "Animation mechanism without animator");
		animator.logWarnings = false;
		onOff = animator.GetBool ("onoff");
		secondaryOnOff = animator.GetBool ("secondary_onoff");
		initSounds (onONSoundList);
		initSounds (onOFFSoundList);
	}

    public override void OnDisable()
    {
        base.OnDisable();
        animator = null;
    }

    public virtual void Update()
    {
    }

	public virtual float animationUpdate() {
		AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);
		if (curState.normalizedTime >= 0.95f)
		{
			if (curState.IsTag("ON") && onOff)
			{
				stopSounds (onONSoundList, true);
				return curState.normalizedTime;
			}
			else if (curState.IsTag("OFF") && !onOff)
			{
				stopSounds (onOFFSoundList, true);
				return curState.normalizedTime;
			} else if (curState.IsTag("SECONDARY_ON") && secondaryOnOff) {
				stopSounds (secondaryOnONSoundList, true);
				return curState.normalizedTime;
			} else if (curState.IsTag("SECONDARY_OFF") && !secondaryOnOff) {
				stopSounds (secondaryOnOFFSoundList, true);
				return curState.normalizedTime;
			}
		}
		return 0.0f;

	}

	#region sounds and notifications
	/// <summary>
	/// Inits the given sound list.
	/// </summary>
	/// <param name="soundList">The sound list to initialize.</param>
	void initSounds(List<OnSceneEvent> soundList) {
		for (int i = 0; i < soundList.Count; ++i)
		{
			soundList[i].gameObjectAttached = soundList[i].gameObjectAttached == null ? this.gameObject : soundList[i].gameObjectAttached;
		}
	}

	/// <summary>
	/// Stops the sounds if there is one to stop.
	/// </summary>
	/// <param name="soundList">Sound list to stop.</param>
	/// <param name="allowFade">If the sound can fade or not.</param> 
	void stopSounds(List<OnSceneEvent> soundList, bool allowFade) {
		for (int i = 0; i < soundList.Count; ++i)
		{
			if (!string.IsNullOrEmpty(soundList[i].soundId))
				OnSceneEvent.unprocess(soundList[i], allowFade);
		}
	}

	/// <summary>
	/// Plays the sounds, if there is one.
	/// </summary>
	/// <param name="soundList">Sound list to stop.</param>
	void playSounds(List<OnSceneEvent> soundList) {
		for (int i = 0; i < soundList.Count; ++i) {
			if (!string.IsNullOrEmpty(soundList[i].soundId))
				OnSceneEvent.process(soundList[i], soundList[i].delaySound);
		}
	}

	/// <summary>
	/// send the notification, if possible.
	/// </summary>
	/// <param name="notice">the notice to send.</param>
	void sendNotification(GuiHud.Notice notice) {
		if (!string.IsNullOrEmpty(notice.content))
		{
			App.Events.Enqueue(new Evt.OnNewNoticeReceived(notice.content, notice.priority, notice.typeNotice));
		}
	}
	#endregion

    public override bool onCommandProcess(MechanismCommand _command) {
        return switchAnimation(_command.activated);
    }

    public virtual bool switchAnimation(bool newState) {
		if (newState != onOff) {
			setOnOFFParameter (newState);

			activateObjectMover (true);
			if (animator.speed > 0f) {
				if (newState) {
					if (!animator.GetCurrentAnimatorStateInfo (0).IsTag ("ON")) {
						sendNotification (onONNotification);
						stopSounds (onOFFSoundList, false);
						playSounds (onONSoundList);
					}
				} else {
					if (!animator.GetCurrentAnimatorStateInfo (0).IsTag ("OFF")) {
						sendNotification (onOFFNotification);
						stopSounds (onONSoundList, false);
						playSounds (onOFFSoundList);
					}
				}
			}
			onOff = newState;
			return true;
		}
        return false;
    }

	public bool switchSecondaryAnimation(bool newState) {
		if (newState != secondaryOnOff) {
			setSecondaryOnOFFParameter (newState);
			activateObjectMover (true);
			if (animator.speed > 0f) {
				if (newState) {
					if (!animator.GetCurrentAnimatorStateInfo (0).IsTag ("SECONDARY_ON")) {
						sendNotification (secondaryOnONNotification);
						stopSounds (secondaryOnOFFSoundList, false);
						playSounds (secondaryOnONSoundList);
					}
				} else {
					if (!animator.GetCurrentAnimatorStateInfo (0).IsTag ("SECONDARY_OFF")) {
						sendNotification (secondaryOnOFFNotification);
						stopSounds (secondaryOnONSoundList, false);
						playSounds (secondaryOnOFFSoundList);
					}
				}
			}
			secondaryOnOff = newState;
			return true;
		}
		return false;
	}

	protected void activateObjectMover(bool activate) {
		if (linkedObjectMover != null) {
			linkedObjectMover.active = activate;
		}
	}

    protected void setOnOFFParameter(bool _value)
    {
        animator.SetBool(ANIM_BOOL_PARAM, _value);
    }

	protected void setSecondaryOnOFFParameter(bool _value)
	{
		animator.SetBool(ANIM_SECONDARY_BOOL_PARAM, _value);
	}
}

