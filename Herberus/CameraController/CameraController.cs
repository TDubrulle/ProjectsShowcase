using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The modifiable parameters of the camera script.
/// </summary>
[System.Serializable]
public class CameraControllerParam
{
    [Tooltip("The (fixed) offset of the camera, applied after distance has been calculated.")]
    public Vector3 baseCamOffset = Vector3.zero;
    /// <summary> The minimum distance the camera will be set from the players if they are gathered at the same place. </summary>
    [Tooltip("The minimum distance the camera is from the ground.")]
    public float minimumCamDistance = 20.0f;
    /// <summary> The maximum distance the camera will be set from the players if they are at the largest difference</summary>
    [Tooltip("The maximum distance the camera is from the ground (when players are far from each other).")]
    public float maximumCamDistance = 25.0f;
    [Tooltip("The camera distance change maximum speed.")]
    public float camDistanceChangeSpeed = 5.0f;
    /// <summary> The distance between the camera (on the ground) and the farthest player at which camera's distance will be maximumCameraDistance. </summary>
    [Tooltip("The distance between the camera (the aimed ground) and the farthest player, at which the camera will not heighten up anymore.")]
    public float maxExtentPlayerDistance = 0.0f;
    /// <summary> The time the camera will need to move toward the target point. </summary>
    [Tooltip("The needed time to move the camera from one point to another.")]
    public float centerMoveTime = 0.1f;
    [Tooltip("The maximum camera speed. It is not affected by POIs.")]
    public float camMaxSpeed = 3.2f;
    [Tooltip("The distance to move at which the camera will have the lowest speed (camMinSpeedRatio)")]
    public float camMaxSpeedDistance = 4f;
    [Tooltip("The lowest speed of the camera, based on camMaxSpeed.")]
    [Range(0f,1f)]
    public float camMinSpeedRatio = 0.24f;

    [Tooltip("The camera rotation speed, in degrees per second.")]
    public float rotationSpeed = 5.0f;
    [Tooltip("If > 0, the camera will be set to where players aim. A greater value means the camera will move more where they aim.")]
    public float playerForwardDistance = 0.0f;
    [Tooltip("Determines at how much distance (in percentage) a player must be from the horizontal screen border (left and right) before the camera extends. It overrides players' distance to center")]
    [Range(0f, 0.5f)]
    public float horizontalScreenLimit = 0.05f;
    [Tooltip("Determines at how much distance (in percentage) a player must be from the vertical screen border (up and down) before the camera extends. It overrides players' distance to center.")]
    [Range(0f, 0.5f)]
    public float verticalScreenLimit = 0.05f;

}

/// <summary>
/// The camera script allows a camera to follow a set of players.
/// </summary>
public class CameraController : Entity
{
    public static CameraController instance
    {
        get {
            return currentCameraController;
        }
    }
    private static CameraController currentCameraController;

    #region variables and const
        const float MIN_DOT_SLIDE = 0.3f;
        protected List<Controllable> followedPlayers = new List<Controllable>();
        public HashSet<PointOfInterest> pointsOfInterest = new HashSet<PointOfInterest>();
        public CameraControllerParam cameraParam = new CameraControllerParam();
        public Camera grassCamera;
        #region screenshake
        public CameraShakeProcess currentShakeProcess = null;
        #endregion
        private Camera cam;
        //Final camera's destination (without screenshake).
        private Vector3 currentCameraPosition;

        #region ground movements
        private Vector3 currentCamCenter = Vector3.zero;
        private Vector3 currentVelocity = Vector3.zero;
        //The camera's destination.
        private Vector3 playersBarycenter = Vector3.zero;
        #endregion

        private Vector3[] groundCameraBorders;

        #region rotation
        private Vector3 defaultCameraEulerRotation = Vector3.zero;
        private Quaternion currentCameraRotation = Quaternion.identity;
    #endregion

        #region distance movements
        private float currentCameraExtentRatio = 0f;
        private float currentCamDistanceVelocity = 0.0f;
        private float currentCameraDistance = 0.0f;
        private float aimedCameraDistance = 0.0f;
        #endregion

        #region points of interests
        private Vector3 POIOffsets = Vector3.zero;
        public Vector3 averagedOffset = Vector3.zero;

        private float POIMoveTimeOffsets = 0.0f;
        private float averagedMoveTimeOffset = 0.0f;

        private float POIDistSpeedOffsets = 0.0f;
        private float averagedDistSpeedOffset = 0.0f;

        private Vector3 POIEulerRotationOffsets = Vector3.zero;
        private Vector3 averagedEulerRotationOffset = Vector3.zero;

        private float POIRotationSpeedOffsets = 0.0f;
        private float averagedRotationSpeedOffset = 0.0f;


        private float POIMinDistanceOffsets = 0.0f;
        private float averagedMinDistanceOffset = 0.0f;

        private float POIMaxDistanceOffsets = 0.0f;
        private float averagedMaxDistanceOffset = 0.0f;
        #endregion

    #region fade to black
    private UnityEngine.UI.Image fadeOverlay;
        private UIImageFadeProcess currentFadingProcess = null;
    #endregion
    //private const float CAMERA_DISTANCE_MOVE_LIMIT = 0.1f;
    #endregion

    public Transform transformPointer;

    private bool hasInit = false;

    public int followedPlayersCount
    {
        get { return followedPlayers.Count; }
    }

    /// <summary>Returns where the camera is currently aiming at.</summary>
    public Vector3 currentCameraCenter
    {
        get { return currentCamCenter; }
    }

