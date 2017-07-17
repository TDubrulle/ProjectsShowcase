using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// A simple implementation of PointOfInterest.
/// It will returns its transform's position and a constant weight.
/// </summary>
public class SimplePointOfInterest : MonoBehaviour, PointOfInterest
{
    public Transform interestCenter = null;
    [Tooltip("The 'force' with which the camera will be dragged toward the center of the point. 1.0 means the same as a player.")]
    public float interestWeight = 1.0f;
    [Tooltip("The offset that will be applied to the camera.")]
    public Vector3 camOffset = Vector3.zero;
    [Tooltip("Camera horizontal speed offset.")]
    public float camMoveTimeOffset = 0.0f;
    [Tooltip("Camera vertical speed offset.")]
    public float camDistanceSpeedChangeOffset = 0.0f;
    [Tooltip("Camera rotation offset (euler angles)")]
    public Vector3 camRotationOffset = Vector3.zero;
    [Tooltip("If the camera is rotating, add or remove speed (in °/s) to the rotation.")]
    public float camRotationSpeedOffset = 0.0f;
    [Tooltip("add or remove minDistance offset.")]
    public float camMinDistanceOffset = 0.0f;
    [Tooltip("add or remove maxDistance offset.")]
    public float camMaxDistanceOffset = 0.0f;


    [Tooltip("Whether the camera's parameters (offsets, usw.) should be affect by the number of players inside the point of interest.")]
    public bool paramDependentOfPlayerNumber = true;

    private HashSet<int> playersInside = new HashSet<int>();

    /// <summary>How much a player is worth. </summary>
    private const float PLAYER_MULTIPLIER = 0.25f;

    private float currentWeightTime = 0.0f;
    /// <summary>In how much time the offset will reach its maximum. </summary>
    public float camInterestWeightChangeTime = 2f;

    //Whether the POI is still in the camera or not.
    private bool POIactive = false;

    void Awake()
    {
        if(interestCenter == null)
        {
            interestCenter = this.transform;
        }
        App.Events.AddListener(OnPlayerDead, new Evt.OnPlayerDeadEvent());
    }

    public void Update()
    {
        if(playersInside.Count > 0)
        {
            currentWeightTime = Mathf.Min(currentWeightTime + Time.deltaTime, camInterestWeightChangeTime
                                                                              * (paramDependentOfPlayerNumber? (playersInside.Count * PLAYER_MULTIPLIER) : 1f));
            if(currentWeightTime < camInterestWeightChangeTime)
            {
                App.Events.Enqueue(new Evt.OnPointOfInterestChanged(this));
            }
        } else
        {
            currentWeightTime = Mathf.Max(currentWeightTime - Time.deltaTime, 0f);
            if (currentWeightTime > 0f)
            {
                App.Events.Enqueue(new Evt.OnPointOfInterestChanged(this));
            } else if (POIactive)
            {
                POIactive = false;
                App.Events.Enqueue(new Evt.OnPointOfInterestExited(this));
            }
        }
    }



    void OnTriggerEnter (Collider other)
    {
        if (other.CompareTag("Player"))
        {
            int identifier = other.GetComponent<Controllable>().identifier;
            if (!playersInside.Contains(identifier))
            {
                playersInside.Add(identifier);
                if (playersInside.Count == 1)
                {
                    POIactive = true;
                    App.Events.Enqueue(new Evt.OnPointOfInterestEntered(this));
                } else
                {
                    if (paramDependentOfPlayerNumber) App.Events.Enqueue(new Evt.OnPointOfInterestChanged(this));
                }
            }
            //if (PresentationManager.instance) PresentationManager.instance.EnterSlide(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            removePlayer(other.GetComponent<Controllable>().identifier);
        }
    }

    public Vector3 getPositionOfInterest()
    {
        return this.interestCenter.position;
    }

    public float getInterestWeight()
    {
        return currentWeightTime / camInterestWeightChangeTime * interestWeight;
    }

    public Vector3 getCamOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camOffset;
    }

    void removePlayer(int id)
    {
        playersInside.Remove(id);
        
        if (paramDependentOfPlayerNumber && playersInside.Count > 0) App.Events.Enqueue(new Evt.OnPointOfInterestChanged(this));
    }

    void OnPlayerDead(IEvent evt)
    {
        Evt.OnPlayerDeadEvent evtPlayer = (Evt.OnPlayerDeadEvent)evt;
        removePlayer(evtPlayer.player.identifier);
    }
    
    public float getCamMoveTimeOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camMoveTimeOffset;
    }
    
    public float getCamDistanceSpeedOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camDistanceSpeedChangeOffset;
    }

    public Vector3 getCamEulerRotationOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camRotationOffset;
    }

    public float getCamRotationSpeedOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camRotationSpeedOffset;
    }

    public float getMaxCamDistanceOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camMaxDistanceOffset;
    }

    public float getMinCamDistanceOffset()
    {
        return currentWeightTime / camInterestWeightChangeTime * camMinDistanceOffset;
    }

    public void OnDestroy()
    {
		if (App.IsAppInstanciated) App.Events.RemoveListener(OnPlayerDead, new Evt.OnPlayerDeadEvent());
        if(playersInside.Count > 0)
        {
			if (App.IsAppInstanciated) App.Events.Enqueue(new Evt.OnPointOfInterestExited(this));
            playersInside.Clear();
        }

    }
}