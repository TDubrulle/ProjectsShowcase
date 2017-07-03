using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class dummyMechanismParam {
	[Range(0.0f, 3.0f)]
	public float fillRendererEmptyingDelay = 0.2f;
	[Range(0.01f, 3.0f)]
	public float fillRendererEmptyingTime = 0.65f;
	[Range(0.0f, 1.0f)]
	public float fillRendererStartPercentage = 0.075f;
}

/// <summary>
/// A fake mechanism that can be used for tests.
/// </summary>
public class DummyMechanism : Mechanism {

    /// <summary>
    /// The global number of turns needed to activate.
    /// </summary>
    public int cyclesNeeded = 0;
    
    protected float turnValue = 1.0f;
    protected float currentValue = 0.0f;

    public Mechanism targetMechanism;

	public dummyMechanismParam dummyParameters;

    public OnSceneEvent fillSoundBubble = new OnSceneEvent(SoundKeys.PLACEHOLDER_LOOP);

	/// <summary> If the dummyMechanism hasEnded, but not resetted. </summary>
	protected bool canFill = true;

	#region fillRenderer params
	const float startOffset = 0.075f;
	const float endOffset = 1.0f;
	const string leftRendererParam = "_AnimateL";
	const string rightRendererParam = "_AnimateR";

	public Renderer leftFillRenderer;
	public Renderer rightFillRenderer;
	#endregion

    public override void Awake() {
        base.Awake();
        interactionMechanism = new interaction.InteractionDummyMechanism(this);
        initModifiers();
        turnValue = cyclesNeeded * BotdanovActionnerAnimationController.ACTIONNER_CYCLE_TIME;
        if (fillSoundBubble.gameObjectAttached == null)
            fillSoundBubble.gameObjectAttached = gameObject;
    }

    public override void init()
    {
		base.init ();
    	interactionMechanism.initMessageDestinations(targetMechanism);
    }

    public override bool onCommandProcess(MechanismCommand _command) {
        bool finished = fill(_command.value);
        if(finished) {
            if (targetMechanism) {
                targetMechanism.onCommandReceived(_command);
            }
        }
        return finished;
    }

	public void start() {
		startFeedback ();
		canFill = true;
	}

    public bool fill(float amount) {
        if (amount > 0f && canFill) {
			updateFeedback ();
            manageSoundMechanism(true);
            currentValue += amount;
			if (currentValue >= turnValue) {
				logDummyMessage ("dummy activated");
				updateFeedback ();
				currentValue = 0.0f;
                manageSoundMechanism(false);
                canFill = false;
				return true;
			}
		} else if (turnValue <= 0.0f) {
			return true;
		}
        else
        {
            manageSoundMechanism(false);
        }
        //logDummyMessage("dummy not activated yet");
        return false;
    }

    void manageSoundMechanism(bool _activateSound)
    {
        if (_activateSound && !fillSoundBubble.isStarted)
        {
            OnSceneEvent.process(fillSoundBubble);
        }
        else if (!_activateSound && fillSoundBubble.isStarted)
        {
            OnSceneEvent.unprocess(fillSoundBubble, true);
        }
        if (fillSoundBubble.isStarted)
        {
            if (turnValue != 0f)
            {
                App.Sound.EventMgr.Enqueue(new Evt.OnUpdateSoundParameter("finished", currentValue / turnValue, fillSoundBubble.soundId, fillSoundBubble.gameObjectAttached));
            }
            else
            {
                App.Sound.EventMgr.Enqueue(new Evt.OnUpdateSoundParameter("finished", 0f, fillSoundBubble.soundId, fillSoundBubble.gameObjectAttached));
            }
        }
    }

	public void end() {
		endFeedback ();
		canFill = true;
	}

    private void logDummyMessage(string msg) {
        Debug.LogFormat("<color=#2020ff>" + msg + "</color>");
    }


	#region feedback
	protected void setFillFeedback(float percent) {
		if (leftFillRenderer != null) {
			leftFillRenderer.material.SetFloat (leftRendererParam, percent);
		}
		if (rightFillRenderer != null) {
			rightFillRenderer.material.SetFloat (rightRendererParam, percent);
		}
	}

	protected void startFeedback() {
		if (leftFillRenderer != null) {
			App.Process.addProcess (new MaterialChangerProcess (leftFillRenderer, leftRendererParam, 0.0f, dummyParameters.fillRendererStartPercentage, 0.1f));
		}
		if (rightFillRenderer != null) {
			App.Process.addProcess (new MaterialChangerProcess (rightFillRenderer, rightRendererParam, 0.0f, dummyParameters.fillRendererStartPercentage, 0.1f));
		}
	}

	protected void updateFeedback() {
       
		if (turnValue != 0.0f) {
			setFillFeedback ((currentValue / turnValue) * (1f - dummyParameters.fillRendererStartPercentage) + dummyParameters.fillRendererStartPercentage);
		} else {
			setFillFeedback (1f);
		}
	}

	protected void endFeedback() {
		if (dummyParameters.fillRendererEmptyingDelay == 0.0f) {
			if (leftFillRenderer != null) {
				App.Process.addProcess (new MaterialChangerProcess (leftFillRenderer, leftRendererParam, 1f, 0f, dummyParameters.fillRendererEmptyingTime));
			}
			if (rightFillRenderer != null) {
				App.Process.addProcess (new MaterialChangerProcess (rightFillRenderer, rightRendererParam, 1f, 0f, dummyParameters.fillRendererEmptyingTime));
			}
		} else {
			WaitProcess wp = new WaitProcess (dummyParameters.fillRendererEmptyingDelay);
			if (leftFillRenderer != null) {
				wp.attachProcess (new MaterialChangerProcess (leftFillRenderer, leftRendererParam, 1f, 0f, dummyParameters.fillRendererEmptyingTime));
			}
			if (rightFillRenderer != null) {
				wp.attachProcess(new MaterialChangerProcess (rightFillRenderer, rightRendererParam, 1f, 0f, dummyParameters.fillRendererEmptyingTime));
			}
			App.Process.addProcess (wp);
		}
	}
	#endregion

}