    #region Unity override
    public override void Awake()
    {
        base.Awake();
        cam = this.GetComponent<Camera>();
        fadeOverlay = this.GetComponentInChildren<UnityEngine.UI.Image>();
        Debug.Assert(cam != null, "CameraController has no camera to control!");
        defaultCameraEulerRotation = this.getTransform().rotation.eulerAngles;
        currentCameraRotation = this.getTransform().rotation;
		App.Game.registerGameCamera(this);
        registerEvents();
        setCameraDistance(cameraParam.minimumCamDistance);
        currentCameraController = this;

    }

	
	
	public void Start()
    {
        initCamera();
    }

    private void registerEvents()
    {
        App.Events.AddListener(addNewPointOfInterest, new Evt.OnPointOfInterestEntered());
        App.Events.AddListener(changePointOfInterest, new Evt.OnPointOfInterestChanged());
        App.Events.AddListener(removePointOfInterest, new Evt.OnPointOfInterestExited());
        App.Events.AddListener(removeDeadPlayer, new Evt.OnPlayerDeadEvent());
        App.Events.AddListener(addNewPlayer, new Evt.OnNewPlayerEvent());
        App.Events.AddListener(endGame, new Evt.OnGameEnded());
        App.Events.AddListener(onNewScene, new Evt.OnNewScene(null));
    }

    private void unregisterEvents()
    {
		if (App.IsAppInstanciated)
		{
			App.Events.RemoveListener(addNewPointOfInterest, new Evt.OnPointOfInterestEntered());
			App.Events.RemoveListener(changePointOfInterest, new Evt.OnPointOfInterestChanged());
			App.Events.RemoveListener(removePointOfInterest, new Evt.OnPointOfInterestExited());
			App.Events.RemoveListener(removeDeadPlayer, new Evt.OnPlayerDeadEvent());
			App.Events.RemoveListener(addNewPlayer, new Evt.OnNewPlayerEvent());
			App.Events.RemoveListener(endGame, new Evt.OnGameEnded());
			App.Events.RemoveListener(onNewScene, new Evt.OnNewScene(null));
		}
    }

    void LateUpdate()
    {
        if(!hasInit)
        {
            hasInit = true;
            initCamera();
        }
        cleanNullPlayers();
        if (followedPlayers.Count > 0)
        {
            updateCameraPosition();
            updateCameraRotation();
            groundCameraBorders = calculateGroundCameraBorders();
            
            //We finally add screen shake.
            updateScreenShake();
        }
        if (transformPointer)
        {
            transformPointer.position = currentCamCenter;
        }

        /*Ray z0 = cam.ViewportPointToRay(Vector3.zero);
        Ray z1 = cam.ViewportPointToRay(new Vector3(0f, 1f));
        Ray u0 = cam.ViewportPointToRay(new Vector3(1f, 0f));
        Ray u1 = cam.ViewportPointToRay(Vector3.one);

        Ray upLeft = cam.ViewportPointToRay(new Vector3(cameraParam.horizontalScreenLimit, cameraParam.verticalScreenLimit));
        Ray upRight = cam.ViewportPointToRay(new Vector3(cameraParam.horizontalScreenLimit, 1f - cameraParam.verticalScreenLimit));
        Ray bottomLeft = cam.ViewportPointToRay(new Vector3(1f - cameraParam.horizontalScreenLimit, cameraParam.verticalScreenLimit));
        Ray bottomRight = cam.ViewportPointToRay(new Vector3(1f - cameraParam.horizontalScreenLimit, 1f - cameraParam.verticalScreenLimit));

        Color camLimitColor = new Color(0.88f, 0.1f, 0.1f);
        float oldhorizontalLimit = cameraParam.horizontalScreenLimit;
        float odlverticalLimit = cameraParam.verticalScreenLimit;
        cameraParam.horizontalScreenLimit = 0f;
        cameraParam.verticalScreenLimit = 0f;

        Vector3[] camLimits = calculateGroundCameraBorders(followedPlayers[0].getTransform().position.y + 1f);
        Debug.DrawLine(z0.origin, camLimits[0], camLimitColor);
        Debug.DrawLine(z1.origin, camLimits[1], camLimitColor);
        Debug.DrawLine(u0.origin, camLimits[2], camLimitColor);
        Debug.DrawLine(u1.origin, camLimits[3], camLimitColor);

        Debug.DrawLine(camLimits[0], camLimits[1], camLimitColor);
        Debug.DrawLine(camLimits[0], camLimits[2], camLimitColor);
        Debug.DrawLine(camLimits[2], camLimits[3], camLimitColor);
        Debug.DrawLine(camLimits[1], camLimits[3], camLimitColor);

        cameraParam.horizontalScreenLimit = oldhorizontalLimit;
        cameraParam.verticalScreenLimit = odlverticalLimit;

        Color insideBorderColor = new Color(0.25f, 0.75f, 1f);
        Vector3[] borders = calculateGroundCameraBorders(followedPlayers[0].getTransform().position.y + 1f);
        if (followedPlayers.Count > 0)
        {
            for (int i = 0; i < borders.Length; ++i)
            {
                Debug.DrawRay(borders[i], Vector3.up, insideBorderColor);
            }
        }
        Debug.DrawLine(upLeft.origin, borders[0], insideBorderColor);
        Debug.DrawLine(upRight.origin, borders[1], insideBorderColor);
        Debug.DrawLine(bottomLeft.origin, borders[2], insideBorderColor);
        Debug.DrawLine(bottomRight.origin, borders[3], insideBorderColor);

        Debug.DrawLine(borders[0], borders[1], insideBorderColor);
        Debug.DrawLine(borders[0], borders[2], insideBorderColor);
        Debug.DrawLine(borders[2], borders[3], insideBorderColor);
        Debug.DrawLine(borders[1], borders[3], insideBorderColor);*/

    }

    public void OnEnable()
    {
        initCamera();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        unregisterEvents();
    }
    #endregion

    #region Event Delegates
    public void addNewPointOfInterest(IEvent evt)
    {
        Evt.OnPointOfInterestEntered downcastedEvent = (Evt.OnPointOfInterestEntered)evt;
        if (!pointsOfInterest.Contains(downcastedEvent.EnteredPOI))
        {
            pointsOfInterest.Add(downcastedEvent.EnteredPOI);
            updatePOIOffsets();
        }
    }

    public void removePointOfInterest(IEvent evt)
    {
        Evt.OnPointOfInterestExited downcastedEvent = (Evt.OnPointOfInterestExited) evt;
        if (pointsOfInterest.Contains(downcastedEvent.ExitedPOI))
        {
            pointsOfInterest.Remove(downcastedEvent.ExitedPOI);
            updatePOIOffsets();
        }
    }

    public void changePointOfInterest(IEvent evt)
    {
        Evt.OnPointOfInterestChanged downcastedEvent = (Evt.OnPointOfInterestChanged)evt;
        if(pointsOfInterest.Contains(downcastedEvent.changedPOI))
        {
            updatePOIOffsets();
        }
    }

    public void removeDeadPlayer(IEvent evt)
    {
        Evt.OnPlayerDeadEvent downcastedEvent = (Evt.OnPlayerDeadEvent) evt;
        this.removeFollowedPlayer(downcastedEvent.player);
    }

    public void addNewPlayer(IEvent evt)
    {
        Evt.OnNewPlayerEvent downcastedEvent = (Evt.OnNewPlayerEvent)evt;
        addFollowedPlayer(downcastedEvent.player);
    }

    public void endGame(IEvent evt)
    {
        resetPOIs();
    }
    #endregion

    #region init
    public void initCamera()
    {
        if (followedPlayers.Count != 0)
        {
            currentCamCenter = calculateCameraCenter() + cameraParam.baseCamOffset + averagedOffset;
        }
        this.transform.position = calculateCameraPosition(currentCamCenter);
    }
    #endregion

    #region players
    public void addFollowedPlayer(Controllable pc)
    {
        if (pc && !this.followedPlayers.Contains(pc))
        {
            this.followedPlayers.Add(pc);
        }
    }

	public void removeFollowedPlayer(Controllable pc)
	{
		if (pc && this.followedPlayers.Contains(pc))
		{
			this.followedPlayers.Remove(pc);
		}
	}

    protected void cleanNullPlayers()
    {
        followedPlayers.RemoveAll(player => player == null);
    }
    #endregion

    #region ScreenShake
    public bool isScreenShakeActive()
    {
        return currentShakeProcess != null;
    }

    /// <summary>
    /// Make the camera shake randomly
    /// </summary>
    void updateScreenShake()
    {
        if (isScreenShakeActive())
        {
            Vector3 offset = Vector3.zero;
            offset.x = (Mathf.PerlinNoise(Time.time*6f, 0f) - 0.5f) * currentShakeProcess.activeForce.x;
            offset.y = (Mathf.PerlinNoise(0f, Time.time*6f) - 0.5f) * currentShakeProcess.activeForce.y;
            offset.z = (Mathf.PerlinNoise(Time.time*6f, Time.time*6f) - 0.5f) * currentShakeProcess.activeForce.z;
            transform.position = currentCameraPosition + new Vector3(offset.x, offset.y, offset.z);
        }
    }

    public void stopShake()
    {
        if(currentShakeProcess != null && !currentShakeProcess.isFinished) currentShakeProcess.Cancel();
        currentShakeProcess = null;
    }

    /// <summary>
    /// Start the screenshake for a given duration
    /// </summary>
    /// <param name="force"></param>
    /// <param name="time"></param>
    public void startShake(Vector3 force, float time)
    {
        if (force != Vector3.zero)
        {
            if (currentShakeProcess == null)
            {
                currentShakeProcess = new CameraShakeProcess(time, force);
                App.Process.addProcess(currentShakeProcess);
            } else
            {
                currentShakeProcess.addMoreShake(time, force);
            }
        }
    }
    #endregion

    /// <summary>
    /// Update the camera position and distance to the ground.
    /// </summary>
    protected void updateCameraPosition()
    {
        float playersWeight;
        float POIsWeight;
        Vector3 destination = calculatePlayerPositionSum(out playersWeight);

        updateCameraDistance(destination / playersWeight);

        destination = destination + calculatePOIPositionSum(out POIsWeight);
        destination = destination / (playersWeight + POIsWeight);

        destination = clampWithPlayersLimits(destination);
        destination = clampWithDistance(destination);
        moveCamera(destination);
        moveCameraDistance();
    }

	void onNewScene(IEvent evt)
	{
		Evt.OnNewScene scene = evt as Evt.OnNewScene;
		if(scene.info.destroyPlayers)
		{
			this.enabled = false;
		}
		if(scene.info.closeCurrentScene)
		{
            unregisterEvents();
		}
	}

    /// <summary>
    /// Update the camera rotation. It only rotates the camera, it does not move it.
    /// </summary>
    protected void updateCameraRotation()
    {
        this.getTransform().rotation = Quaternion.RotateTowards(this.getTransform().rotation, currentCameraRotation, (cameraParam.rotationSpeed + averagedRotationSpeedOffset) * Time.deltaTime);
    }

    /// <summary>
    /// Clamps the camera's destination so that all player's stays within the limits..
    /// </summary>
    /// <param name="destination">The destination to clamp</param>
    /// <returns>the clamped destination</returns>
    private Vector3 clampWithPlayersLimits(Vector3 destination)
    {
        if(!canMoveHorizontallyTo(destination))
        {
            destination.x = playersBarycenter.x;
        }
        if(!canMoveVerticallyTo(destination))
        {
            destination.z = playersBarycenter.z;
        }
        Debug.DrawRay(destination, Vector3.up, Color.green);
        return destination;
    }

    /// <summary>
    /// Clamp the destination so that its distance with the current center does not exceed a threshold.
    /// </summary>
    /// <param name="destination">destination of the camera.</param>
    /// <returns>The clamped destination.</returns>
    private Vector3 clampWithDistance(Vector3 destination)
    {
        //Speed reduction = (1 - (distanceToMove*0.82/maxDistance)², with a minimum of camMinSpeedRatio. 0.82 reduces the curve strength on the limit. 
        float distanceRatio = 1f - Mathf.Clamp01((Vector3.Magnitude(destination - currentCamCenter) * 0.82f/ (cameraParam.camMaxSpeedDistance * cameraParam.camMaxSpeedDistance)));
        distanceRatio = Mathf.Max(distanceRatio, cameraParam.camMinSpeedRatio);
        return currentCamCenter + Vector3.ClampMagnitude(destination - currentCamCenter, distanceRatio * cameraParam.camMaxSpeed);
    }

    /// <summary>
    /// Returns whether the camera can reach the destination horizontally.
    /// </summary>
    /// <param name="destination">Destination to reach.</param>
    /// <returns>True if the camera can reach the destination, false otherwise.</returns>
    public bool canMoveHorizontallyTo(Vector3 destination)
    {
        Vector3 moveDirection = (destination - currentCamCenter);
        //We check against limits on the left and right.
        List<Controllable> playersOnLimits = getAllPlayersOnHorizontalLimits();

        Vector3 playerDir;
        float playerHorizontalVelocity = 0f;

        for (int i = 0; i < playersOnLimits.Count; ++i)
        {
            playerDir = playersOnLimits[i].getTransform().position - this.currentCamCenter;
            if (!Mathf.Approximately(Mathf.Sign(playerDir.x), Mathf.Sign(moveDirection.x)))
            {                
                //We need to compare the velocity of the player against the overall direction of the camera.
                playerHorizontalVelocity = playersOnLimits[i].getRigid().velocity.x;
                if (playerHorizontalVelocity > 0.06f || playerHorizontalVelocity < -0.06f)
                {
                    //We compare the direction of the player.
                    return Mathf.Approximately(Mathf.Sign(moveDirection.x), Mathf.Sign(playerHorizontalVelocity));
                } else
                {
                    //Velocity is near zero, we consider the player is not moving in the direction of the camera. 
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Returns whether the camera can reach the destination vertically.
    /// </summary>
    /// <param name="destination">Destination to reach.</param>
    /// <returns>True if the camera can reach the destination, false otherwise.</returns>
    public bool canMoveVerticallyTo(Vector3 destination)
    {
        Vector3 moveDirection = (destination - currentCamCenter);
        //We check against limits on the top and bottom.
        List<Controllable> playersOnLimits = getAllPlayersOnVerticalLimits();

        Vector3 playerDir;
        float playerVerticalVelocity = 0f;

        for(int i = 0; i < playersOnLimits.Count; ++i)
        {
            playerDir = playersOnLimits[i].getTransform().position - this.currentCamCenter;
            if(!Mathf.Approximately(Mathf.Sign(playerDir.z), Mathf.Sign(moveDirection.z)))
            {
                //We need to compare the velocity of the player against the overall direction of the camera.
                playerVerticalVelocity = playersOnLimits[i].getRigid().velocity.z;
                if (playerVerticalVelocity > 0.06f || playerVerticalVelocity < -0.06f)
                {
                    //We compare the direction of the player.
                    return Mathf.Approximately(Mathf.Sign(moveDirection.z), Mathf.Sign(playerVerticalVelocity));
                } else {
                    //Velocity is near zero, we consider the player is not moving in the direction of the camera. 
                    return false;
                }
            }
        }
        return true;
    }
    
    /// <summary>
    /// Move the camera to the position.
    /// </summary>
    /// <param name="destination"></param>
    protected void moveCamera(Vector3 destination)
    {
        playersBarycenter = destination;
        currentCamCenter = Vector3.SmoothDamp(currentCamCenter, destination + cameraParam.baseCamOffset + averagedOffset, ref currentVelocity, cameraParam.centerMoveTime + averagedMoveTimeOffset);
    }

    protected void moveCameraDistance()
    {
        currentCameraDistance = Mathf.SmoothDamp(currentCameraDistance, aimedCameraDistance, ref currentCamDistanceVelocity, cameraParam.centerMoveTime);
        this.currentCameraPosition = calculateCameraPosition(currentCamCenter);
        this.transform.position = currentCameraPosition;
    }


    /// <summary>
    /// Calculate the camera rotation, given a center position and the current camera rotation.
    /// </summary>
    /// <param name="cameraBarycenter">the center of the camera, on the floor. Usually the averaged position of the players.</param>
    /// <returns></returns>
    public Vector3 calculateCameraPosition(Vector3 cameraBarycenter)
    {
        //Converting the distance to cartesian coordinates .
        cameraBarycenter.y += Mathf.Sin(Mathf.Deg2Rad * this.getTransform().rotation.eulerAngles.x) * currentCameraDistance;
        cameraBarycenter.z -= Mathf.Cos(Mathf.Deg2Rad * this.getTransform().rotation.eulerAngles.x) * currentCameraDistance;
        return cameraBarycenter;
    }

    /// <summary>
    /// Calculate the camera rotation, given a center position and the rotation of the camera (in euler angles).
    /// </summary>
    /// <param name="cameraBarycenter">the center of the camera, on the floor. Usually the averaged position of the players.</param>
    /// <param name="cameraRotation">the rotation of the camera to calculate the position with.</param>
    /// <returns></returns>
    public Vector3 calculateCameraPosition(Vector3 cameraBarycenter, Vector3 cameraRotation)
    {
        //Converting the distance to cartesian coordinates .
        cameraBarycenter.y += Mathf.Sin(Mathf.Deg2Rad * cameraRotation.x) * currentCameraDistance;
        cameraBarycenter.z -= Mathf.Cos(Mathf.Deg2Rad * cameraRotation.x) * currentCameraDistance;
        return cameraBarycenter;
    }

    /// <summary>
    /// Calculate and returns the barycenter (~gravity mass) of the players' positions followed by the camera.
    /// </summary>
    /// <returns>the barycenter of the players, or (0,0,0) if no player is followed (with an error message).</returns>
    public Vector3 calculateCameraCenter()
    {
        float playersWeight = 0.0f;
        float pointsOfInterestWeight = 0.0f;
        Vector3 r = calculatePlayerPositionSum(out playersWeight);
        r += calculatePOIPositionSum(out pointsOfInterestWeight);
        r /= (playersWeight + pointsOfInterestWeight);
        return r;
    }

    /// <summary>
    /// Calculate the sum of all player's positions. 
    /// </summary>
    /// <param name="playersWeight">returns all the player's weight.</param>
    /// <returns></returns>
    protected Vector3 calculatePlayerPositionSum(out float playersWeight)
    {
        Vector3 r = Vector3.zero;
        if(followedPlayers.Count > 0)
        {
            //Calculating players' barycenter.
            for (int i = 0; i < followedPlayers.Count; ++i)
            {
                if (followedPlayers[i] == null) continue;
                r += (followedPlayers[i].getTransform().position + followedPlayers[i].getTransform().forward * cameraParam.playerForwardDistance);
            }
        } else
        {
            Debug.LogError("Cannot calculate players' center with no players!");
        }
        playersWeight = followedPlayers.Count;
        return r;
    }

    protected Vector3 calculatePOIPositionSum(out float POIsWeight)
    {
        Vector3 r = Vector3.zero;
        POIsWeight = 0f;
        foreach (PointOfInterest poi in pointsOfInterest)
        {
            POIsWeight += poi.getInterestWeight();
            r += (poi.getPositionOfInterest() * poi.getInterestWeight());
        }
        return r;
    }

    #region CameraDistance
    protected void updateCameraDistance(Vector3 destination)
    {
        updateCameraExtentRatio(destination);
        float minDistance = cameraParam.minimumCamDistance + averagedMinDistanceOffset;
        float maxDistance = cameraParam.maximumCamDistance + averagedMaxDistanceOffset;
        if (canShrinkCameraDistance(minDistance, maxDistance))
        {
            shrinkCameraDistance(minDistance, maxDistance);
        } else if (canExtendCameraDistance(minDistance, maxDistance))
        {
            extendCameraDistance(minDistance, maxDistance);
        }
    }

    public void updateCameraExtentRatio(Vector3 barycenter)
    {
        float maxExtentRatio = 0.0f;
        float testedExtentRatio = 0.0f;
        for (int i = 0; i < followedPlayers.Count; ++i)
		{
			if (followedPlayers[i] == null) continue;

			testedExtentRatio = (followedPlayers[i].getTransform().position - barycenter).sqrMagnitude / cameraParam.maxExtentPlayerDistance;
            if (testedExtentRatio > maxExtentRatio)
            {
                maxExtentRatio = testedExtentRatio;
            }
        }
        currentCameraExtentRatio = Mathf.Clamp01(maxExtentRatio);
    }

    /// <summary> Extend the camera distance by a fixed step. Camera will slide slowly toward the new value.</summary>
    public void extendCameraDistance(float minDistance, float maxDistance) {
        float baseDistance = minDistance + (maxDistance - minDistance) * currentCameraExtentRatio;
        aimedCameraDistance = Mathf.Min(aimedCameraDistance + Time.deltaTime * (cameraParam.camDistanceChangeSpeed + averagedDistSpeedOffset), maxDistance, baseDistance);
    }

    /// <summary> Shrink the camera distance by a fixed step. Camera will slide slowly toward the new value.</summary>
    public void shrinkCameraDistance(float minDistance, float maxDistance) {
        float baseDistance = minDistance + (maxDistance - minDistance) * currentCameraExtentRatio;
        aimedCameraDistance = Mathf.Max(aimedCameraDistance - Time.deltaTime * (cameraParam.camDistanceChangeSpeed + averagedDistSpeedOffset), minDistance, baseDistance);
    }

    /// <summary> Set the camera distance to a fixed value. It will change instantly the camera distance.</summary>
    public void setCameraDistance(float value)
    {
        aimedCameraDistance = Mathf.Clamp(value, cameraParam.minimumCamDistance, cameraParam.maximumCamDistance);
        currentCameraDistance = aimedCameraDistance;
    }

    public bool canShrinkCameraDistance(float minDistance, float maxDistance)
    {
        if (aimedCameraDistance <= minDistance)
        {
            return false;
        }
        if(hasPlayerOnCameraLimits())
        {
            return false;
        }
        float baseDistance = minDistance + (maxDistance - minDistance) * currentCameraExtentRatio;
        return aimedCameraDistance > baseDistance;
    }

    public bool canExtendCameraDistance(float minDistance, float maxDistance)
    {
        if(aimedCameraDistance >= maxDistance)
        {
            return false;
        }
        float baseDistance = minDistance + (maxDistance - minDistance) * currentCameraExtentRatio;
        return aimedCameraDistance < baseDistance || hasPlayerOnCameraLimits();
    }

    /// <summary> Check if the camera distance is at its maximum.</summary>
    /// <returns>True if the camera is at its maximum extent, otherwise false.</returns>
    public bool isAtMaxExtent()
    {
        return Mathf.Approximately(currentCameraDistance, cameraParam.maximumCamDistance);
    }
    #endregion

    #region limits
    public bool isPlayerOnLimits(Controllable pc)
    {
        Vector3[] borders = getGroundCameraBorders();
        return isPlayerOnHorizontalLimits(pc, borders) || isPlayerOnVerticalLimits(pc, borders);
    }

    public bool isPlayerOnHorizontalLimits(Controllable pc, Vector3[] borders)
    {
        if (pc != null)
        {
            Vector3 playerPos = pc.getTransform().position;
            //We check if the player is on the main rectangle.
            if (playerPos.x >= borders[2].x && playerPos.x <= borders[3].x)
            {
                return false;
            }
            //We check if the player is on the left triangle of the trapezoid.
            if (MathUtility.isInsideTriangle(playerPos, borders[0], new Vector3(borders[2].x, borders[0].y, borders[0].z), borders[2]))
            {
                return false;
            }
            //We check if the player is on the right triangle of the trapezoid.
            if (MathUtility.isInsideTriangle(playerPos, borders[1], new Vector3(borders[3].x, borders[1].y, borders[1].z), borders[3]))
            {
                return false;
            }

        }
        return true;
    }


    public bool isPlayerOnVerticalLimits(Controllable pc, Vector3[] borders)
    {
        if (pc != null)
        {
            Vector3 playerPos = pc.getTransform().position;
            if(playerPos.z <= borders[0].z && playerPos.z >= borders[3].z)
            {
                return false;
            }
            //We check if the player is on the left triangle of the trapezoid.
            if (MathUtility.isInsideTriangle(playerPos, borders[0], new Vector3(borders[2].x, borders[0].y, borders[0].z), borders[2]))
            {
                return false;
            }
            //We check if the player is on the right triangle of the trapezoid.
            if (MathUtility.isInsideTriangle(playerPos, borders[1], new Vector3(borders[3].x, borders[1].y, borders[1].z), borders[3]))
            {
                return false;
            }
        }
        return true;
    }



    public List<Controllable> getAllPlayersOnHorizontalLimits()
    {
        List<Controllable> playersOnLimits = new List<Controllable>(followedPlayers.Count);
        Vector3[] borders = getGroundCameraBorders();
        for (int i = 0; i < followedPlayers.Count; ++i)
        {
            if (isPlayerOnHorizontalLimits(followedPlayers[i], borders))
            {
                playersOnLimits.Add(followedPlayers[i]);
            }
        }
        return playersOnLimits;
    }

    public List<Controllable> getAllPlayersOnVerticalLimits()
    {
        List<Controllable> playersOnLimits = new List<Controllable>(followedPlayers.Count);
        Vector3[] borders = getGroundCameraBorders();
        for (int i = 0; i < followedPlayers.Count; ++i)
        {
            if (isPlayerOnVerticalLimits(followedPlayers[i], borders))
            {
                playersOnLimits.Add(followedPlayers[i]);
            }
        }
        return playersOnLimits;
    }

    /// <summary>
    /// Tells if a player is at the camera's limit.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    bool hasPlayerOnCameraLimits() {
        for (int i = 0; i < followedPlayers.Count; ++i)
        {
            if(isPlayerOnLimits(followedPlayers[i]))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Return if a player is allowed to move to a direction
    /// </summary>
    /// <param name="PC"></param>
    /// <returns></returns>
    public bool canPlayerMove(Controllable pc)
    {
        bool r = true;
        if (pc != null)
        {
            Vector3 screenPoint = cam.WorldToViewportPoint(pc.getTransform().position);
            if (screenPoint.x < cameraParam.horizontalScreenLimit)
            {
                r = r && pc.InputAcceleration.x > 0f;
            }
            if (screenPoint.x > (1f - cameraParam.horizontalScreenLimit))
            {
                r = r && pc.InputAcceleration.x < 0f;
            }
            if (screenPoint.y < cameraParam.verticalScreenLimit)
            {
                r = r && pc.InputAcceleration.z > 0f;
            }
            if (screenPoint.y > (1f - cameraParam.verticalScreenLimit))
            {
                r = r && pc.InputAcceleration.z < 0f;
            }
            
        }
        return r;
    }

	public Vector3 correctedDirection(Controllable pc)
	{
		Vector3 direction = Utility.Vec3.ZERO;
		if (pc != null)
		{
			Vector3 screenPoint = Camera.main.WorldToViewportPoint(pc.getTransform().position);
			if (screenPoint.x < cameraParam.horizontalScreenLimit)
			{
				if (direction == Utility.Vec3.ZERO)
				{
					Vector3[] borders = getGroundCameraBorders();

					// Left vertical camera border = botLeft - topLeft
					direction = borders[2] - borders[0];
				}
				else
					return Utility.Vec3.ZERO;
			}
			if (screenPoint.x > (1f - cameraParam.horizontalScreenLimit))
			{
				if (direction == Utility.Vec3.ZERO)
				{
					Vector3[] borders = getGroundCameraBorders();
					// Right vertical camera border = botRight - topRight
					direction = borders[3] - borders[1];
				}
				else
					return Utility.Vec3.ZERO;
			}
			if (screenPoint.y < cameraParam.verticalScreenLimit)
			{
				if (direction == Utility.Vec3.ZERO)
				{
					direction = transform.right;
				}
				else
					return Utility.Vec3.ZERO;
			}
			if (screenPoint.y > (1f - cameraParam.verticalScreenLimit))
			{
				if (direction == Utility.Vec3.ZERO)
				{
					direction = transform.right;
				}
				else
					return Utility.Vec3.ZERO;
			}
		}

		if (direction != Utility.Vec3.ZERO)
		{
			float dot = Vector3.Dot(pc.InputAcceleration , direction);
			if(dot < MIN_DOT_SLIDE && dot > -MIN_DOT_SLIDE)
			{
				direction = Utility.Vec3.ZERO;
			}
			else if(dot < 0)
			{
				direction *= -1;
			}
		}
		
		return direction;
	}

    #endregion

    #region other utilities
    /// <summary>
    /// Rotates a vector so it corresponds to the camera's rotation. For instance, a camera rotated 180° clockwise will invert x and z.
    /// </summary>
    /// <param name="input">Vector to change.</param>
    /// <returns>The new vector, relative to the camera.</returns>
    public Vector3 makeRelativeToCamera(Vector3 input)
    {
        float camRotation = -this.getTransform().rotation.eulerAngles.y * Mathf.Deg2Rad;
        Vector3 oldinput = input;
        input.x = Mathf.Cos(camRotation) * oldinput.x - Mathf.Sin(camRotation) * oldinput.z;
        input.z = Mathf.Cos(camRotation) * oldinput.z + Mathf.Sin(camRotation) * oldinput.x;
        return input;
    }

    /// <summary>
    /// returns the camera borders on the ground. 
    /// </summary>
    /// <param name="groundHeight">Where the ground is, worldSpace-wise. For instance can be a player's height.</param>
    /// <returns>An array of 4 vector3 that represents the limits of the camera on the ground. 0: top-left, 1: top-right, 2: bottom-left, 3:bottom-right.</returns>
    private Vector3[] calculateGroundCameraBorders()
    {
        float minPlayerHeight = 0.0f;
        for(int i = 0; i < followedPlayers.Count; ++i)
        {
            minPlayerHeight = Mathf.Min(minPlayerHeight, followedPlayers[i].getTransform().position.y);
        }
        return calculateGroundCameraBorders(minPlayerHeight);
    }

    /// <summary>
    /// returns the camera borders on the ground. 
    /// </summary>
    /// <param name="groundHeight">Where the ground is, worldSpace-wise. For instance can be a player's height.</param>
    /// <returns>An array of 4 vector3 that represents the limits of the camera on the ground. 0: top-left, 1: top-right, 2: bottom-left, 3:bottom-right.</returns>
    private Vector3[] calculateGroundCameraBorders(float groundHeight)
    {
        float heightToGround = this.getTransform().position.y - groundHeight;

        Vector3[] r = new Vector3[4];
        //Calculating where the limits are exactly in the world, with the distance to the ground.
        Ray bottomLeft = cam.ViewportPointToRay(new Vector3(cameraParam.horizontalScreenLimit, cameraParam.verticalScreenLimit));
        Ray upLeft = cam.ViewportPointToRay(new Vector3(cameraParam.horizontalScreenLimit, 1f - cameraParam.verticalScreenLimit));
        Ray bottomRight = cam.ViewportPointToRay(new Vector3(1f - cameraParam.horizontalScreenLimit, cameraParam.verticalScreenLimit));
        Ray upRight = cam.ViewportPointToRay(new Vector3(1f - cameraParam.horizontalScreenLimit, 1f - cameraParam.verticalScreenLimit));
        //We use triangle distance measurement with trigonometry (cos).
        //Triangles here have a down segment of heightToGroundLength, and we know the angle between this segment and the ray where we want to point to.
        //float angleBottomLeft = Vector3.Angle(Vector3.down, bottomLeft.direction);
        //float angleUpLeft = Vector3.Angle(Vector3.down, upLeft.direction);
        //float angleBottomRight = Vector3.Angle(Vector3.down, bottomRight.direction);
        //float angleUpRight = Vector3.Angle(Vector3.down, upRight.direction);
        r[0] = upLeft.GetPoint(heightToGround / Mathf.Cos(Vector3.Angle(Vector3.down, upLeft.direction) * Mathf.Deg2Rad));
        r[1] = upRight.GetPoint(heightToGround / Mathf.Cos(Vector3.Angle(Vector3.down, upRight.direction) * Mathf.Deg2Rad));
        r[2] = bottomLeft.GetPoint(heightToGround / Mathf.Cos(Vector3.Angle(Vector3.down, bottomLeft.direction) * Mathf.Deg2Rad));
        r[3] = bottomRight.GetPoint(heightToGround / Mathf.Cos(Vector3.Angle(Vector3.down, bottomRight.direction) * Mathf.Deg2Rad));
        groundCameraBorders = r;
        return r;
    }

    /// <summary>
    /// returns the camera borders on the ground. It is more performant to use it over calculateGroundCameraBorders, since it will not recalculate each time it is called.
    /// </summary>
    /// <returns>An array of 4 vector3 that represents the limits of the camera on the ground. 0: top-left, 1: top-right, 2: bottom-left, 3:bottom-right.</returns>
    public Vector3[] getGroundCameraBorders()
    {
        return groundCameraBorders != null? groundCameraBorders : calculateGroundCameraBorders();
    }
    #endregion

    #region Points of interests
    /// <summary>
    /// Resets all current Points of interest that is active on the camera.
    /// WARNING : it resets all POIs, even if they are still active.
    /// </summary>
    public void resetPOIs()
    {
        pointsOfInterest.Clear();
        POIOffsets = Vector3.zero;
        POIMoveTimeOffsets = 0f;
        POIDistSpeedOffsets = 0f;
        POIEulerRotationOffsets = Vector3.zero;
        POIRotationSpeedOffsets = 0f;
        updatePOIOffsets();
    }
    
    /// <summary>
    /// Recalculate POI offsets.
    /// </summary>
    protected void updatePOIOffsets()
    {
        if (pointsOfInterest.Count > 0)
        {
            POIOffsets = Vector3.zero;
            POIMoveTimeOffsets = 0f;
            POIDistSpeedOffsets = 0f;
            POIEulerRotationOffsets = Vector3.zero;
            POIRotationSpeedOffsets = 0f;
            POIMinDistanceOffsets = 0f;
            POIMaxDistanceOffsets = 0f;
            foreach (PointOfInterest poi in pointsOfInterest)
            {
                POIOffsets += poi.getCamOffset();
                POIMoveTimeOffsets += poi.getCamMoveTimeOffset();
                POIDistSpeedOffsets += poi.getCamDistanceSpeedOffset();
                POIEulerRotationOffsets += poi.getCamEulerRotationOffset();
                POIRotationSpeedOffsets += poi.getCamRotationSpeedOffset();
                POIMinDistanceOffsets += poi.getMinCamDistanceOffset();
                POIMaxDistanceOffsets += poi.getMaxCamDistanceOffset();
            }
            averagedOffset = POIOffsets / pointsOfInterest.Count;
            averagedMoveTimeOffset = Mathf.Max(-cameraParam.centerMoveTime, POIMoveTimeOffsets / pointsOfInterest.Count);
            averagedDistSpeedOffset = Mathf.Max(-cameraParam.camDistanceChangeSpeed, POIDistSpeedOffsets / pointsOfInterest.Count);
            averagedEulerRotationOffset = POIEulerRotationOffsets / pointsOfInterest.Count;
            averagedRotationSpeedOffset = POIRotationSpeedOffsets / pointsOfInterest.Count;
            averagedMinDistanceOffset = POIMinDistanceOffsets / pointsOfInterest.Count;
            averagedMaxDistanceOffset = POIMaxDistanceOffsets / pointsOfInterest.Count;
        } else
        {
            averagedOffset = Vector3.zero;
            averagedMoveTimeOffset = 0.0f;
            averagedDistSpeedOffset = 0.0f;
            averagedEulerRotationOffset = Vector3.zero;
            averagedRotationSpeedOffset = 0.0f;
        }
        currentCameraRotation = Quaternion.Euler(defaultCameraEulerRotation + averagedEulerRotationOffset);
    }
    #endregion

	// Fade to black no more on camera (see event OnLoadingScreen)
    #region fade to black
    /// <summary>
    /// Fade the camera screen to black.
    /// </summary>
    /*public UIImageFadeProcess startFadingInToBlack(float fadeTime)
    {
        if(fadeOverlay != null)
        {
            if(currentFadingProcess != null && currentFadingProcess.isRunning)
            {
                currentFadingProcess.Cancel();
                currentFadingProcess = null;
            }
            if(fadeTime <= 0f)
            {
                Color c = fadeOverlay.color;
                c.a = 1f;
                fadeOverlay.color = c;
            } else
            {
                currentFadingProcess = new UIImageFadeProcess(fadeOverlay, 1f, fadeTime);
                App.Process.addProcess<UIImageFadeProcess>(currentFadingProcess);
            }
            return currentFadingProcess;
        }
        return null;
    }
    
    /// <summary>
    /// Fade out from a black screen.
    /// </summary>
    public void startFadingOutFromBlack(float fadeTime)
    {
        if (fadeOverlay != null)
        {
            if (currentFadingProcess != null)
            {
                currentFadingProcess.Cancel();
            }
            if (fadeTime <= 0f)
            {
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
            } else
            {
                currentFadingProcess = new UIImageFadeProcess(fadeOverlay, 0f, fadeTime);
                App.Process.addProcess<UIImageFadeProcess>(currentFadingProcess);
            }
        }
    }*/
    #endregion
}